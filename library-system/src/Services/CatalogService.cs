using LibrarySystem.Models;

namespace LibrarySystem.Services;

public sealed class CatalogService(InMemoryStore store)
{
    public IReadOnlyCollection<Book> Search(string? title, string? author)
    {
        return store.Books.Values
            .Where(b => string.IsNullOrEmpty(title) ||
                        b.Title.Contains(title, StringComparison.OrdinalIgnoreCase))
            .Where(b => string.IsNullOrEmpty(author) ||
                        b.Author.Contains(author, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public Book? GetById(Guid id) => store.Books.TryGetValue(id, out var book) ? book : null;

    public Result<Book> Register(string isbn, string title, string author, int totalCopies)
    {
        if (totalCopies < 0)
            return Result<Book>.Fail("TotalCopies cannot be negative");

        if (store.Books.Values.Any(b => b.Isbn == isbn))
            return Result<Book>.Fail($"ISBN {isbn} already exists", conflict: true);

        var book = new Book
        {
            Isbn = isbn,
            Title = title,
            Author = author,
            TotalCopies = totalCopies,
            AvailableCopies = totalCopies
        };
        store.Books[book.BookId] = book;
        return Result<Book>.Ok(book);
    }

    public Result<Book> UpdateCopies(Guid bookId, int newTotal)
    {
        if (!store.Books.TryGetValue(bookId, out var book))
            return Result<Book>.Fail("Book not found", notFound: true);

        if (newTotal < 0)
            return Result<Book>.Fail("TotalCopies cannot be negative");

        var loaned = book.TotalCopies - book.AvailableCopies;
        if (newTotal < loaned)
            return Result<Book>.Fail($"Cannot reduce TotalCopies below currently loaned ({loaned})", conflict: true);

        book.TotalCopies = newTotal;
        book.AvailableCopies = newTotal - loaned;
        return Result<Book>.Ok(book);
    }
}

public sealed record Result<T>(bool Success, T? Value, string? Error, bool NotFound, bool Conflict)
{
    public static Result<T> Ok(T value) => new(true, value, null, false, false);
    public static Result<T> Fail(string error, bool notFound = false, bool conflict = false)
        => new(false, default, error, notFound, conflict);
}
