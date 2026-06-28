# Mascoteach Admin Dashboard - Todo & Scope

## Mục tiêu

Admin Dashboard của Mascoteach nên là bảng điều khiển vận hành hệ thống, không chỉ là trang xem thống kê. Trang Admin cần giúp đội ngũ quản lý:

- Người dùng đang sử dụng hệ thống như thế nào.
- Giáo viên tạo tài liệu, quiz, flashcard và phiên chơi ra sao.
- AI generate có ổn định không.
- Live session/game có hoạt động tốt không.
- Subscription, thanh toán, quota và doanh thu có vấn đề gì cần xử lý không.

Admin không nên can thiệp quá sâu vào workspace riêng của giáo viên nếu không cần thiết. Với tài liệu upload, quiz và flashcard, Admin nên quản lý ở tầng vận hành, an toàn, quota và hỗ trợ.

## Nguyên tắc phạm vi

### Admin nên quản lý

- Xem tổng số tài liệu, quiz, flashcard theo giáo viên.
- Theo dõi trạng thái xử lý: upload thành công, AI generate thành công, lỗi, bị xoá, bị ẩn.
- Theo dõi dung lượng và lượt tạo nội dung để kiểm soát quota Free/Pro.
- Tìm tài liệu/quiz theo giáo viên khi cần support.
- Xoá, ẩn hoặc khôi phục nội dung trong trường hợp vi phạm, spam, lỗi dữ liệu hoặc user yêu cầu.
- Retry hoặc reset trạng thái nếu tài liệu bị kẹt ở bước xử lý.
- Xem log lỗi generate để debug AI/backend.
- Xem nội dung ở mức cần thiết cho support/moderation, có audit log.

### Admin chưa cần quản lý ở MVP

- Duyệt thủ công từng quiz trước khi giáo viên dùng.
- Sửa nội dung câu hỏi thay giáo viên.
- Quản lý tài liệu/quiz như một CMS công khai.
- Can thiệp sâu vào nội dung riêng tư của giáo viên nếu không có lý do support, abuse hoặc compliance.

## MVP đề xuất

Làm trước 5 khu vực chính:

1. `/admin` - Tổng quan hệ thống.
2. `/admin/users` - Quản lý người dùng.
3. `/admin/content` - Theo dõi tài liệu, quiz, flashcard và trạng thái AI.
4. `/admin/sessions` - Theo dõi phiên live/game.
5. `/admin/billing` - Gói Pro, đơn thanh toán, doanh thu.

Sau MVP mới thêm:

- `/admin/ai-usage`
- `/admin/audit-logs`
- `/admin/settings`
- `/admin/support`

## Module 1 - Tổng Quan Hệ Thống

### Chức năng

- Tổng số giáo viên.
- Tổng số học sinh đã tham gia phiên chơi.
- Tài khoản mới theo ngày/tuần/tháng.
- Tổng số tài liệu đã upload.
- Tổng số quiz đã tạo.
- Tổng số flashcard đã tạo.
- Tổng số live session/game đã mở.
- Tổng số lượt học sinh tham gia bằng PIN.
- Tỷ lệ Free/Pro.
- Doanh thu tháng.
- Số đơn thanh toán thành công, pending, cancelled, failed.

### Cảnh báo nhanh

- AI generate lỗi nhiều trong ngày.
- Tài liệu bị kẹt xử lý.
- Thanh toán thành công nhưng user chưa được nâng cấp Pro.
- Live session có lỗi realtime hoặc reconnect nhiều.
- User dùng quota bất thường.
- Số lượng upload/generate tăng đột biến.

### UI gợi ý

- Cards thống kê chính.
- Biểu đồ tăng trưởng user.
- Biểu đồ nội dung tạo mới.
- Biểu đồ doanh thu.
- Bảng cảnh báo cần xử lý.

## Module 2 - Quản Lý Người Dùng

Frontend hiện có `userService` với các API cơ bản:

- `getAllUsers()`
- `getUserById(id)`
- `updateUser(id, data)`
- `deleteUser(id)`

### Chức năng

- Danh sách user.
- Tìm kiếm theo tên, email.
- Filter theo role: Teacher, Student, Parent, Admin.
- Filter theo subscription: Free, Pro, hết hạn.
- Xem chi tiết user.
- Xem tài liệu đã upload của user.
- Xem quiz/flashcard đã tạo.
- Xem session đã tổ chức.
- Xem billing/subscription hiện tại.
- Chỉnh role trong trường hợp cần thiết.
- Chỉnh subscription thủ công cho support.
- Khoá/mở tài khoản nếu backend hỗ trợ.
- Soft delete hoặc restore user nếu backend hỗ trợ.

### Cột dữ liệu nên có

- Tên.
- Email.
- Role.
- Subscription tier.
- Trạng thái tài khoản.
- Ngày tạo.
- Lần hoạt động gần nhất.
- Số tài liệu.
- Số quiz/flashcard.
- Số session.

### Hành động

- View detail.
- Edit role.
- Edit subscription.
- Disable/enable account.
- Delete hoặc soft delete.
- Impersonation chỉ nên cân nhắc sau, cần audit rất chặt.

