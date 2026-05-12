# Finance Portfolio API

A microservices-based **stock market tracking and sentiment analysis platform** built with **.NET 10**.

**Framework**: .NET 10  
**Architecture**: Microservices — YARP Gateway, 6 independent services  
**Frontend**: [Finance.Portfolio.Web](https://github.com/akshayborkarpro/Finance.Portfolio.Web) (Angular 19)  
**License**: MIT

---

## Architecture

```
Angular Frontend
      │
      ▼
 API Gateway (YARP) :5000
      │
      ├── /api/auth/**          → Identity Service      :5001
      ├── /api/portfolio/**     → Portfolio Service     :5002
      ├── /api/stocksector/**   → Portfolio Service     :5002
      ├── /api/marketdata/**    → Market Data Service   :5003
      ├── /api/alerts/**        → Alert Service         :5004
      ├── /api/sentiment/**     → Sentiment Service     :5005
      └── /hubs/**              → Notification Service  :5006
```

### Services

| Service | Responsibility | Database |
|---------|---------------|----------|
| **Identity** | User registration, JWT issuance | `IdentityDB` (SQL Server) |
| **Portfolio** | Stocks, sectors, investments | `PortfolioDB` (SQL Server) |
| **Market Data** | Real-time prices, Hangfire jobs, OHLCV | Redis |
| **Alert** | Price alert rules, trigger evaluation | `AlertDB` (SQL Server) |
| **Sentiment** | Yahoo Finance RSS + ML.NET inference | None |
| **Notification** | SignalR hub, SendGrid email | Redis (SignalR backplane) |

### Messaging

- **gRPC**: Portfolio → Market Data (`GetCurrentPrice`, `GetOhlcv`)
- **RabbitMQ / MassTransit**: async events between services

| Publisher | Event | Consumer(s) |
|-----------|-------|-------------|
| Portfolio | `StockAdded` | Market Data |
| Portfolio | `StockRemoved` | Market Data |
| Market Data | `StockPriceUpdated` | Alert, Notification |
| Alert | `AlertTriggered` | Notification |

---

## Running Locally

### Option A — Full Docker stack

```bash
cp .env.example .env          # fill in secrets
docker compose up --build
```

Access:
- Gateway: `http://localhost:5000`
- Frontend: `http://localhost:4200`
- RabbitMQ UI: `http://localhost:15672`

### Option B — Infrastructure via Docker, services via dotnet run

```bash
# Start only SQL Server, Redis, RabbitMQ
docker compose -f docker-compose.dev.yml up -d

# Run each service in separate terminals
dotnet run --project services/Gateway/Finance.Gateway
dotnet run --project services/Identity/Finance.IdentityService.API
dotnet run --project services/Portfolio/Finance.PortfolioService.API
dotnet run --project services/MarketData/Finance.MarketDataService.API
dotnet run --project services/Alert/Finance.AlertService.API
dotnet run --project services/Sentiment/Finance.SentimentService.API
dotnet run --project services/Notification/Finance.NotificationService.API

# Run frontend
cd ../Finance.Portfolio.Web
ng serve    # proxies /api and /hubs to http://localhost:5000
```

### EF Migrations

```bash
# Identity
dotnet ef database update --project services/Identity/Finance.IdentityService.Persistence \
  --startup-project services/Identity/Finance.IdentityService.API

# Portfolio
dotnet ef database update --project services/Portfolio/Finance.PortfolioService.Persistence \
  --startup-project services/Portfolio/Finance.PortfolioService.API

# Alert
dotnet ef database update --project services/Alert/Finance.AlertService.Persistence \
  --startup-project services/Alert/Finance.AlertService.API
```

---

## Project Structure

```
Finance.Portfolio.API/
├── shared/
│   ├── Finance.Contracts/           # MassTransit event records
│   └── Finance.SharedKernel.Auth/   # Shared JWT validation extension
├── services/
│   ├── Gateway/                     # YARP reverse proxy
│   ├── Identity/                    # Auth — 5 projects (Clean Architecture)
│   ├── Portfolio/                   # Portfolio — 5 projects
│   ├── MarketData/                  # Market data — 3 projects
│   ├── Alert/                       # Alerts — 4 projects
│   ├── Sentiment/                   # Sentiment — 2 projects
│   └── Notification/                # Notifications — 2 projects
├── docker-compose.yml               # Full stack
├── docker-compose.override.yml      # Dev port overrides
├── docker-compose.dev.yml           # Infrastructure only (local dev)
└── Finance.Portfolio.Solution.slnx
```

---

## Environment Variables

Copy `.env.example` to `.env` and fill in values. See `.env.example` for all required variables.

Key variables:

| Variable | Purpose |
|----------|---------|
| `JWT_SECRET` | Shared signing key across all services (min 32 chars) |
| `SQL_PASSWORD` | SQL Server SA password |
| `RABBITMQ_USERNAME/PASSWORD` | RabbitMQ credentials |
| `SENDGRID_API_KEY` | SendGrid email delivery |
