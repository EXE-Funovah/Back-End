# Mascoteach Backend - Audit Checklist (theo Codex Skills)

## 1. Auth & Permission (mascoteach-auth-permission.md)

- [x] AuthController có `[AllowAnonymous]` trên Register và Login
- [x] AuthController có namespace `Mascoteach.API.Controllers`
- [x] BaseController có namespace `Mascoteach.API.Controllers`
- [x] AuthService.RegisterAsync dùng `GetByEmailAsync` thay vì `GetAllAsync` để check email trùng
- [x] RegisterRequest.Role có `[Required]`
- [x] UserController: Update/Delete chỉ cho phép owner hoặc admin
- [x] Không trust role/user id từ request body cho protected operations

## 2. Naming Convention (mascoteach-new-entity.md)

- [x] Rename `GetAllIncludingDeletedAsync(int id)` → `GetByIdIncludingDeletedAsync(int id)` (tất cả interfaces + implementations + codex skill)

## 3. Ownership Checks (mascoteach-auth-permission.md + mascoteach-existing-feature.md)

- [x] QuizController/QuizService: ownership check qua Document.TeacherId
- [x] QuestionController/QuestionService: ownership check qua Quiz → Document → TeacherId
- [x] OptionController/OptionService: ownership check qua Question → Quiz → Document → TeacherId
- [x] LiveSessionController: Update/Delete cần ownership check qua TeacherId
- [x] GameHub: dùng `UpdateStatusByPinAsync` (không cần ownership — hub validates qua PIN)

## 4. Missing Features (mascoteach-new-entity.md)

- [x] OptionController: thêm toggle-delete endpoint
- [x] OptionService: thêm ToggleDeleteAsync
- [x] IOptionRepository: thêm GetByIdIncludingDeletedAsync

## 5. DTO Validation (mascoteach-new-entity.md)

- [x] OptionUpdateRequest: thêm `[Required]` cho OptionText
- [x] QuestionUpdateRequest: thêm `[Required]` cho QuestionText
- [x] RegisterRequest.Role: thêm `[Required]`

## 6. Build Verification (mascoteach-debug-build.md)

- [x] Manual code review — tất cả method signatures khớp nhau
- [ ] `dotnet build EXE101-Mascoteach-Backend.sln --no-restore` (cần chạy trên máy local — sandbox không có dotnet SDK)
