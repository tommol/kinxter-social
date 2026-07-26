namespace Kinxter.Tags.Model;

public sealed class EntityTagAssignment
{
    private EntityTagAssignment() { EntityType = null!; }

    public EntityTagAssignment(string entityType, Guid entityId, Guid tagId, DateTimeOffset assignedAt)
    {
        EntityType = entityType;
        EntityId = entityId;
        TagId = tagId;
        AssignedAt = assignedAt;
    }

    public string EntityType { get; private set; }
    public Guid EntityId { get; private set; }
    public Guid TagId { get; private set; }
    public DateTimeOffset AssignedAt { get; private set; }
}
