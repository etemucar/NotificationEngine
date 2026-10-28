using System.ComponentModel.DataAnnotations;

namespace NotificationEngine.Api.Models;

public class LoginModel
{
    [Required]
    public string Identifier { get; set; } = null!;
    [Required]
    public string Password { get; set; } = null!;
}
