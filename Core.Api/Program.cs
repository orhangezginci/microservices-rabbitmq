using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

// Konfiguration laden
var rabbitHostName = builder.Configuration["MessageBroker:HostName"] ?? "localhost";
var exchangeName = "transformation_events";

// RabbitMQ Verbindung
builder.Services.AddSingleton<IConnection>(sp => {
    var factory = new ConnectionFactory { HostName = rabbitHostName };
    return factory.CreateConnection();
});

// Kanal registrieren
builder.Services.AddSingleton<IModel>(sp => {
    var connection = sp.GetRequiredService<IConnection>();
    var channel = connection.CreateModel();
    channel.ExchangeDeclare(exchange: exchangeName, type: ExchangeType.Fanout, durable: true);
    return channel;
});

var app = builder.Build();

// Der POST Endpoint
app.MapPost("/data", (DataEvent data, IModel channel) => {
    var message = JsonSerializer.Serialize(data);
    var body = Encoding.UTF8.GetBytes(message);

    channel.BasicPublish(
        exchange: exchangeName,
        routingKey: "", 
        basicProperties: null,
        body: body);

    return Results.Ok(new { status = "Gesendet", data });
});

app.Run();

// WICHTIG: Das Record MUSS ganz unten stehen!
record DataEvent(string Name, string Value);