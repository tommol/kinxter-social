using Kinxter.Profiles.Model;

namespace Kinxter.Profiles.Api;

internal static class ProfileEndpointValidation
{
    public static Dictionary<string, string[]> ValidateRequiredText(
        (string FieldName, string? Value, int MaxLength)[] fields)
    {
        var errors = new Dictionary<string, string[]>();

        foreach (var field in fields)
        {
            if (string.IsNullOrWhiteSpace(field.Value))
            {
                errors[field.FieldName] = [$"{field.FieldName} is required."];
                continue;
            }

            if (field.Value.Trim().Length > field.MaxLength)
            {
                errors[field.FieldName] = [$"{field.FieldName} cannot be longer than {field.MaxLength} characters."];
            }
        }

        return errors;
    }

    public static Dictionary<string, string[]> ValidateProfileOnboarding(
        string? bio,
        string? profilePictureUrl)
    {
        var errors = new Dictionary<string, string[]>();

        if (!string.IsNullOrWhiteSpace(bio) && bio.Trim().Length > Profile.BioMaxLength)
        {
            errors["Bio"] = [$"Bio cannot be longer than {Profile.BioMaxLength} characters."];
        }

        if (string.IsNullOrWhiteSpace(profilePictureUrl))
        {
            return errors;
        }

        var trimmedUrl = profilePictureUrl.Trim();

        if (trimmedUrl.Length > Profile.ProfilePictureUrlMaxLength)
        {
            errors["ProfilePictureUrl"] =
                [$"Profile picture URL cannot be longer than {Profile.ProfilePictureUrlMaxLength} characters."];
        }
        else if (!Uri.TryCreate(trimmedUrl, UriKind.Absolute, out _))
        {
            errors["ProfilePictureUrl"] = ["Profile picture URL must be an absolute URL."];
        }

        return errors;
    }
}
