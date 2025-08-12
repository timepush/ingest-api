
using Timepush.IngestApi.Configurations;
using Timepush.IngestApi.Errors;
using Timepush.IngestApi.Features.Ingest;
using Timepush.IngestApi.Lib;

//TODO: 
// Add Rate limit
// Add response for Accepted

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddExceptionHandlers();
builder.ConfigureKestrelServer();
builder.ConfigureOptions();
builder.ConfigurePostgres();
builder.ConfigureRedis();
builder.ConfigureKafka();
builder.ConfigureIngestEndpoints();



var app = builder.Build();
app.UseExceptionHandler();
app.MapIngestEndpoints();
app.MapNotFound();


await app.RunAsync();
