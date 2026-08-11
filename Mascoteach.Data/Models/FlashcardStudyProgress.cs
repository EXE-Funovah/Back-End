using System;
using System.Collections.Generic;

namespace Mascoteach.Data.Models;

public partial class FlashcardStudyProgress
{
    public int Id { get; set; }

    public int AssignmentId { get; set; }

    public int StudentId { get; set; }

    public int QuestionId { get; set; }

    public string Status { get; set; } = null!;

    public int ReviewCount { get; set; }

    public int KnownCount { get; set; }

    public DateTime? LastReviewedAt { get; set; }

    public DateTime? MasteredAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual FlashcardAssignment Assignment { get; set; } = null!;

    public virtual Question Question { get; set; } = null!;

    public virtual User Student { get; set; } = null!;
}
