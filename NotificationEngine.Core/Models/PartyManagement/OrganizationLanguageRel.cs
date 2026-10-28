using NotificationEngine.Core.Base;

namespace NotificationEngine.Core.Model;

public class OrganizationLanguageRel : ModelBase<int>
{
    public int OrganizationId { get; set; }
    public int LanguageId { get; set; }

    public virtual Organization Organization { get; set; } = null!;
    public virtual Language Language { get; set; } = null!;
}
