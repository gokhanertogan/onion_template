# Onion / Clean Architecture .NET 10 Microservice Template

A `dotnet new` template that generates a production-ready **ASP.NET Core 10 Web API microservice** built on
**Onion Architecture / Clean Architecture**, **DDD** and **CQRS**, with **zero third-party NuGet packages**.

No MediatR, no FluentValidation, no AutoMapper: the mediator, pipeline behaviors, validation, result pattern,
transactional outbox and permission-based authorization are part of the generated code. No commercial-license
surprises, no supply-chain risk, nothing to upgrade but Microsoft packages.

```bash
dotnet new install OnionArchitecture.Templates
dotnet new onion -n Order
cd Order && dotnet test
```

The template also appears in the Visual Studio and JetBrains Rider "New Project" dialogs after installation.

## What you get

- **Onion / Clean Architecture** layers (Domain, Application, Infrastructure, Persistence, API, Contracts) with the
  dependency rule enforced by architecture tests
- **CQRS with a built-in mediator** (MediatR alternative): commands, queries, domain event handlers and pipeline
  behaviors for validation, logging, caching, unit of work and idempotency
- **DDD building blocks**: aggregates, entities, value objects, domain events, strongly typed errors
- **Result pattern** instead of exceptions, mapped to ProblemDetails
- **Built-in validation** with a FluentValidation-style API (FluentValidation alternative)
- **EF Core 10** with SQL Server, PostgreSQL or SQLite, auditing, soft-delete and optimistic
  concurrency interceptors, migrations
- **Transactional outbox** and integration events
- **HybridCache** query caching and **idempotency keys**
- **Security by default**: JWT bearer authentication, permission-based authorization, rate limiting, security
  headers, CORS
- **Minimal API** endpoints, **OpenAPI 3.1**, health checks, correlation ids, structured logging
- Optional **OpenTelemetry** (`--otel`)
- **Tests**: unit, integration (WebApplicationFactory) and architecture tests with MSTest
- **Docker** (chiseled image), docker-compose, GitHub Actions CI and Dependabot

## Options

| Option | Default | Description |
| --- | --- | --- |
| `-n` | `Sample` | Service name (projects, namespaces, routes, permissions) |
| `--database`, `-db` | `sqlserver` | `sqlserver`, `postgres` or `sqlite` |
| `--service-only` | `false` | Add another service to an existing solution (run with `-o .`) |
| `--otel` | `false` | Add OpenTelemetry traces, metrics and logs |
| `--solution-name`, `-sn` | service name | Solution file name |

```bash
dotnet new onion -n Order --database postgres --otel   # PostgreSQL + OpenTelemetry
dotnet new onion -n Order --database sqlite            # No infrastructure needed
dotnet new onion -n Payment --service-only -o .        # Second microservice in the same solution
```

## Documentation

Full documentation, architecture guide and examples:
**https://github.com/gokhanertogan/onion_template**

If the template saves you time, a star on GitHub helps other developers find it.
