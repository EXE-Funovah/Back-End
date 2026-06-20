# Mascoteach Backend Summary

## Mục đích

File này là bản tóm tắt duy nhất ở thư mục gốc để teammate nắm nhanh kiến trúc, các tính năng backend
đã hoàn thành, thay đổi flashcard mới nhất và các việc còn phải làm trước production.

File này thay thế:

- `CHECKLIST.md`
- `PROGRESS_REPORT.md`
- `SOLUTION_SUMMARY.md`

Chi tiết quy tắc kỹ thuật theo từng domain nằm trong `.codex/skills/`.

## Kiến trúc backend

Backend dùng ASP.NET Core 9 theo ba tầng:

```text
Mascoteach.API
    -> Mascoteach.Service
        -> Mascoteach.Data
            -> SQL Server
```

- `Mascoteach.API`: Controller, JWT, Swagger, CORS và SignalR `GameHub`.
- `Mascoteach.Service`: business logic, DTO, AutoMapper và tích hợp dịch vụ ngoài.
- `Mascoteach.Data`: EF Core model, DbContext và Repository.
- `Mascoteach.Tests`: xUnit và Moq.
- Database dùng DB-first; không dùng EF migrations.

Luồng mặc định:

```text
Controller -> Service Interface -> Service -> Repository Interface -> Repository -> DbContext
```

Các nguyên tắc chính:

- Controller phải mỏng; business logic nằm trong Service.
- Dữ liệu sở hữu bởi user phải lấy user ID từ JWT, không tin user ID trong request body.
- Custom query phải lọc `is_deleted = 0`.
- `DELETE` hiện tại là soft-delete khi entity có `IsDeleted`.
- `PATCH /toggle-delete` dùng để xóa mềm hoặc khôi phục.
- Thao tác cập nhật nhiều bảng liên quan phải dùng transaction.

## Các domain đang có

### Authentication và authorization

- JWT Bearer authentication.
- Local register/login bằng BCrypt.
- Xác minh email trước khi local login.
- Google login và liên kết tài khoản Local/Google.
- Forgot/reset password bằng token được hash và có hạn sử dụng.
- JWT chứa `UserId`, role, email và full name.
- Ownership được kiểm tra cho Document, Quiz, Question, Option và LiveSession.

### Document và AWS S3

- Frontend lấy presigned upload URL rồi upload trực tiếp lên S3.
- Database chỉ lưu S3 key trong `Documents.file_url`.
- Không lưu upload URL hoặc presigned URL vào database.
- API tạo presigned download URL mới khi trả Document.
- `Documents.owner_id` là cột ownership hiện tại.
- Freemium quota dựa trên số document đang active; mặc định là 5.
- Premium đang còn hạn được upload không giới hạn.
- `Users.documents_processed` chỉ là bộ đếm lịch sử, không dùng tính quota.

### Quiz, Question và Option

- Quiz thuộc một Document.
- Question thuộc một Quiz.
- Option thuộc một Question.
- Create/update/delete/toggle-delete đều kiểm tra ownership qua Document owner.
- Question create hỗ trợ tạo nested Options trong một transaction.
- Question không truyền `position` sẽ tự nhận vị trí tiếp theo trong Quiz.

### Gamification

- `User_Stats` lưu XP, streak và thống kê tổng hợp.
- `Quiz_Attempts` lưu từng lần làm bài.
- Backend tự chấm đáp án; không tin score hoặc XP từ client.
- Attempt và cập nhật stats được lưu trong một transaction.
- Endpoint stats hiện chỉ cho phép xem `/api/UserStats/me`.

### Live session và SignalR

- SignalR route: `/hubs/game`.
- Group được phân theo game PIN.
- Current question được giữ trong memory cache.
- LiveSession dùng `teacher_id`; không đổi thành `owner_id`.
- Ended session không thể tìm lại bằng PIN.
- Điểm realtime hiện cộng 1000 cho đáp án đúng.

### PayOS billing

