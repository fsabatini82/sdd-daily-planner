namespace LibrarySystem.Models;

public sealed class Book
{
    public Guid BookId { get; init; } = Guid.NewGuid();
    public required string Isbn { get; init; }
    public required string Title { get; set; }
    public required string Author { get; set; }
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
}
