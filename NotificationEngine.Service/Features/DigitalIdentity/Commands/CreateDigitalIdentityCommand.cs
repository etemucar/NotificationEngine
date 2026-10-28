using MediatR;
using NotificationEngine.Core.Enums;
using NotificationEngine.Service.Dtos.Requests;
using NotificationEngine.Service.Dtos.Responses;
using NotificationEngine.Core.Common.Behaviors;

namespace NotificationEngine.Service.Features.Commands;

public class CreateDigitalIdentityCommand : IRequest<DigitalIdentityResponse>, ITransactionalRequest
{
    public string? Nickname { get; set; }
    public int PartyRoleId { get; set; }
    public List<CredentialRequest> Credentials { get; set; } = new();
}

public record UpdateDigitalIdentityStatusCommand(
    Guid DigitalIdentityId,
    GeneralStatus Status
) : IRequest<bool>, ITransactionalRequest;

public class PatchDigitalIdentityCommand : IRequest<DigitalIdentityResponse>, ITransactionalRequest
{
    public Guid DigitalIdentityId { get; set; }

    // Her zaman uygulanır — null gönderilirse Nickname null'a çevrilir (TMF PATCH semantiği:
    // alan bu DTO'da var demek, değeri ne olursa olsun set edilir).
    public string? Nickname { get; set; }

    // null  => credential set'ine dokunma
    // dolu  => mevcut tüm credential set'i BU listeyle tamamen senkronize edilir:
    //          - Id'si gelen ve mevcutta olan  → güncelle (CredentialType/TrustLevel + child collection'lar replace)
    //          - Id'si null olan               → yeni credential olarak ekle
    //          - mevcutta olup listede Id'si geçmeyen → sil
    public List<CredentialPatchRequest>? Credentials { get; set; }
}

public class CredentialPatchRequest
{
    // null = yeni credential; dolu = mevcut credential'ın güncellenmesi
    public Guid? Id { get; set; }
    public CredentialType CredentialType { get; set; }
    public int? TrustLevel { get; set; }
    public List<CredentialCharacteristicRequest> Characteristics { get; set; } = new();
    public List<ContactMediumRequest> ContactMedia { get; set; } = new();
}
