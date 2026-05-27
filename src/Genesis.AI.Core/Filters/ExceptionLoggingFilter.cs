using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Genesis.AI.Core.Filters;

public sealed class ExceptionLoggingFilter(ILogger<ExceptionLoggingFilter> logger) : IExceptionLoggingFilter
{
    private readonly ILogger<ExceptionLoggingFilter> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    public void OnException(ExceptionContext context)
    {
        if (context.Exception is OperationCanceledException or TaskCanceledException)
        {
            throw context.Exception;
        }

        _logger.LogError(context.Exception, "Unhandled Exception");
    }
}
