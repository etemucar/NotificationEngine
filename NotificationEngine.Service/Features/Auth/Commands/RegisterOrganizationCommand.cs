using MediatR;
using NotificationEngine.Core.Common.Behaviors;   // TEYİT: ITransactionalRequest namespace'i
using NotificationEngine.Service.Dtos.Requests;
using NotificationEngine.Service.Dtos.Responses;

namespace NotificationEngine.Service.Features.Commands;

/// <summary>
/// Bir Organization'ı, bağlı PartyRole/DigitalIdentity/Credential'larıyla
/// birlikte tek seferde kaydeder.
///
/// PartyRoleTypeId = ExternalService ise ApplicationUser OLUŞTURULMAZ
/// (FinYo/Censitor gibi, sadece ApiKey ile kimlik doğrulayan servisler).
/// Başka her PartyRoleTypeId için ApplicationUser oluşturulur (insan login
/// edebilir) - bu durumda LanguageId zorunludur.
/// </summary>
public class RegisterOrganizationCommand : IRequest<RegisterOrganizationResult>, ITransactionalRequest
{
    public string  Name                { get; set; } = null!;
    public string? TaxOffice           { get; set; }
    public long    TaxNumber           { get; set; }
    public long    IdentityNumber      { get; set; }
    public string? TradeName           { get; set; }
    public long    TradeRegisterNumber { get; set; }
    public long    MersisNo            { get; set; }
    public DateTime? ValidForStart { get; set; }
    public DateTime? ValidForEnd   { get; set; }

    public int     PartyRoleTypeId { get; set; }
    public string? Nickname        { get; set; }

    /// <summary>Sadece ApplicationUser oluşacaksa (PartyRoleTypeId != ExternalService) zorunlu.</summary>
    public int? LanguageId { get; set; }

    public List<CredentialRequest> Credentials { get; set; } = new();
}