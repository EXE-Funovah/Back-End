using Amazon.S3;
using Amazon.S3.Model;
using Mascoteach.Service.DTOs;
using Mascoteach.Service.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Mascoteach.Service.Implementations;

public class S3Service : IS3Service
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly int _urlExpirationMinutes;

    public S3Service(IAmazonS3 s3Client, IConfiguration configuration)
    {
        _s3Client = s3Client;
        _bucketName = configuration["AWS:BucketName"] ?? throw new ArgumentNullException("AWS:BucketName configuration is missing");
        _urlExpirationMinutes = int.Parse(configuration["AWS:PresignedUrlExpirationMinutes"] ?? "60");
    }

    public async Task<PresignedUrlResponse> GeneratePresignedUploadUrlAsync(string fileName, string contentType)
    {
        var fileExtension = Path.GetExtension(fileName);
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
        
        var s3Key = $"documents/{DateTime.UtcNow:yyyy/MM/dd}/{uniqueFileName}";

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = s3Key,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddMinutes(_urlExpirationMinutes),
            ContentType = contentType
        };

        var uploadUrl = await Task.Run(() => _s3Client.GetPreSignedURL(request));
        
        var fileUrl = $"https://{_bucketName}.s3.amazonaws.com/{s3Key}";

        return new PresignedUrlResponse
        {
            UploadUrl = uploadUrl,
            S3Key = s3Key,
            FileUrl = fileUrl,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_urlExpirationMinutes)
        };
    }

    public async Task<string> GeneratePresignedDownloadUrlAsync(string s3Key)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = s3Key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddMinutes(_urlExpirationMinutes)
        };

        return await Task.Run(() => _s3Client.GetPreSignedURL(request));
    }
}
