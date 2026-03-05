namespace Mascoteach.Service.DTOs;

public class QuizResponse
{
    public int Id { get; set; }
    public int DocumentId { get; set; }
    public string Title { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime? CreatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
