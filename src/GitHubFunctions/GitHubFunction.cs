using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace GitHubFunctions;

public record GitHubRequest(string Action, string Owner, string Repo, string? Title, string? Body);

public class GitHubFunction(IHttpClientFactory httpClientFactory)
{
    // Called by the Logic App: POST /api/github
    [Function("GitHub")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "github")] HttpRequest req,
        CancellationToken ct)
    {
        GitHubRequest? input;
        try
        {
            input = await req.ReadFromJsonAsync<GitHubRequest>(
                new JsonSerializerOptions(JsonSerializerDefaults.Web), ct);
        }
        catch (JsonException)
        {
            return new BadRequestObjectResult(new { error = "Invalid JSON" });
        }

        if (input is null || string.IsNullOrWhiteSpace(input.Owner) || string.IsNullOrWhiteSpace(input.Repo))
            return new BadRequestObjectResult(new { error = "owner and repo are required" });

        var client = httpClientFactory.CreateClient("github");
        var path = $"repos/{Uri.EscapeDataString(input.Owner)}/{Uri.EscapeDataString(input.Repo)}/issues";

        HttpResponseMessage response;
        switch (input.Action)
        {
            case "listIssues":
                response = await client.GetAsync(path + "?state=open&per_page=30", ct);
                break;
            case "createIssue":
                if (string.IsNullOrWhiteSpace(input.Title))
                    return new BadRequestObjectResult(new { error = "title is required" });
                response = await client.PostAsJsonAsync(path, new { title = input.Title, body = input.Body }, ct);
                break;
            default:
                return new BadRequestObjectResult(new { error = "action must be listIssues or createIssue" });
        }

        using (response)
        {
            var content = await response.Content.ReadAsStringAsync(ct);
            return new ContentResult
            {
                Content = content,
                ContentType = "application/json",
                StatusCode = (int)response.StatusCode
            };
        }
    }
}
