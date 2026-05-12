# Finance Portfolio — Microservices Migration Plan

> **Status:** In Progress  
> **Started:** 2026-05-12  
> **Architecture:** Strangler Fig — monolith stays live, one service extracted per phase  

---

## Target Architecture

```
                        ┌─────────────────────────────────────────────┐
                        │              API Gateway (YARP)              │
                        │  JWT validation · route forwarding · CORS    │
                        └────────┬─────────┬───────┬───────┬──────────┘
                                 │         │       │       │
              ┌──────────────────┘   ┌─────┘  ┌───┘  ┌────┘
              ▼                      ▼        ▼      ▼
     ┌─────────────────┐  ┌──────────────┐  ┌────────────────┐  ┌──────────────────┐
     │ Identity Service│  │  Portfolio   │  │  Alert Service │  │Sentiment Service │
     │  /api/auth/**   │  │   Service    │  │  /api/alerts/**│  │/api/sentiment/** │
     │  IdentityDB     │  │/api/portfolio│  │  AlertDB        │  │  (stateless)     │
     └─────────────────┘  │/api/stocksect│  └───────┬────────┘  └──────────────────┘
                          │  PortfolioDB │          │
                          └──────┬───────┘          │ AlertTriggered
                                 │ StockAdded        │
                                 │ StockRemoved      ▼
                                 ▼          ┌─────────────────────┐
                        ┌────────────────┐  │ Notification Service│
                        │  Market Data   │  │  /hubs/stockprice   │
                        │    Service     │──│  SignalR + SendGrid  │
                        │  Redis + Jobs  │  └─────────────────────┘
                        │  gRPC server   │
                        └────────────────┘

Message Bus: MassTransit + RabbitMQ
Shared Cache: Redis
```

---

## Services Summary

| # | Service | Transport | DB | Est. Days |
|---|---|---|---|---|
| 0 | Shared projects (Contracts, Auth) | — | — | 0.5 |
| 1 | API Gateway (YARP) | HTTP/WS | None | 0.5 |
| 2 | Identity Service | HTTP | IdentityDB | 2 |
| 3 | Sentiment Service | HTTP | None | 1.5 |
| 4 | Market Data Service | gRPC + Events | Redis | 3.5 |
| 5 | Portfolio Service | HTTP + Events + gRPC | PortfolioDB | 4.5 |
| 6 | Alert Service | HTTP + Events | AlertDB | 3.5 |
| 7 | Notification Service | Events + SignalR | None | 2.5 |
| 8 | Decommission Monolith | — | — | 1 |
| **Total** | | | | **~19 days** |

---

## Solution Structure (Target)

```
Finance.Portfolio.Solution/
├── shared/
│   ├── Finance.Contracts/                  ← MassTransit event records
│   └── Finance.SharedKernel.Auth/          ← Shared JWT AddAuthentication extension
├── services/
│   ├── Identity/
│   │   ├── Finance.IdentityService.Domain/
│   │   ├── Finance.IdentityService.Application/
│   │   ├── Finance.IdentityService.Persistence/
│   │   ├── Finance.IdentityService.Infrastructure/
│   │   └── Finance.IdentityService.API/
│   ├── Portfolio/
│   │   ├── Finance.PortfolioService.Domain/
│   │   ├── Finance.PortfolioService.Application/
│   │   ├── Finance.PortfolioService.Persistence/
│   │   ├── Finance.PortfolioService.Infrastructure/
│   │   └── Finance.PortfolioService.API/
│   ├── MarketData/
│   │   ├── Finance.MarketDataService.Application/
│   │   ├── Finance.MarketDataService.Infrastructure/
│   │   └── Finance.MarketDataService.API/
│   ├── Alert/
│   │   ├── Finance.AlertService.Domain/
│   │   ├── Finance.AlertService.Application/
│   │   ├── Finance.AlertService.Persistence/
│   │   └── Finance.AlertService.API/
│   ├── Sentiment/
│   │   ├── Finance.SentimentService.Infrastructure/
│   │   └── Finance.SentimentService.API/
│   ├── Notification/
│   │   ├── Finance.NotificationService.Infrastructure/
│   │   └── Finance.NotificationService.API/
│   └── Gateway/
│       └── Finance.Gateway/
├── .env                                    ← gitignored — JWT_SECRET, SENDGRID keys
├── docker-compose.yml
├── docker-compose.override.yml
└── Finance.Portfolio.Solution.sln
```

