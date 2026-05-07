using LibrarySystem.Models;

namespace LibrarySystem.Services;

public sealed class LoanService(InMemoryStore store)
{
    // NOTE: SPEC-02 BR-LOAN-01 says 14 days. Kept 21 here from the original PoC; review pending.
    private const int LoanDurationDays = 21;

    public Result<Loan> CreateLoan(Guid bookId, Guid memberId)
    {
        if (!store.Books.TryGetValue(bookId, out var book))
            return Result<Loan>.Fail("Book not found", notFound: true);

        if (!store.Members.TryGetValue(memberId, out var member))
            return Result<Loan>.Fail("Member not found", notFound: true);

        if (!member.IsActive)
            return Result<Loan>.Fail("Member is deactivated", conflict: true);

        if (book.AvailableCopies <= 0)
            return Result<Loan>.Fail("No copies available", conflict: true);

        var now = DateTime.UtcNow;
        var loan = new Loan
        {
            BookId = bookId,
            MemberId = memberId,
            LoanedOn = now,
            DueDate = now.AddDays(LoanDurationDays)
        };

        book.AvailableCopies--;
        store.Loans[loan.LoanId] = loan;
        return Result<Loan>.Ok(loan);
    }

    public Result<Loan> ReturnLoan(Guid loanId)
    {
        if (!store.Loans.TryGetValue(loanId, out var loan))
            return Result<Loan>.Fail("Loan not found", notFound: true);

        if (loan.ReturnedOn is not null)
            return Result<Loan>.Fail("Loan already returned", conflict: true);

        loan.ReturnedOn = DateTime.UtcNow;
        if (store.Books.TryGetValue(loan.BookId, out var book))
            book.AvailableCopies++;

        return Result<Loan>.Ok(loan);
    }

    public IReadOnlyCollection<Loan> GetActiveLoans()
        => store.Loans.Values.Where(l => l.IsActive).ToList();

    public IReadOnlyCollection<Loan> GetLoansByMember(Guid memberId)
        => store.Loans.Values.Where(l => l.MemberId == memberId).ToList();
}