## Module 3 - Nội Dung & Xử Lý AI

Module này không phải CMS. Đây là nơi Admin theo dõi sức khỏe vận hành của tài liệu, quiz, flashcard và AI generation.

### Tài liệu upload

Nên có:

- Danh sách tài liệu toàn hệ thống.
- Tên file.
- Giáo viên sở hữu.
- Dung lượng.
- Ngày upload.
- Trạng thái: uploaded, ready, processing, failed, deleted.
- Số quiz/flashcard sinh ra từ tài liệu.
- Lỗi xử lý gần nhất nếu có.
- Link xem metadata.

Hành động:

- View metadata.
- Retry processing nếu lỗi.
- Ẩn hoặc xoá tài liệu vi phạm.
- Restore nếu dùng soft delete.
- Xem log xử lý.

### Quiz và flashcard

Nên có:

- Danh sách quiz/flashcard toàn hệ thống.
- Chủ sở hữu.
- Tài liệu nguồn.
- Loại activity: quiz, flashcards.
- Số câu hỏi/thẻ.
- Trạng thái: AI_Drafted, Teacher_Approved, Published, Deleted.
- Ngày tạo.
- Lần dùng gần nhất.

Hành động:

- View summary.
- Xem câu hỏi ở mức support/moderation.
- Ẩn hoặc xoá nội dung vi phạm.
- Restore nếu backend hỗ trợ.
- Retry generate nếu nội dung lỗi.

### Không ưu tiên trong MVP

- Duyệt thủ công từng quiz.
- Sửa câu hỏi thay giáo viên.
- Tạo thư viện học liệu công khai.

## Module 4 - Live Session & Game Monitoring

Hệ thống hiện có:

- LiveSession.
- PIN join.
- Student waiting page.
- Student live game.
- Treasure Hunt.
- Adventure game.
- SignalR realtime.

### Chức năng

- Danh sách phiên đang chạy.
- Danh sách phiên đã kết thúc.
- Tìm theo PIN, giáo viên, quiz, trạng thái.
- Xem số học sinh tham gia.
- Xem game mode/template.
- Xem trạng thái session.
- Xem thời gian bắt đầu/kết thúc.
- Xem lỗi realtime nếu backend log được.

### Hành động

- Xem chi tiết session.
- Kết thúc phiên khẩn cấp.
- Xoá hoặc archive session lỗi.
- Mở quiz gốc của session.
- Mở báo cáo session.

### Metrics nên có

- Số session theo ngày.
- Số học sinh trung bình mỗi session.
- Game mode được dùng nhiều nhất.
- Tỷ lệ session lỗi.
- Thời lượng session trung bình.

## Module 5 - Billing & Subscription

Hệ thống hiện có:

- Gói `PRO_MONTHLY`.
- Gói `PRO_YEARLY`.
- Checkout.
- Payment success/cancel.
- Billing status.

### Chức năng

- Danh sách đơn thanh toán.
- Trạng thái đơn: pending, paid, cancelled, failed.
- User mua gói.
- Plan code.
- Số tiền.
- Ngày tạo đơn.
- Ngày thanh toán.
- Ngày hết hạn Pro.
- Doanh thu theo ngày/tháng.
- Tỷ lệ Free sang Pro.

### Hành động

- Xem chi tiết đơn.
- Sync lại billing status.
- Nâng cấp/gia hạn Pro thủ công cho user.
- Hủy Pro thủ công nếu cần.
- Kiểm tra đơn đã paid nhưng user chưa được nâng cấp.
- Xem payment callback/webhook log nếu backend có.

## Module 6 - AI Usage & Quota

Nên làm sau MVP, nhưng cần thiết khi hệ thống tăng trưởng.

### Chức năng

- Số lượt generate theo ngày.
- Số lượt generate theo user.
- Số lượt generate theo loại: quiz, flashcards.
- Tỷ lệ generate thành công/thất bại.
- Thời gian xử lý trung bình.
- Tài liệu nào generate lỗi nhiều.
- User nào dùng quota bất thường.
- Ước tính chi phí AI nếu backend có tracking token/cost.

### Quota

- Quota tài liệu upload.
- Quota quiz/flashcard.
- Quota câu hỏi.
- Quota live session.
- Quota theo Free/Pro.

### Hành động

- Reset quota user.
- Tăng quota tạm thời.
- Chặn generate nếu phát hiện abuse.
- Xem lý do bị giới hạn.

## Module 7 - Support Console

Nên làm sau khi có user/content/session/billing cơ bản.

### Chức năng

- Tìm user bằng email.
- Xem timeline hoạt động của user:
  - Đăng ký.
  - Upload tài liệu.
  - Tạo quiz/flashcard.
  - Mở session.
  - Học sinh tham gia.
  - Thanh toán.
  - Lỗi gần nhất.
- Ghi chú nội bộ cho support.
- Đánh dấu ticket/case nếu có hệ thống support sau này.

## Module 8 - Audit Log & Phân Quyền Admin

Vì Admin Dashboard có quyền mạnh, audit log rất quan trọng.

### Cần ghi log

