# Local HTTPS development

The VibeCast web project includes `http` and `https` project launch profiles under `src/VibeCast.Web/Properties/launchSettings.json`.

Trust the ASP.NET Core development certificate once on the workstation:

```bash
dotnet dev-certs https --trust
```

Restore, build, and launch the application without Docker:

```bash
dotnet restore
dotnet build --configuration Release
dotnet run --project src/VibeCast.Web --launch-profile https
```

The HTTPS profile opens the login page and returns to the dashboard after authentication.

Local endpoints:

- `https://localhost:7188`
- `http://localhost:5188`

Visual Studio and JetBrains Rider can select the `https` project profile directly from their run and debug profile selector.
