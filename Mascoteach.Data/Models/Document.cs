using System;
using System.Collections.Generic;

namespace Mascoteach.Data.Models;

public partial class Document
{
    public int Id { get; set; }

    public int TeacherId { get; set; }

    public string FileUrl { get; set; } = null!;

    public DateTime? UploadedAt { get; set; }

    public virtual ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();

    public virtual User Teacher { get; set; } = null!;
}
