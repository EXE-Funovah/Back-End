using Mascoteach.Service.Implementations;
using System.Text.Json;
using Xunit;

namespace Mascoteach.Tests;

public class PayOsSignatureServiceTests
{
    [Fact]
    public void CreatePaymentRequestSignature_UsesPayOsAlphabeticalFields()
    {
        var sut = new PayOsSignatureService("checksum-key");

        var signature = sut.CreatePaymentRequestSignature(
            amount: 119000,
            cancelUrl: "https://dev.mascoteach.com/checkout/cancel",
            description: "MT123456",
            orderCode: 123456,
            returnUrl: "https://dev.mascoteach.com/checkout");

        Assert.Equal("c1056968d1c65b5f1cc2b3699b8e3428684ed79929fc5d0730710ce6e8f0159f", signature);
    }

    [Fact]
    public void IsValidWebhookData_ReturnsTrueForMatchingSignature()
    {
        var sut = new PayOsSignatureService("checksum-key");
        using var document = JsonDocument.Parse("""
        {
          "orderCode": 123456,
          "amount": 119000,
          "description": "MT123456",
          "accountNumber": "12345678",
          "reference": "TF230204212323",
          "transactionDateTime": "2026-06-18 10:00:00",
          "currency": "VND",
          "paymentLinkId": "link_123",
          "code": "00",
          "desc": "Thanh cong"
        }
        """);
        var signature = sut.CreateSignature(document.RootElement);

        Assert.True(sut.IsValidWebhookData(document.RootElement, signature));
    }

    [Fact]
    public void IsValidWebhookData_ReturnsFalseForTamperedAmount()
    {
        var sut = new PayOsSignatureService("checksum-key");
        using var original = JsonDocument.Parse("""
        {
          "orderCode": 123456,
          "amount": 119000,
          "description": "MT123456",
          "reference": "TF230204212323",
          "paymentLinkId": "link_123",
          "code": "00",
          "desc": "Thanh cong"
        }
        """);
        using var tampered = JsonDocument.Parse("""
        {
          "orderCode": 123456,
          "amount": 5000,
          "description": "MT123456",
          "reference": "TF230204212323",
          "paymentLinkId": "link_123",
          "code": "00",
          "desc": "Thanh cong"
        }
        """);
        var signature = sut.CreateSignature(original.RootElement);

        Assert.False(sut.IsValidWebhookData(tampered.RootElement, signature));
    }
}
