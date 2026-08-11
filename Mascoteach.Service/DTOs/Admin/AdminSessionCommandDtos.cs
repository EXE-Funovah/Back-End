namespace Mascoteach.Service.DTOs.Admin;

public sealed class AdminSessionEndRequest
{
    public string Reason { get; set; } = string.Empty;
}

public sealed class AdminSessionEndResponse
{
    public int SessionId { get; set; }
    public string GamePin { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool Changed { get; set; }
}

public enum AdminSessionEndStatus
{
    Updated,
    NoChange,
    SessionNotFound,
    InvalidState
}

public sealed class AdminSessionEndResult
{
    public AdminSessionEndStatus Status { get; set; }
    public AdminSessionEndResponse? Response { get; set; }
}
