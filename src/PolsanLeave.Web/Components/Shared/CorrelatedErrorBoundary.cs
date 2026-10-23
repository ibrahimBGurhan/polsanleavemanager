using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Localization;

namespace PolsanLeave.Web.Components.Shared;

/// <summary>
/// Catches unexpected errors in interactive components: logs them with a correlation id and shows only a
/// generic Turkish message with that id (AC-38; docs/conventions.md, Error handling).
/// </summary>
public sealed partial class CorrelatedErrorBoundary : ErrorBoundary
{
    private string? _correlationId;

    [Inject]
    private ILogger<CorrelatedErrorBoundary> Logger { get; set; } = default!;

    [Inject]
    private IStringLocalizer<Messages> L { get; set; } = default!;

    protected override Task OnErrorAsync(Exception exception)
    {
        _correlationId = Guid.NewGuid().ToString("N");
        LogUnhandled(Logger, _correlationId, exception);
        return Task.CompletedTask;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (CurrentException is null)
        {
            builder.AddContent(0, ChildContent);
            return;
        }

        builder.OpenElement(1, "div");
        builder.AddAttribute(2, "class", "alert alert-danger");
        builder.AddAttribute(3, "role", "alert");
        builder.OpenElement(4, "p");
        builder.AddContent(5, L["Error.Text"]);
        builder.CloseElement();
        builder.OpenElement(6, "p");
        builder.AddContent(7, L["Error.CorrelationId"] + ": ");
        builder.OpenElement(8, "code");
        builder.AddAttribute(9, "data-testid", "correlation-id");
        builder.AddContent(10, _correlationId);
        builder.CloseElement();
        builder.CloseElement();
        builder.CloseElement();
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled UI error {CorrelationId}")]
    private static partial void LogUnhandled(ILogger logger, string correlationId, Exception exception);
}
