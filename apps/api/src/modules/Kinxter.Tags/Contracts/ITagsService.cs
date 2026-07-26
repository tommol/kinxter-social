namespace Kinxter.Tags.Contracts;

public sealed record TagState(
    Guid Id,
    string Slug,
    string Category,
    string NamePl,
    string NameEn,
    string? DescriptionPl,
    string? DescriptionEn,
    int SortOrder,
    bool IsActive);

public interface ITagsService
{
    Task<IReadOnlyCollection<TagState>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlySet<Guid>> GetTagIdsAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default);
    Task SetTagsAsync(string entityType, Guid entityId, IReadOnlyCollection<Guid> tagIds, CancellationToken cancellationToken = default);
}
