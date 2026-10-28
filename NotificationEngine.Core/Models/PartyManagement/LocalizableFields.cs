
using NotificationEngine.Core.Base;

namespace NotificationEngine.Core.Model;

public class LocalizableFields : ModelBase<int>
{
    public string EntityType { get; set; } = null!;
    public string EntityField { get; set; } = null!;

}
