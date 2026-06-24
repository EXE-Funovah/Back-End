using Amazon.S3;
using Amazon.S3.Model;
using Mascoteach.Service.Implementations;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Mascoteach.Tests;

public class S3ServiceTests
{
    [Fact]
    public async Task GeneratePresignedUploadUrlAsync_UsesZipExtensionAndContentType()
    {
        var s3Client = new Mock<IAmazonS3>();
        GetPreSignedUrlRequest? capturedRequest = null;

        s3Client.Setup(x => x.GetPreSignedURL(It.IsAny<GetPreSignedUrlRequest>()))
            .Callback<GetPreSignedUrlRequest>(request => capturedRequest = request)
            .Returns("https://signed-upload-url");

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AWS:BucketName"] = "test-bucket",
                ["AWS:PresignedUrlExpirationMinutes"] = "60"
            })
            .Build();

        var sut = new S3Service(s3Client.Object, configuration);

        var result = await sut.GeneratePresignedUploadUrlAsync("biology.pdf", "application/zip");

        Assert.NotNull(capturedRequest);
        Assert.Equal("application/zip", capturedRequest!.ContentType);
        Assert.EndsWith(".zip", capturedRequest.Key, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(capturedRequest.Key, result.S3Key);
    }

    [Fact]
    public async Task GeneratePresignedUploadUrlAsync_ZipRequest_IsAccepted()
    {
        var s3Client = new Mock<IAmazonS3>();
        GetPreSignedUrlRequest? capturedRequest = null;
        s3Client.Setup(x => x.GetPreSignedURL(It.IsAny<GetPreSignedUrlRequest>()))
            .Callback<GetPreSignedUrlRequest>(request => capturedRequest = request)
            .Returns("https://signed-upload-url");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AWS:BucketName"] = "test-bucket",
                ["AWS:PresignedUrlExpirationMinutes"] = "60"
            })
            .Build();

        var sut = new S3Service(s3Client.Object, configuration);

        var result = await sut.GeneratePresignedUploadUrlAsync("archive.zip", "application/zip");

        Assert.NotNull(capturedRequest);
        Assert.Equal("application/zip", capturedRequest!.ContentType);
        Assert.EndsWith(".zip", result.S3Key, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteObjectAsync_UsesBucketAndKey()
    {
        var s3Client = new Mock<IAmazonS3>();
        DeleteObjectRequest? capturedRequest = null;

        s3Client.Setup(x => x.DeleteObjectAsync(It.IsAny<DeleteObjectRequest>(), It.IsAny<CancellationToken>()))
            .Callback<DeleteObjectRequest, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(new DeleteObjectResponse());

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AWS:BucketName"] = "test-bucket",
                ["AWS:PresignedUrlExpirationMinutes"] = "60"
            })
            .Build();

        var sut = new S3Service(s3Client.Object, configuration);

        await sut.DeleteObjectAsync("documents/file.zip");

        Assert.NotNull(capturedRequest);
        Assert.Equal("test-bucket", capturedRequest!.BucketName);
        Assert.Equal("documents/file.zip", capturedRequest.Key);
    }

    [Fact]
    public async Task DeleteObjectsAsync_DeduplicatesAndBatchesKeys()
    {
        var s3Client = new Mock<IAmazonS3>();
        DeleteObjectsRequest? capturedRequest = null;

        s3Client.Setup(x => x.DeleteObjectsAsync(It.IsAny<DeleteObjectsRequest>(), It.IsAny<CancellationToken>()))
            .Callback<DeleteObjectsRequest, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(new DeleteObjectsResponse());

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AWS:BucketName"] = "test-bucket",
                ["AWS:PresignedUrlExpirationMinutes"] = "60"
            })
            .Build();

        var sut = new S3Service(s3Client.Object, configuration);

        await sut.DeleteObjectsAsync(["documents/file-1.zip", "documents/file-1.zip", "avatars/user.png", "", "   "]);

        Assert.NotNull(capturedRequest);
        Assert.Equal("test-bucket", capturedRequest!.BucketName);
        Assert.Equal(2, capturedRequest.Objects.Count);
        Assert.Equal("documents/file-1.zip", capturedRequest.Objects[0].Key);
        Assert.Equal("avatars/user.png", capturedRequest.Objects[1].Key);
    }
}
