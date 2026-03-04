using System;
using System.Collections.Generic;

namespace Mascoteach.Data.Models;

public partial class Quiz
{
    public int Id { get; set; }

    public int DocumentId { get; set; }

    public string Title { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public virtual Document Document { get; set; } = null!;

    public virtual ICollection<LiveSession> LiveSessions { get; set; } = new List<LiveSession>();

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
}
