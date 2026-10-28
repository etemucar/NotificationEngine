using AutoMapper;
using NotificationEngine.Api.Models.TMFOpenApi5;
using NotificationEngine.Service.Features.Commands;
using NotificationEngine.Service.Dtos.Requests;
using NotificationEngine.Service.Dtos.Responses;
using NotificationEngine.Api.Models;

namespace NotificationEngine.Api.Mapping;

public class ApiMappingProfile : Profile
{
    public ApiMappingProfile()
    {
        // ── Request mappings (Model → Command) ──────────────────────────

        CreateMap<TimePeriodModel, TimePeriodRequest>();

        CreateMap<ContactMediumModel, ContactMediumRequest>()
            .ForMember(dest => dest.Characteristic, opt => opt.MapFrom(src =>
                src.Characteristic != null
                    ? ObjectToDictionary(src.Characteristic)
                    : new Dictionary<string, object>()));

        CreateMap<RelatedPartyModel, RelatedPartyRequest>()
            .ForMember(dest => dest.PartyOrPartyRole, opt => opt.MapFrom(src => src.PartyOrPartyRole));

        CreateMap<PartyOrPartyRoleModel, PartyOrPartyRoleRequest>();

        CreateMap<IndividualModel, CreateIndividualCommand>()
            .ForMember(dest => dest.ContactMedium, opt => opt.MapFrom(src => src.ContactMedium))
            .ForMember(dest => dest.RelatedParty,  opt => opt.MapFrom(src => src.RelatedParty));

        CreateMap<IndividualModel, PatchIndividualCommand>()
            .ForMember(dest => dest.Id, opt => opt.Ignore()); // Controller'da set edilir


        // ── Response mappings (Response → Model) ────────────────────────

        CreateMap<TimePeriodResponse, TimePeriodModel>();

        CreateMap<IndividualResponse, IndividualModel>()
            .ForMember(dest => dest.Id,       opt => opt.MapFrom(src => src.Id.ToString()))
            .ForMember(dest => dest.Href,     opt => opt.Ignore()) // Controller'da set edilir
            .ForMember(dest => dest.Type,     opt => opt.MapFrom(_ => "Individual"))
            .ForMember(dest => dest.BaseType, opt => opt.MapFrom(_ => "Party"));

        // Request mappings
        CreateMap<CredentialCharacteristicModel, CredentialCharacteristicRequest>();

        CreateMap<CredentialModel, CredentialRequest>()
            .ForMember(dest => dest.ContactMedia, opt => opt.MapFrom(src => src.ContactMedia));

        CreateMap<DigitalIdentityModel, CreateDigitalIdentityCommand>()
            .ForMember(dest => dest.Credentials, opt => opt.MapFrom(src => src.Credentials));

        // ── B7: DigitalIdentity Patch mapping'i (NotificationEngine'dan taşındı) ─────
        CreateMap<CredentialModel, CredentialPatchRequest>()
            .ForMember(dest => dest.ContactMedia, opt => opt.MapFrom(src => src.ContactMedia));

        CreateMap<DigitalIdentityModel, PatchDigitalIdentityCommand>()
            .ForMember(dest => dest.DigitalIdentityId, opt => opt.Ignore()) // Controller'da route'tan set edilir
            .ForMember(dest => dest.Credentials, opt => opt.MapFrom(src => src.Credentials));

        // Response mappings
        CreateMap<DigitalIdentityResponse, DigitalIdentityModel>()
            .ForMember(dest => dest.Id,     opt => opt.MapFrom(src => src.Id.ToString()))
            .ForMember(dest => dest.Href,   opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));


        CreateMap<RegisterOrganizationModel, RegisterOrganizationCommand>();

        // ── MessageTemplate (TMF681 ailesinde TMF-tarzı kaynak) ───────────
        // Shared: OrganizationId boş = tüm kuruluşların kullandığı ortak şablon.
        // MessageType kanal adlandırması CommunicationMessageMappings'te tek yerde tutulur.
        CreateMap<MessageTemplateResponse, MessageTemplateModel>()
            .ForMember(dest => dest.Id,          opt => opt.MapFrom(src => src.Id.ToString()))
            .ForMember(dest => dest.Href,        opt => opt.Ignore()) // Controller'da set edilir
            .ForMember(dest => dest.Type,        opt => opt.MapFrom(_ => "MessageTemplate"))
            .ForMember(dest => dest.BaseType,    opt => opt.MapFrom(_ => "MessageTemplate"))
            .ForMember(dest => dest.Shared,      opt => opt.MapFrom(src => src.OrganizationId == null))
            .ForMember(dest => dest.MessageType, opt => opt.MapFrom(src => CommunicationMessageMappings.ToMessageType(src.Channel)))
            .ForMember(dest => dest.Status,      opt => opt.MapFrom(src => src.Status.ToString().ToLowerInvariant()));        
    }

    private static Dictionary<string, object> ObjectToDictionary(object obj)
    {
        var dictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        if (obj == null) return dictionary;

        foreach (var prop in obj.GetType().GetProperties())
        {
            var value = prop.GetValue(obj);
            if (value != null)
                dictionary[char.ToLowerInvariant(prop.Name[0]) + prop.Name[1..]] = value;
        }

        return dictionary;
    }
}
