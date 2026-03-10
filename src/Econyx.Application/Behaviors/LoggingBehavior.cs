namespace Econyx.Application.Behaviors;

using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

public sealed partial class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        LogHandling(_logger, requestName);

        var sw = Stopwatch.StartNew();

        try
        {
            var response = await next();
            sw.Stop();

            LogHandled(_logger, requestName, sw.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            LogError(_logger, requestName, sw.ElapsedMilliseconds, ex);
            throw;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Handling {RequestName}")]
    private static partial void LogHandling(ILogger logger, string requestName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Handled {RequestName} in {ElapsedMs}ms")]
    private static partial void LogHandled(ILogger logger, string requestName, long elapsedMs);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error handling {RequestName} after {ElapsedMs}ms")]
    private static partial void LogError(ILogger logger, string requestName, long elapsedMs, Exception ex);
}
