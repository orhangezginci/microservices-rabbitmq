using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using MySqlConnector;

Console.WriteLine("=== Worker.Persistence (MySQL) startet ===");

var rabbitHostName = Environment.GetEnvironmentVariable("MessageBroker__HostName") ?? "localhost";
var dbConnString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
var exchangeName = "transformation_events";
var queueName = "persistence_queue";

// 1. Auf Datenbank warten und Tabelle erstellen
await SetupDatabase(dbConnString);

// 2. RabbitMQ Verbindung aufbauen
var factory = new ConnectionFactory { HostName = rabbitHostName };
IConnection connection = null;
while (connection == null)
{
    try {
        connection = factory.CreateConnection();
    }
    catch {
        Console.WriteLine("Warte auf RabbitMQ...");
        Thread.Sleep(2000);
    }
}

using var channel = connection.CreateModel();

// 3. Infrastruktur deklarieren
channel.ExchangeDeclare(exchange: exchangeName, type: ExchangeType.Fanout, durable: true);
channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false);
channel.QueueBind(queue: queueName, exchange: exchangeName, routingKey: "");

Console.WriteLine($"[*] Warte auf Nachrichten in {queueName}...");

var consumer = new EventingBasicConsumer(channel);
consumer.Received += async (model, ea) =>
{
    var body = ea.Body.ToArray();
    var jsonMessage = Encoding.UTF8.GetString(body);
    
    try 
    {
        var data = JsonSerializer.Deserialize<DataEvent>(jsonMessage);
        if (data != null)
        {
            using var conn = new MySqlConnection(dbConnString);
            await conn.OpenAsync();
            
            var sql = "INSERT INTO events (name, value) VALUES (@name, @value)";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@name", data.Name);
            cmd.Parameters.AddWithValue("@value", data.Value);
            
            await cmd.ExecuteNonQueryAsync();
            Console.WriteLine($"[DB] Gespeichert: {data.Name} = {data.Value}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"DB Fehler: {ex.Message}");
    }

    channel.BasicAck(ea.DeliveryTag, false);
};

channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);

var exitEvent = new ManualResetEvent(false);
Console.CancelKeyPress += (s, e) => exitEvent.Set();
exitEvent.WaitOne();

// Methode zur Tabellen-Erstellung
async Task SetupDatabase(string connString)
{
    bool connected = false;
    while (!connected)
    {
        try {
            using var conn = new MySqlConnection(connString);
            await conn.OpenAsync();
            var sql = "CREATE TABLE IF NOT EXISTS events (id INT AUTO_INCREMENT PRIMARY KEY, name VARCHAR(255), value VARCHAR(255), created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP);";
            using var cmd = new MySqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
            connected = true;
            Console.WriteLine("MySQL Datenbank ist bereit.");
        }
        catch {
            Console.WriteLine("Warte auf MySQL...");
            Thread.Sleep(2000);
        }
    }
}

public record DataEvent(string Name, string Value);