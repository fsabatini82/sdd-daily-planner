using LibrarySystem.Models;

namespace LibrarySystem.Services;

public sealed class MemberService(InMemoryStore store)
{
    public Result<Member> Register(string fullName, string email, DateOnly birthDate)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return Result<Member>.Fail("FullName required");

        if (string.IsNullOrWhiteSpace(email))
            return Result<Member>.Fail("Email required");

        if (store.Members.Values.Any(m => m.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
            return Result<Member>.Fail($"Email {email} already registered", conflict: true);

        var member = new Member
        {
            FullName = fullName,
            Email = email,
            BirthDate = birthDate
        };
        store.Members[member.MemberId] = member;
        return Result<Member>.Ok(member);
    }

    public Member? GetById(Guid id) => store.Members.TryGetValue(id, out var m) ? m : null;

    public Result<Member> Deactivate(Guid memberId)
    {
        if (!store.Members.TryGetValue(memberId, out var member))
            return Result<Member>.Fail("Member not found", notFound: true);

        member.IsActive = false;
        return Result<Member>.Ok(member);
    }
}
