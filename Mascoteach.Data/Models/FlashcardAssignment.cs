using System;
using System.Collections.Generic;

namespace Mascoteach.Data.Models;

public partial class FlashcardAssignment
{
    public int Id { get; set; }

    public int ClassId { get; set; }

    public int QuizId { get; set; }

    public int AssignedBy { get; set; }

    public string? Instructions { get; set; }

    public DateTime? DueAt { get; set; }

    public DateTime AssignedAt { get; set; }

    public bool IsDeleted { get; set; }

    public virtual User AssignedByNavigation { get; set; } = null!;

    public virtual Class Class { get; set; } = null!;

    public virtual ICollection<FlashcardStudyProgress> FlashcardStudyProgresses { get; set; } = new List<FlashcardStudyProgress>();

    public virtual Quiz Quiz { get; set; } = null!;
}
