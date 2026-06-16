using System;
using System.Collections.Generic;

namespace Mascoteach.Data.Models;

public partial class Document
{
    public int Id { get; set; }

    public int OwnerId { get; set; }

    public string FileUrl { get; set; } = null!;

    public DateTime? UploadedAt { get; set; }

    public bool IsDeleted { get; set; }

    public string? FileName { get; set; }

    public virtual User Owner { get; set; } = null!;

    public virtual ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
}
