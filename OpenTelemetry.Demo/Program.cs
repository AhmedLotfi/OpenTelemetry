using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenTelemetry()
    .WithTracing(b =>
    {
        b.AddZipkinExporter(o =>
        {
            o.Endpoint = new Uri(builder.Configuration.GetValue<string>("zipkin:url")!);
        })
        .AddSource(DiagnosticsConfig.ActivitySource.Name)
        .ConfigureResource(resource => resource.AddService(DiagnosticsConfig.ServiceName))
        .AddAspNetCoreInstrumentation();
    });

/*   --------------ConsoleExporter
builder.Services.AddOpenTelemetry()
  .WithMetrics(b => b.AddPrometheusExporter()); */

/*   --------------ConsoleExporter
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
        tracerProviderBuilder
            .AddSource(DiagnosticsConfig.ActivitySource.Name)
            .ConfigureResource(resource => resource.AddService(DiagnosticsConfig.ServiceName))
            .AddAspNetCoreInstrumentation()
            .AddConsoleExporter())
    .WithMetrics(metricsProviderBuilder =>
        metricsProviderBuilder
            .ConfigureResource(resource => resource
                .AddService(DiagnosticsConfig.ServiceName))
            .AddAspNetCoreInstrumentation()
            .AddConsoleExporter());*/



WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

string[] summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    WeatherForecast[] forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();

    using Activity? activity = DiagnosticsConfig.ActivitySource.StartActivity("weatherforecast triggered");
    activity?.SetTag("response", forecast);
    activity?.SetTag("rnd", forecast.First().TemperatureF);

    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

await app.RunAsync();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}


public static class DiagnosticsConfig
{
    public const string ServiceName = "MyService";
    public static ActivitySource ActivitySource = new ActivitySource(ServiceName);
}