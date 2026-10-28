using NotificationEngine.Core.Base;

namespace NotificationEngine.Core.Model;

public class RefreshToken : ModelBase<int>
{
    public string Token { get; set; } = null!; 
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public string? CreatedByIp { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ApplicationUserId { get; set; }
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive   => !IsRevoked && !IsExpired;

    public ApplicationUser ApplicationUser { get; set; } = null!;
}
