using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using System;
using NLog;
using NLog.Web;

namespace BidWorker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly string _rabbitHost;
        private readonly string _queueName = "bidsQueue"; // Køen, der modtager budbeskeder
        private static List<Bid> Bids = new List<Bid>(); // Simuleret lagring af bud i hukommelsen
        private readonly IMongoCollection<Bid> _bidCollection;
        private readonly IModel _channel;

        public Worker(ILogger<Worker> logger, IConfiguration configuration, IMongoDatabase mongoDatabase, ConnectionFactory rabbitConnectionFactory)
        {
            _logger = logger;
            _rabbitHost = configuration["RabbitHost"] ?? "rabbitmq"; // Hent RabbitHost fra appsettings.json eller brug standard localhost
            _bidCollection = mongoDatabase.GetCollection<Bid>("BidCollection"); // Tilslut til den rigtige samling i MongoDB

            // Opret forbindelse til RabbitMQ
            var connection = rabbitConnectionFactory.CreateConnection();
            _channel = connection.CreateModel();

            // Deklarer køen, hvis den ikke allerede eksisterer
            _channel.QueueDeclare(queue: _queueName,
                                 durable: false,  // Køen vil ikke overleve server genstart
                                 exclusive: false, // Køen er tilgængelig for andre forbindelser
                                 autoDelete: false,  // Køen slettes ikke, når den ikke bruges
                                 arguments: null);

            _logger.LogInformation($"Worker initialized and connected to RabbitMQ at {_rabbitHost}");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"Worker started at {DateTime.UtcNow}");

            var consumer = new EventingBasicConsumer(_channel);

            // Når en besked modtages, behandles den her
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                // Deserialiser buddet fra JSON
                var bid = JsonSerializer.Deserialize<Bid>(message);

                if (bid != null)
                {
                    // Tilføj buddet til listen (i hukommelsen)
                    AddBid(bid);
                    _logger.LogInformation($"Received bid: {bid.BidderName}, AuctionId: {bid.AuctionId}, Amount: {bid.Amount}");

                    // Muligvis gemme bud i MongoDB også, hvis ønsket
                    await AddBidAsync(bid);
                }
                else
                {
                    // Hvis buddet ikke er gyldigt
                    _logger.LogWarning("Invalid bid message received.");
                }
            };

            // Begynd at lytte på køen, auto-acknowledge (bekræftelse) sker automatisk
            _channel.BasicConsume(queue: _queueName, autoAck: true, consumer: consumer);

            // Kør, indtil applikationen stoppes
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }

            _logger.LogInformation($"Worker stopped at {DateTime.UtcNow}");
        }

        // Tilføj et bud til listen i hukommelsen
        private void AddBid(Bid bid)
        {
            lock (Bids)
            {
                bid.Id = Guid.NewGuid();  // Brug Guid.NewGuid() til at generere et unikt ID
                bid.Timestamp = DateTime.UtcNow;
                Bids.Add(bid);  // Tilføj til den in-memory liste
                _logger.LogInformation($"Bid added to in-memory list: {bid.BidderName}, AuctionId: {bid.AuctionId}");
            }
        }

        // Asynkron metode til at tilføje bud til MongoDB
        private async Task AddBidAsync(Bid bid)
        {
            bid.Timestamp = DateTime.UtcNow;
            await _bidCollection.InsertOneAsync(bid);
            _logger.LogInformation($"Bid saved to MongoDB: {bid.BidderName}, AuctionId: {bid.AuctionId}");
        }

        // Asynkron metode til at hente alle bud fra MongoDB
        public async Task<List<Bid>> GetAllBidsAsync()
        {
            _logger.LogInformation("Fetching all bids from MongoDB.");
            return await _bidCollection.Find(Builders<Bid>.Filter.Empty).ToListAsync();
        }

        // Asynkron metode til at hente bud baseret på auktionens ID
        public async Task<List<Bid>> GetBidsByAuctionIdAsync(Guid auctionId)
        {
            _logger.LogInformation($"Fetching bids for AuctionId: {auctionId} from MongoDB.");
            var filter = Builders<Bid>.Filter.Eq(b => b.AuctionId, auctionId);
            return await _bidCollection.Find(filter).ToListAsync();
        }

        // Asynkron metode til at hente et specifikt bud baseret på ID
        public async Task<Bid> GetBidByIdAsync(Guid id)
        {
            _logger.LogInformation($"Fetching bid with Id: {id} from MongoDB.");
            var filter = Builders<Bid>.Filter.Eq(b => b.Id, id);
            return await _bidCollection.Find(filter).FirstOrDefaultAsync();
        }

        // Asynkron metode til at slette bud baseret på ID
        public async Task DeleteBidByIdAsync(Guid id)
        {
            _logger.LogInformation($"Deleting bid with Id: {id} from MongoDB.");
            var filter = Builders<Bid>.Filter.Eq(b => b.Id, id);
            await _bidCollection.DeleteOneAsync(filter);
        }

        // Metoder til at hente alle bud fra hukommelsen
        public List<Bid> GetAllBids()
        {
            lock (Bids)
            {
                _logger.LogInformation("Fetching all bids from in-memory list.");
                return new List<Bid>(Bids);
            }
        }

        // Hent bud baseret på auktionens ID fra hukommelsen
        public List<Bid> GetBidsByAuctionId(Guid auctionId)
        {
            lock (Bids)
            {
                _logger.LogInformation($"Fetching bids for AuctionId: {auctionId} from in-memory list.");
                return Bids.Where(b => b.AuctionId.Equals(auctionId)).ToList();  // Brug Equals i stedet for ==
            }
        }

        // Hent et specifikt bud baseret på ID fra hukommelsen
        public Bid GetBidById(Guid id)
        {
            lock (Bids)
            {
                _logger.LogInformation($"Fetching bid with Id: {id} from in-memory list.");
                return Bids.FirstOrDefault(b => b.Id.Equals(id));  // Brug Equals i stedet for ==
            }
        }
    }
}