---

## Communication Contracts

### gRPC (synchronous, latency-sensitive)
Portfolio Service → Market Data Service
```proto
service MarketDataService {
  rpc GetCurrentPrice (GetPriceRequest) returns (GetPriceResponse);
  rpc GetOhlcv (GetOhlcvRequest) returns (GetOhlcvResponse);
}
```

### MassTransit Events (asynchronous)
Defined in `Finance.Contracts`:
```
StockAdded          { UserId, Ticker, OccurredAt }       Portfolio → MarketData
StockRemoved        { UserId, Ticker, OccurredAt }       Portfolio → MarketData
StockPriceUpdated   { Ticker, Price, UnixTimestamp }     MarketData → Alert, Notification
AlertTriggered      { AlertId, UserId, UserEmail,
                      Ticker, TargetPrice, CurrentPrice,
                      Direction }                         Alert → Notification
```

---

## Database Strategy

| DB | Tables | Owned By |
|---|---|---|
| `IdentityDB` | Users, Roles, UserRoles | Identity Service |
| `PortfolioDB` | Stocks, StockSectors, Investments | Portfolio Service |
| `AlertDB` | StockPriceAlerts | Alert Service |

Dev: single SQL Server, 3 databases.  
Prod: Azure SQL — separate instances per service.

---

---

# PHASE 0 — Shared Infrastructure & Solution Scaffold
**Duration: Day 1 (morning)**

### Goal
Create the new solution file and the two shared library projects that every service will reference.

### Checklist

#### New Solution
- [ ] Create `Finance.Portfolio.Solution.sln` at repo root
- [ ] Add all existing monolith projects to it (so nothing breaks today)

#### Finance.Contracts (class library, net10.0)
Location: `shared/Finance.Contracts/`
- [ ] Create project
- [ ] Add `MassTransit` NuGet (interfaces only — no transport)
- [ ] Create event records:
  - `Events/StockAdded.cs`
  - `Events/StockRemoved.cs`
  - `Events/StockPriceUpdated.cs`
  - `Events/AlertTriggered.cs`

#### Finance.SharedKernel.Auth (class library, net10.0)
Location: `shared/Finance.SharedKernel.Auth/`
- [ ] Create project
- [ ] Add `Microsoft.AspNetCore.Authentication.JwtBearer`
- [ ] Create `JwtAuthenticationExtensions.cs`:
  ```csharp
  public static IServiceCollection AddSharedJwtAuthentication(
      this IServiceCollection services, IConfiguration configuration)
  ```
  — reads `JwtSettings:Key/Issuer/Audience`, calls `AddAuthentication().AddJwtBearer()`

### Verification
- `dotnet build` on both shared projects passes

---

---

# PHASE 1 — API Gateway
**Duration: Day 1 (afternoon)**

### Goal
Stand up a YARP reverse proxy that today routes all traffic to the monolith. Later each route is redirected as services are extracted.

### Checklist

#### Finance.Gateway
Location: `services/Gateway/Finance.Gateway/`
- [ ] `dotnet new web -n Finance.Gateway`
- [ ] Add `Yarp.ReverseProxy` NuGet
- [ ] `Program.cs`: `builder.Services.AddReverseProxy().LoadFromConfig(...)` + `app.MapReverseProxy()`
- [ ] `appsettings.json` route table — initially **all routes → monolith**:
  ```json
  "/api/{**catch-all}" → monolith:5000
  "/hubs/{**catch-all}" → monolith:5000
  ```
- [ ] JWT validation middleware (reads `JwtSettings:Key` from env)
- [ ] After validation inject `X-User-Id` and `X-User-Email` headers from claims

