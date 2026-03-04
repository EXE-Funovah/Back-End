using System;
using System.Collections.Generic;

namespace Mascoteach.Data.Models;

public partial class SessionParticipant
{
    public int Id { get; set; }

    public int SessionId { get; set; }

    public string StudentName { get; set; } = null!;

    public int? TotalScore { get; set; }

    public bool IsDeleted { get; set; }

    public virtual LiveSession Session { get; set; } = null!;
}