- Admin đổi role user.
- Admin đổi subscription user.
- Admin xoá/ẩn/restore tài liệu.
- Admin xoá/ẩn/restore quiz.
- Admin kết thúc session.
- Admin reset quota.
- Admin xem nội dung nhạy cảm nếu backend yêu cầu tracking.

### Phân quyền admin đề xuất

- Owner: toàn quyền.
- Admin: quản lý user, content, billing, sessions.
- Support: xem user, session, billing cơ bản; không xoá dữ liệu.
- Content Moderator: xử lý nội dung vi phạm; không chỉnh billing.
- Billing Manager: xử lý thanh toán/subscription; không xem nội dung chi tiết.

## Đối Chiếu DB Schema Backend Hiện Tại

Schema hiện tại trong `MascoteachDbContext` đã có các bảng chính:

- `Users`
- `User_Stats`
- `Documents`
- `Quizzes`
- `Questions`
- `Options`
- `Live_Sessions`
- `Session_Participants`
- `Quiz_Attempts`
- `Game_Templates`
- `Payment_Orders`
- `Payment_Webhook_Events`

Kết luận nhanh: phần lớn dashboard MVP có thể tận dụng bảng hiện tại và tạo thêm API admin tổng hợp. Không nên tạo table mới cho những màn hình chỉ cần đọc danh sách, thống kê, lọc, hoặc join dữ liệu đã có.

### Có thể tận dụng ngay, chủ yếu cần API admin tổng hợp

#### Users

Tận dụng `Users` và `User_Stats`.

Dữ liệu đã có:

- `Users.id`, `full_name`, `email`, `role`, `subscription_tier`, `premium_expires_at`, `documents_processed`, `created_at`, `is_deleted`, `avatar_url`.
- `User_Stats.user_id`, `xp`, `current_streak`, `last_active_date`, `total_learning_seconds`, `total_correct_answers`, `total_questions_answered`, `updated_at`.

Dùng được cho:

- Danh sách người dùng.
- Lọc theo role, gói, trạng thái xoá mềm.
- Tổng giáo viên/học sinh/admin.
- Hoạt động học tập cơ bản.
- Trang chi tiết user.

Cần API, chưa cần bảng mới:

- `GET /api/Admin/users`
- `GET /api/Admin/users/{id}`
- `PATCH /api/Admin/users/{id}/role`
- `PATCH /api/Admin/users/{id}/subscription`
- `PATCH /api/Admin/users/{id}/status`

Lưu ý: backend đã có `UserController.GetAll`, `GetById`, `Update`, `ToggleDelete`, nhưng dashboard vẫn nên có `AdminController/AdminUserService` riêng để trả thêm aggregate: document count, quiz count, flashcard count, live session count, paid order count, last active.

#### Content: tài liệu, quiz, flashcard

Tận dụng `Documents`, `Quizzes`, `Questions`, `Options`.

Dữ liệu đã có:

- `Documents`: owner, file URL/S3 key, file name, uploaded date, deleted flag.
- `Quizzes`: document, title, status, activity type `Quiz` hoặc `Flashcard`, created date, deleted flag.
- `Questions`, `Options`: nội dung câu hỏi/thẻ và đáp án.

Dùng được cho:

- Danh sách tài liệu toàn hệ thống.
- Danh sách quiz/flashcard toàn hệ thống.
- Đếm số nội dung theo giáo viên.
- Lọc theo chủ sở hữu, loại activity, ngày tạo, trạng thái, deleted.
- Xem metadata và detail ở mức support/moderation.
- Ẩn/khôi phục bằng `is_deleted`.

Cần API, chưa cần bảng mới:

- `GET /api/Admin/content`
- `GET /api/Admin/documents`
- `GET /api/Admin/documents/{id}`
- `GET /api/Admin/quizzes`
- `GET /api/Admin/quizzes/{id}`
- `PATCH /api/Admin/documents/{id}/hide`
- `PATCH /api/Admin/documents/{id}/restore`
- `PATCH /api/Admin/quizzes/{id}/hide`
- `PATCH /api/Admin/quizzes/{id}/restore`

Lưu ý: hiện `Document.ToggleDelete` và `Quiz.ToggleDelete` đang check owner, nên Admin API riêng cần bypass owner check nhưng bắt buộc ghi audit log.

#### Live sessions/game

Tận dụng `Live_Sessions`, `Session_Participants`, `Game_Templates`, `Quiz_Attempts`.

Dữ liệu đã có:

- `Live_Sessions`: teacher, quiz, template, game pin, status, created date, deleted flag.
- `Session_Participants`: session, student name, total score.
- `Game_Templates`: tên mode/game, bundle, thumbnail.
- `Quiz_Attempts`: user, quiz, correct count, total questions, duration, XP, completed date.

Dùng được cho:

- Danh sách phiên đang chạy/đã kết thúc.
- Tìm theo PIN.
- Đếm học sinh tham gia.
- Xem game mode/template.
- Xem điểm participant.
- Thống kê số phiên theo ngày và mode.

Cần API, chưa cần bảng mới:

- `GET /api/Admin/sessions`
- `GET /api/Admin/sessions/{id}`
- `GET /api/Admin/sessions/{id}/participants`
- `POST /api/Admin/sessions/{id}/end`

