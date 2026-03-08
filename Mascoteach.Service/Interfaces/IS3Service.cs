using Mascoteach.Service.DTOs;

namespace Mascoteach.Service.Interfaces;

public interface IS3Service
{
    Task<PresignedUrlResponse> GeneratePresignedUploadUrlAsync(string fileName, string contentType);
    Task<string> GeneratePresignedDownloadUrlAsync(string s3Key);
}
