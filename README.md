Microsoft Teams meeting

Join: https://teams.microsoft.com/meet/259205588311202?p=ruYnnEPPTZMGp7NJ1v

Meeting ID: 259 205 588 311 202

Passcode: JC7Ti6rr

# Order Processing Service

.NET 8 Web API that manages orders with **MongoDB** persistence, **Redis** caching for read-heavy endpoints, **RabbitMQ** events when orders are created, and **Docker Compose** for one-command startup.

## Prerequisites

- [.NET SDK 8.0](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for container workflow)

## Run with Docker (recommended)

From the repository root:

```bash
docker-compose up --build
```

- API: `http://localhost:8080`
- RabbitMQ management UI: `http://localhost:15672` (guest / guest)
- MongoDB: `localhost:27017`
- Redis: `localhost:6379`

Configuration is supplied through environment variables (see `docker-compose.yml`). The API reads nested settings via the standard `Section__Property` pattern.

## Run locally (development)

1. Start MongoDB, Redis, and RabbitMQ (or point to cloud endpoints).
2. Update `src/OrderProcessingService.Api/appsettings.Development.json` (or user secrets) with connection strings.
3. Run:

```bash
dotnet run --project src/OrderProcessingService.Api
```

The API listens on `http://localhost:8080` by default (`Urls` in `appsettings.json`).

## API surface

| Method | Route | Description |
| ------ | ----- | ----------- |
| `POST` | `/api/orders` | Validates stock, reserves inventory atomically, persists `Pending` order, publishes `order.created`. |
| `GET` | `/api/orders/{id}` | Order lookup (`404` when missing). Cached in Redis. |
| `PATCH` | `/api/orders/{id}/status` | Legal transitions only: `Pending → Confirmed → Processing → Shipped → Delivered`; cancellation allowed from non-terminal pre-delivery states and restores stock. |
| `GET` | `/api/products` | Lists catalog with stock (seeded with five products on empty database). Cached. |
| `GET` | `/api/products/{id}` | Single product (`404` when missing). Cached. |

### Order status rules

Forward progression cannot skip steps or move backwards. `Delivered` and `Cancelled` are terminal. Cancelling increments stock back for each line item.

### Caching

- **Products** (`GET /api/products`, `GET /api/products/{id}`): Redis TTL from `Redis:ProductCacheTtlSeconds`.
- **Orders** (`GET /api/orders/{id}`): TTL from `Redis:OrderCacheTtlSeconds`.
- Mutations invalidate relevant keys (product list, touched products, affected order).

### Messaging

On successful order creation the API publishes JSON to exchange `RabbitMq:ExchangeName` with routing key `RabbitMq:OrderCreatedRoutingKey` (defaults in `appsettings.json`).

## Testing

```bash
dotnet test
```

Tests mock persistence and infrastructure; **minimum seven** scenarios cover transitions, validation/stock rollback, publishing, cancellation stock restore, and cache behavior.

## Design notes

- **Concurrency**: Per-product stock reservation uses MongoDB `findOneAndUpdate` with a `stock >= qty` guard so two concurrent orders cannot oversell without one failing fast; failed multi-item orders compensate earlier increments.
- **Secrets**: No connection credentials are hardcoded; compose supplies environment variables, local runs use `appsettings`/secrets.

## AI assistance

See `AI-USAGE.md` for the assignment-required disclosure of AI tooling, review choices, and verification steps.
