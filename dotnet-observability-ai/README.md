# .NET 10 End-to-End Observability And AI Log Analysis

This repository implements a production-oriented distributed .NET 10 system with OpenTelemetry, Elasticsearch, Kibana, GPT-based analysis, and GitHub issue-to-PR automation.

## Architecture

- Api: API Gateway (HTTP entry point)
- Basket.API: basket orchestration service
- Order.API: order service with deterministic + random failure paths
- ChaosWorker: background fault injector and traffic generator
- AiAnalyzerWorker: background AI diagnostics worker
- Shared: shared contracts and log context models

Request flow:

1. Client -> Api
2. Api -> Basket.API
3. Basket.API -> Order.API
4. ChaosWorker continuously generates traffic and chaos scenarios
5. OpenTelemetry exports traces/logs over OTLP to Collector
6. Collector writes telemetry into Elasticsearch
7. Kibana visualizes logs and traces
8. AiAnalyzerWorker reads error logs from Elasticsearch, asks GPT for RCA + fix + diff suggestions, then can open a GitHub issue and linked PR

## Solution Structure

```text
src/
├── Api
├── Basket.API
├── Order.API
├── ChaosWorker
├── AiAnalyzerWorker
└── Shared
```

## Dotnet CLI Scaffolding Commands

```bash
dotnet new sln -n ObservabilityAiSystem
dotnet new webapi -f net10.0 -n Api -o src/Api --use-controllers
dotnet new webapi -f net10.0 -n Basket.API -o src/Basket.API --use-controllers
dotnet new webapi -f net10.0 -n Order.API -o src/Order.API --use-controllers
dotnet new worker -f net10.0 -n ChaosWorker -o src/ChaosWorker
dotnet new worker -f net10.0 -n AiAnalyzerWorker -o src/AiAnalyzerWorker
dotnet new classlib -f net10.0 -n Shared -o src/Shared
dotnet sln add src/Api/Api.csproj src/Basket.API/Basket.API.csproj src/Order.API/Order.API.csproj src/ChaosWorker/ChaosWorker.csproj src/AiAnalyzerWorker/AiAnalyzerWorker.csproj src/Shared/Shared.csproj
```

## Observability Design

- Distributed tracing enabled by OpenTelemetry:
  - ASP.NET Core instrumentation
  - HttpClient instrumentation
  - ActivitySource spans in controllers/workers
- Structured logs include log context model fields:
  - timestamp
  - level
  - message
  - service
  - traceId
  - spanId
  - correlationId
  - optional userId
  - optional exception details
- Correlation propagation via headers:
  - x-correlation-id
  - x-user-id

## AI Analysis Strategy

AiAnalyzerWorker applies input reduction before invoking LLM:

1. Read only error-centric logs from Elasticsearch
2. Group by traceId
3. Select top problematic traces
4. Trim each trace to max 20-30 log lines

Prompt contract:

- Always starts with: "You are a senior .NET SRE."
- Requests strict JSON response with fields:
  - title
  - root_cause
  - impact
  - fix
  - severity
  - pr_diff

Worker then writes:

- analysis JSON artifact
- GitHub issue + linked PR (when automation is enabled)

## Infrastructure

- docker-compose.yml
- infra/otel-collector/config.yaml

Containers:

- Elasticsearch (9200)
- Kibana (5601)
- OpenTelemetry Collector (4317 gRPC, 4318 HTTP)

## Run Instructions

Security note:

- Do not commit API keys or tokens.
- Provide `OPENAI_API_KEY` and `GITHUB_TOKEN` only at runtime (environment variables), or keep them in local-only `src/AiAnalyzerWorker/appsettings.Local.json`.
- `appsettings.Local.json` is ignored by git.

1. Start infra stack:

```bash
docker compose up -d
```

2. Configure local runtime secrets (do not commit):

```bash
cp src/AiAnalyzerWorker/appsettings.Local.example.json src/AiAnalyzerWorker/appsettings.Local.json
```

Update `src/AiAnalyzerWorker/appsettings.Local.json` with your own values:

- `OpenAI.ApiKey`
- `GitHub.Token`
- set `GitHub.EnableIssuePrAutomation` to `true`

3. Build all projects:

```bash
dotnet build ObservabilityAiSystem.sln
```

4. Start services in separate terminals:

```bash
dotnet run --project src/Order.API --urls http://localhost:5002
dotnet run --project src/Basket.API --urls http://localhost:5001
dotnet run --project src/Api --urls http://localhost:5000
dotnet run --project src/ChaosWorker
dotnet run --project src/AiAnalyzerWorker
```

5. Generate extra test traffic (optional):

```bash
curl -H "x-correlation-id: manual-001" -H "x-user-id: user-1" "http://localhost:5000/api/orders/REQ-MANUAL-001?userId=user-1"
```

6. Open Kibana:

- http://localhost:5601
- Discover -> create data view for `observability-logs*`
- Query examples:
  - `severity_text: "ERROR"`
  - `service.name: "order-api"`
  - `traceid: *`

7. Check AI artifacts and automation output:

- AiAnalyzerWorker writes files under `analysis-output`
- Example expected payload is in `samples/example-ai-output.json`
- If GitHub automation is enabled, a new issue and linked PR should be created in the configured repository

## Performance Notes For 16GB RAM Local Environment

- Elasticsearch heap fixed at 1GB
- Collector memory limiter enabled (512 MiB)
- Batch processor tuned for smaller local bursts
- AI analyzer caps fetched logs and logs per trace

## Important Simplifications

- No authentication
- No PII masking
- GitHub API automation is configuration-driven (disabled by default, can create issue and linked PR)

## Key Files

- API gateway endpoint: src/Api/Controllers/GatewayController.cs
- Basket.API processing endpoint: src/Basket.API/Controllers/ProcessingController.cs
- Order.API order + failure logic: src/Order.API/Controllers/InventoryController.cs
- Chaos engine: src/ChaosWorker/Worker.cs
- AI analyzer: src/AiAnalyzerWorker/Worker.cs
- Collector config: infra/otel-collector/config.yaml
- Compose stack: docker-compose.yml
