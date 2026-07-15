using System.ComponentModel.DataAnnotations;

namespace Mascoteach.Service.DTOs.Admin;

public class AdminUserRoleUpdateRequest
{
    [Required]
    public string Role { get; set; } = null!;

    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = null!;
}

public class AdminActorContext
{
    public int UserId { get; set; }
    public string Email { get; set; } = null!;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

public class AdminUserRoleUpdateResponse
{
    public int UserId { get; set; }
    public string PreviousRole { get; set; } = null!;
    public string Role { get; set; } = null!;
    public bool Changed { get; set; }
}

public enum AdminUserRoleChangeStatus
{
    Updated,
    NoChange,
    UserNotFound,
    SelfChangeForbidden,
    LastAdminForbidden
}

public class AdminUserRoleChangeResult
{
    public AdminUserRoleChangeStatus Status { get; set; }
    public AdminUserRoleUpdateResponse? Response { get; set; }
}

