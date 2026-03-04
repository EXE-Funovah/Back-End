using System;
using System.Collections.Generic;

namespace Mascoteach.Data.Models;

public partial class Question
{
    public int Id { get; set; }

    public int QuizId { get; set; }

    public string QuestionText { get; set; } = null!;

    public string Options { get; set; } = null!;

    public string CorrectAnswer { get; set; } = null!;

    public virtual Quiz Quiz { get; set; } = null!;
}
