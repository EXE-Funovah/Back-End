using System;
using System.Collections.Generic;

namespace Mascoteach.Data.Models;

public partial class Class
{
    public int Id { get; set; }

    public int TeacherId { get; set; }

    public string Name { get; set; } = null!;

    public string ClassCode { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public string? JoinPasswordHash { get; set; }

    public virtual ICollection<ClassMember> ClassMembers { get; set; } = new List<ClassMember>();

    public virtual ICollection<ClassTeacher> ClassTeachers { get; set; } = new List<ClassTeacher>();

    public virtual ICollection<FlashcardAssignment> FlashcardAssignments { get; set; } = new List<FlashcardAssignment>();

    public virtual User Teacher { get; set; } = null!;
}
