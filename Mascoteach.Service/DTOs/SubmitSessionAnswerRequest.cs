using System.ComponentModel.DataAnnotations;

namespace Mascoteach.Service.DTOs;

public sealed class SubmitSessionAnswerRequest
{
    [Required]
    public string GamePin { get; set; } = null!;

    [Range(1, int.MaxValue)]
    public int ParticipantId { get; set; }

    [Range(1, int.MaxValue)]
    public int QuestionId { get; set; }

    [Range(1, int.MaxValue)]
    public int SelectedOptionId { get; set; }
}
