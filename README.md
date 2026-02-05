# 🚀 Distributed Event-Driven Microservices Platform

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?style=for-the-badge&logo=docker&logoColor=white)](https://www.docker.com/)
[![RabbitMQ](https://img.shields.io/badge/RabbitMQ-FF6600?style=for-the-badge&logo=rabbitmq&logoColor=white)](https://www.rabbitmq.com/)
[![MySQL](https://img.shields.io/badge/MySQL-8.0-4479A1?style=for-the-badge&logo=mysql&logoColor=white)](https://www.mysql.com/)
[![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)](LICENSE)

A production-ready, event-driven microservices architecture demonstrating the Publisher/Subscriber pattern with RabbitMQ Fanout Exchange, containerized with Docker Compose.

---

## 📐 Architecture Overview

This project implements the **Fanout Exchange** pattern in RabbitMQ, enabling efficient one-to-many message distribution across multiple consumer services.

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                            SYSTEM ARCHITECTURE                              │
└─────────────────────────────────────────────────────────────────────────────┘

                              ┌───────────────┐
                              │   Client      │
                              │  (curl/HTTP)  │
                              └───────┬───────┘
                                      │ REST API
                                      ▼
                              ┌───────────────┐
                              │   Core.Api    │
                              │  (Publisher)  │
                              │   Port: 8080  │
                              └───────┬───────┘
                                      │ Publish Event
                                      ▼
                        ┌─────────────────────────┐
                        │       RabbitMQ          │
                        │    Fanout Exchange      │
                        │  ━━━━━━━━━━━━━━━━━━━━   │
                        │  "events.fanout"        │
                        └──────────┬──────────────┘
                                   │
                   ┌───────────────┴───────────────┐
                   │ Broadcast to ALL bound queues │
                   ▼                               ▼
        ┌──────────────────┐            ┌──────────────────┐
        │  Worker.Reports  │            │Worker.Persistence│
        │   (Subscriber)   │            │   (Subscriber)   │
        │                  │            │                  │
        │  ┌────────────┐  │            │  ┌────────────┐  │
        │  │ CSV Writer │  │            │  │   MySQL    │  │
        │  └─────┬──────┘  │            │  │   Writer   │  │
        └────────┼─────────┘            └───────┼────────┘
                 │                              │
                 ▼                              ▼
        ┌──────────────┐               ┌──────────────────┐
        │ ./reports/   │               │     MySQL 8.0    │
        │  data.csv    │               │   Database: appdb│
        └──────────────┘               └──────────────────┘
```

### How Fanout Exchange Works

| Concept | Description |
|---------|-------------|
| **Publisher** | `Core.Api` receives HTTP requests and publishes messages to the exchange |
| **Fanout Exchange** | Broadcasts every message to ALL bound queues (no routing keys) |
| **Subscribers** | Each worker service has its own queue and receives a copy of every message |
| **Decoupling** | Publishers don't know about subscribers; new consumers can be added without code changes |

---

## ⚡ Quick Start

### Prerequisites

- Docker Desktop (v20.10+)
- Docker Compose (v2.0+)
- curl (for testing)

### Launch the Entire Stack

```bash
# Clone the repository
git clone https://github.com/your-username/microservices-rabbitmq.git
cd microservices-rabbitmq

# Build and start all services
docker-compose up --build

# Or run in detached mode
docker-compose up --build -d
```

### Verify All Services Are Running

```bash
docker-compose ps
```

Expected output:

```
NAME                    STATUS          PORTS
core-api                Up              0.0.0.0:8080->8080/tcp
worker-reports          Up              
worker-persistence      Up              
rabbitmq                Up (healthy)    0.0.0.0:5672->5672/tcp, 0.0.0.0:15672->15672/tcp
mysql                   Up (healthy)    0.0.0.0:3306->3306/tcp
adminer                 Up              0.0.0.0:8085->8080/tcp
```

---

## 📊 Monitoring & Observability

### RabbitMQ Management Dashboard

The RabbitMQ Management UI provides real-time insights into your message broker.

| Property | Value |
|----------|-------|
| **URL** | [http://localhost:15672](http://localhost:15672) |
| **Username** | `guest` |
| **Password** | `guest` |

#### What You Can Monitor

| Tab | Insights |
|-----|----------|
| **Overview** | Message rates, connections, channels, global statistics |
| **Connections** | Active connections from Core.Api and Worker services |
| **Channels** | Open channels per connection, prefetch counts |
| **Exchanges** | View `events.fanout` exchange, message publish rates |
| **Queues** | Queue depth, consumer count, message acknowledgment rates |

#### Key Metrics to Watch

```
Exchanges Tab → events.fanout
├── Type: fanout
├── Message rate in: X msg/s
└── Bindings: Shows connected queues

Queues Tab
├── reports-queue (Worker.Reports)
│   ├── Ready: X messages
│   ├── Unacked: X messages
│   └── Consumers: 1
└── persistence-queue (Worker.Persistence)
    ├── Ready: X messages
    ├── Unacked: X messages
    └── Consumers: 1-N (scalable)
```

---

### Adminer (MySQL Database GUI)

Adminer provides a lightweight web interface for MySQL database management.

| Property | Value |
|----------|-------|
| **URL** | [http://localhost:8085](http://localhost:8085) |
| **System** | `MySQL` |
| **Server** | `mysql` |
| **Username** | `root` |
| **Password** | `rootpassword` |
| **Database** | `appdb` |

#### Login Steps

1. Open [http://localhost:8085](http://localhost:8085)
2. Select **MySQL** from the System dropdown
3. Enter the credentials as shown above
4. Click **Login**

#### Database Schema

```sql
-- Events table structure (auto-created by Worker.Persistence)
CREATE TABLE events (
    id INT AUTO_INCREMENT PRIMARY KEY,
    event_id VARCHAR(36) NOT NULL,
    event_type VARCHAR(100) NOT NULL,
    payload JSON NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_event_type (event_type),
    INDEX idx_created_at (created_at)
);
```

#### Useful SQL Queries

```sql
-- Count total persisted events
SELECT COUNT(*) as total_events FROM events;

-- View latest 10 events
SELECT * FROM events ORDER BY created_at DESC LIMIT 10;

-- Events per type
SELECT event_type, COUNT(*) as count 
FROM events 
GROUP BY event_type;

-- Events in the last hour
SELECT * FROM events 
WHERE created_at > NOW() - INTERVAL 1 HOUR;
```

---

### Container Logs

#### Stream All Service Logs

```bash
# Follow all logs in real-time
docker-compose logs -f

# Follow specific service logs
docker-compose logs -f core-api
docker-compose logs -f worker-reports
docker-compose logs -f worker-persistence
docker-compose logs -f rabbitmq
```

#### Log Output Examples

```bash
# Core.Api (Publisher)
core-api        | info: Core.Api[0] Event published: OrderCreated - ID: a1b2c3d4

# Worker.Reports (CSV Subscriber)
worker-reports  | info: Worker.Reports[0] Event received: OrderCreated
worker-reports  | info: Worker.Reports[0] Written to CSV: ./reports/data.csv

# Worker.Persistence (MySQL Subscriber)
worker-persistence | info: Worker.Persistence[0] Event received: OrderCreated
worker-persistence | info: Worker.Persistence[0] Persisted to MySQL: events table
```

---

### CSV Output (Worker.Reports)

The `Worker.Reports` service writes all received events to a CSV file.

| Property | Value |
|----------|-------|
| **Host Path** | `./reports/data.csv` |
| **Container Path** | `/app/reports/data.csv` |
| **Format** | CSV with headers |

#### View CSV Output

```bash
# Watch CSV file grow in real-time
tail -f ./reports/data.csv

# View entire CSV content
cat ./reports/data.csv

# Count processed events
wc -l ./reports/data.csv
```

#### CSV Structure

```csv
Timestamp,EventId,EventType,Payload
2024-01-15T10:30:00Z,a1b2c3d4-...,OrderCreated,"{""orderId"":123,""amount"":99.99}"
2024-01-15T10:30:01Z,e5f6g7h8-...,UserRegistered,"{""userId"":456,""email"":""test@example.com""}"
```

---

## 🔥 Load Testing & Horizontal Scaling

### Sending Test Events with curl

#### Single Event

```bash
curl -i -X POST http://localhost:8080/data \
  -H "Content-Type: application/json" \
  -d '{
    "Name": "Sensor_Alpha",
    "Value": "95.5"
  }'
```

#### Batch Load Test (100 Events)

```bash
# Bash loop for load testing
for i in {1..100}; do
  curl -s -X POST http://localhost:8080/data \
    -H "Content-Type: application/json" \
    -d "{\"Name\": \"LoadTest_$i\", \"Value\": \"$RANDOM\"}" &
done
wait
echo "Loadtest beendet."
```

#### High-Volume Load Test (1000 Events with xargs)

```bash
seq 1 1000 | xargs -P 50 -I {} curl -s -X POST http://localhost:8080/data \
  -H "Content-Type: application/json" \
  -d "{\"Name\": \"LoadTest_$i\", \"Value\": \"$RANDOM\"}"
```

---

### Horizontal Scaling

Scale the `Worker.Persistence` service to handle higher message throughput.

#### Scale to 3 Instances

```bash
docker-compose up -d --scale worker-persistence=3
```

#### Verify Scaled Instances

```bash
docker-compose ps | grep worker-persistence
```

Expected output:

```
worker-persistence-1    Up
worker-persistence-2    Up
worker-persistence-3    Up
```

#### Monitor Load Distribution in RabbitMQ

1. Open [http://localhost:15672/#/queues](http://localhost:15672/#/queues)
2. Click on `persistence-queue`
3. Observe **Consumers: 3** (one per scaled instance)
4. Watch **Consumer utilisation** to verify round-robin distribution

#### Scale Back Down

```bash
docker-compose up -d --scale worker-persistence=1
```

---

## 🛡️ Resilienz & Reliability

### Message Persistence

All messages are configured for durability to survive broker restarts.

| Feature | Implementation |
|---------|----------------|
| **Durable Exchange** | `events.fanout` exchange persists across RabbitMQ restarts |
| **Durable Queues** | All subscriber queues are durable |
| **Persistent Messages** | DeliveryMode set to `Persistent` (2) |
| **Publisher Confirms** | Enabled for guaranteed delivery acknowledgment |

### Auto-Recovery

The system implements automatic recovery mechanisms.

| Scenario | Recovery Behavior |
|----------|-------------------|
| **RabbitMQ Restart** | Services automatically reconnect with exponential backoff |
| **Network Partition** | Connection recovery with configurable retry intervals |
| **Consumer Crash** | Unacknowledged messages are requeued automatically |
| **MySQL Unavailable** | Worker.Persistence retries with circuit breaker pattern |

### Configuration Example

```csharp
// RabbitMQ connection with auto-recovery (in services)
var factory = new ConnectionFactory
{
    HostName = "rabbitmq",
    AutomaticRecoveryEnabled = true,
    NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
    RequestedHeartbeat = TimeSpan.FromSeconds(30)
};
```

---

## 📁 Project Structure

```
microservices-rabbitmq/
│
├── 📄 docker-compose.yml          # Orchestration configuration
├── 📄 .env                        # Environment variables
├── 📄 README.md                   # This documentation
│
├── 📂 src/
│   │
│   ├── 📂 Core.Api/               # REST API Publisher Service
│   │   ├── 📄 Dockerfile
│   │   ├── 📄 Core.Api.csproj
│   │   ├── 📄 Program.cs
│   │   ├── 📂 Controllers/
│   │   │   └── 📄 EventsController.cs
│   │   ├── 📂 Services/
│   │   │   └── 📄 RabbitMqPublisher.cs
│   │   └── 📂 Models/
│   │       └── 📄 EventMessage.cs
│   │
│   ├── 📂 Worker.Reports/         # CSV Writer Subscriber
│   │   ├── 📄 Dockerfile
│   │   ├── 📄 Worker.Reports.csproj
│   │   ├── 📄 Program.cs
│   │   └── 📂 Services/
│   │       ├── 📄 RabbitMqConsumer.cs
│   │       └── 📄 CsvWriterService.cs
│   │
│   ├── 📂 Worker.Persistence/     # MySQL Writer Subscriber
│   │   ├── 📄 Dockerfile
│   │   ├── 📄 Worker.Persistence.csproj
│   │   ├── 📄 Program.cs
│   │   ├── 📂 Services/
│   │   │   ├── 📄 RabbitMqConsumer.cs
│   │   │   └── 📄 DatabaseService.cs
│   │   └── 📂 Data/
│   │       └── 📄 AppDbContext.cs
│   │
│   └── 📂 Shared/                 # Shared libraries
│       ├── 📄 Shared.csproj
│       └── 📂 Models/
│           └── 📄 EventMessage.cs
│
├── 📂 reports/                    # CSV output directory (mounted volume)
│   └── 📄 data.csv
│
├── 📂 infrastructure/
│   ├── 📂 rabbitmq/
│   │   └── 📄 definitions.json    # Exchange/Queue pre-configuration
│   └── 📂 mysql/
│       └── 📄 init.sql            # Database initialization script
│
└── 📂 tests/
    ├── 📂 Core.Api.Tests/
    ├── 📂 Worker.Reports.Tests/
    └── 📂 Worker.Persistence.Tests/
```

---

## 🔧 Configuration Reference

### Environment Variables

| Variable | Service | Default | Description |
|----------|---------|---------|-------------|
| `RABBITMQ_HOST` | All | `rabbitmq` | RabbitMQ hostname |
| `RABBITMQ_PORT` | All | `5672` | AMQP port |
| `RABBITMQ_USER` | All | `guest` | RabbitMQ username |
| `RABBITMQ_PASS` | All | `guest` | RabbitMQ password |
| `MYSQL_HOST` | Worker.Persistence | `mysql` | MySQL hostname |
| `MYSQL_DATABASE` | Worker.Persistence | `appdb` | Database name |
| `MYSQL_USER` | Worker.Persistence | `root` | MySQL username |
| `MYSQL_PASSWORD` | Worker.Persistence | `rootpassword` | MySQL password |
| `CSV_OUTPUT_PATH` | Worker.Reports | `/app/reports` | CSV output directory |

### Exposed Ports

| Service | Port | Protocol | Purpose |
|---------|------|----------|---------|
| Core.Api | 8080 | HTTP | REST API endpoint |
| RabbitMQ | 5672 | AMQP | Message broker |
| RabbitMQ | 15672 | HTTP | Management UI |
| MySQL | 3306 | TCP | Database |
| Adminer | 8085 | HTTP | Database GUI |

---

## 🛑 Stopping the Stack

```bash
# Stop all services
docker-compose down

# Stop and remove volumes (clean slate)
docker-compose down -v

# Stop, remove volumes, and remove images
docker-compose down -v --rmi all
```

---

## 📜 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

<p align="center">
  <strong>Built with ❤️ using .NET 8, RabbitMQ, and Docker</strong>
</p>