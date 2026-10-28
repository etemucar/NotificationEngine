using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace NotificationEngine.Service.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;

    public ValidationBehavior(
        IEnumerable<IValidator<TRequest>> validators,
        ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    {
        _validators = validators;
        _logger     = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Bu request için tanımlı validator yoksa direkt devam et
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        // Not (2026-08): Validate(context) -> ValidateAsync(context, ct) olarak
        // değiştirildi. Bazı validator'larda (örn. CreateHolidaysBulkCommandValidator,
        // PatchInstrumentCommandValidator) repository'ye giden async MustAsync
        // kuralları var; senkron Validate çağrısı FluentValidation tarafından
        // kasıtlı olarak AsyncValidatorInvokedSynchronouslyException ile
        // reddediliyor (async kuralı sessizce atlamak yerine patlatıyor).
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count > 0)
        {
            _logger.LogWarning(
                "Validation hatası: {Request} → {Errors}",
                typeof(TRequest).Name,
                string.Join(", ", failures.Select(f => f.ErrorMessage)));

            throw new ValidationException(failures);
        }

        return await next();
    }
}