#### docker-compose.yml (initial)
- [ ] `sqlserver`, `redis`, `rabbitmq` containers
- [ ] `monolith` container (existing `Finance.Portfolio.API`)
- [ ] `gateway` container on port `5000`

### Verification
- `docker-compose up gateway monolith`
- `POST /api/auth/login` through gateway returns JWT (proxied to monolith)
- All existing endpoints still work through gateway

---

---

# PHASE 2 — Identity Service
**Duration: Days 2–3**

### Goal
Extract auth into its own service. The monolith's `AuthController` stays alive as fallback until gateway is switched.

### Day 2

#### Create Projects
- [ ] `Finance.IdentityService.Domain` — copy `ApplicationUser.cs`
- [ ] `Finance.IdentityService.Persistence` — copy `IdentityDbContext`, `UserConfiguration`, `RoleConfiguration`, `UserRoleConfiguration`; connection string → `IdentityDB` (same existing DB)
- [ ] `Finance.IdentityService.Application` — copy `IAuthService`, `AuthRequest/Response`, `RegistrationRequest/Response`
- [ ] `Finance.IdentityService.Infrastructure` — copy `AuthService` (JWT generation logic)
- [ ] `Finance.IdentityService.API`:
  - `dotnet new webapi`
  - Copy `AuthController.cs`
  - Wire `IdentityServicesRegistration`, `PersistenceServiceRegistration`
  - Reference `Finance.SharedKernel.Auth`
  - Add `ExceptionMiddleware`

### Day 3

#### Harden & Integrate
- [ ] Add `Finance.IdentityService.API` to `docker-compose.yml` as `identity-svc`
  - Env: `ConnectionStrings__IdentityDb`, `JwtSettings__*`
- [ ] Update gateway `appsettings.json`:
  - `/api/auth/**` → `identity-svc:8080`
  - all other routes still → monolith
- [ ] Smoke test:
  - `POST /api/auth/register` → creates user
  - `POST /api/auth/login` → returns JWT
  - `GET /api/portfolio/summary` with that JWT → still proxied to monolith, still works
- [ ] Remove `AuthController` from monolith (or comment out and mark deprecated)
- [ ] Add health check endpoint (`/health`) to Identity Service

### Verification
- Login/register through gateway hit Identity Service (check logs)
- Portfolio endpoints still work (monolith handles them)
- `dotnet ef database update` runs clean on `IdentityDB`

---

---

# PHASE 3 — Sentiment Service
**Duration: Days 4–5**

### Goal
Stateless extraction — no DB, no message bus. Simplest possible service.

### Day 4

#### Create Projects
- [ ] `Finance.SentimentService.Infrastructure`:
  - Copy `YahooFinanceService` (RSS scraping only)
  - Copy `SentimentAnalysisService` (restore ML.NET pipeline or keep stub)
  - Add `Microsoft.ML`, `Microsoft.ML.FastTree`
- [ ] `Finance.SentimentService.API`:
  - Copy `SentimentController`
  - Register services
  - Reference `Finance.SharedKernel.Auth` (JWT validation)
  - Add `ExceptionMiddleware`

### Day 5

#### Docker & Gateway
- [ ] Add `sentiment-svc` to `docker-compose.yml`
- [ ] Update gateway: `/api/sentiment/**` → `sentiment-svc:8080`
- [ ] Remove `SentimentController` from monolith
- [ ] Add health check to Sentiment Service

### Verification
- `GET /api/sentiment/analyze-yahoo-news/AAPL` returns sentiment result through gateway

---

---

# PHASE 4 — Market Data Service
**Duration: Days 6–9**

### Goal
Extract price fetching, Hangfire jobs, and Redis caching. Expose a gRPC server for Portfolio Service to call. Fix the hardcoded `"testUser"` bug.

### Day 6 — Project Setup & Yahoo Finance / Redis

#### Create Projects
- [ ] `Finance.MarketDataService.Application`:
  - Job interfaces: `IStockPriceUpdateJob`
  - MassTransit consumer interfaces: `IConsumer<StockAdded>`, `IConsumer<StockRemoved>`
