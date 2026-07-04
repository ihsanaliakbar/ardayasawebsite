using Microsoft.AspNetCore.Identity;

namespace Ardayasa.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public required string FullName { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Soft-delete / deactivation flag. Inactive users cannot authenticate.</summary>
    public bool IsActive { get; set; } = true;
}
