---
description: |
  Use this skill when working on Mascoteach document upload/download, AWS S3, presigned URLs, S3 keys,
  DocumentController, DocumentService, S3Service, document ownership, or AI-service document access. Triggers:
  "S3", "upload document", "presigned URL", "download URL", "403 Forbidden", "document file", "s3Key",
  "DocumentService", "GeneratePresigned".
---

# Mascoteach - S3 Document Flow Skill

## Current rule

The database stores the permanent S3 key in `Document.FileUrl`.

Do not store presigned URLs in the database. Presigned URLs expire and are generated on demand.

Mapping:

- DB column: `file_url`
- Entity property: `Document.FileUrl`
- API request/response property: `S3Key`
- Runtime download property: `PresignedUrl`

`DocumentProfile` maps `Document.FileUrl -> DocumentResponse.S3Key` and ignores `PresignedUrl`.

## Upload flow

1. Frontend calls `POST /api/Document/generate-upload-url` with `fileName` and `contentType`.
2. `S3Service.GeneratePresignedUploadUrlAsync` returns:
   - `UploadUrl`
   - `S3Key`
   - `FileUrl`
   - `ExpiresAt`
3. Frontend uploads directly to S3 using `UploadUrl`.
4. Frontend calls `POST /api/Document` with `{ "s3Key": "..." }`.
5. `DocumentService.UploadDocumentAsync` stores `S3Key` in `Document.FileUrl`.

## Download flow

When returning documents:

1. Map entity to `DocumentResponse`.
2. Generate a fresh download URL with `IS3Service.GeneratePresignedDownloadUrlAsync(response.S3Key)`.
3. Set `response.PresignedUrl`.

Apply this to all document-returning methods:

- `GetAllDocumentsAsync`
- `GetMyDocumentsAsync`
- `GetDocumentByIdAsync`
- `UploadDocumentAsync`
- `ToggleDeleteAsync`

## Ownership and quota

- Document create/update/delete uses `CurrentUserId` from controller.
- Only the owner teacher can update/delete/toggle a document.
- Freemium users currently have a configurable 5 active document limit.
- Active document quota counts rows in `Documents` where `owner_id == CurrentUserId` and `is_deleted == false`.
- Read the quota from `Plans:FreemiumActiveDocumentLimit`; default/fallback is 5.
- Deploy override uses `Plans__FreemiumActiveDocumentLimit`.
- Premium users are unlimited for document uploads; do not add a Premium limit config unless the product plan changes.
- `User.DocumentsProcessed` is a lifetime upload counter/analytics field, not the Freemium quota source.
- `UploadDocumentAsync` still increments `DocumentsProcessed` in the same transaction as document creation.
- `DELETE /api/Document/{id}` soft-deletes documents through `GenericRepository.Delete` because `Document` has `IsDeleted`.
- `PATCH /api/Document/{id}/toggle-delete` toggles soft-delete state. Restoring a deleted document must also check active document quota.
- Account deletion through `DELETE /api/User/{id}` is different: it hard-deletes the account graph and permanently removes the user's document rows along with dependent quiz data.
- After the account hard-delete transaction commits, backend best-effort deletes the related S3 objects for document keys and avatar key. If S3 cleanup fails, the database account deletion still stands.

## S3 service conventions

- S3 keys are generated under `documents/{yyyy}/{MM}/{dd}/{guid}{extension}`.
- Upload URLs use HTTP PUT.
- Download URLs use HTTP GET.
- Expiration comes from `AWS:PresignedUrlExpirationMinutes`, defaulting to 60.
- Bucket name comes from `AWS:BucketName`.

## Common fixes

### 403 from old document link

Likely cause: a presigned URL was stored or reused after expiration.

Fix pattern:

- Store only `S3Key`.
- Call the Mascoteach API to get a fresh `PresignedUrl`.
- Use that fresh URL to download.

### Added a new document response method

Remember to generate `PresignedUrl`; AutoMapper will not fill it.

## Validation checklist

- No code saves `UploadUrl` or `PresignedUrl` to `Document.FileUrl`.
- Response DTO exposes both `S3Key` and fresh `PresignedUrl`.
- Owner checks use current teacher id.
- Freemium quota checks count active documents, not `DocumentsProcessed`.
- Transactions commit document create and processed-count update together.
- Restore/toggle from deleted to active cannot bypass Freemium active document quota.
- `dotnet build EXE101-Mascoteach-Backend.sln --no-restore` succeeds.
