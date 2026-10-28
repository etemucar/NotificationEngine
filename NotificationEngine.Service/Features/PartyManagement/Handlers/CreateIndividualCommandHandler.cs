using MediatR;
using AutoMapper;
using Microsoft.Extensions.Logging;
using NotificationEngine.Core.Common.References;
using NotificationEngine.Core.Data;
using NotificationEngine.Core.Enums;
using NotificationEngine.Core.Exceptions;
using NotificationEngine.Service.Common;
using NotificationEngine.Service.Dtos.Responses;
using NotificationEngine.Core.Repository;
using NotificationEngine.Core.Model;
using NotificationEngine.Core.TMFCommon;
using NotificationEngine.Service.Features.Commands;

namespace NotificationEngine.Service.Features.Handlers;

public class CreateIndividualCommandHandler : IRequestHandler<CreateIndividualCommand, IndividualResponse>
{
    private readonly IRepository<Party, int> _partyRepository;
    private readonly IRepository<RelatedParty, int> _relatedPartyRepository;
    private readonly IRepository<RelatedPartyType, int> _relatedPartyTypeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateIndividualCommandHandler> _logger;

    public CreateIndividualCommandHandler(
        IRepository<Party, int> partyRepository,
        IRepository<RelatedParty, int> relatedPartyRepository,
        IRepository<RelatedPartyType, int> relatedPartyTypeRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CreateIndividualCommandHandler> logger)
    {
        _partyRepository = partyRepository;
        _relatedPartyRepository = relatedPartyRepository;
        _relatedPartyTypeRepository = relatedPartyTypeRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IndividualResponse> Handle(CreateIndividualCommand request, CancellationToken cancellationToken)
    {
        var individual = new Individual
        {
            GivenName = request.GivenName,
            FamilyName = request.FamilyName,
            MiddleName = request.MiddleName,
            Title = request.Title,
            Gender = request.Gender,
            Nationality = request.Nationality,
            BirthDate = request.BirthDate,
            PlaceOfBirth = request.PlaceOfBirth,
            CountryOfBirth = request.CountryOfBirth,
            MaritalStatus = request.MaritalStatus,
            ValidForStart = request.ValidForStart ?? DateTime.MinValue,
            ValidForEnd = request.ValidForEnd ?? DateTime.MaxValue,
        };

        // İletişim bilgileri önce doğrulanır: geçersiz istekte hiçbir şey yazılmasın.
        var contactMedia = (request.ContactMedium ?? new())
            .Select(ContactMediumFactory.Create)
            .ToList();

        // FK artık elle .Id okunarak DEĞİL, navigation property ile kuruluyor —
        // party.Id SaveChanges'ten önce 0 olsa bile EF bu referanstan FK'yi kendisi çözer.
        var party = new Party
        {
            Individual = individual,
            ContactMedium = contactMedia
        };

        // Önce ilişkileri doğrula: geçersiz istekte hiçbir şey yazılmasın.
        var relatedParties = await BuildRelatedPartiesAsync(request, party, cancellationToken);

        await _partyRepository.AddAsync(party, cancellationToken);
        foreach (var rp in relatedParties)
            await _relatedPartyRepository.AddAsync(rp, cancellationToken);

        // Log ve response'ta gerçek Id'lerin görünmesi için SaveChanges burada, handler içinde
        // MANUEL tetikleniyor (TransactionalBehavior'ın SaveChanges'i next() döndükten sonra çağrılır).
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Individual oluşturuldu. PartyId: {PartyId}, IndividualId: {IndividualId}, RelatedParty: {RelatedCount}, ContactMedium: {ContactCount}",
            party.Id, individual.Id, relatedParties.Count, contactMedia.Count);

        return _mapper.Map<IndividualResponse>(individual);
    }

    /// <summary>
    /// TMF relatedParty öğelerini doğrular ve RelatedParty kayıtlarına çevirir:
    /// role metni RelatedPartyType.Code'a çözülür (bilinmeyen → 400);
    /// OrganizationMember için karşı taraf mevcut bir Organization olmalıdır.
    /// </summary>
    private async Task<List<RelatedParty>> BuildRelatedPartiesAsync(
        CreateIndividualCommand request, Party party, CancellationToken ct)
    {
        var result = new List<RelatedParty>();
        if (request.RelatedParty is not { Count: > 0 })
            return result;

        // Yalnızca birkaç satır: bellekte eşleştirmek yeterli.
        var types = (await _relatedPartyTypeRepository.FindAsync(_ => true, ct: ct)).ToList();
        var now = DateTime.UtcNow;

        foreach (var item in request.RelatedParty)
        {
            var type = types.FirstOrDefault(t =>
                string.Equals(t.Code, item.Role, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidInputException($"Geçersiz relatedParty role: '{item.Role}'.");

            if (!int.TryParse(item.PartyOrPartyRole?.Id, out var targetPartyId))
                throw new InvalidInputException("relatedParty.partyOrPartyRole.id geçerli bir sayı olmalı.");

            var target = await _partyRepository.FindOneAsync(p => p.Id == targetPartyId, ct: ct)
                ?? throw new InvalidInputException($"İlişkili taraf bulunamadı: {targetPartyId}.");

            if (type.Id == ReferenceData.RelatedPartyType.OrganizationMember
                && target.PartyType != PartyType.Organization)
                throw new InvalidInputException("OrganizationMember için ilişkili taraf bir Organization olmalı.");

            result.Add(new RelatedParty
            {
                Party = party,                 // yeni Party: FK'yi EF çözer
                RelatedPartyId = target.Id,
                RelatedPartyTypeId = type.Id,
                ValidFrom = now
            });
        }

        // Kullanıcı başına tek açık OrganizationMember bağlantısı (veritabanında kısmi unique index de var).
        if (result.Count(r => r.RelatedPartyTypeId == ReferenceData.RelatedPartyType.OrganizationMember) > 1)
            throw new InvalidInputException("Bir kullanıcı yalnızca bir organizasyonun üyesi olabilir.");

        return result;
    }
}