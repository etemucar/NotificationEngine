// CreateDigitalIdentityCommandHandler.cs
using MediatR;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NotificationEngine.Core.Repository;
using NotificationEngine.Core.Model;
using NotificationEngine.Core.Enums;
using NotificationEngine.Core.Security;
using NotificationEngine.Core.Services;
using NotificationEngine.Core.Exceptions;
using NotificationEngine.Service.Features.Commands;
using NotificationEngine.Service.Dtos.Responses;
using NotificationEngine.Service.Dtos.Requests;

namespace NotificationEngine.Service.Features.Handlers;

public class CreateDigitalIdentityCommandHandler
    : IRequestHandler<CreateDigitalIdentityCommand, DigitalIdentityResponse>
{
    private readonly IRepository<DigitalIdentity, Guid> _digitalIdentityRepository;
    private readonly IRepository<ApplicationUser, int> _userRepository;
    private readonly IRepository<PartyRole, int> _partyRoleRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateDigitalIdentityCommandHandler> _logger;

    public CreateDigitalIdentityCommandHandler(
        IRepository<DigitalIdentity, Guid> digitalIdentityRepository,
        IRepository<ApplicationUser, int> userRepository,
        IRepository<PartyRole, int> partyRoleRepository,
        ICurrentUserService currentUserService,
        IPasswordHasher passwordHasher,
        IMapper mapper,
        ILogger<CreateDigitalIdentityCommandHandler> logger)
    {
        _digitalIdentityRepository = digitalIdentityRepository;
        _userRepository = userRepository;
        _partyRoleRepository = partyRoleRepository;
        _currentUserService = currentUserService;
        _passwordHasher = passwordHasher;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<DigitalIdentityResponse> Handle(
        CreateDigitalIdentityCommand request,
        CancellationToken cancellationToken)
    {
        var actingPartyRoleId = await _currentUserService.GetPartyRoleIdAsync(cancellationToken);
        var actingRole = await _partyRoleRepository.FindOneAsync(
            pr => pr.Id == actingPartyRoleId,
            include: q => q.Include(pr => pr.PartyRoleType),
            ct: cancellationToken);

        if (actingRole?.PartyRoleType?.PartyRoleTypeCd != "SITE_ADMIN")
            throw new UnauthorizedException("Bu işlem için yetkiniz yok.");

        var partyRole = await _partyRoleRepository.FindOneAsync(
            pr => pr.Id == request.PartyRoleId,
            ct: cancellationToken);

        if (partyRole is null)
            throw new NotFoundException($"PartyRole bulunamadı. Id: {request.PartyRoleId}");

        var credentials = request.Credentials.Select(cr => new Credential
        {
            CredentialType = cr.CredentialType,
            TrustLevel = cr.TrustLevel,
            Characteristics = cr.Characteristics.Select(ch => new CredentialCharacteristic
            {
                Name = ch.Name == "password" ? "passwordHash" : ch.Name,
                Value = ch.Name == "password"
                    ? _passwordHasher.Hash(ch.Value)
                    : ch.Value
            }).ToList(),
            ContactMedia = cr.ContactMedia.Select(cm => MapContactMedium(cm, partyRole.PartyId)).ToList()
        }).ToList();

        var digitalIdentity = new DigitalIdentity
        {
            Nickname = request.Nickname,
            Status = GeneralStatus.Active,
            DigitalIdentityDate = DateTime.UtcNow,
            PartyRoleId = request.PartyRoleId,
            Credentials = credentials
        };

        await _digitalIdentityRepository.AddAsync(digitalIdentity, cancellationToken);

        var applicationUser = new ApplicationUser
        {
            DigitalIdentityId = digitalIdentity.Id
        };

        await _userRepository.AddAsync(applicationUser, cancellationToken);

        _logger.LogInformation(
            "DigitalIdentity oluşturuldu. Id: {Id}, PartyRoleId: {PartyRoleId}, OluşturanRoleId: {ActingRoleId}",
            digitalIdentity.Id, request.PartyRoleId, actingPartyRoleId);

        return _mapper.Map<DigitalIdentityResponse>(digitalIdentity);
    }

    private static ContactMedium MapContactMedium(ContactMediumRequest cm, int partyId)
    {
        var mediumType = Enum.Parse<ContactMediumType>(cm.MediumType, ignoreCase: true);

        string? email = null;
        string? phoneNumber = null;

        switch (mediumType)
        {
            case ContactMediumType.EmailAddress:
                email = cm.Characteristic.GetValueOrDefault("emailAddress")?.ToString();
                break;
            case ContactMediumType.PhoneNumber:
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
