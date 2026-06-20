using System.Text.Json;

namespace Mascoteach.Service.Interfaces;

public interface IPayOsSignatureService
{
    string CreatePaymentRequestSignature(int amount, string cancelUrl, string description, long orderCode, string returnUrl);
    string CreateSignature(JsonElement data);
    bool IsValidWebhookData(JsonElement data, string signature);
}
