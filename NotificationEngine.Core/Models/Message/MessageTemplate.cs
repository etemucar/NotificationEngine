using NotificationEngine.Core.Base;
using NotificationEngine.Core.Enums;

namespace NotificationEngine.Core.Model;

public class MessageTemplate : ModelBase<int>
{
    public int? OrganizationId { get; set; }                    // null = tüm kuruluşlar için ortak
    public string Code { get; set; } = default!;          // örn. OTP_PASSWORD, QUESTION_ANSWERED
    public Channel Channel { get; set; }
    public string Culture { get; set; } = "tr-TR";
    public string? Subject { get; set; }                  // SMS/Push'ta boş olabilir (Push'ta başlık olarak kullanılabilir)
    public string Body { get; set; } = default!;          // {{parametre}} yer tutucuları; SMS gövdesi ASCII
    // Aktiflik: ModelBase.Status == GeneralStatus.Active (varsayılan Created → oluştururken Active set edilmeli)

    public Organization? Organization { get; set; }
}