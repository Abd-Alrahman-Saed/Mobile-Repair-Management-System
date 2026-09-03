using Serilog;

namespace MRMS.UI.Services;

/// <summary>
/// Single place that turns a caught exception into (a) a full Serilog record - so nothing fails
/// silently in <c>logs/mrms-*.log</c> - and (b) a short message safe to show in the Arabic UI.
/// Domain-validation exceptions (<see cref="InvalidOperationException"/>, <see cref="ArgumentException"/>)
/// are thrown by the service layer with a ready Arabic message, so those are surfaced verbatim; any
/// other exception is logged in full and replaced with a generic line rather than leaking raw
/// .NET/EF/SQL text to the user.
/// </summary>
public static class UiError
{
    public const string Generic = "حدث خطأ غير متوقّع. يرجى المحاولة مجددًا أو مراجعة السجل.";

    /// <param name="action">Short description of what was being attempted, e.g. "حفظ العميل".</param>
    public static string Describe(Exception ex, string action)
    {
        Log.Error(ex, "UI action failed: {Action}", action);
        return ex is InvalidOperationException or ArgumentException ? ex.Message : Generic;
    }
}