Thiếu dữ liệu nếu muốn giám sát realtime sâu:

- Chưa có bảng lưu event/reconnect/disconnect theo thời gian.
- Chưa có thời điểm ended_at riêng, hiện chỉ có `created_at` và `status`.

#### Billing/subscription

Tận dụng `Payment_Orders`, `Payment_Webhook_Events`, `Users.subscription_tier`, `Users.premium_expires_at`.

Dữ liệu đã có:

- `Payment_Orders`: user, order code, plan code, amount, currency, status, provider, payment link, checkout URL, QR, reference, paid/cancelled/created/updated date.
- `Payment_Webhook_Events`: provider, order code, reference, payment link id, signature, raw payload, processed date, processed flag, processing error.
- `Users`: subscription tier và premium expiry.

Dùng được cho:

- Danh sách đơn hàng.
- Revenue theo ngày/tháng.
- Đơn pending/paid/cancelled/failed/expired.
- Tra lỗi webhook.
- Phát hiện đơn đã paid nhưng user chưa được Premium.

Cần API, chưa cần bảng mới:

- `GET /api/Admin/billing/orders`
- `GET /api/Admin/billing/orders/{id}`
- `GET /api/Admin/billing/revenue`
- `GET /api/Admin/billing/webhook-events`
- `POST /api/Admin/billing/orders/{orderCode}/sync`
- `PATCH /api/Admin/billing/users/{userId}/subscription`

Lưu ý: backend đã có `BillingController` cho flow người dùng, nhưng chưa có endpoint admin để xem toàn bộ orders hoặc webhook events.

### Nên thêm table mới

#### 1. `Admin_Audit_Logs` - bắt buộc cho Admin Dashboard

Lý do:

- Admin có quyền đổi role, đổi subscription, ẩn/khôi phục nội dung, kết thúc phiên, reset quota.
- Không nên chỉ dựa vào log ứng dụng vì cần truy vấn trong dashboard và phục vụ truy vết.

Schema đề xuất:

- `id`
- `actor_user_id`
- `actor_email`
- `action`
- `target_type`
- `target_id`
- `severity` hoặc `risk_level`
- `reason`
- `before_json`
- `after_json`
- `ip_address`
- `user_agent`
- `created_at`

Áp dụng cho:

- Đổi role/subscription/status user.
- Ẩn/restore document/quiz.
- Xem nội dung chi tiết nếu có dữ liệu nhạy cảm.
- Kết thúc session.
- Sync billing hoặc chỉnh Premium thủ công.
- Reset/tăng quota.
- Thay đổi admin settings.

#### 2. `Admin_Settings` hoặc `System_Settings`

Lý do:

- Hiện hạn mức Freemium document đang lấy từ config `Plans:FreemiumActiveDocumentLimit`.
- Dashboard có trang cài đặt: giới hạn gói miễn phí, bật/tắt tính năng, ngưỡng cảnh báo, phân quyền.
- Nếu muốn admin chỉnh từ web mà không deploy lại backend, cần lưu DB.

Schema tối giản đề xuất:

- `id`
- `setting_key`
- `setting_value`
- `value_type`
- `description`
- `updated_by`
- `updated_at`

Nhóm setting cần có:

- `freemium.active_document_limit`
- `freemium.quiz_limit`
- `freemium.flashcard_limit`
- `freemium.live_session_limit`
- `feature.flashcards_enabled`
- `feature.adventure_game_enabled`
- `alerts.ai_error_rate_threshold`
- `alerts.paid_unsynced_minutes`
- `alerts.realtime_reconnect_threshold`

#### 3. `Ai_Processing_Logs` hoặc `Content_Processing_Logs`

Lý do:

- Admin dashboard cần biết tài liệu/AI bị lỗi ở bước nào.
- Schema hiện tại không lưu document processing status/error. `Documents` chỉ có file info và deleted flag; `Quizzes` có status nhưng không có error message, duration, retry count.

Schema đề xuất:

- `id`
- `document_id` nullable
- `quiz_id` nullable
- `user_id`
- `job_type` như `DocumentParsing`, `QuizGeneration`, `FlashcardGeneration`
- `status` như `Queued`, `Processing`, `Succeeded`, `Failed`, `Cancelled`
- `error_code`
- `error_message`
- `retry_count`
- `started_at`
- `finished_at`
- `duration_ms`
- `provider`
- `model`
- `input_tokens`
- `output_tokens`
- `estimated_cost`
- `created_at`

Nếu AI service đang tách repo, vẫn nên đồng bộ một bản log tối thiểu về backend DB để Admin Dashboard tra cứu.

#### 4. `User_Quota_Overrides` hoặc `User_Quotas`

Lý do:

- Hiện quota document Freemium được tính bằng `Documents` active count và config global.
- Admin Dashboard có nhu cầu reset quota, tăng quota tạm thời, chặn generate khi abuse.
- Không nên sửa trực tiếp `Users.DocumentsProcessed` cho mọi loại quota vì field này chỉ phản ánh document processed, không đủ cho quiz/flashcard/session.

Schema đề xuất:

