# AI usage (assignment requirement)

## AI strategy

- **Tooling**: Cursor with an AI coding agent was used to scaffold the .NET 8 solution, wire MongoDB/Redis/RabbitMQ clients, implement endpoints and Docker assets, and iterate until `dotnet test` passed.
- **Context management**: The assignment brief was mirrored into `README.md` so requirements stayed in-repo. Implementation work focused on one vertical slice at a time (domain and repositories, then services, then HTTP surface, then tests and containers) to keep prompts and diffs reviewable.

## Human audit (accepted vs rejected)

- **Accepted**: Layered layout (`Controllers` → application services → Mongo/Redis/RabbitMQ adapters), MongoDB `FindOneAndUpdate` for atomic stock moves, compensating increments when partial reservation fails, Redis cache keys for product list/single product/order reads, RabbitMQ topic exchange `orders.events` with routing key `order.created`, and docker-compose wiring solely through configuration keys (no hardcoded broker/DB hosts in code).
- **Rejected / adjusted**: Initial RabbitMQ.Client **7.x** suggestions relied on an async-first API incompatible with the quick synchronous publisher pattern; the dependency was pinned to **6.8.1** so `IConnection`/`IModel` remain stable without rewriting the messaging adapter for this exercise.

## Verification

- **Tests**: Unit tests target pure transition rules, order orchestration with mocked repositories/cache/publisher, and cache short-circuit behavior—no running MongoDB, Redis, or RabbitMQ required.
- **Correctness checks**: After AI-assisted edits, `dotnet build` and `dotnet test` were run locally until green; Docker image build validates the published API artifact inside the runtime container.
