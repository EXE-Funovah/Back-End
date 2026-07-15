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
        var uniqueFileName = $"{Guid.NewGuid()}.zip";
        
        var s3Key = $"documents/{DateTime.UtcNow:yyyy/MM/dd}/{uniqueFileName}";

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = s3Key,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddMinutes(_urlExpirationMinutes),
            ContentType = "application/zip"
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

    public async Task<PresignedUrlResponse> GeneratePresignedAvatarUploadUrlAsync(string fileName, string contentType)
    {
        var ext = System.IO.Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";

        var s3Key = $"avatars/{Guid.NewGuid()}{ext}";

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = s3Key,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddMinutes(_urlExpirationMinutes),
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "image/jpeg" : contentType
        };

        var uploadUrl = await Task.Run(() => _s3Client.GetPreSignedURL(request));

        return new PresignedUrlResponse
        {
            UploadUrl = uploadUrl,
            S3Key = s3Key,
            FileUrl = $"https://{_bucketName}.s3.amazonaws.com/{s3Key}",
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

    public async Task<long?> GetObjectSizeAsync(string s3Key)
    {
        if (string.IsNullOrWhiteSpace(s3Key)) return null;

        try
        {
            var metadata = await _s3Client.GetObjectMetadataAsync(new GetObjectMetadataRequest
            {
                BucketName = _bucketName,
                Key = s3Key.Trim(),
            });
            return metadata.ContentLength;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task DeleteObjectAsync(string s3Key)
    {
        if (string.IsNullOrWhiteSpace(s3Key)) return;

        await _s3Client.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = _bucketName,
            Key = s3Key.Trim(),
        });
    }

    public async Task DeleteObjectsAsync(IEnumerable<string> s3Keys)
    {
        var keys = s3Keys
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Select(key => key.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (keys.Count == 0) return;

        if (keys.Count == 1)
        {
            await DeleteObjectAsync(keys[0]);
            return;
        }

        await _s3Client.DeleteObjectsAsync(new DeleteObjectsRequest
        {
            BucketName = _bucketName,
            Objects = keys.Select(key => new KeyVersion { Key = key }).ToList(),
        });
    }
}