- [ ] `Finance.MarketDataService.Infrastructure`:
  - Copy `StockQuoteService` (Yahoo Finance quote + OHLCV)
  - Copy `RedisCacheService` — **fix `"testUser"` bug**:
    - Replace hardcoded key with `mkt:subscriptions:active-tickers` Redis set
    - `StockAdded` consumer: `SADD mkt:subscriptions:active-tickers {ticker}`
    - `StockRemoved` consumer: `SREM mkt:subscriptions:active-tickers {ticker}`
  - Copy `JobSchedulerService`, `BackgroundJobService`, Hangfire job classes
  - Copy `StockPriceBackgroundService` — **remove** `emailSender` and `signalRService` calls (those move to Notification Service in Phase 7); for now just do price fetch + Redis cache + publish `StockPriceUpdated` event

### Day 7 — gRPC Server

- [ ] Install `Grpc.AspNetCore` in `Finance.MarketDataService.API`
- [ ] Create `Protos/marketdata.proto`:
  ```proto
  syntax = "proto3";
  package marketdata;
  service MarketDataService {
    rpc GetCurrentPrice (GetPriceRequest) returns (GetPriceResponse);
    rpc GetOhlcv (GetOhlcvRequest) returns (GetOhlcvResponse);
  }
  message GetPriceRequest  { string ticker = 1; }
  message GetPriceResponse { string ticker = 1; double price = 2; int64 timestamp = 3; }
  message GetOhlcvRequest  { string ticker = 1; string interval = 2; string range = 3; }
  message GetOhlcvResponse { repeated OhlcvBar bars = 1; }
  message OhlcvBar { int64 time=1; double open=2; double high=3; double low=4; double close=5; int64 volume=6; }
  ```
- [ ] Implement `MarketDataGrpcService : MarketDataService.MarketDataServiceBase`
  - `GetCurrentPrice` → fetch from Redis cache; if miss → call Yahoo Finance
  - `GetOhlcv` → call Yahoo Finance `FetchOhlcvAsync`
- [ ] Map gRPC service in `Program.cs` on port `8081`

### Day 8 — MassTransit + RabbitMQ

- [ ] Add `MassTransit.RabbitMQ` to Market Data Service
- [ ] Register consumers: `StockAddedConsumer`, `StockRemovedConsumer`
- [ ] `StockPriceBackgroundService` publishes `StockPriceUpdated` via `IPublishEndpoint`
- [ ] Add RabbitMQ container to `docker-compose.yml` (if not already)
- [ ] Add Market Data Service to `docker-compose.yml`:
  - Ports: `8080` (HTTP/health), `8081` (gRPC)
  - Env: `ConnectionStrings__Redis`, `RabbitMq__*`

### Day 9 — Integration & Hangfire

- [ ] Hangfire uses Redis storage (`UseRedisStorage`) — verify jobs schedule correctly
- [ ] Verify `StockPriceUpdated` events appear on RabbitMQ management UI
- [ ] Add health check
- [ ] **Do not update gateway yet** — Portfolio Service needs to be extracted first for gRPC to be useful

### Verification
- Call gRPC `GetCurrentPrice("AAPL")` directly → returns price
- `StockAdded` event published manually → ticker appears in `mkt:subscriptions:active-tickers` Redis set
- Hangfire dashboard accessible at Market Data Service `/hangfire`

---

---

# PHASE 5 — Portfolio Service
**Duration: Days 10–13**

### Goal
Extract the portfolio domain. Wire gRPC client to Market Data. Publish `StockAdded`/`StockRemoved` events.

### Day 10 — Domain, Persistence

- [ ] `Finance.PortfolioService.Domain`: Copy `Stock`, `StockSector`, `Investment`, `BaseEntity`
- [ ] `Finance.PortfolioService.Persistence`:
  - Create `PortfolioDbContext` with `Stocks`, `StockSectors`, `Investments` DbSets
  - Copy entity configurations from existing `Configurations/` folder
  - `dotnet ef migrations add InitialPortfolio` (points at `PortfolioDB` — same SQL Server, new DB name)
  - `dotnet ef database update`

