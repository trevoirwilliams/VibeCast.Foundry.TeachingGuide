using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace VibeCast.ServiceDefaults;

public static class Extensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";
    private const string VibeCastActivitySourceName = "VibeCast";
    private const string VibeCastAiSourceName = "VibeCast.AI";

    public static TBuilder AddServiceDefaults<TBuilder>(
        this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(
        this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        string? insightsConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];

        string? otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        OpenTelemetryBuilder openTelemetry =
            builder.Services
                .AddOpenTelemetry()
                .WithMetrics(metrics =>
                {
                    metrics
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation()
                        .AddMeter(VibeCastAiSourceName);
                })
                .WithTracing(tracing =>
                {
                    tracing
                        .AddSource(
                            builder.Environment.ApplicationName)
                        .AddSource(
                            VibeCastActivitySourceName)
                        .AddSource(
                            VibeCastAiSourceName)
                        .AddAspNetCoreInstrumentation(options =>
                        {
                            options.Filter = context =>
                                !context.Request.Path
                                    .StartsWithSegments(
                                        HealthEndpointPath)
                                &&
                                !context.Request.Path
                                    .StartsWithSegments(
                                        AlivenessEndpointPath);
                        })
                        .AddHttpClientInstrumentation();
                });

        if (builder.Environment.IsProduction() && !string.IsNullOrWhiteSpace(insightsConnectionString))
        {
            // PRACTICE S09-07: Export production telemetry to Application Insights.
            // Lesson: Add Application Insights and Production Health Monitoring.
            // 1. Add the Azure Monitor exporter to the existing openTelemetry builder.
            // 2. In its options callback, assign the supplied insightsConnectionString.
            //    Configure the existing builder; do not create a second telemetry pipeline.
            // Keep the supplied OTLP fallback for local Aspire.
            // Check: trigger a normal application request and find its trace in Application Insights.
            // /health and /alive are deliberately excluded from request tracing.
            // Optional API hint: docs/practice/README.md#s09-07-telemetry
            throw new NotImplementedException("S09-07: configure the production telemetry exporter.");
        }
        else if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            openTelemetry.UseOtlpExporter();
        }

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(
        this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services
            .AddHealthChecks()
            .AddCheck(
                "self",
                () => HealthCheckResult.Healthy(),
                ["live"]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(
        this WebApplication app)
    {
        app.MapHealthChecks(HealthEndpointPath);

        app.MapHealthChecks(
            AlivenessEndpointPath,
            new HealthCheckOptions
            {
                Predicate =
                    registration =>
                        registration.Tags.Contains("live")
            });

        return app;
    }
}
