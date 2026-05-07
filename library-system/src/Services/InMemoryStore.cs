using System.Collections.Concurrent;
using LibrarySystem.Models;

namespace LibrarySystem.Services;

public sealed class InMemoryStore
{
    public ConcurrentDictionary<Guid, Book> Books { get; } = new();
    public ConcurrentDictionary<Guid, Loan> Loans { get; } = new();
    public ConcurrentDictionary<Guid, Member> Members { get; } = new();
}
