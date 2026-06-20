-- ============================================================
-- Migration: thêm cột avatar_url vào bảng Users (ảnh đại diện)
-- Chạy trên DB dev TRƯỚC khi deploy backend; chạy trên prod khi lên prod.
-- avatar_url lưu S3 key (vd. "avatars/{guid}.jpg"), KHÔNG lưu presigned URL.
-- ============================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE [Name] = N'avatar_url'
      AND [Object_ID] = Object_ID(N'dbo.Users')
)
BEGIN
    ALTER TABLE dbo.Users ADD avatar_url VARCHAR(500) NULL;
END
GO
