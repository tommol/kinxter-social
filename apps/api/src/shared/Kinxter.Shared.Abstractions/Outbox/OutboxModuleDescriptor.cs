namespace Kinxter.Shared.Abstractions.Outbox;

public sealed record OutboxModuleDescriptor(
    string ModuleName,
    string SchemaName,
    string TableName = OutboxDefaults.TableName);
