// NotificationEngine.Service/Behaviors/TransactionalBehavior.cs

using NotificationEngine.Core.Common.Behaviors;
using NotificationEngine.Core.Data;
using MediatR;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace NotificationEngine.Service.Behaviors;

public sealed class TransactionalBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TransactionalBehavior<TRequest, TResponse>> _logger;

    public TransactionalBehavior(
        IUnitOfWork unitOfWork,
        ILogger<TransactionalBehavior<TRequest, TResponse>> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Bu command transaction istemiyor → direkt devam et
        if (request is not ITransactionalRequest)
        {
            return await next();
        }

        // Zaten dış bir transaction açıksa → hiçbir şey yapma, iç içe transaction açma
        if (_unitOfWork.HasActiveTransaction)
        {
            _logger.LogDebug("Dış transaction algılandı → yeni transaction açılmıyor: {Request}", 
                typeof(TRequest).Name);
            return await next();
        }

        // Yeni transaction başlat
        var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var response = await next();

            // Sadece başarıyla biterse commit et
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Transaction başarıyla commit edildi: {Request}", 
                typeof(TRequest).Name);

            return response;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Transaction rollback edildi: {Request}", typeof(TRequest).Name);
            throw; // hata yukarı fırlasın, caller karar versin
        }
    }
}
