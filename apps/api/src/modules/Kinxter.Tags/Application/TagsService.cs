using Kinxter.Shared.Abstractions.Time;
using Kinxter.Tags.Contracts;
using Kinxter.Tags.Infrastructure;
using Kinxter.Tags.Model;
using Microsoft.EntityFrameworkCore;

namespace Kinxter.Tags.Application;

internal sealed class TagsService(TagsDbContext dbContext, IClock clock) : ITagsService
{
    public async Task<IReadOnlyCollection<TagState>> GetActiveAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Tags.AsNoTracking().Where(tag => tag.IsActive)
            .OrderBy(tag => tag.SortOrder).ThenBy(tag => tag.Slug)
            .Select(tag => new TagState(tag.Id, tag.Slug, tag.Category, tag.NamePl, tag.NameEn, tag.DescriptionPl, tag.DescriptionEn, tag.SortOrder, tag.IsActive))
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlySet<Guid>> GetTagIdsAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default) =>
        (await dbContext.Assignments.AsNoTracking()
            .Where(assignment => assignment.EntityType == entityType && assignment.EntityId == entityId)
            .Select(assignment => assignment.TagId).ToArrayAsync(cancellationToken)).ToHashSet();

    public async Task SetTagsAsync(string entityType, Guid entityId, IReadOnlyCollection<Guid> tagIds, CancellationToken cancellationToken = default)
    {
        var uniqueIds = tagIds.Distinct().ToArray();
        var validCount = await dbContext.Tags.CountAsync(tag => uniqueIds.Contains(tag.Id) && tag.IsActive, cancellationToken);

        if (validCount != uniqueIds.Length)
        {
            throw new ArgumentException("One or more kinktags are invalid or inactive.", nameof(tagIds));
        }

        await dbContext.Assignments
            .Where(assignment => assignment.EntityType == entityType && assignment.EntityId == entityId)
            .ExecuteDeleteAsync(cancellationToken);
        dbContext.Assignments.AddRange(uniqueIds.Select(tagId => new EntityTagAssignment(entityType, entityId, tagId, clock.UtcNow)));
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
