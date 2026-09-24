namespace ProductService.Application.Common.Behaviors;

using MediatR;

/// <summary>
/// MediatR Pipeline Behavior: logs every request and its execution time.
/// Automatically inserted into the CQRS pipeline for ALL Commands and Queries.
/// This is a cross-cutting concern handled via the Decorator/Pipeline pattern.
/// </summary>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("[CQRS] Handling {RequestName}: {@Request}", requestName, request);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var response = await next();
        sw.Stop();

        _logger.LogInformation("[CQRS] Handled {RequestName} in {ElapsedMs}ms", requestName, sw.ElapsedMilliseconds);
        return response;
    }
}
