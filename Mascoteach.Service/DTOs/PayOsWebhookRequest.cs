using System.Text.Json;

namespace Mascoteach.Service.DTOs;

public class PayOsWebhookRequest
{
    public string Code { get; set; } = null!;
    public string Desc { get; set; } = null!;
    public bool Success { get; set; }
    public JsonElement Data { get; set; }
    public string Signature { get; set; } = null!;
}
