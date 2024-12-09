using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using BidWorker;
using Microsoft.Extensions.Logging;

var builder = Host.CreateDefaultBuilder(args);

// Konfigurer service container og baggrundstjenester
builder.ConfigureServices((hostContext, services) =>
{
    // Tilføj Worker som en HostedService
    services.AddHostedService<Worker>();
});

var app = builder.Build();

// Start applikationen og kør worker-tjenesten
await app.RunAsync();