- `id`
- `user_id`
- `quota_type` như `Document`, `Quiz`, `Flashcard`, `LiveSession`, `AiGeneration`
- `limit_value`
- `used_value`
- `period_start`
- `period_end`
- `override_reason`
- `updated_by`
- `updated_at`

MVP có thể chưa tạo bảng này nếu chỉ hiển thị quota document hiện tại. Tạo khi backend bắt đầu cho Admin reset/tăng quota trên web.

#### 5. `Live_Session_Events` - chỉ cần nếu muốn debug realtime

Lý do:

- Hiện `Live_Sessions` chỉ có status và `Session_Participants` chỉ có tổng điểm.
- Admin muốn thấy lỗi realtime/reconnect nhiều thì cần event history.

Schema đề xuất:

- `id`
- `session_id`
- `participant_id` nullable
- `event_type` như `Joined`, `Disconnected`, `Reconnected`, `AnswerSubmitted`, `SessionStarted`, `SessionEnded`, `Error`
- `payload_json`
- `created_at`

MVP có thể bỏ qua nếu chỉ cần danh sách session và participants.

#### 6. `Support_Cases` hoặc `Admin_Notes` - sau MVP

Lý do:

- Trang Support Console cần ghi chú nội bộ/ticket.
- Hiện schema chưa có support case.

Schema tối giản:

- `id`
- `user_id`
- `related_type`
- `related_id`
- `note`
- `status`
- `created_by`
- `created_at`
- `updated_at`

### Có thể chỉ thêm cột, chưa cần table riêng

Tùy mức độ muốn làm MVP nhanh, backend có thể thêm vài cột thay vì tạo bảng lớn:

- `Documents.processing_status`
- `Documents.processing_error`
- `Documents.processing_started_at`
- `Documents.processing_finished_at`
- `Documents.file_size_bytes`
- `Live_Sessions.ended_at`
- `Live_Sessions.last_error`
- `Users.last_login_at` hoặc `last_active_at`

Tuy nhiên nếu muốn có lịch sử lỗi/retry nhiều lần thì vẫn nên dùng `Content_Processing_Logs` thay vì chỉ cột cuối cùng.

### Không nên thêm table mới ở MVP

- Không cần bảng riêng cho dashboard cards; query aggregate từ bảng hiện có.
- Không cần bảng riêng cho `Teachers`, `Students`, `Parents`; hiện `Users.role` đủ.
- Không cần bảng riêng cho Pro plan nếu chỉ có `PRO_MONTHLY` và `PRO_YEARLY`; có thể hard-code trong service như hiện tại. Chỉ tạo `Billing_Plans` nếu product cần chỉnh giá/gói từ admin.
- Không cần bảng CMS/public content library ở giai đoạn đầu.

## Backend/API Cần Bổ Sung

Nếu backend hiện chưa có API admin tổng hợp, nên thêm các endpoint riêng để tránh frontend phải gọi quá nhiều API lẻ.

Ưu tiên triển khai API trước khi thêm nhiều bảng mới. Với schema hiện tại, MVP có thể query trực tiếp từ các bảng đang có:

- `Users`, `User_Stats`
- `Documents`, `Quizzes`, `Questions`, `Options`
- `Live_Sessions`, `Session_Participants`, `Game_Templates`, `Quiz_Attempts`
- `Payment_Orders`, `Payment_Webhook_Events`

Các bảng nên thêm trước khi bật thao tác admin thật:

- `Admin_Audit_Logs`
- `Admin_Settings` hoặc `System_Settings`
- `Content_Processing_Logs` hoặc `Ai_Processing_Logs`

### Overview

- `GET /api/Admin/overview`
- `GET /api/Admin/overview/growth`
- `GET /api/Admin/overview/alerts`

### Users

- `GET /api/Admin/users`
- `GET /api/Admin/users/{id}`
- `PATCH /api/Admin/users/{id}/role`
- `PATCH /api/Admin/users/{id}/subscription`
- `PATCH /api/Admin/users/{id}/status`
- `DELETE /api/Admin/users/{id}`

### Content

- `GET /api/Admin/documents`
- `GET /api/Admin/documents/{id}`
- `PATCH /api/Admin/documents/{id}/hide`
- `PATCH /api/Admin/documents/{id}/restore`
- `POST /api/Admin/documents/{id}/retry-processing`
- `GET /api/Admin/quizzes`
- `GET /api/Admin/quizzes/{id}`
- `PATCH /api/Admin/quizzes/{id}/hide`
- `PATCH /api/Admin/quizzes/{id}/restore`
- `POST /api/Admin/quizzes/{id}/retry-generation`

### Sessions

- `GET /api/Admin/sessions`
- `GET /api/Admin/sessions/{id}`
- `POST /api/Admin/sessions/{id}/end`
- `GET /api/Admin/sessions/{id}/participants`
- `GET /api/Admin/sessions/{id}/events`

### Billing

- `GET /api/Admin/billing/orders`
- `GET /api/Admin/billing/orders/{id}`
- `GET /api/Admin/billing/webhook-events`
- `POST /api/Admin/billing/orders/{id}/sync`
- `PATCH /api/Admin/billing/users/{userId}/subscription`
- `GET /api/Admin/billing/revenue`