### Day 11 — Application Layer (CQRS)

- [ ] `Finance.PortfolioService.Application`:
  - Copy all commands/queries from `Features/Portfolio/` and `Features/StockSector/`
  - Adjust namespaces
  - In `AddStockCommandHandler`: after saving stock, publish `StockAdded` via `IPublishEndpoint`
  - In `DeleteStockCommandHandler`: after deleting, publish `StockRemoved` via `IPublishEndpoint`
  - In `GetPortfolioSummaryQueryHandler`: **replace `IStockQuoteService` calls** with gRPC client call to Market Data Service (`MarketDataService.MarketDataServiceClient`)
  - In `GetPortfolioSummaryQueryHandler`: replace OHLCV call similarly

### Day 12 — Infrastructure + API

- [ ] `Finance.PortfolioService.Infrastructure`:
  - Add `Grpc.Net.Client`
  - Register `MarketDataService.MarketDataServiceClient` pointed at `http://marketdata-svc:8081`
  - Add `MassTransit.RabbitMQ`
- [ ] `Finance.PortfolioService.API`:
  - Copy `PortfolioController`, `StockSectorController`
  - Reference `Finance.SharedKernel.Auth`
  - Add `ExceptionMiddleware`
  - Add health check

### Day 13 — Docker & Gateway Cutover

- [ ] Add `portfolio-svc` to `docker-compose.yml`
  - Env: `ConnectionStrings__PortfolioDb`, `JwtSettings__*`, `MarketData__GrpcUrl`, `RabbitMq__*`
- [ ] Update gateway:
  - `/api/portfolio/**` → `portfolio-svc:8080`
  - `/api/stocksector/**` → `portfolio-svc:8080`
- [ ] Remove `PortfolioController`, `StockSectorController` from monolith
- [ ] Remove `Stocks`, `StockSectors`, `Investments` DbSets from monolith's `FinanceStockMarketDatabaseContext`

### Verification
- `GET /api/portfolio/summary` → returns real-time prices fetched via gRPC from Market Data Service
- `POST /api/portfolio/stock` → `StockAdded` event visible in RabbitMQ management UI
- `GET /api/portfolio/chart/AAPL` → returns OHLCV data

---

---

# PHASE 6 — Alert Service
**Duration: Days 14–16**

### Goal
Extract alerts. Consume `StockPriceUpdated` events to trigger alerts. Publish `AlertTriggered`.

### Day 14 — Domain, Persistence

- [ ] `Finance.AlertService.Domain`: Copy `StockPriceAlert`, `AlertCondition` enum
- [ ] `Finance.AlertService.Persistence`:
  - Create `AlertDbContext` with `StockPriceAlerts` DbSet
  - Copy `StockPriceAlertRepository`
  - `dotnet ef migrations add InitialAlert` → `AlertDB`
  - `dotnet ef database update`

### Day 15 — Application Layer + Event Consumer

- [ ] `Finance.AlertService.Application`:
  - Copy all commands/queries from `Features/Alerts/`
  - Create `StockPriceUpdatedConsumer`:
    - Receives `StockPriceUpdated`
    - Queries active alerts for that ticker via `IStockPriceAlertRepository.GetActiveAlertsByTickerAsync`
    - Evaluates `AlertCondition.Above/Below`
    - Marks alert `IsTriggered = true`
    - Publishes `AlertTriggered` via `IPublishEndpoint`
  - Remove alert-checking from Market Data `StockPriceBackgroundService`

### Day 16 — API + Docker + Gateway

- [ ] `Finance.AlertService.API`: Copy `AlertsController`, add JWT auth, health check
- [ ] Add `alert-svc` to `docker-compose.yml`
  - Env: `ConnectionStrings__AlertDb`, `JwtSettings__*`, `RabbitMq__*`
- [ ] Update gateway: `/api/alerts/**` → `alert-svc:8080`
- [ ] Remove `AlertsController` from monolith; remove `StockPriceAlerts` from monolith's DbContext

