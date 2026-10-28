// GetIndividualListQueryHandler.cs
using MediatR;
using AutoMapper;
using Microsoft.Extensions.Logging;
using NotificationEngine.Service.Dtos.Responses;
using NotificationEngine.Service.Features.Queries;
using NotificationEngine.Core.Repository;
using NotificationEngine.Core.Model;

namespace NotificationEngine.Service.Features.Handlers;

public class GetIndividualListQueryHandler : IRequestHandler<GetIndividualListQuery, IEnumerable<IndividualResponse>>
{
    private readonly IRepository<Individual, int> _individualRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetIndividualListQueryHandler> _logger;

    public GetIndividualListQueryHandler(
        IRepository<Individual, int> individualRepository,
        IMapper mapper,
        ILogger<GetIndividualListQueryHandler> logger)
    {
        _individualRepository = individualRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<IndividualResponse>> Handle(GetIndividualListQuery request, CancellationToken cancellationToken)
    {
        var individuals = await _individualRepository.FindAsync(
            predicate: _ => true,
            orderBy: q => q.OrderBy(x => x.Id),
            ct: cancellationToken);

        return individuals.Select(_mapper.Map<IndividualResponse>);
    }
}
