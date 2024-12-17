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

    // Hent MongoDB connection string og database navn fra miljøvariabler
    var mongoConnectionString = configuration["MONGO_CONNECTION_STRING"];
    var mongoDatabaseName = configuration["BidDatabaseName"];
    var mongoCollectionName = configuration["BidCollectionName"];

    if (string.IsNullOrEmpty(mongoConnectionString) || string.IsNullOrEmpty(mongoDatabaseName))
    {
        throw new Exception("MongoDB connection string or database name is not set in the environment variables.");
    }


    // Opret MongoDB-klient og database
    var mongoClient = new MongoClient(mongoConnectionString);
    var database = mongoClient.GetDatabase(mongoDatabaseName);

    // Hent collection fra databasen
    var collection = database.GetCollection<Bid>(mongoCollectionName);

    // Registrer MongoDB-database som singleton
    services.AddSingleton(database);

    services.AddSingleton(collection);


    // RabbitMQ hostname fra miljøvariabel
    var rabbitHostName = configuration["RabbitHost"] ?? "localhost";

    var rabbitConnectionFactory = new ConnectionFactory
    {
        HostName = rabbitHostName
    };

    // Registrer RabbitMQ-forbindelse som singleton
    services.AddSingleton(rabbitConnectionFactory);

    // Tilføj Worker som en HostedService
    services.AddHostedService<Worker>();
});

var app = builder.Build();

// Start applikationen og kør worker-tjenesten
app.Run();
