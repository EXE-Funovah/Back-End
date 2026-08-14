namespace Mascoteach.Data.Models;

public partial class ClassTeacher
{
    public int Id { get; set; }

    public int ClassId { get; set; }

    public int TeacherId { get; set; }

    public string Role { get; set; } = "Teacher";

    public DateTime JoinedAt { get; set; }

    public bool IsDeleted { get; set; }

    public virtual Class Class { get; set; } = null!;

    public virtual User Teacher { get; set; } = null!;
}
