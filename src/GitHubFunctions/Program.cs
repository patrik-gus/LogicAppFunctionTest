using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddHttpClient("github", c =>
        {
            c.BaseAddress = new Uri("https://api.github.com/");
            c.DefaultRequestHeaders.UserAgent.ParseAdd("GitHubLogicApp-Function/1.0");
            c.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            c.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
            var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
            if (!string.IsNullOrEmpty(token))
                c.DefaultRequestHeaders.Authorization = new("Bearer", token);
        });
    })
    .Build();

host.Run();
