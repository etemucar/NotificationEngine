using System.ComponentModel.DataAnnotations;

namespace NotificationEngine.Api.Models;

public class RegisterModel
{
    [Required] public string GivenName  { get; set; } = null!;
    [Required] public string FamilyName { get; set; } = null!;

    [Required] public string Identifier { get; set; } = null!; // email veya telefon

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = null!;

    public int LanguageId { get; set; } = 1; // varsayılan dil
}
