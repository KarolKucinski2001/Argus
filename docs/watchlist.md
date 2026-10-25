# Watchlist storage and configuration

Authenticated users save assets from `/analysis` and manage them on
`/watchlist`. Entries are stored in Azure Table Storage, one row per
`(user, asset)`, scoped by the Entra `sub` claim. Each user can save up to 50
assets, and analysis started from the watchlist accepts at most five assets.

## Settings

| Key | Purpose |
| --- | --- |
| `Watchlist:ConnectionString` | Storage connection string. Use for Azurite or a connection-string account. |
| `Watchlist:Endpoint` | Table service endpoint (for example `https://<account>.table.core.windows.net`). Authenticates with `DefaultAzureCredential`, so the App Service managed identity needs the Storage Table Data Contributor role. |
| `Watchlist:TableName` | Table name. Defaults to `ArgusWatchlist`; must be 3-63 alphanumeric characters starting with a letter. The table is created on first use. |
| `Watchlist:MaximumEntries` | Fixed at 50; any other value fails validation. |

Provide either `ConnectionString` or `Endpoint`. Never commit either value:
the tracked `appsettings*.json` files keep them empty.

## Local development

1. Start Azurite (requires Node.js):

   ```powershell
   npx --yes azurite --silent --location $env:TEMP\azurite --skipApiVersionCheck
   ```

2. Store the development connection string in user secrets:

   ```powershell
   dotnet user-secrets set "Watchlist:ConnectionString" "UseDevelopmentStorage=true" --project .bootstrap-scaffold.csproj
   ```

   An environment variable works too: `Watchlist__ConnectionString`.

3. Run the app and sign in (see `docs/authentication.md`), then smoke test:
   save an asset on `/analysis`, open `/watchlist`, check it is listed, remove
   it, and confirm the empty state.

## Azure App Service

Add `Watchlist__Endpoint` (and optionally `Watchlist__TableName`) as
application settings and grant the app's managed identity the
Storage Table Data Contributor role on the storage account. Use
`Watchlist__ConnectionString` only if a managed identity is not available, and
store it as an App Service setting or Key Vault reference.

## Failure behavior

When storage is not configured or unreachable, the app still starts. Saving,
listing, and removing show an explicit Polish error message and never report
success. `/analysis` itself keeps working without the watchlist.

## Tests

`dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore`
uses an in-memory store and needs no Azure account or network.
