using Mascoteach.Service.Implementations;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Mascoteach.Tests;

public class PayOsClientTests
{
    [Fact]
    public async Task CancelPaymentLinkAsync_SendsAuthenticatedCancelRequest()
    {
        var handler = new RecordingHandler
        {
            Response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "code": "00",
                      "desc": "success",
                      "data": {
                        "status": "CANCELLED"
                      }
                    }
                    """,
                    Encoding.UTF8,
                    "application/json")
            }
        };
        var sut = new PayOsClient(new HttpClient(handler), MakeConfiguration());

        var result = await sut.CancelPaymentLinkAsync(123456, "Cancelled by user");

        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal(
            "https://api-merchant.payos.vn/v2/payment-requests/123456/cancel",
            handler.RequestUri?.ToString());
        Assert.Equal("client-id", handler.ClientId);
        Assert.Equal("api-key", handler.ApiKey);
        using var body = JsonDocument.Parse(handler.RequestBody!);
        Assert.Equal(
            "Cancelled by user",
            body.RootElement.GetProperty("cancellationReason").GetString());
        Assert.Equal("CANCELLED", result.Status);
    }

    [Fact]
    public async Task CancelPaymentLinkAsync_NonSuccessPayOsResponse_Throws()
    {
        var handler = new RecordingHandler
        {
            Response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "code": "01",
                      "desc": "Payment request cannot be cancelled",
                      "data": null
                    }
                    """,
                    Encoding.UTF8,
                    "application/json")
            }
        };
        var sut = new PayOsClient(new HttpClient(handler), MakeConfiguration());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.CancelPaymentLinkAsync(123456, "Cancelled by user"));

        Assert.Contains("Payment request cannot be cancelled", exception.Message);
    }

    private static IConfiguration MakeConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PayOS:ClientId"] = "client-id",
                ["PayOS:ApiKey"] = "api-key",
                ["PayOS:BaseUrl"] = "https://api-merchant.payos.vn"
            })
            .Build();
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public HttpResponseMessage Response { get; init; } = null!;
        public HttpMethod? Method { get; private set; }
        public Uri? RequestUri { get; private set; }
        public string? ClientId { get; private set; }
        public string? ApiKey { get; private set; }
        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Method = request.Method;
            RequestUri = request.RequestUri;
            ClientId = request.Headers.GetValues("x-client-id").Single();
            ApiKey = request.Headers.GetValues("x-api-key").Single();
            RequestBody = request.Content == null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return Response;
        }
    }
}
