using NotificationEngine.Core.Base;

namespace NotificationEngine.Core.Model;

public class UserGroup : ModelBase<int>
{
    public int OrganizationId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public Organization Organization { get; set; } = null!;
    public ICollection<ApplicationUser> Users { get; set; } = [];
}
