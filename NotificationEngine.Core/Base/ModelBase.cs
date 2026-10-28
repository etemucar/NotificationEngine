using NotificationEngine.Core.Enums;

namespace NotificationEngine.Core.Base;

public abstract class ModelBase
{
    public virtual GeneralStatus Status { get; set; }
    public virtual int IsDeleted { get; set; } = 0;
    public virtual DateTime? CreateDate { get; set; }
    public virtual int? CreatedBy { get; set; }
    public virtual DateTime? UpdateDate { get; set; }
    public virtual int? UpdatedBy { get; set; }
}

public class ModelBase<TKey> : ModelBase
{
    public virtual TKey Id { get; set; } = default!;
}
