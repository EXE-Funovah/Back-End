using System;
using System.Collections.Generic;

namespace Mascoteach.Data.Models;

public partial class Question
{
    public int Id { get; set; }

    public int QuizId { get; set; }

    public string QuestionText { get; set; } = null!;

    public string QuestionType { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public int Position { get; set; }

    public virtual ICollection<FlashcardStudyProgress> FlashcardStudyProgresses { get; set; } = new List<FlashcardStudyProgress>();

    public virtual ICollection<Option> Options { get; set; } = new List<Option>();

    public virtual Quiz Quiz { get; set; } = null!;

    public virtual ICollection<SessionAnswer> SessionAnswers { get; set; } = new List<SessionAnswer>();
}
