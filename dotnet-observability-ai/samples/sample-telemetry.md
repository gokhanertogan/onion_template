# Sample Logs And Trace Snapshot

## Sample Structured Log

```json
{
  "Timestamp": "2026-03-19T11:55:18.402Z",
  "Level": "Error",
  "Message": "Order.API resolution failed for REQ-115518-4392",
  "Service": "order-api",
  "TraceId": "72f2bc2dfc935f82b512311ac36d9f5f",
  "SpanId": "5f3f0a4f8b16dc32",
  "CorrelationId": "chaos-20260319115518-15",
  "UserId": "user-3",
  "Exception": "System.InvalidOperationException: Deterministic order rule failure in Order.API."
}
```

## Sample Trace Chain

- TraceId: `72f2bc2dfc935f82b512311ac36d9f5f`
- api-gateway: `Gateway.GetOrder`
- basket-api: `BasketApi.ProcessRequest`
- order-api: `OrderApi.GetOrder`
- chaos-worker: `ChaosWorker.Iteration`

## Sample Failure Narrative

1. ChaosWorker generated request `REQ-115518-4392` with correlation id `chaos-20260319115518-15`.
2. Api Gateway forwarded request to Basket.API with propagated correlation header.
3. Basket.API called Order.API for order payload.
4. Order.API deterministic rule injected `InvalidOperationException`.
5. Error logs and trace spans were exported to OTel Collector and indexed in Elasticsearch.
