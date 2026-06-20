using System.ComponentModel.DataAnnotations;

namespace Mascoteach.Service.DTOs;

public class CreatePaymentLinkRequest
{
    [Required]
    public string PlanCode { get; set; } = null!;
}
