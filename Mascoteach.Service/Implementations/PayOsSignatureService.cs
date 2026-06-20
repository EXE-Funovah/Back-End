using Mascoteach.Service.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Mascoteach.Service.Implementations;

public class PayOsSignatureService : IPayOsSignatureService
{
    private readonly string _checksumKey;

    public PayOsSignatureService(IConfiguration configuration)
        : this(configuration["PayOS:ChecksumKey"] ?? string.Empty)
    {
    }

    public PayOsSignatureService(string checksumKey)
    {
        _checksumKey = checksumKey;
    }

    public string CreatePaymentRequestSignature(
        int amount,
        string cancelUrl,
        string description,
        long orderCode,
        string returnUrl)
    {
        var data = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["amount"] = amount.ToString(),
            ["cancelUrl"] = cancelUrl,
            ["description"] = description,
            ["orderCode"] = orderCode.ToString(),
            ["returnUrl"] = returnUrl
        };

        return ComputeHmac(BuildQueryString(data));
    }

    public string CreateSignature(JsonElement data)
    {
        var values = new SortedDictionary<string, string>(StringComparer.Ordinal);

        foreach (var property in data.EnumerateObject())
        {
            values[property.Name] = ConvertJsonValue(property.Value);
        }

        return ComputeHmac(BuildQueryString(values));
    }

    public bool IsValidWebhookData(JsonElement data, string signature)
    {
        if (string.IsNullOrWhiteSpace(signature)) return false;

        var expected = CreateSignature(data);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected.ToLowerInvariant()),
            Encoding.UTF8.GetBytes(signature.ToLowerInvariant()));
    }

    private string ComputeHmac(string data)
    {
        if (string.IsNullOrWhiteSpace(_checksumKey))
            throw new InvalidOperationException("PayOS checksum key is not configured.");

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_checksumKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string BuildQueryString(IEnumerable<KeyValuePair<string, string>> values)
    {
        return string.Join("&", values.Select(v => $"{v.Key}={v.Value}"));
    }

    private static string ConvertJsonValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => string.Empty,
            JsonValueKind.String => NormalizeNullish(value.GetString()),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Array or JsonValueKind.Object => value.GetRawText(),
            _ => value.GetRawText()
        };
    }

    private static string NormalizeNullish(string? value)
    {
        return value is null or "null" or "undefined" ? string.Empty : value;
    }
}