- Gói tháng: `119000` VND, cộng 30 ngày.
- Gói năm: `1188000` VND, cộng 365 ngày.
- Premium chỉ active khi tier là `Premium` và `premium_expires_at` còn hạn.
- PayOS webhook đã xác thực chữ ký và là nguồn xác nhận thanh toán cuối cùng.
- Duplicate webhook không được cộng Premium hai lần.
- Payment link hỗ trợ tái sử dụng, hết hạn 5 phút và rate limit.
- Hủy payment phải gọi PayOS thành công trước khi đổi trạng thái local.

## Flashcard — thay đổi mới nhất

### Quyết định thiết kế

Không tạo bảng Flashcard riêng trong giai đoạn hiện tại.

Flashcard dùng cấu trúc có sẵn:

```text
Quizzes.activity_type = "Flashcard"
Questions.question_type = "Flashcard"
Questions.question_text = mặt trước
Options[0].option_text = mặt sau
Options[0].is_correct = true
Questions.position = thứ tự thẻ, bắt đầu từ 0
```

Quiz trắc nghiệm dùng:

```text
Quizzes.activity_type = "Quiz"
Questions.question_type = "MultipleChoice"
```

### Database

Development database đã được cập nhật và scaffold với:

- `Quizzes.activity_type VARCHAR(50) NOT NULL`, mặc định `Quiz`.
- `Questions.question_type` chuyển thành non-null.
- `Questions.position INT NOT NULL`.
- Check constraint cho activity type, question type và position.
- Index cho Quiz theo Document/activity, Question theo Quiz/position và Option theo Question.

Entity hiện có:

- `Quiz.ActivityType`
- `Question.Position`
- `Question.QuestionType` non-null

Production database chưa được xác nhận rollout. Phải chạy cùng idempotent SQL script trước khi backend production
sử dụng các cột mới.

### API flashcard mới

#### Publish cả bộ trong một lần

```http
POST /api/Quiz/publish
```

Request chính:

```json
{
  "documentId": 1,
  "title": "Bộ thẻ Chương 1",
  "activityType": "Flashcard",
  "questions": [
    {
      "questionText": "Mặt trước",
      "questionType": "Flashcard",
      "position": 0,
      "options": [
        {
          "optionText": "Mặt sau",
          "isCorrect": true
        }
      ]
    }
  ]
}
```

Backend thực hiện:

1. Lấy owner từ JWT.
2. Kiểm tra owner của Document.
3. Kiểm tra toàn bộ bộ thẻ trước khi lưu.
4. Lưu Quiz, Questions và Options trong một transaction.
5. Nếu một phần lỗi thì rollback toàn bộ.

Quy tắc Flashcard:

- Mỗi thẻ có đúng một option.
- Option đó phải có nội dung và `isCorrect = true`.
- `position` phải không âm và không được trùng.
- `activityType` và `questionType` phải khớp.

#### Danh sách của user hiện tại

```http
GET /api/Quiz/me
GET /api/Quiz/me?activityType=Quiz
GET /api/Quiz/me?activityType=Flashcard
```

- Chỉ trả Quiz thuộc Document của user hiện tại.
- Trả `activityType` và `questionCount`.
- Không trả dữ liệu soft-deleted.

#### Chi tiết Quiz/Flashcard

```http
GET /api/Quiz/{id}/detail
```

- Kiểm tra ownership.
- Trả Quiz cùng Questions và Options.
- Questions được sắp theo `position`.
- Nested data được tải trong Repository, không query Option riêng cho từng Question.

### API cũ vẫn giữ

- `POST /api/Quiz`: tạo container `AI_Drafted`; thiếu activity type sẽ mặc định `Quiz`.
- `PUT /api/Quiz/{id}`: sửa title/status.
- `DELETE /api/Quiz/{id}`: soft-delete.
- `PATCH /api/Quiz/{id}/toggle-delete`: xóa mềm/khôi phục.
- Question và Option CRUD vẫn hoạt động.
- Question create/update hỗ trợ `position`.

