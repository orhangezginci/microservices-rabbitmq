using System.Text;
using System.Text.Json; // Neu hinzugefügt
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

Console.WriteLine("=== Worker.Reports (Echtes CSV) startet ===");

var rabbitHostName = Environment.GetEnvironmentVariable("MessageBroker__HostName") ?? "localhost";
var exchangeName = "transformation_events";
var queueName = "reports_queue";
var csvPath = "/app/reports/data.csv";

// CSV-Header schreiben, falls Datei neu ist
if (!File.Exists(csvPath))
{
    File.WriteAllText(csvPath, "Zeitstempel;Name;Wert" + Environment.NewLine);
}

var factory = new ConnectionFactory { HostName = rabbitHostName };
IConnection connection = null;
while (connection == null)
{
    try { connection = factory.CreateConnection(); }
    catch { Thread.Sleep(2000); }
}

using var channel = connection.CreateModel();
channel.ExchangeDeclare(exchange: exchangeName, type: ExchangeType.Fanout, durable: true);
channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false);
channel.QueueBind(queue: queueName, exchange: exchangeName, routingKey: "");

var consumer = new EventingBasicConsumer(channel);
consumer.Received += (model, ea) =>
{
    var body = ea.Body.ToArray();
    var jsonMessage = Encoding.UTF8.GetString(body);
    
    try 
    {
        // JSON in Objekt umwandeln
        var data = JsonSerializer.Deserialize<DataEvent>(jsonMessage);
        
        if (data != null)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            
            // Format für Excel: Semikolon-getrennt
            var csvLine = $"{timestamp};{data.Name};{data.Value}{Environment.NewLine}";
            
            File.AppendAllText(csvPath, csvLine);
            Console.WriteLine($"[CSV] Gespeichert: {data.Name} = {data.Value}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Fehler beim Verarbeiten: {ex.Message}");
    }

    channel.BasicAck(ea.DeliveryTag, false);
};

channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
new ManualResetEvent(false).WaitOne();

// Das Model muss mit dem der API übereinstimmen (Case-Insensitive)
public record DataEvent(string Name, string Value);