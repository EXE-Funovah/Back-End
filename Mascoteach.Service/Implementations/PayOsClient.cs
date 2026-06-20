using Mascoteach.Service.DTOs;
using Mascoteach.Service.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mascoteach.Service.Implementations;

public class PayOsClient : IPayOsClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public PayOsClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<PayOsCreatePaymentLinkResult> CreatePaymentLinkAsync(PayOsCreatePaymentLinkRequest request)
    {
        var clientId = _configuration["PayOS:ClientId"];
        var apiKey = _configuration["PayOS:ApiKey"];
        var baseUrl = _configuration["PayOS:BaseUrl"] ?? "https://api-merchant.payos.vn";

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("PayOS client id or api key is not configured.");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/v2/payment-requests");
        httpRequest.Headers.Add("x-client-id", clientId);
        httpRequest.Headers.Add("x-api-key", apiKey);
        httpRequest.Content = JsonContent.Create(request, options: JsonOptions);

        using var response = await _httpClient.SendAsync(httpRequest);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"PayOS create payment link failed: {(int)response.StatusCode} {body}");

        var payOsResponse = JsonSerializer.Deserialize<PayOsApiResponse<PayOsCreatePaymentLinkData>>(body, JsonOptions)
            ?? throw new InvalidOperationException("PayOS returned an empty response.");

        if (payOsResponse.Code != "00" || payOsResponse.Data == null)
            throw new InvalidOperationException($"PayOS create payment link failed: {payOsResponse.Desc}");

        return new PayOsCreatePaymentLinkResult
        {
            PaymentLinkId = payOsResponse.Data.PaymentLinkId,
            CheckoutUrl = payOsResponse.Data.CheckoutUrl,
            QrCode = payOsResponse.Data.QrCode,
            Status = payOsResponse.Data.Status
        };
    }

    public async Task<PayOsCancelPaymentLinkResult> CancelPaymentLinkAsync(
        long orderCode,
        string cancellationReason)
    {
        var clientId = _configuration["PayOS:ClientId"];
        var apiKey = _configuration["PayOS:ApiKey"];
        var baseUrl = _configuration["PayOS:BaseUrl"] ?? "https://api-merchant.payos.vn";

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("PayOS client id or api key is not configured.");

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"{baseUrl.TrimEnd('/')}/v2/payment-requests/{orderCode}/cancel");
        httpRequest.Headers.Add("x-client-id", clientId);
        httpRequest.Headers.Add("x-api-key", apiKey);
        httpRequest.Content = JsonContent.Create(
            new { cancellationReason },
            options: JsonOptions);

        using var response = await _httpClient.SendAsync(httpRequest);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"PayOS cancel payment link failed: {(int)response.StatusCode} {body}");

        var payOsResponse = JsonSerializer.Deserialize<PayOsApiResponse<PayOsCancelPaymentLinkData>>(
                body,
                JsonOptions)
            ?? throw new InvalidOperationException("PayOS returned an empty cancel response.");

        if (payOsResponse.Code != "00" || payOsResponse.Data == null)
            throw new InvalidOperationException(
                $"PayOS cancel payment link failed: {payOsResponse.Desc}");

        if (!string.Equals(
                payOsResponse.Data.Status,
                "CANCELLED",
                StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"PayOS did not cancel payment link {orderCode}. Current status: {payOsResponse.Data.Status}.");

        return new PayOsCancelPaymentLinkResult
        {
            Status = payOsResponse.Data.Status
        };
    }

    private class PayOsApiResponse<T>
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = null!;

        [JsonPropertyName("desc")]
        public string Desc { get; set; } = null!;

        [JsonPropertyName("data")]
        public T? Data { get; set; }
    }

    private class PayOsCreatePaymentLinkData
    {
        [JsonPropertyName("paymentLinkId")]
        public string PaymentLinkId { get; set; } = null!;

        [JsonPropertyName("checkoutUrl")]
        public string CheckoutUrl { get; set; } = null!;

        [JsonPropertyName("qrCode")]
        public string? QrCode { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = null!;
    }

    private class PayOsCancelPaymentLinkData
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = null!;
    }
}
