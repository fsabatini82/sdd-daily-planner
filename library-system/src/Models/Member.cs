namespace LibrarySystem.Models;

public sealed class Member
{
    public Guid MemberId { get; init; } = Guid.NewGuid();
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public required DateOnly BirthDate { get; init; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedOn { get; init; } = DateTime.UtcNow;
}
