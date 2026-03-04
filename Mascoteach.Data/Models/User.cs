using System;
using System.Collections.Generic;

namespace Mascoteach.Data.Models;

public partial class User
{
    public int Id { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Role { get; set; } = null!;

    public string SubscriptionTier { get; set; } = null!;

    public int? DocumentsProcessed { get; set; }

    public DateTime? CreatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();

    public virtual ICollection<LiveSession> LiveSessions { get; set; } = new List<LiveSession>();
}
