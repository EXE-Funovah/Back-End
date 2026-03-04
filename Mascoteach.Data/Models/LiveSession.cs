using System;
using System.Collections.Generic;

namespace Mascoteach.Data.Models;

public partial class LiveSession
{
    public int Id { get; set; }

    public int TeacherId { get; set; }

    public int QuizId { get; set; }

    public int TemplateId { get; set; }

    public string GamePin { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual Quiz Quiz { get; set; } = null!;

    public virtual ICollection<SessionParticipant> SessionParticipants { get; set; } = new List<SessionParticipant>();

    public virtual User Teacher { get; set; } = null!;

    public virtual GameTemplate Template { get; set; } = null!;
}
