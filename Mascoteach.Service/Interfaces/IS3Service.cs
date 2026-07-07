using Mascoteach.Service.DTOs;

namespace Mascoteach.Service.Interfaces;

public interface IS3Service
{
    Task<PresignedUrlResponse> GeneratePresignedUploadUrlAsync(string fileName, string contentType);

    /// <summary>
    /// Presign upload cho ảnh đại diện — cho phép contentType ảnh (image/jpeg,
    /// image/png) và key prefix "avatars/", khác document (ép .zip).
    /// </summary>
    Task<PresignedUrlResponse> GeneratePresignedAvatarUploadUrlAsync(string fileName, string contentType);

    Task<string> GeneratePresignedDownloadUrlAsync(string s3Key);

    /// <summary>
    /// Lấy kích thước (bytes) của object đã upload lên S3, hoặc null nếu không
    /// tồn tại. Dùng để enforce giới hạn dung lượng phía server (client upload
    /// thẳng lên S3 qua presigned URL nên backend không thấy bytes lúc PUT).
    /// </summary>
    Task<long?> GetObjectSizeAsync(string s3Key);

    Task DeleteObjectAsync(string s3Key);
    Task DeleteObjectsAsync(IEnumerable<string> s3Keys);
}
