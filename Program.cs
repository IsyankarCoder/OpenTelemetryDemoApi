using Microsoft.AspNetCore.Http.HttpResults;
using System.Text.Json.Serialization;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using System.Diagnostics.Metrics;


namespace OpenTelemetryDemoApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateSlimBuilder(args);

            builder.Services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
            });

            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            builder.Services.AddOpenTelemetry()
                   .ConfigureResource(resource => resource.AddService("OtelDemoApi"))
                   .WithMetrics(metrics =>
                   {
                       metrics
                        .AddMeter("OtelDemoApi.IsMetrikleri")
                        .AddAspNetCoreInstrumentation() // HTTP istek süreleri ve sayıları
                        .AddRuntimeInstrumentation() // CPU, RAM, Garbage Collection metrikleri
                        .AddOtlpExporter(otlOptions =>
                        {
                            // Verileri dışa port açmadan Collector'a gönderiyoruz
                            otlOptions.Endpoint = new Uri("http://localhost:4317");
                        });
                   });
                   

            var app = builder.Build();

            // 1. Özel bir Meter (Sayaç Grubu) ve Counter tanımlayın
            var myMeter = new Meter("OtelDemoApi.IsMetrikleri", "1.0.0");
            var testCounter = myMeter.CreateCounter<int>("volkan_tolkan_istek_sayisi", description: "Test endpointine gelen toplam istek");
            app.MapGet("/test", () =>
            {
                testCounter.Add(1, new KeyValuePair<string, object?>("kullanici_tipi", "admin"));
                return Results.Ok(new { Mesaj = "Metrik uretildi!", Zaman = DateTime.Now });
            });

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

          

            Todo[] sampleTodos =
            [
                new(1, "Walk the dog"),
                new(2, "Do the dishes", DateOnly.FromDateTime(DateTime.Now)),
                new(3, "Do the laundry", DateOnly.FromDateTime(DateTime.Now.AddDays(1))),
                new(4, "Clean the bathroom"),
                new(5, "Clean the car", DateOnly.FromDateTime(DateTime.Now.AddDays(2)))
            ];

            var todosApi = app.MapGroup("/todos");
            todosApi.MapGet("/", () => sampleTodos)
                    .WithName("GetTodos");

            todosApi.MapGet("/{id}", Results<Ok<Todo>, NotFound> (int id) =>
                sampleTodos.FirstOrDefault(a => a.Id == id) is { } todo
                    ? TypedResults.Ok(todo)
                    : TypedResults.NotFound())
                .WithName("GetTodoById");

            app.Run();
        }
    }

    public record Todo(int Id, string? Title, DateOnly? DueBy = null, bool IsComplete = false);

    [JsonSerializable(typeof(Todo[]))]
    internal partial class AppJsonSerializerContext : JsonSerializerContext
    {

    }
}
