# Mascoteach Backend - Progress Report

## Tổng quan
- **Ngày**: 2026-05-30
- **Phạm vi**: Audit & refactor toàn bộ codebase theo codex skills
- **Tổng issues**: 16
- **Đã fix**: 16
- **Còn lại**: 0

---

## Issues tìm được & đã fix

### CRITICAL (Bảo mật)
| # | Issue | File | Status |
|---|-------|------|--------|
| 1 | AuthService dùng GetAllAsync() để check email trùng — O(n) | AuthService.cs | ✅ Fixed |
| 2 | UserController: ai cũng update/delete user khác | UserController.cs | ✅ Fixed |
| 3 | Quiz/Question/Option/LiveSession thiếu ownership check | Multiple | ✅ Fixed |

### HIGH (Convention violations)
| # | Issue | File | Status |
|---|-------|------|--------|
| 4 | AuthController thiếu namespace + [AllowAnonymous] | AuthController.cs | ✅ Fixed |
| 5 | BaseController thiếu namespace | BaseController.cs | ✅ Fixed |
| 6 | GetAllIncludingDeletedAsync tên sai | 21 files | ✅ Renamed → GetByIdIncludingDeletedAsync |

### MEDIUM (Thiếu features/validation)
| # | Issue | File | Status |
|---|-------|------|--------|
| 7 | Option thiếu toggle-delete | OptionController + Service | ✅ Added |
| 8 | OptionUpdateRequest thiếu [Required] | OptionUpdateRequest.cs | ✅ Fixed |
| 9 | QuestionUpdateRequest thiếu [Required] | QuestionUpdateRequest.cs | ✅ Fixed |
| 10 | RegisterRequest.Role thiếu [Required] | RegisterRequest.cs | ✅ Fixed |

---

## Log thay đổi

### AuthController.cs
- Thêm namespace `Mascoteach.API.Controllers`
- Thêm `[AllowAnonymous]` cho Register và Login

### BaseController.cs
- Thêm namespace `Mascoteach.API.Controllers`

### AuthService.cs
- Đổi `GetAllAsync()` → `GetByEmailAsync()` trong RegisterAsync (performance fix)

### 21 files (.cs)
- Rename `GetAllIncludingDeletedAsync` → `GetByIdIncludingDeletedAsync`

### IOptionRepository.cs + OptionRepository.cs
- Thêm `GetByIdIncludingDeletedAsync(int id)`

### IQuizService.cs + QuizService.cs
- Thêm `teacherId` param cho Create/Update/Delete/ToggleDelete
- Thêm ownership check qua Document.TeacherId
- Inject IDocumentRepository

### IQuestionService.cs + QuestionService.cs
- Thêm `teacherId` param cho Create/Update/Delete/ToggleDelete
- Thêm ownership check qua Quiz → Document → TeacherId
- Inject IQuizRepository + IDocumentRepository
- Helper method `IsOwnerAsync()`

### IOptionService.cs + OptionService.cs
- Thêm `teacherId` param cho Create/Update/Delete
- Thêm `ToggleDeleteAsync` (mới)
- Thêm ownership check qua Question → Quiz → Document → TeacherId
- Inject IQuestionRepository + IQuizRepository + IDocumentRepository
- Helper method `IsOwnerAsync()`

### ILiveSessionService.cs + LiveSessionService.cs
- Thêm `teacherId` param cho Update/Delete/ToggleDelete
- Thêm ownership check qua LiveSession.TeacherId
- Thêm `UpdateStatusByPinAsync()` cho GameHub (không cần ownership)

### GameHub.cs
- Đổi sang dùng `UpdateStatusByPinAsync()` thay vì `UpdateAsync()`

### QuizController.cs
- Truyền `CurrentUserId` vào Create/Update/Delete/ToggleDelete
- Đổi NotFound → Forbid cho ownership failures

### QuestionController.cs
- Truyền `CurrentUserId` vào Create/Update/Delete/ToggleDelete
- Đổi NotFound → Forbid cho ownership failures

### OptionController.cs
- Truyền `CurrentUserId` vào Create/Update/Delete
- Thêm endpoint `PATCH toggle-delete`
- Đổi NotFound → Forbid cho ownership failures

### LiveSessionController.cs
- Truyền `CurrentUserId` vào Update/Delete/ToggleDelete
- Đổi NotFound → Forbid cho ownership failures

### UserController.cs
- Update/Delete: chỉ cho phép owner (CurrentUserId == id) hoặc Admin
- ToggleDelete: chỉ Admin

### OptionUpdateRequest.cs
- Thêm `[Required]` cho OptionText

### QuestionUpdateRequest.cs
- Thêm `[Required]` cho QuestionText

### RegisterRequest.cs
- Thêm `[Required]` cho Role

### mascoteach-new-entity.md (codex skill)
- Rename GetAllIncludingDeletedAsync → GetByIdIncludingDeletedAsync
