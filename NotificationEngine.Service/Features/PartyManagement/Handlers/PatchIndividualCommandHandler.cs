using MediatR;
using AutoMapper;
using Microsoft.Extensions.Logging;
using NotificationEngine.Core.Exceptions;
using NotificationEngine.Service.Common;
using NotificationEngine.Service.Dtos.Responses;
using NotificationEngine.Service.Features.Commands;
using NotificationEngine.Core.Repository;
using NotificationEngine.Core.Model;
using NotificationEngine.Core.TMFCommon;

namespace NotificationEngine.Service.Features.Handlers;

public class PatchIndividualCommandHandler : IRequestHandler<PatchIndividualCommand, IndividualResponse>
{
    private readonly IRepository<Individual, int> _individualRepository;
    private readonly IRepository<ContactMedium, int> _contactMediumRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<PatchIndividualCommandHandler> _logger;

    public PatchIndividualCommandHandler(
        IRepository<Individual, int> individualRepository,
        IRepository<ContactMedium, int> contactMediumRepository,
        IMapper mapper,
        ILogger<PatchIndividualCommandHandler> logger)
    {
        _individualRepository = individualRepository;
        _contactMediumRepository = contactMediumRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IndividualResponse> Handle(PatchIndividualCommand request, CancellationToken cancellationToken)
    {
        var individual = await _individualRepository.GetByIdAsync(request.Id, cancellationToken);

        if (individual is null)
            throw new NotFoundException($"Individual bulunamadı: {request.Id}");

        // Gelen iletişim bilgileri silmeden ÖNCE doğrulanır: geçersizse hiçbir şey değişmesin.
        var newContactMedia = request.ContactMedium?
            .Select(ContactMediumFactory.Create)
            .ToList();

        // Sadece gelen alanları güncelle (merge-patch)
        if (request.GivenName is not null) individual.GivenName = request.GivenName;
        if (request.FamilyName is not null) individual.FamilyName = request.FamilyName;
        if (request.MiddleName is not null) individual.MiddleName = request.MiddleName;
        if (request.Title is not null) individual.Title = request.Title;
        if (request.Gender is not null) individual.Gender = request.Gender;
        if (request.Nationality is not null) individual.Nationality = request.Nationality;
        if (request.BirthDate is not null) individual.BirthDate = request.BirthDate;
        if (request.PlaceOfBirth is not null) individual.PlaceOfBirth = request.PlaceOfBirth;
        if (request.CountryOfBirth is not null) individual.CountryOfBirth = request.CountryOfBirth;
        if (request.MaritalStatus is not null) individual.MaritalStatus = request.MaritalStatus;

        individual.ValidForStart = request.ValidForStart ?? individual.ValidForStart;
        individual.ValidForEnd = request.ValidForEnd ?? individual.ValidForEnd;

        await _individualRepository.UpdateAsync(individual, cancellationToken);

        // İletişim bilgileri: gönderildiyse "sil ve yeniden yükle".
        // CredentialId dolu kayıtlar (giriş e-postası/telefonu) kişinin değil credential'ın; asla silinmez.
        if (newContactMedia is not null)
        {
            var partyId = individual.PartyId;

            var existing = await _contactMediumRepository.FindAsync(
                m => m.PartyId == partyId && m.CredentialId == null,
                asNoTracking: false,
                ct: cancellationToken);

            if (existing.Count > 0)
                await _contactMediumRepository.RemoveRangeAsync(existing, cancellationToken);

            foreach (var medium in newContactMedia)
            {
                medium.PartyId = partyId;
                await _contactMediumRepository.AddAsync(medium, cancellationToken);
            }
        }

        _logger.LogInformation("Individual güncellendi. IndividualId: {IndividualId}", individual.Id);

        return _mapper.Map<IndividualResponse>(individual);
    }
}