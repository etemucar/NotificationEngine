using MediatR;
using AutoMapper;
using Microsoft.Extensions.Logging;
using NotificationEngine.Core.Exceptions;
using NotificationEngine.Service.Dtos.Responses;
using NotificationEngine.Service.Features.Queries;
using NotificationEngine.Core.Repository;
using NotificationEngine.Core.Model;

namespace NotificationEngine.Service.Features.Handlers;

public class GetIndividualQueryHandler : IRequestHandler<GetIndividualQuery, IndividualResponse>
{
    private readonly IRepository<Individual, int> _individualRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetIndividualQueryHandler> _logger;

    public GetIndividualQueryHandler(
        IRepository<Individual, int> individualRepository,
        IMapper mapper,
        ILogger<GetIndividualQueryHandler> logger)
    {
        _individualRepository = individualRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IndividualResponse> Handle(GetIndividualQuery request, CancellationToken cancellationToken)
    {
        var individual = await _individualRepository.GetByIdAsync(request.Id, cancellationToken);

        if (individual is null)
            throw new NotFoundException($"Individual bulunamadı: {request.Id}");

        return _mapper.Map<IndividualResponse>(individual);
    }
}
