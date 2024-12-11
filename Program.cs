using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using RabbitMQ.Client;
using BidWorker;
using NLog;
using NLog.Web;
using NLog.Extensions.Logging; // Tilføj denne reference til NLog.Extensions.Logging

var builder = Host.CreateDefaultBuilder(args);

// Konfigurer NLog
builder.ConfigureLogging((context, logging) =>
{
    // Brug NLog som logger
    logging.ClearProviders();
    logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace); // Logniveauet, du ønsker
    logging.AddNLog(); // Brug NLog
});

// Konfigurer service container og baggrundstjenester
builder.ConfigureServices((hostContext, services) =>
{
    // Oprindelig konfiguration
    var configuration = hostContext.Configuration;

    // Konfigurer MongoDB-forbindelsen
    var mongoConnectionString = "mongodb+srv://Admin:Password@auktionshuscluster.koi2w.mongodb.net/";
    var mongoDatabaseName = "BidDB"; // Din faste database

    // Opret MongoDB-klient og database
    var mongoClient = new MongoClient(mongoConnectionString);
    var database = mongoClient.GetDatabase(mongoDatabaseName);

    // Registrer MongoDB-database som singleton
    services.AddSingleton(database);

    // Konfigurer RabbitMQ-forbindelsen
    var rabbitConnectionFactory = new ConnectionFactory
    {
        HostName = "rabbitmq"  // Hardkode RabbitMQ hostname
    };

    // Registrer RabbitMQ-forbindelse som singleton
    services.AddSingleton(rabbitConnectionFactory);

    // Tilføj Worker som en HostedService
    services.AddHostedService<Worker>();
});

// Byg og start applikationen
var app = builder.Build();

// Start applikationen og kør worker-tjenesten
app.Run();
