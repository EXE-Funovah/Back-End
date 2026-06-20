# Thiết kế backend cho Flashcard

## Mục tiêu

Hoàn thiện luồng tạo, lưu và đọc lại bộ flashcard mà không tạo bảng flashcard riêng. Giữ tương thích với luồng trắc nghiệm hiện tại và chuẩn bị API rõ ràng để frontend tích hợp sau khi backend dev ổn định.

## Phạm vi

- Dùng `Quizzes` làm bộ nội dung chung.
- Dùng `Questions` làm mặt trước flashcard.
- Dùng một `Option` đúng làm mặt sau flashcard.
- Phân biệt rõ `Quiz` và `Flashcard` ở cấp `Quizzes`.
- Lưu đúng thứ tự câu hỏi/thẻ.
- Tạo cả bộ nội dung trong một transaction để tránh dữ liệu dở dang.
- Cung cấp API danh sách và chi tiết để frontend không phải tự suy đoán loại nội dung.

Chưa làm bookmark, archive, share, lớp học, độ khó từng thẻ hoặc tiến độ học kiểu Quizlet.

## Thay đổi database

### Quizzes

Thêm `activity_type VARCHAR(50) NOT NULL`:

- Giá trị hợp lệ: `Quiz`, `Flashcard`.
- Mặc định: `Quiz`.
- Dữ liệu hiện có được giữ nguyên và nhận giá trị `Quiz`.

### Questions

Thêm `position INT NOT NULL`:

- Lưu thứ tự câu hỏi/thẻ do frontend sắp xếp.
- Dữ liệu hiện có được đánh số theo từng quiz một lần khi rollout.
- Bản ghi mới phải được backend gán vị trí rõ ràng.

Chuẩn hóa `question_type`:

- Không cho phép `NULL`.
- Giá trị hợp lệ: `MultipleChoice`, `Flashcard`.
- Mặc định: `MultipleChoice`.

### Chỉ mục

Thêm chỉ mục phục vụ các truy vấn chính:

- Quiz theo `document_id`, `activity_type`, `is_deleted` và thời gian tạo.
- Question theo `quiz_id`, `is_deleted`, `position`.
- Option theo `question_id`, `is_deleted`.

Không thêm bảng mới.

## Script rollout

Tạo một script SQL duy nhất dùng cho cả dev và production:

- Không chứa lệnh `USE` hoặc tên database cố định.
- Kiểm tra cột, constraint và index trước khi tạo.
- Chỉ đánh số dữ liệu question cũ khi cột `position` được tạo lần đầu.
- Chạy trong transaction và rollback toàn bộ nếu có lỗi.
- Có phần kiểm tra kết quả cuối script.
- Có thể chạy lại an toàn.

Thứ tự dev:

1. Backup database dev.
2. Chạy script.
3. Kiểm tra schema và dữ liệu cũ.
4. Scaffold từ database dev.
5. Review diff model và DbContext.
6. Implement backend, chạy test/build rồi deploy dev.
7. Frontend tích hợp và kiểm tra end-to-end trên dev.

Thứ tự production:

1. Backup production.
2. Chạy chính script đã kiểm chứng ở dev.
3. Kiểm tra schema.
4. Deploy backend production sau khi database đã sẵn sàng.
5. Deploy frontend tương thích.

Các cột mới chỉ là bổ sung, nên backend cũ có thể tạm thời chạy nếu cần rollback ứng dụng. Không xóa cột mới ngay khi rollback.

## API backend

### Publish trọn bộ

Thêm `POST /api/Quiz/publish` và yêu cầu JWT.

Request chứa:

- `documentId`
- `title`
- `activityType`: `Quiz` hoặc `Flashcard`
- `questions[]`
- Mỗi question có `questionText`, `questionType`, `position`, `options[]`

Backend thực hiện trong một transaction:

1. Lấy user từ JWT.
2. Xác nhận user sở hữu document.
3. Kiểm tra toàn bộ request trước khi lưu.
4. Tạo Quiz với trạng thái `Teacher_Approved`.
5. Tạo toàn bộ Questions và Options.
6. Commit khi mọi bản ghi đều hợp lệ.
7. Nếu một phần lỗi, rollback toàn bộ.

Quy tắc flashcard:

- `activityType` phải là `Flashcard`.
- Mọi question phải có `questionType = Flashcard`.
- Mặt trước và mặt sau không được trống.
- Mỗi thẻ có đúng một option đang hoạt động.
- Option đó phải có `isCorrect = true`.
- `position` không được trùng trong cùng bộ.

Quy tắc quiz giữ nguyên hành vi MultipleChoice hiện tại.

### Danh sách của tôi

Thêm `GET /api/Quiz/me`:

- Chỉ trả quiz thuộc document của user hiện tại.
- Có thể lọc `activityType=Quiz` hoặc `activityType=Flashcard`.
- Không trả dữ liệu soft-deleted.
- Trả `activityType`, số câu/thẻ và metadata cần cho Library.

### Chi tiết

Thêm `GET /api/Quiz/{id}/detail`:

- Kiểm tra ownership.
- Trả Quiz cùng Questions và Options.
- Questions sắp xếp theo `position`.
- Không trả dữ liệu soft-deleted.
- Repository tải questions/options trong một truy vấn hợp lý, tránh gọi database riêng cho từng question.

Các endpoint CRUD hiện tại được giữ để không làm hỏng frontend cũ.

## Xử lý lỗi

- Document không tồn tại hoặc không thuộc user: từ chối truy cập.
- Loại activity/question không hợp lệ: trả lỗi request rõ ràng.
- Flashcard thiếu mặt trước/mặt sau hoặc có nhiều option: không lưu gì.
- Vị trí bị trùng: không lưu gì.
- Lỗi database giữa quá trình publish: rollback toàn bộ.
- Không trả thông tin nội bộ hoặc stack trace cho frontend.

## Thay đổi code backend

- Model và DbContext được scaffold từ database dev sau khi chạy SQL.
- DTO bổ sung `ActivityType`, `Position` và request publish trọn bộ.
- Mapper bổ sung các field mới.
- Repository có truy vấn danh sách owner-scoped và detail kèm options.
- Service giữ validation, ownership và transaction.
- Controller chỉ nhận request, lấy user từ JWT và trả response.

## Kiểm thử

Tối thiểu phải có test cho:

- Publish flashcard hợp lệ lưu đủ Quiz, Questions và Options.
- Thứ tự thẻ được giữ nguyên.
- User không thể publish trên document của người khác.
- Flashcard thiếu mặt sau bị từ chối và không lưu dữ liệu dở dang.
- Flashcard có nhiều option bị từ chối.
- `activityType` và `questionType` không khớp bị từ chối.
- Danh sách `/me` lọc đúng Quiz/Flashcard và ownership.
- Detail trả questions/options đúng thứ tự, không gồm record đã xóa.
- Luồng MultipleChoice hiện tại vẫn hoạt động.
- Toàn bộ test và solution build thành công.

## Bàn giao frontend

Sau khi backend dev ổn định, frontend cần:

- Gửi `activityType`.
- Dùng một request `POST /api/Quiz/publish` thay cho tạo Quiz rồi tạo từng Question riêng.
- Dùng `GET /api/Quiz/me` cho Library.
- Dùng `activityType` để gắn nhãn và lọc Quiz/Flashcard.
- Dùng endpoint detail để mở lại bộ thẻ và giữ đúng thứ tự.

AI service hiện đã trả đúng cấu trúc mặt trước/mặt sau và chưa cần thay đổi trong phạm vi này.
