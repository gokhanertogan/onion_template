# SolutionName

Production-ready .NET 10 microservice solution built on Onion / Clean Architecture, DDD and CQRS.
It has **no third-party runtime dependencies**: only the BCL and first-party Microsoft packages
(ASP.NET Core, EF Core, HybridCache, JwtBearer, OpenApi).

## Quick start

```bash
dotnet tool restore
dotnet build
dotnet test

# Run the API (Development: schema is created automatically, OpenAPI is exposed)
dotnet run --project src/Services/ServiceNameService/ServiceName.API

# Create a development token with the service permissions
dotnet user-jwts create --project src/Services/ServiceNameService/ServiceName.API \
  --scope "service-names:read" --scope "service-names:write"
```

Then use `src/Services/ServiceNameService/ServiceName.API/ServiceName.API.http`, or open `/openapi/v1.json`.

With Docker (API + database):

```bash
docker compose up --build
```

## Solution layout

```
src/
  BuildingBlocks/                 Shared framework (reused by every service)
    BuildingBlocks.Domain         Entity, AggregateRoot, ValueObject, DomainEvent, Result/Error
    BuildingBlocks.Application    Mediator, CQRS, pipeline behaviors, validation, caching & event abstractions
    BuildingBlocks.Infrastructure EF Core base context, interceptors, outbox, HybridCache, in-memory event bus
    BuildingBlocks.Web            Service defaults: security, problem details, rate limiting, health, OpenAPI
  Services/ServiceNameService/
    ServiceName.Core/
      ServiceName.Domain          Aggregate, strongly typed id, domain events, errors, repository interface
      ServiceName.Application     Use cases (vertical slices): commands, queries, validators, event handlers
    ServiceName.Infrastructure/
      ServiceName.Infrastructure  External integrations, integration event consumers
      ServiceName.Persistence     DbContext, EF configurations, repositories, migrations
    ServiceName.Contracts         Public DTOs and integration events (dependency free)
    ServiceName.API               Minimal API endpoints, composition root
    tests/                        Unit, architecture and integration tests
tests/BuildingBlocks.Tests        Tests of the shared framework
```

Dependencies point inwards only (`Domain ← Application ← Infrastructure/Persistence ← API`).
`ServiceName.ArchitectureTests` fails the build if a layer violates that rule.

## What is included

| Concern | Implementation |
| --- | --- |
| CQRS / mediator | Own `ISender` with cached, compiled pipelines; `ICommand`, `IQuery`, handlers |
| Pipeline (outer → inner) | Logging + tracing + metrics → Validation → Idempotency → Caching → Cache invalidation → Unit of work |
| Validation | Own fluent `AbstractValidator<T>` (`RuleFor`, `RuleForEach`, `When`, `SetValidator`, async rules) |
| Errors | Result pattern (`Result`, `Result<T>`, typed `Error`), no exceptions for control flow; RFC 9457 problem details |
| Domain | Rich aggregates, private setters, factory methods, strongly typed ids (GUID v7), domain events |
| Persistence | EF Core, auditing, soft delete with global filter, provider-agnostic optimistic concurrency, retries |
| Reliable messaging | Transactional outbox + background processor; `IEventBus` for integration events |
| Caching | HybridCache (L1 memory + optional L2 distributed) with stampede protection and tag invalidation |
| Idempotency | `Idempotency-Key` header → `IIdempotentCommand`, concurrent duplicates coalesced |
| Security | JWT bearer, permission/scope based authorization, secure-by-default fallback policy, CORS allow-list, OWASP security headers, Kestrel limits, no `Server` header, HSTS |
| Abuse protection | Partitioned rate limiting (per user / per IP) with `Retry-After`, request timeouts, body size limits |
| Observability | Structured JSON logs, source-generated logging, correlation id, `ActivitySource` + `Meter`, optional OpenTelemetry (`--otel`) |
| Operations | Liveness/readiness health checks, chiseled non-root Docker image, docker-compose, CI workflow |
| Quality | .NET analyzers (Recommended) with warnings as errors, central package management, NuGet audit, deterministic builds |
| Tests | MSTest (Microsoft.Testing.Platform), hand-written fakes, reflection-based architecture tests, `WebApplicationFactory` integration tests |

## Adding a feature

1. Domain: add behavior to the aggregate (method + invariant + domain event + error).
2. Application: add a folder under `ServiceNames/` with the command/query record, its validator and its handler.
3. API: add an `IEndpoint` class under `Endpoints/ServiceNames/`. It is discovered automatically.
4. Persistence: update the EF configuration and add a migration.

## Adding another service

From the solution root:

```bash
dotnet new onion -n Payment --service-only -o .
```

The new projects are added to the solution. Add `src/Services/PaymentService/compose.yaml` to the `include`
list in `docker-compose.yml`; CI builds every `src/Services/*/Dockerfile` automatically.
If the new service uses a different database provider, add its package version to `Directory.Packages.props`
and its database server to `docker-compose.yml`.

## Database migrations

See `src/Services/ServiceNameService/ServiceName.Infrastructure/ServiceName.Persistence/Migrations/README.md`.
In Development the schema is created on startup (`Database:ApplyMigrationsOnStartup`); in production
apply migrations with an EF Core migration bundle from your pipeline.

## Configuration

| Key | Purpose |
| --- | --- |
| `ConnectionStrings:ServiceNameDb` | Database connection (use environment variables / a secret store in production) |
| `Authentication:Schemes:Bearer:Authority` | OpenID Connect authority (Entra ID, Keycloak, Auth0...) |
| `Authentication:Schemes:Bearer:ValidAudiences` | Accepted token audiences |
| `Cors:AllowedOrigins` | Browser origins allowed to call the API |
| `RateLimiting:*` | Permits per window and per caller |
| `Outbox:*` | Polling interval, batch size, max attempts |
| `OpenApi:Enabled` | Expose `/openapi/v1.json` (default: Development only) |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | OTLP endpoint when created with `--otel` |

To enable a distributed L2 cache (e.g. Redis), register an `IDistributedCache` before
`AddBuildingBlocksInfrastructure()`; HybridCache picks it up automatically. A distributed cache is
also required for idempotency across multiple instances.

## Production checklist

- Set `AllowedHosts`, `Cors:AllowedOrigins` and the JWT `Authority`/audiences.
- Supply secrets through environment variables or a vault, never `appsettings.json`.
- Generate migrations and run them through a migration bundle.
- Use a broker-backed `IEventBus` for cross-service events, and a distributed cache for multiple instances.
- Run the outbox processor on one instance, or add provider-specific row locking.
- Behind a reverse proxy, set `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`.