### Verification
- `POST /api/alerts` creates alert
- Manually publish a `StockPriceUpdated` event that crosses a threshold → alert's `IsTriggered` flips to `true` in `AlertDB`
- `AlertTriggered` event visible in RabbitMQ management UI

---

---

# PHASE 7 — Notification Service ✅ Complete
**Duration: Days 17–18**

### Goal
Move SignalR hub and SendGrid email out of Market Data / Infrastructure into their own service. Consume events from Market Data and Alert Service.

### Day 17 — Infrastructure

- [x] `Finance.NotificationService.Infrastructure`:
  - `StockPriceHub` (SignalR hub with Authorize)
  - `EmailSender` (SendGrid)
  - `StockPriceUpdatedConsumer`: receives event → broadcasts via IHubContext
  - `AlertTriggeredConsumer`: receives event → sends SendGrid email
  - `Microsoft.AspNetCore.SignalR.StackExchangeRedis` backplane (conditional on Redis connection string)
  - FrameworkReference Microsoft.AspNetCore.App (required for Hub in class library)

### Day 18 — API + Docker + Gateway

- [x] `Finance.NotificationService.API`:
  - Maps `StockPriceHub` at `/hubs/stockprice`
  - MassTransit + RabbitMQ consumers registered
  - Redis SignalR backplane conditionally applied
  - References `Finance.SharedKernel.Auth`
- [x] `notification-svc` added to `docker-compose.yml`
- [x] `docker-compose.override.yml` port 5006:8080
- [x] Gateway: `/hubs/**` → `notification` cluster → `notification-svc:8080`

### Verification
- Open browser WebSocket connection to `ws://localhost:5000/hubs/stockprice`
- Market Data emits `StockPriceUpdated` → Notification Service broadcasts → browser receives push
- Trigger alert → `AlertTriggered` event → SendGrid email received

---

---

# PHASE 8 — Decommission Monolith ✅ Complete
**Duration: Day 19**

### Checklist

- [x] Remove `monolith` service from `docker-compose.yml`
- [x] Remove monolith port from `docker-compose.override.yml`
- [x] Remove `monolith` cluster from gateway `appsettings.json` (all routes now point to microservices)
- [x] Remove all 6 monolith projects from `Finance.Portfolio.Solution.slnx`
- [ ] Confirm all 5 gateway routes return correct responses with monolith stopped (runtime verification)
  - `/api/auth/*` → Identity
  - `/api/portfolio/*`, `/api/stocksector/*` → Portfolio
  - `/api/alerts/*` → Alert
  - `/api/sentiment/*` → Sentiment
  - `/hubs/*` → Notification

### Final Verification
- Full end-to-end flow:
  1. Register user → login → get JWT
  2. Add stock → `StockAdded` event → Market Data starts tracking ticker
  3. Get portfolio summary → real prices via gRPC
  4. Create price alert
  5. Market Data background job updates price → `StockPriceUpdated` event
  6. Alert Service evaluates → `AlertTriggered` event
  7. Notification: email received + SignalR push in browser

---

---

# Docker Compose Reference

## docker-compose.yml
```yaml
version: "3.9"
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      SA_PASSWORD: "YourStrong!Passw0rd"
      ACCEPT_EULA: "Y"
    ports: ["1433:1433"]
    volumes: ["sqldata:/var/opt/mssql"]

  redis:
    image: redis:7-alpine
    ports: ["6379:6379"]

  rabbitmq:
    image: rabbitmq:3.13-management-alpine
    ports: ["5672:5672", "15672:15672"]
    environment:
      RABBITMQ_DEFAULT_USER: finance
      RABBITMQ_DEFAULT_PASS: finance

  gateway:
    build: ./services/Gateway/Finance.Gateway
    ports: ["5000:8080"]
    env_file: .env
    depends_on: [identity-svc, portfolio-svc, marketdata-svc, alert-svc, sentiment-svc, notification-svc]

  identity-svc:
    build: ./services/Identity/Finance.IdentityService.API
    env_file: .env
    depends_on: [sqlserver]

  portfolio-svc:
    build: ./services/Portfolio/Finance.PortfolioService.API
    env_file: .env
    depends_on: [sqlserver, rabbitmq, marketdata-svc]

  marketdata-svc:
    build: ./services/MarketData/Finance.MarketDataService.API
    env_file: .env
    depends_on: [redis, rabbitmq]

  alert-svc:
    build: ./services/Alert/Finance.AlertService.API
    env_file: .env
    depends_on: [sqlserver, rabbitmq]

  sentiment-svc:
    build: ./services/Sentiment/Finance.SentimentService.API
    env_file: .env

  notification-svc:
    build: ./services/Notification/Finance.NotificationService.API
    env_file: .env
    depends_on: [redis, rabbitmq]

volumes:
  sqldata:
```

