# Migrations

Generate the first migration after creating the service (run from the solution root):

```bash
dotnet tool restore
dotnet ef migrations add Initial \
  --project src/Services/ServiceNameService/ServiceName.Infrastructure/ServiceName.Persistence \
  --startup-project src/Services/ServiceNameService/ServiceName.API \
  --output-dir Migrations
```

Production: build an idempotent migration bundle in CI instead of migrating on startup:

```bash
dotnet ef migrations bundle --idempotent --self-contained -o efbundle \
  --project src/Services/ServiceNameService/ServiceName.Infrastructure/ServiceName.Persistence \
  --startup-project src/Services/ServiceNameService/ServiceName.API
```
