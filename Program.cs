using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using RabbitMQ.Client;
using BidWorker;

var builder = Host.CreateDefaultBuilder(args);

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

var app = builder.Build();

// Start applikationen og kør worker-tjenesten
app.Run();
