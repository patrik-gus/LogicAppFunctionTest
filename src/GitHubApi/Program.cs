using System.Net.Http.Json;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient("logicapp");
var app = builder.Build();

// LogicApp:TriggerUrl = HTTP trigger callback URL of the Logic App (includes the sig query parameter).
string TriggerUrl() => app.Configuration["LogicApp:TriggerUrl"] is { Length: > 0 } url
    ? url
    : throw new InvalidOperationException("LogicApp:TriggerUrl is not configured.");

async Task<IResult> Forward(IHttpClientFactory factory, object payload, CancellationToken ct)
{
    var client = factory.CreateClient("logicapp");
    using var response = await client.PostAsJsonAsync(TriggerUrl(), payload, ct);
    var body = await response.Content.ReadAsStringAsync(ct);
    return Results.Content(body, "application/json", statusCode: (int)response.StatusCode);
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/api/repos/{owner}/{repo}/issues",
    (string owner, string repo, IHttpClientFactory f, CancellationToken ct) =>
        Forward(f, new { action = "listIssues", owner, repo }, ct));

app.MapPost("/api/repos/{owner}/{repo}/issues",
    (string owner, string repo, CreateIssueRequest req, IHttpClientFactory f, CancellationToken ct) =>
        string.IsNullOrWhiteSpace(req.Title)
            ? Task.FromResult(Results.BadRequest(new { error = "title is required" }))
            : Forward(f, new { action = "createIssue", owner, repo, title = req.Title, body = req.Body }, ct));

app.Run();

record CreateIssueRequest(string Title, string? Body);
