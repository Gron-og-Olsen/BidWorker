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

namespace BidWorker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly string _rabbitHost;
        private readonly string _queueName = "bidsQueue"; // Køen, der modtager budbeskeder
        private static List<Bid> Bids = new List<Bid>(); // Simuleret lagring af bud i hukommelsen

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _rabbitHost = configuration["RabbitHost"] ?? "localhost"; // Hent RabbitHost fra appsettings.json eller brug standard localhost
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"Connecting to RabbitMQ at {_rabbitHost}");
            var factory = new ConnectionFactory() { HostName = _rabbitHost };

            // Opret forbindelse til RabbitMQ og kanal
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                // Deklarer køen, hvis den ikke allerede eksisterer
                channel.QueueDeclare(queue: _queueName,
                                     durable: false,  // Køen vil ikke overleve server genstart
                                     exclusive: false, // Køen er tilgængelig for andre forbindelser
                                     autoDelete: false,  // Køen slettes ikke, når den ikke bruges
                                     arguments: null);

                var consumer = new EventingBasicConsumer(channel);

                // Når en besked modtages, behandles den her
                consumer.Received += (model, ea) =>
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
                    }
                    else
                    {
                        // Hvis buddet ikke er gyldigt
                        _logger.LogWarning("Invalid bid message received.");
                    }
                };

                // Begynd at lytte på køen, auto-acknowledge (bekræftelse) sker automatisk
                channel.BasicConsume(queue: _queueName, autoAck: true, consumer: consumer);

                // Kør, indtil applikationen stoppes
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }

        // Tilføj et bud til listen i hukommelsen
        private void AddBid(Bid bid)
        {
            lock (Bids)
            {
                bid.Id = Bids.Count + 1;  // Simpel måde at give buddet et ID
                bid.Timestamp = DateTime.UtcNow;
                Bids.Add(bid);  // Tilføj til den in-memory liste
            }
        }

        // Metoder til at hente alle bud
        public List<Bid> GetAllBids()
        {
            lock (Bids)
            {
                return new List<Bid>(Bids);
            }
        }

        // Hent bud baseret på auktionens ID
        public List<Bid> GetBidsByAuctionId(int auctionId)
        {
            lock (Bids)
            {
                return Bids.Where(b => b.AuctionId == auctionId).ToList();
            }
        }

        // Hent et specifikt bud baseret på ID
        public Bid GetBidById(int id)
        {
            lock (Bids)
            {
                return Bids.FirstOrDefault(b => b.Id == id);
            }
        }
    }
}
