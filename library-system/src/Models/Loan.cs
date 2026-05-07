namespace LibrarySystem.Models;

public sealed class Loan
{
    public Guid LoanId { get; init; } = Guid.NewGuid();
    public required Guid BookId { get; init; }
    public required Guid MemberId { get; init; }
    public DateTime LoanedOn { get; init; }
    public DateTime DueDate { get; init; }
    public DateTime? ReturnedOn { get; set; }

    public bool IsActive => ReturnedOn is null;
}
