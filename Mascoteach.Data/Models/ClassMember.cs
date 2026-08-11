using System;
using System.Collections.Generic;

namespace Mascoteach.Data.Models;

public partial class ClassMember
{
    public int Id { get; set; }

    public int ClassId { get; set; }

    public int StudentId { get; set; }

    public DateTime JoinedAt { get; set; }

    public bool IsDeleted { get; set; }

    public virtual Class Class { get; set; } = null!;

    public virtual User Student { get; set; } = null!;
}
