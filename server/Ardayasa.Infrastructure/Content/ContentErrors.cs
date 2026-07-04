using Ardayasa.Application.Common;

namespace Ardayasa.Infrastructure.Content;

/// <summary>Stable error codes for content management, mapped to Indonesian in the client.</summary>
public static class ContentErrors
{
    public static readonly Error NotFound = new("content.not_found", "The requested item does not exist.");
    public static readonly Error SlugTaken = new("content.slug_taken", "An item with this slug already exists.");
    public static readonly Error CategoryNotFound = new("content.category_not_found", "The referenced category does not exist.");
    public static readonly Error CategoryNotEmpty = new("content.category_not_empty", "The category still contains items.");
    public static readonly Error InvalidRating = new("content.invalid_rating", "Rating must be between 1 and 5.");
    public static readonly Error PsychologistNotFound = new("content.psychologist_not_found", "The referenced psychologist does not exist.");
    public static readonly Error InvalidFile = new("content.invalid_file", "The uploaded file is not an allowed image type or exceeds the size limit.");
}
