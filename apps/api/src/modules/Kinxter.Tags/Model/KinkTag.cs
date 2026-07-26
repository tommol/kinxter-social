namespace Kinxter.Tags.Model;

public sealed class KinkTag
{
    private KinkTag()
    {
        Slug = Category = NamePl = NameEn = DescriptionPl = DescriptionEn = null!;
    }

    public KinkTag(
        Guid id,
        string slug,
        string category,
        string namePl,
        string nameEn,
        string? descriptionPl,
        string? descriptionEn,
        int sortOrder,
        DateTimeOffset createdAt)
    {
        Category = NamePl = NameEn = "";
        Id = id;
        Slug = NormalizeSlug(slug);
        Update(category, namePl, nameEn, descriptionPl, descriptionEn, sortOrder, true, createdAt);
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public string Slug { get; private set; }
    public string Category { get; private set; }
    public string NamePl { get; private set; }
    public string NameEn { get; private set; }
    public string? DescriptionPl { get; private set; }
    public string? DescriptionEn { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public void Update(
        string category,
        string namePl,
        string nameEn,
        string? descriptionPl,
        string? descriptionEn,
        int sortOrder,
        bool isActive,
        DateTimeOffset updatedAt)
    {
        Category = Required(category, 80, nameof(category));
        NamePl = Required(namePl, 120, nameof(namePl));
        NameEn = Required(nameEn, 120, nameof(nameEn));
        DescriptionPl = Optional(descriptionPl, 500);
        DescriptionEn = Optional(descriptionEn, 500);
        SortOrder = sortOrder;
        IsActive = isActive;
        UpdatedAt = updatedAt;
    }

    private static string NormalizeSlug(string value)
    {
        var slug = Required(value, 80, nameof(value)).ToLowerInvariant();

        if (slug.Any(character => !(character is >= 'a' and <= 'z' or >= '0' and <= '9' or '-')))
        {
            throw new ArgumentException("Slug may contain lowercase letters, numbers and hyphens.", nameof(value));
        }

        return slug;
    }

    private static string Required(string value, int max, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, name);
        var result = value.Trim();
        return result.Length <= max ? result : throw new ArgumentException($"Value exceeds {max} characters.", name);
    }

    private static string? Optional(string? value, int max) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().Length <= max ? value.Trim() : throw new ArgumentException($"Value exceeds {max} characters.");
}