### AI Usage & Quota

- `GET /api/Admin/ai-usage`
- `GET /api/Admin/ai-usage/errors`
- `GET /api/Admin/quota/users/{userId}`
- `PATCH /api/Admin/quota/users/{userId}`
- `POST /api/Admin/quota/users/{userId}/reset`

### Audit

- `GET /api/Admin/audit-logs`
- `GET /api/Admin/audit-logs/{id}`

### Settings

- `GET /api/Admin/settings`
- `PATCH /api/Admin/settings/{key}`
- `GET /api/Admin/settings/history`

### Support

- `GET /api/Admin/support/search?q=`
- `GET /api/Admin/support/users/{userId}/timeline`
- `POST /api/Admin/support/notes`

## Frontend Việc Cần Làm

### Routing

- Tạo route `/admin`.
- Tạo nested routes:
  - `/admin`
  - `/admin/users`
  - `/admin/content`
  - `/admin/sessions`
  - `/admin/billing`
  - `/admin/ai-usage`
  - `/admin/audit-logs`
  - `/admin/settings`

### Auth/Role

- Cập nhật `ProtectedRoute` để hỗ trợ role `Admin`.
- Route admin dùng:

```jsx
<ProtectedRoute allowedRoles={['Admin']}>
  <AdminLayout />
</ProtectedRoute>
```

### Layout

- Tạo `AdminLayout`.
- Tạo `AdminSidebar`.
- Tạo `AdminHeader`.
- Tạo component shared:
  - `AdminStatCard`
  - `AdminDataTable`
  - `AdminFilterBar`
  - `AdminStatusBadge`
  - `AdminConfirmDialog`
  - `AdminEmptyState`
  - `AdminLoadingState`

### Services

Tạo service riêng:

- `src/services/adminService.js`

Nên gom các API admin vào đây thay vì rải trong từng page.

### Pages

- `src/pages/admin/AdminOverviewPage.jsx`
- `src/pages/admin/AdminUsersPage.jsx`
- `src/pages/admin/AdminUserDetailPage.jsx`
- `src/pages/admin/AdminContentPage.jsx`
- `src/pages/admin/AdminSessionsPage.jsx`
- `src/pages/admin/AdminBillingPage.jsx`
- `src/pages/admin/AdminAiUsagePage.jsx`
- `src/pages/admin/AdminAuditLogsPage.jsx`
- `src/pages/admin/AdminSettingsPage.jsx`

## UI/UX Đề Xuất

Admin Dashboard nên mang cảm giác vận hành, rõ ràng, scan nhanh:

- Không dùng hero/marketing layout.
- Sidebar cố định.
- Header có search global.
- Table là trung tâm của các trang quản lý.
- Filter rõ ràng: role, status, date range, plan, owner.
- Các hành động nguy hiểm phải có confirm dialog.
- Dữ liệu nhạy cảm cần hiển thị vừa đủ.
- Màu trạng thái thống nhất:
  - Xanh: active/success.
  - Vàng: pending/warning.
  - Đỏ: failed/deleted/blocked.
  - Xám: inactive/archived.

## Checklist MVP

### Security blockers trước khi mở rộng Admin API

- [x] Giới hạn `GET /api/User` và `GET /api/User/{id}` cho role `Admin`.
  - Status: Completed
  - `/api/User/me` vẫn dành cho mọi user đã đăng nhập.
  - Verified by `UserControllerSecurityTests`; full suite `137/137` passed and solution build succeeded.
- [x] Chặn profile update thay đổi `Role` hoặc `SubscriptionTier`.
  - Status: Completed
  - `PUT /api/User/{id}` chỉ nhận và cập nhật `FullName`, `Email`.
  - Payload chứa `Role` hoặc `SubscriptionTier` bị JSON binding bỏ qua; service bảo toàn giá trị đang lưu.
  - Thay đổi role/subscription sau này phải đi qua Admin API riêng có audit log.
  - Verified by attacker-payload regression tests; full suite `137/137` passed and solution build succeeded.

### Giai đoạn 0 - Backend schema/API audit

- [ ] Xác nhận backend dùng schema trong `D:\Projects\Back-End\Mascoteach.Data\Models\MascoteachDbContext.cs`.
- [ ] Tạo `AdminController` hoặc nhóm controller `AdminUsersController`, `AdminContentController`, `AdminBillingController`.
- [ ] Tạo service đọc aggregate trực tiếp từ bảng hiện có, tránh frontend gọi nhiều API lẻ.
- [ ] Tạo DTO riêng cho admin list/detail để trả count, owner, status, timestamps.
- [ ] Thêm bảng `Admin_Audit_Logs` trước khi bật thao tác thay đổi dữ liệu.
- [ ] Thêm bảng `Admin_Settings` nếu muốn chỉnh hạn mức/tính năng/ngưỡng cảnh báo từ dashboard.
- [ ] Thêm bảng `Content_Processing_Logs` hoặc `Ai_Processing_Logs` nếu muốn hiển thị lỗi AI/retry/duration/token/cost.
- [ ] Chưa cần thêm bảng mới cho overview cards, users, documents, quizzes, sessions, billing orders.
- [ ] Chưa cần thêm bảng `Billing_Plans` nếu gói Pro vẫn cố định trong `BillingService`.
- [ ] Chưa cần thêm bảng `Live_Session_Events` trong MVP nếu chỉ xem session và participants.

