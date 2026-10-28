
using NotificationEngine.Core.Base;

namespace NotificationEngine.Core.Model;

public class Localization : ModelBase<int>
{
    public string EntityType { get; set; } = null!;
    public int EntityId { get; set; }
    public string EntityField { get; set; } = null!;
    public string LanguageCd { get; set; } = null!;
    public string Value { get; set; } = null!;

}
