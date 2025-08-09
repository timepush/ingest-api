using System.Text.Json;
using FluentValidation;
using KafkaFlow;
using Npgsql;
using Timepush.Ingest.Exceptions;
using Timepush.Ingest.Lib;
using Timepush.Ingest.Middlewares;
using KafkaFlow.OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;

// TODO:
// Handle logging 
// Performance tweakings

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureApp();

builder.Services.AddExceptionHandlers();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddServerTiming();

builder.ConfigureObservability();

builder.Services.AddPostgres();
builder.Services.AddRedis();
builder.Services.AddKafkaProducer(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseMiddleware<RequestTimingMiddleware>();
app.UseServerTiming();
app.MapIngestEndpoints();

await app.RunAsync();
