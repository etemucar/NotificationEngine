using NotificationEngine.Core.Model;
using AutoMapper;
using NotificationEngine.Service.Dtos.Responses;

namespace NotificationEngine.Service.Mapper;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // ── PartyManagement ──────────────────────────────────────────────
        // ValidForStart/ValidForEnd (düz) → ValidFor.StartDateTime/EndDateTime
        // (nested) — convention flatten'ın TERSİ bir dönüşüm olduğu için
        // (unflatten), Watchlist.Tags'teki gibi explicit ForMember şart.

        CreateMap<Individual, IndividualResponse>()
            .ForMember(dest => dest.ValidFor, opt => opt.MapFrom(src => new TimePeriodResponse
            {
                StartDateTime = src.ValidForStart,
                EndDateTime = src.ValidForEnd
            }))
            // Handler'da ayrıca doldurulur; AutoMapper doldurmaz.
            .ForMember(dest => dest.ContactMedium, opt => opt.Ignore())
            .ForMember(dest => dest.RelatedParty, opt => opt.Ignore());

        CreateMap<Organization, OrganizationResponse>()
            .ForMember(dest => dest.ValidFor, opt => opt.MapFrom(src => new TimePeriodResponse
            {
                StartDateTime = src.ValidForStart,
                EndDateTime = src.ValidForEnd
            }))
            .ForMember(dest => dest.ContactMedium, opt => opt.Ignore())
            .ForMember(dest => dest.RelatedParty, opt => opt.Ignore());

        CreateMap<PartyRole, PartyRoleResponse>()
            .ForMember(dest => dest.ValidFor, opt => opt.MapFrom(src => new TimePeriodResponse
            {
                StartDateTime = src.ValidForStart,
                EndDateTime = src.ValidForEnd
            }));        

        CreateMap<DigitalIdentity, DigitalIdentityResponse>();               

        // ── Message (Outbox) ─────────────────────────────────────────────
        // Düz alan kopyalama — örtüşen property adları, explicit ForMember gerekmez.
        // DeliveryStatus → TMF "state" çevirisi Api katmanında (ApiMappingProfile).
        CreateMap<OutboxMessage, MessageResponse>();

        CreateMap<MessageTemplate, MessageTemplateResponse>();
    }
}