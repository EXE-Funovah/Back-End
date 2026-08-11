using System;
using System.Collections.Generic;

namespace Mascoteach.Data.Models;

public partial class SessionAnswer
{
    public int Id { get; set; }

    public int SessionId { get; set; }

    public int ParticipantId { get; set; }

    public int QuestionId { get; set; }

    public int SelectedOptionId { get; set; }

    public bool IsCorrect { get; set; }

    public int ScoreAwarded { get; set; }

    public DateTime AnsweredAt { get; set; }

    public virtual SessionParticipant Participant { get; set; } = null!;

    public virtual Question Question { get; set; } = null!;

    public virtual Option SelectedOption { get; set; } = null!;

    public virtual LiveSession Session { get; set; } = null!;
}
