// PatchDigitalIdentityCommandHandler.cs
using MediatR;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NotificationEngine.Core.Repository;
using NotificationEngine.Core.Model;
using NotificationEngine.Core.Security;
using NotificationEngine.Core.Services;
using NotificationEngine.Core.Exceptions;
using NotificationEngine.Service.Features.Commands;
using NotificationEngine.Service.Dtos.Responses;
using NotificationEngine.Service.Dtos.Requests;

namespace NotificationEngine.Service.Features.Handlers;

public class PatchDigitalIdentityCommandHandler
    : IRequestHandler<PatchDigitalIdentityCommand, DigitalIdentityResponse>
{
    private readonly IRepository<DigitalIdentity, Guid> _digitalIdentityRepository;
    private readonly IRepository<Credential, Guid> _credentialRepository;
    private readonly IRepository<CredentialCharacteristic, int> _credentialCharacteristicRepository;
    private readonly IRepository<ContactMedium, int> _contactMediumRepository;
    private readonly IRepository<PartyRole, int> _partyRoleRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IMapper _mapper;
    private readonly ILogger<PatchDigitalIdentityCommandHandler> _logger;

    public PatchDigitalIdentityCommandHandler(
        IRepository<DigitalIdentity, Guid> digitalIdentityRepository,
        IRepository<Credential, Guid> credentialRepository,
        IRepository<CredentialCharacteristic, int> credentialCharacteristicRepository,
        IRepository<ContactMedium, int> contactMediumRepository,
        IRepository<PartyRole, int> partyRoleRepository,
        ICurrentUserService currentUserService,
        IPasswordHasher passwordHasher,
        IMapper mapper,
        ILogger<PatchDigitalIdentityCommandHandler> logger)
    {
        _digitalIdentityRepository = digitalIdentityRepository;
        _credentialRepository = credentialRepository;
        _credentialCharacteristicRepository = credentialCharacteristicRepository;
        _contactMediumRepository = contactMediumRepository;
        _partyRoleRepository = partyRoleRepository;
        _currentUserService = currentUserService;
        _passwordHasher = passwordHasher;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<DigitalIdentityResponse> Handle(
        PatchDigitalIdentityCommand request,
        CancellationToken cancellationToken)
    {
        var digitalIdentity = await _digitalIdentityRepository.FindOneAsync(
            d => d.Id == request.DigitalIdentityId,
            i => i.Include(d => d.PartyRole)
                  .Include(d => d.Credentials).ThenInclude(c => c.Characteristics)
                  .Include(d => d.Credentials).ThenInclude(c => c.ContactMedia),
            asNoTracking: false,
            ct: cancellationToken);

        if (digitalIdentity is null)
            throw new NotFoundException($"DigitalIdentity bulunamadı. Id: {request.DigitalIdentityId}");

        var actingPartyRoleId = await _currentUserService.GetPartyRoleIdAsync(cancellationToken);

        if (digitalIdentity.PartyRoleId != actingPartyRoleId)
        {
            var actingRole = await _partyRoleRepository.FindOneAsync(
                pr => pr.Id == actingPartyRoleId,
                include: q => q.Include(pr => pr.PartyRoleType),
                ct: cancellationToken);

            if (actingRole?.PartyRoleType?.PartyRoleTypeCd != "SITE_ADMIN")
                throw new UnauthorizedException("Bu kaydı düzenleme yetkiniz yok.");
        }

        digitalIdentity.Nickname = request.Nickname;

        if (request.Credentials is not null)
        {
            var incomingIds = request.Credentials
                .Where(c => c.Id.HasValue)
                .Select(c => c.Id!.Value)
                .ToHashSet();

            var toRemove = digitalIdentity.Credentials
                .Where(c => !incomingIds.Contains(c.Id))
                .ToList();

            foreach (var credential in toRemove)
            {
                foreach (var contactMedium in credential.ContactMedia.ToList())
                    await _contactMediumRepository.RemoveAsync(contactMedium, cancellationToken);

                await _credentialRepository.RemoveAsync(credential, cancellationToken);
                digitalIdentity.Credentials.Remove(credential);
            }

            foreach (var incoming in request.Credentials.Where(c => c.Id.HasValue))
            {
                var existing = digitalIdentity.Credentials.First(c => c.Id == incoming.Id!.Value);

                existing.CredentialType = incoming.CredentialType;
                existing.TrustLevel = incoming.TrustLevel;

                foreach (var ch in existing.Characteristics.ToList())
                    await _credentialCharacteristicRepository.RemoveAsync(ch, cancellationToken);
                existing.Characteristics.Clear();
                foreach (var chReq in incoming.Characteristics)
                    existing.Characteristics.Add(MapCharacteristic(chReq));

                foreach (var cm in existing.ContactMedia.ToList())
                    await _contactMediumRepository.RemoveAsync(cm, cancellationToken);
                existing.ContactMedia.Clear();
                foreach (var cmReq in incoming.ContactMedia)
                    existing.ContactMedia.Add(MapContactMedium(cmReq, digitalIdentity.PartyRole.PartyId));

                await _credentialRepository.UpdateAsync(existing, cancellationToken);
            }

            foreach (var incoming in request.Credentials.Where(c => !c.Id.HasValue))
            {
                var newCredential = new Credential
                {
                    CredentialType = incoming.CredentialType,
                    TrustLevel = incoming.TrustLevel,
                    DigitalIdentityId = digitalIdentity.Id,
                    Characteristics = incoming.Characteristics.Select(MapCharacteristic).ToList(),
                    ContactMedia = incoming.ContactMedia
                        .Select(cm => MapContactMedium(cm, digitalIdentity.PartyRole.PartyId))
                        .ToList()
                };

                await _credentialRepository.AddAsync(newCredential, cancellationToken);
                digitalIdentity.Credentials.Add(newCredential);
            }
        }

        await _digitalIdentityRepository.UpdateAsync(digitalIdentity, cancellationToken);

        _logger.LogInformation(
            "DigitalIdentity patch edildi. Id: {Id}, CredentialSync: {Sync}, İşlemYapan: {ActingRoleId}",
            digitalIdentity.Id, request.Credentials is not null, actingPartyRoleId);

        return _mapper.Map<DigitalIdentityResponse>(digitalIdentity);
    }

    private CredentialCharacteristic MapCharacteristic(CredentialCharacteristicRequest ch) => new()
    {
        Name = ch.Name == "password" ? "passwordHash" : ch.Name,
        Value = ch.Name == "password" ? _passwordHasher.Hash(ch.Value) : ch.Value
    };

    private static ContactMedium MapContactMedium(ContactMediumRequest cm, int partyId)
    {
        var mediumType = Enum.Parse<NotificationEngine.Core.Enums.ContactMediumType>(cm.MediumType, ignoreCase: true);

        string? email = null;
        string? phoneNumber = null;

        switch (mediumType)
        {
            case NotificationEngine.Core.Enums.ContactMediumType.EmailAddress:
                email = cm.Characteristic.GetValueOrDefault("emailAddress")?.ToString();
                break;
            case NotificationEngine.Core.Enums.ContactMediumType.PhoneNumber:
                phoneNumber = cm.Characteristic.GetValueOrDefault("phoneNumber")?.ToString();
                break;
        }

        return new ContactMedium
        {
            PartyId = partyId,
            MediumType = mediumType,
            IsPreferred = cm.Preferred,
            Email = email,
            PhoneNumber = phoneNumber
        };
    }
}
