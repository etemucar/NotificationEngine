using MediatR;
using NotificationEngine.Core.Constants;
using NotificationEngine.Service.Features.Commands;


namespace NotificationEngine.Api.Workers;

/// <summary>Outbox'ı periyodik tarayıp ProcessOutboxCommand gönderen arka plan servisi.</summary>
public sealed class OutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxWorker> _logger;

    public OutboxWorker(IServiceScopeFactory scopeFactory, ILogger<OutboxWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var poll = TimeSpan.FromSeconds(NotificationConstants.PollIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            var claimed = 0;
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var result = await mediator.Send(new ProcessOutboxCommand(), stoppingToken);
                claimed = result.Claimed;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox turu başarısız oldu, bir sonraki turda yeniden denenecek.");
            }

            // Tam batch geldiyse kuyrukta daha fazlası olabilir: beklemeden devam et.
            if (claimed >= NotificationConstants.ClaimBatchSize) continue;

            try { await Task.Delay(poll, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }
}