### Giai đoạn 1 - Nền admin

- [ ] Thêm role `Admin` vào luồng phân quyền nếu backend/frontend chưa hỗ trợ.
- [ ] Tạo route `/admin`.
- [ ] Tạo `AdminLayout`.
- [ ] Tạo `AdminSidebar`.
- [ ] Tạo `adminService.js`.
- [ ] Tạo shared table/card/status components.

### Giai đoạn 2 - Overview

#### Backend Admin Overview v2

- [x] Hoàn thiện `GET /api/Admin/overview?range=7d|30d|12m`.
  - Status: Completed
  - Range không hợp lệ trả HTTP 400; response ghi rõ `range`, `from`, `to`.
- [x] Thống kê user/role/subscription/content/session/participant/payment bằng dữ liệu DB hiện có.
  - Status: Completed
  - Participant là tổng lượt join bằng PIN, không phải số học sinh duy nhất.
  - Revenue là tổng Payment Order `Paid` trong kỳ; series là doanh thu Paid 12 tháng.
- [x] Không đưa AI/realtime/quota alerts vào response khi chưa có telemetry.
  - Status: Confirmed scope
- Verification: focused Overview/Admin Controller tests `10/10`, full suite `153/153`, Release build thành công.
- Remaining check: smoke test aggregate queries trên SQL Server dev vì unit tests hiện mock repository.

- [ ] Tạo `AdminOverviewPage`.
- [ ] Hiển thị user count.
- [ ] Hiển thị document count.
- [ ] Hiển thị quiz/flashcard count.
- [ ] Hiển thị live session count.
- [ ] Hiển thị Pro/Free count.
- [ ] Hiển thị alerts cơ bản.

### Giai đoạn 3 - Users

#### Backend Admin Users read-only API

- [x] `GET /api/Admin/users` với search, role, subscription và pagination.
  - Status: Completed
  - Filters: role `Teacher|Student|Parent|Admin`; subscription `Freemium|Premium|Expired`.
- [x] `GET /api/Admin/users/{id}` với content/activity/billing aggregates không nhạy cảm.
  - Status: Completed
  - Trả content counts, learning stats và latest payment summary; không trả secret/token/S3/PayOS payload.
- [x] Xóa legacy `GET /api/Admin/accounts` và vertical slice riêng vì frontend chưa sử dụng.
  - Status: Completed
  - `/api/Admin/users` là contract duy nhất cho danh sách/quản lý user phía Admin.
- [x] Chưa thêm role/subscription/status mutation trước khi có `Admin_Audit_Logs`.
  - Status: Confirmed scope
- Verification: focused Admin tests `10/10`, full suite `147/147`, solution build thành công.
- Remaining check: smoke test EF projection trên SQL Server dev vì unit tests hiện mock repository.

- [ ] Tạo `AdminUsersPage`.
- [ ] Hiển thị danh sách user.
- [ ] Search theo tên/email.
- [ ] Filter role.
- [ ] Filter subscription.
- [ ] Xem detail user.
- [ ] Cập nhật role nếu backend hỗ trợ.
- [ ] Cập nhật subscription nếu backend hỗ trợ.

### Giai đoạn 4 - Content Monitoring

#### Backend Admin Content read-only API

- [x] `GET /api/Admin/documents` và `GET /api/Admin/documents/{id}` chỉ trả metadata an toàn.
  - Status: Completed
- [x] `GET /api/Admin/quizzes` và `GET /api/Admin/quizzes/{id}` dùng chung cho Quiz/Flashcard metadata.
  - Status: Completed
- [x] Search/filter/date/pagination và validation HTTP 400/404.
  - Status: Completed
- [x] Không trả S3 key, presigned URL, question/option text hoặc correct answer.
  - Status: Verified contract
- [x] Chưa thêm hide/restore/retry trước khi có `Admin_Audit_Logs`.
  - Status: Confirmed scope
- Verification: TDD RED xác nhận contract chưa tồn tại; focused Admin tests `25/25`, full suite `173/173`,
  Release build thành công.
- Remaining check: smoke test bốn aggregate/projection endpoint trên SQL Server dev vì unit tests mock repository.

- [ ] Tạo `AdminContentPage`.
- [ ] Tab tài liệu.
- [ ] Tab quiz/flashcard.
- [ ] Filter theo owner.
- [ ] Filter theo trạng thái.
- [ ] Filter theo ngày tạo.
- [ ] Xem metadata.
- [ ] Retry processing/generation nếu backend hỗ trợ.
- [ ] Hide/delete/restore nếu backend hỗ trợ.

### Giai đoạn 5 - Sessions

#### Backend Admin Sessions read-only API

- [x] `GET /api/Admin/sessions` với search/filter/date/deletion/pagination.
  - Status: Completed