## Frontend cần cập nhật

Frontend hiện đã có UI chọn Flashcard, gọi AI, preview và chỉnh sửa thẻ. Phần còn lại:

1. Chuyển giá trị UI `flashcards` thành API value `Flashcard`.
2. Thay chuỗi request create Quiz -> create từng Question -> update Quiz bằng một request
   `POST /api/Quiz/publish`.
3. Dùng `GET /api/Quiz/me` cho Library thay vì gọi Quiz riêng cho từng Document.
4. Thêm filter `Quiz` / `Flashcard` dựa trên `activityType`.
5. Dùng `GET /api/Quiz/{id}/detail` để mở lại bộ thẻ.
6. Hiển thị `questionCount` là số câu hoặc số thẻ.
7. Giữ API update/delete/toggle-delete hiện có.

Nếu frontend vẫn dùng publish flow cũ, Flashcard có thể bị phân loại thành `Quiz` do `POST /api/Quiz` mặc định
activity type là `Quiz`.

AI service hiện đã trả đúng cấu trúc mặt trước/mặt sau và chưa cần thay đổi cho phase này.

## Kiểm thử và build gần nhất

Các lệnh đã chạy sau implementation flashcard:

```powershell
dotnet test Mascoteach.Tests\Mascoteach.Tests.csproj --no-restore
dotnet build EXE101-Mascoteach-Backend.sln --no-restore
```

Kết quả tại thời điểm hoàn thành:

- `127/127` tests passed.
- Build thành công.
- `0` build errors.
- Còn cảnh báo `NU1903` vì AutoMapper `13.0.1` có advisory mức high.
- AutoMapper chưa được nâng version vì việc đó nằm ngoài phạm vi flashcard và cần một thay đổi dependency riêng.

Test flashcard bao phủ:

- Publish hợp lệ.
- Ownership.
- Thiếu mặt sau.
- Nhiều option cho một flashcard.
- Activity/question type không khớp.
- Position trùng.
- Rollback khi database lỗi.
- Filter danh sách.
- Detail đúng thứ tự.
- Controller lấy user ID từ JWT.
- Question tự tính/cập nhật position.

## Deployment

### Development

- Database dev đã rollout.
- Backend flashcard đã được merge vào `develop` theo trạng thái được báo cáo.
- Cần kiểm tra CI/CD và smoke test API trên môi trường dev.
- Frontend dev cần cập nhật theo API contract mới.

### Production

Trước khi deploy production:

1. Backup database production.
2. Chạy đúng idempotent SQL script đã kiểm chứng ở dev.
3. Kiểm tra `activity_type`, `question_type`, `position`, constraints và indexes.
4. Sau đó mới deploy backend production.
5. Deploy frontend tương thích sau backend.

Không deploy backend production trước schema vì ứng dụng mới đọc trực tiếp các cột flashcard.

## Việc còn lại

- Frontend tích hợp API flashcard mới.
- Smoke test end-to-end trên dev: upload -> AI generate -> preview -> publish -> Library -> detail.
- Kiểm tra pipeline deploy develop sau merge.
- Chuẩn bị và chạy production database rollout trước khi merge/deploy main.
- Tách việc đánh giá/nâng AutoMapper thành task riêng.
- Chỉ thêm study progress, bookmark, share, archive hoặc bảng Flashcard riêng khi product scope yêu cầu.

## Tài liệu kỹ thuật nguồn chuẩn

Đọc `.codex/skills/` trước khi sửa backend, đặc biệt:

- `.codex/skills/mascoteach-flashcards.md`
- `.codex/skills/mascoteach-existing-feature.md`
- `.codex/skills/mascoteach-debug-build.md`
- `.codex/skills/mascoteach-deployment.md`
- `.codex/skills/mascoteach-auth-permission.md`
- `.codex/skills/mascoteach-s3-document-flow.md`
- `.codex/skills/mascoteach-payos-billing.md`

