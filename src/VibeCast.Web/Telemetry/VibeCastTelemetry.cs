using System.Diagnostics;

namespace VibeCast.Web.Telemetry;

public static class VibeCastTelemetry
{
    public const string ServiceName = "VibeCast.Web";
    public const string ActivitySourceName = "VibeCast";
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}
