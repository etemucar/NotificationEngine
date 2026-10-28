using System.ComponentModel.DataAnnotations;

namespace NotificationEngine.Api.Models;

public class ChangePasswordModel
{
    [Required]
    public string CurrentPassword { get; set; } = null!;

    [Required]
    public string NewPassword { get; set; } = null!;
}
