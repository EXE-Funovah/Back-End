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

public class AdminUserSubscriptionUpdateRequest
{
    [Required]
    public string SubscriptionTier { get; set; } = null!;

    public DateTimeOffset? PremiumExpiresAt { get; set; }

    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = null!;
}

public class AdminUserStatusUpdateRequest
{
    [Required]
    public string Status { get; set; } = null!;

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

public class AdminUserSubscriptionUpdateResponse
{
    public int UserId { get; set; }
    public string PreviousSubscriptionTier { get; set; } = null!;
    public DateTimeOffset? PreviousPremiumExpiresAt { get; set; }
    public string SubscriptionTier { get; set; } = null!;
    public DateTimeOffset? PremiumExpiresAt { get; set; }
    public bool Changed { get; set; }
}

public class AdminUserStatusUpdateResponse
{
    public int UserId { get; set; }
    public string PreviousStatus { get; set; } = null!;
    public string Status { get; set; } = null!;
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

public enum AdminUserSubscriptionChangeStatus
{
    Updated,
    NoChange,
    UserNotFound
}

public class AdminUserSubscriptionChangeResult
{
    public AdminUserSubscriptionChangeStatus Status { get; set; }
    public AdminUserSubscriptionUpdateResponse? Response { get; set; }
}

public enum AdminUserStatusChangeStatus
{
    Updated,
    NoChange,
    UserNotFound,
    SelfLockForbidden,
    LastAdminForbidden
}

public class AdminUserStatusChangeResult
{
    public AdminUserStatusChangeStatus Status { get; set; }
    public AdminUserStatusUpdateResponse? Response { get; set; }
}
