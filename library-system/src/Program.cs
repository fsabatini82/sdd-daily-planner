using LibrarySystem.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<InMemoryStore>();
builder.Services.AddSingleton<CatalogService>();
builder.Services.AddSingleton<LoanService>();
builder.Services.AddSingleton<MemberService>();

var app = builder.Build();

// === Catalog endpoints ===
app.MapGet("/api/books", (CatalogService svc, string? title, string? author)
    => Results.Ok(svc.Search(title, author)));

app.MapGet("/api/books/{id:guid}", (CatalogService svc, Guid id)
    => svc.GetById(id) is { } b ? Results.Ok(b) : Results.NotFound());

app.MapPost("/api/books", (CatalogService svc, RegisterBookRequest req) =>
{
    var r = svc.Register(req.Isbn, req.Title, req.Author, req.TotalCopies);
    if (!r.Success) return r.Conflict ? Results.Conflict(r.Error) : Results.BadRequest(r.Error);
    return Results.Created($"/api/books/{r.Value!.BookId}", r.Value);
});

app.MapPut("/api/books/{id:guid}/copies", (CatalogService svc, Guid id, UpdateCopiesRequest req) =>
{
    var r = svc.UpdateCopies(id, req.TotalCopies);
    if (r.Success) return Results.Ok(r.Value);
    if (r.NotFound) return Results.NotFound();
    return r.Conflict ? Results.Conflict(r.Error) : Results.BadRequest(r.Error);
});

// === Loan endpoints ===
app.MapPost("/api/loans", (LoanService svc, CreateLoanRequest req) =>
{
    var r = svc.CreateLoan(req.BookId, req.MemberId);
    if (r.Success) return Results.Created($"/api/loans/{r.Value!.LoanId}", r.Value);
    if (r.NotFound) return Results.NotFound(r.Error);
    return r.Conflict ? Results.Conflict(r.Error) : Results.BadRequest(r.Error);
});

app.MapPut("/api/loans/{id:guid}/return", (LoanService svc, Guid id) =>
{
    var r = svc.ReturnLoan(id);
    if (r.Success) return Results.Ok(r.Value);
    if (r.NotFound) return Results.NotFound();
    return r.Conflict ? Results.Conflict(r.Error) : Results.BadRequest(r.Error);
});

app.MapGet("/api/loans/active", (LoanService svc) => Results.Ok(svc.GetActiveLoans()));
app.MapGet("/api/loans", (LoanService svc, Guid memberId) => Results.Ok(svc.GetLoansByMember(memberId)));

// === Member endpoints ===
app.MapPost("/api/members", (MemberService svc, RegisterMemberRequest req) =>
{
    var r = svc.Register(req.FullName, req.Email, req.BirthDate);
    if (r.Success) return Results.Created($"/api/members/{r.Value!.MemberId}", r.Value);
    return r.Conflict ? Results.Conflict(r.Error) : Results.BadRequest(r.Error);
});

app.MapGet("/api/members/{id:guid}", (MemberService svc, Guid id)
    => svc.GetById(id) is { } m ? Results.Ok(m) : Results.NotFound());

app.MapPut("/api/members/{id:guid}/deactivate", (MemberService svc, Guid id) =>
{
    var r = svc.Deactivate(id);
    if (r.Success) return Results.Ok(r.Value);
    return r.NotFound ? Results.NotFound() : Results.BadRequest(r.Error);
});

app.Run();

public record RegisterBookRequest(string Isbn, string Title, string Author, int TotalCopies);
public record UpdateCopiesRequest(int TotalCopies);
public record CreateLoanRequest(Guid BookId, Guid MemberId);
public record RegisterMemberRequest(string FullName, string Email, DateOnly BirthDate);
