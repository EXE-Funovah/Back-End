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
}