## docker-compose.override.yml (dev — expose individual ports)
```yaml
services:
  identity-svc:     { ports: ["5001:8080"] }
  portfolio-svc:    { ports: ["5002:8080"] }
  marketdata-svc:   { ports: ["5003:8080", "5013:8081"] }
  alert-svc:        { ports: ["5004:8080"] }
  sentiment-svc:    { ports: ["5005:8080"] }
  notification-svc: { ports: ["5006:8080"] }
```

## .env (gitignored)
```
JWT_SECRET=your-very-long-secret-key-at-least-32-characters-here
JWT_ISSUER=FinanceStockMarket.Api
JWT_AUDIENCE=FinanceStockMarketUser
JWT_DURATION_MINUTES=15

SENDGRID_API_KEY=SG.xxx
SENDGRID_FROM_ADDRESS=alerts@yourapp.com
SENDGRID_FROM_NAME=StockMarket Alerts

RABBITMQ_HOST=rabbitmq
RABBITMQ_USERNAME=finance
RABBITMQ_PASSWORD=finance

SQL_SERVER=sqlserver
SQL_PASSWORD=YourStrong!Passw0rd
REDIS_CONNECTION=redis:6379
```

---

---

# NuGet Packages Quick Reference

| Service | Key Packages |
|---|---|
| All | `MediatR 12`, `FluentValidation 12`, `AutoMapper 13`, `Serilog.AspNetCore` |
| Identity | `Microsoft.AspNetCore.Identity.EntityFrameworkCore 9`, `Microsoft.IdentityModel.Tokens 8` |
| Portfolio | `MassTransit.RabbitMQ`, `Grpc.Net.Client`, `EF Core SqlServer 9` |
| Market Data | `Hangfire.Core`, `Hangfire.Redis.StackExchange`, `Grpc.AspNetCore`, `StackExchange.Redis` |
| Alert | `MassTransit.RabbitMQ`, `EF Core SqlServer 9` |
| Sentiment | `Microsoft.ML 4`, `Microsoft.ML.FastTree 4` |
| Notification | `MassTransit.RabbitMQ`, `Microsoft.AspNetCore.SignalR.StackExchangeRedis`, `SendGrid 9` |
| Gateway | `Yarp.ReverseProxy 2` |
| Shared Contracts | `MassTransit` (interfaces only) |
| Shared Auth | `Microsoft.AspNetCore.Authentication.JwtBearer 9` |

---

---

# Progress Tracker

| Phase | Status | Completed Date |
|---|---|---|
| Phase 0 — Shared Projects | ✅ Complete | 2026-05-12 |
| Phase 1 — API Gateway | ✅ Complete | 2026-05-12 |
| Phase 2 — Identity Service | ✅ Complete | 2026-05-12 |
| Phase 3 — Sentiment Service | ✅ Complete | 2026-05-12 |
| Phase 4 — Market Data Service | ✅ Complete | 2026-05-12 |
| Phase 5 — Portfolio Service | ✅ Complete | 2026-05-12 |
| Phase 6 — Alert Service | ✅ Complete | 2026-05-12 |
| Phase 7 — Notification Service | ⬜ Not Started | — |
| Phase 8 — Decommission Monolith | ⬜ Not Started | — |

---

*Last updated: 2026-05-12*