- [x] `GET /api/Admin/sessions/{id}` trả session/teacher/quiz/template metadata và participant count.
  - Status: Completed
- [x] `GET /api/Admin/sessions/{id}/participants` trả display name/score metadata có pagination.
  - Status: Completed
- [x] Không trả JS bundle, quiz content, storage fields hoặc realtime data không được lưu.
  - Status: Verified contract
- [x] Chưa thêm end/delete/restore trước khi có `Admin_Audit_Logs`.
  - Status: Confirmed scope
- Verification: TDD RED xác nhận contract chưa tồn tại; focused Admin tests `28/28`, full suite `188/188`,
  Release build thành công.
- Remaining check: smoke test ba projection endpoint trên SQL Server dev vì unit tests mock repository.

- [ ] Tạo `AdminSessionsPage`.
- [ ] Danh sách live sessions.
- [ ] Search theo PIN.
- [ ] Filter trạng thái.
- [ ] Filter game mode.
- [ ] Xem participants.
- [ ] Kết thúc session nếu backend hỗ trợ.

### Giai đoạn 6 - Billing

#### Backend Admin Billing read-only API

- [x] `GET /api/Admin/billing/orders` với search/filter/date/deletion/pagination.
  - Status: Completed
- [x] `GET /api/Admin/billing/orders/{id}` trả order/user/subscription metadata an toàn.
  - Status: Completed
- [x] `GET /api/Admin/billing/webhook-events` trả processed state và `ProcessingError`.
  - Status: Completed
- [x] Không trả checkout URL, QR, payment-link id, signature hoặc raw payload.
  - Status: Verified contract
- [x] Chưa thêm sync/retry/manual subscription trước khi có `Admin_Audit_Logs`.
  - Status: Confirmed scope
- Verification: TDD RED xác nhận contract chưa tồn tại; focused Admin tests `34/34`, full suite `203/203`,
  Release build thành công.
- Remaining check: smoke test ba projection endpoint trên SQL Server dev vì unit tests mock repository.

#### Legacy Admin Revenue cleanup

- [x] Xóa `GET /api/Admin/revenue` và vertical slice cũ không theo roadmap.
  - Status: Completed
- [x] Giữ revenue Paid trong Overview và `PaidRevenueByMonthAsync`.
  - Status: Verified unchanged
- Verification: regression test RED/GREEN; full suite `188/188`, Release build thành công.

- [ ] Tạo `AdminBillingPage`.
- [ ] Danh sách orders.
- [ ] Filter trạng thái payment.
- [ ] Filter plan.
- [ ] Revenue summary.
- [ ] Sync order nếu backend hỗ trợ.
- [ ] Update subscription thủ công nếu backend hỗ trợ.

### Giai đoạn 7 - Sau MVP

- [ ] AI Usage page.
- [ ] Quota management.
- [ ] Audit logs.
- [ ] Support console.
- [ ] Admin settings.
- [ ] Feature flags.

## Câu Hỏi Cần Chốt Với Backend/Product

- [Đã chốt] Tài khoản Admin đầu tiên đã được provision thủ công trong DB. JWT hỗ trợ role `Admin`,
  còn public register phải tiếp tục chặn tự đăng ký Admin.
- Admin có được xem nội dung chi tiết của tài liệu/quiz không, hay chỉ metadata?
- Có cần audit khi Admin xem nội dung riêng của giáo viên không?
- Subscription có cho phép chỉnh thủ công không?
- Quota Free/Pro hiện mới có `Plans:FreemiumActiveDocumentLimit` và đếm active documents; cần chốt quota cho quiz, flashcard, live session, AI generation.
- Tài liệu/quiz/session/user hiện có `is_deleted`, nhưng một số endpoint delete vẫn hard-delete; cần chuẩn hóa Admin dùng soft delete hay hard delete.
- AI generate hiện chưa có bảng log lỗi đủ chi tiết; cần chốt tạo `Content_Processing_Logs`/`Ai_Processing_Logs`.
- Live session hiện có participants nhưng chưa có event/reconnect history; cần chốt có cần `Live_Session_Events` không.
- Billing webhook/payment callback đã có `Payment_Webhook_Events`; cần tạo API admin để xem/search bảng này.
- Có cần phân quyền nhiều cấp admin ngay từ đầu không?
- Admin settings có cần chỉnh trực tiếp trên web không, hay backend vẫn dùng appsettings/config trong MVP?
- Có cần lưu lý do khi Admin ẩn/khôi phục nội dung, chỉnh subscription, reset quota không? Nếu có, đưa vào `Admin_Audit_Logs.reason`.

## Kết Luận

Admin Dashboard nên ưu tiên vận hành hệ thống hơn là biên tập nội dung giáo viên. MVP tốt nhất là:

- Quản lý user.
- Theo dõi content và AI processing.
- Theo dõi live session.
- Theo dõi billing/subscription.
- Có overview và cảnh báo.

Phần tài liệu, quiz, flashcard của giáo viên nên được quản lý ở mức metadata, trạng thái, quota, lỗi xử lý và moderation. Không nên biến Admin thành nơi duyệt/sửa từng nội dung học tập trong giai đoạn đầu.
