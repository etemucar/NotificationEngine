using NotificationEngine.Core.Base;

namespace NotificationEngine.Core.Model;

public class ApplicationUser : ModelBase<int>
{
    public string? ExternalUserId { get; set; }
    public int LanguageId { get; set; }
    public Guid DigitalIdentityId { get; set; }

    public Language Language { get; set; } = null!;    
    public DigitalIdentity DigitalIdentity { get; set; } = null!;
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}

