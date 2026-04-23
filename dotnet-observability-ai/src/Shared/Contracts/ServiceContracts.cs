namespace Shared.Contracts;

public sealed record InventoryResult(string ItemId, bool Available, string Warehouse, int Remaining);

public sealed record ProcessingResult(string RequestId, string Status, InventoryResult Inventory, string Service);

public sealed record GatewayResponse(string RequestId, string Status, string Service, ProcessingResult Processing);
