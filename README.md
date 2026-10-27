# GitHubLogicApp

REST API (ASP.NET Core) → Azure Logic App (HTTP trigger) → Azure Function (isolated .NET) → GitHub REST API.

```
Client → src/GitHubApi → logicapp/logicapp.template.json → src/GitHubFunctions (/api/github) → api.github.com
```

## Endpoints (GitHubApi)
- `GET  /api/repos/{owner}/{repo}/issues` – list open issues
- `POST /api/repos/{owner}/{repo}/issues` – body `{ "title": "...", "body": "..." }`

## Deploy
1. Publish the function: `func azure functionapp publish <functionAppName>` (from `src/GitHubFunctions`), and set app setting `GITHUB_TOKEN` (PAT with `repo` / issues scope).
2. Deploy the Logic App:
   `az deployment group create -g <rg> --template-file logicapp/logicapp.template.json --parameters functionAppName=<name> functionKey=<key>`
3. Get the trigger URL: `az rest --method post --uri "<logicAppResourceId>/triggers/manual/listCallbackUrl?api-version=2019-05-01" --query value`
4. Set `LogicApp__TriggerUrl` for the API (user-secrets locally: `dotnet user-secrets set LogicApp:TriggerUrl "<url>"`) and run/deploy `GitHubApi`.

## Publish this repo to GitHub
```
git add . && git commit -m "Initial commit"
gh repo create GitHubLogicApp --private --source . --push
```
