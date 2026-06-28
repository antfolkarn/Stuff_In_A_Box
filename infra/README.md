# StuffInABox — Azure infrastructure (Bicep)

Infrastructure-as-code for hosting StuffInABox on Azure. Designed to run cheaply (Free
tier) to start and to make a later move to "real" tiers a parameter change, not a rewrite.

## What it provisions

| Resource | Default | Notes |
|---|---|---|
| Resource group | `rg-stuffinabox-<env>` | created by the subscription-scoped deploy |
| App Service plan + Web App (Linux, .NET) | **F1 Free** | hosts the API + the built SPA (wwwroot) |
| Key Vault (RBAC) | **bootstrap** (`enableKeyVault`) | holds ALL app secrets; the app reads them via managed identity |
| Azure PostgreSQL Flexible | **off** | prepared module — the DB is **Supabase** today |
| Storage Account + blob container | **off** | prepared module for photos (app uses local disk now) |

The database is **external (Supabase free-tier Postgres)** — the app just gets its
connection string as an app setting. Email goes through **Brevo SMTP** (also app settings).

## Prerequisites

1. **An Azure subscription.** The `Standardkatalog` tenant currently has none — create a
   free one at <https://azure.microsoft.com/free> ($200 credit / 12 months free popular
   services; a card is required but you aren't charged unless you upgrade).
2. Azure CLI + Bicep (already installed): `az version`, `az bicep version`.

## Deploy

**Secrets are NOT passed to the deploy** — they live in Key Vault and the app reads them at
runtime via its managed identity (see "Secrets in Key Vault" below). The deploy only takes
non-secret config.

One-time bootstrap (creates the Key Vault + grants the app access — needs Owner):

```pwsh
az login
az account set --subscription "<your subscription>"
# Edit infra/main.bicepparam (appName, keyVaultName), then:
az deployment sub create -l swedencentral -f infra/main.bicep -p infra/main.bicepparam `
  --parameters enableKeyVault=true
# Then add the secrets to the vault (once):
$kv = "kv-stuffinabox-andree"
az keyvault secret set --vault-name $kv --name Db-Connection            --value "Host=...pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.<ref>;Password=<pwd>;SSL Mode=Require;Trust Server Certificate=true"
az keyvault secret set --vault-name $kv --name Jwt-Secret               --value "<random 32+ chars>"
az keyvault secret set --vault-name $kv --name Email-Smtp-Password      --value "<brevo SMTP key xsmtpsib-...>"
az keyvault secret set --vault-name $kv --name OAuth-Google-ClientSecret    --value "<google client secret>"
az keyvault secret set --vault-name $kv --name OAuth-Microsoft-ClientSecret --value "<microsoft client secret>"
```

Normal deploys (CI/CD, or by hand) leave `enableKeyVault=false` — they just reference the
existing vault:

```pwsh
az deployment sub create -l swedencentral -f infra/main.bicep -p infra/main.bicepparam
```

The output `webAppUrl` is your site, e.g. `https://stuffinabox-andree.azurewebsites.net`.

## Deploy the app code

Bicep provisions infra; the code is published separately. Build the SPA into wwwroot
first (the .NET publish picks it up):

```pwsh
cd src/StuffInABox.Web/ClientApp; npm run build; cd ../../..
dotnet publish src/StuffInABox.Web -c Release -o publish
Compress-Archive -Path publish/* -DestinationPath publish.zip -Force
az webapp deploy --resource-group rg-stuffinabox-dev --name <appName> --src-path publish.zip --type zip
```

(Or wire a GitHub Action later — the infra is unchanged.)

## Secrets in Key Vault

All sensitive values live in the Key Vault (`keyVaultName`, default `kv-stuffinabox-andree`),
never in code, the repo, or the pipeline. The app's system-assigned managed identity has the
**Key Vault Secrets User** role and resolves these app settings at runtime:

| App setting | Key Vault secret |
|---|---|
| `ConnectionStrings__Default` | `Db-Connection` (Supabase, via the IPv4 **session pooler**) |
| `Jwt__Secret` | `Jwt-Secret` |
| `Email__Smtp__Password` | `Email-Smtp-Password` |
| `OAuth__Google__ClientSecret` | `OAuth-Google-ClientSecret` |
| `OAuth__Microsoft__ClientSecret` | `OAuth-Microsoft-ClientSecret` |

Rotate a secret by setting a new version in the vault — no redeploy needed (App Service
re-resolves). Check resolution: `az rest --method get --url ".../sites/<app>/config/configreferences/appsettings?api-version=2022-03-01"`.

> **Supabase note:** the *direct* host `db.<ref>.supabase.co` is IPv6-only and unreachable
> from App Service (IPv4-only outbound). Use the **session pooler** host
> (`aws-1-eu-north-1.pooler.supabase.com:5432`, user `postgres.<ref>`).

## After deploying — don't forget

- **Google OAuth:** add `https://<appName>.azurewebsites.net/api/v1/auth/google/callback`
  to the Authorized redirect URIs in Google Cloud Console (the Bicep sets `App:BaseUrl`
  and the redirect app setting, but Google must allow-list the URL).
- **DB migrations:** the app runs `Database.Migrate()` on startup for Postgres, so the
  Supabase schema is applied automatically on first boot.

## Free-tier caveats

- **F1 Free**: no Always-On (the app cold-starts after idle), ~60 CPU-min/day, 1 GB RAM,
  no custom-domain TLS. Fine for testing. If F1 Linux isn't offered in your region, set
  `appServiceSku` to `B1` (~$13/mo).
- **Supabase free**: pauses after inactivity and has row/space limits — fine to start.

## Migrating to "real" later (parameter changes only)

- Scale compute: `appServiceSku = 'B1'` (or higher) → gets Always-On.
- Move DB onto Azure: `enablePostgres = true` + set `SIB_PG_ADMIN_PASSWORD`, then point
  `SIB_DB_CONNECTION` at the new server (`connectionStringTemplate` output helps).
- Move photos onto Azure Blob: `enableStorage = true` (then add an Azure Blob storage
  provider in the app — it currently supports `local` and `r2`/`s3`).
- Centralize secrets: `enableKeyVault = true`, move secrets into the vault, switch the
  web app's app settings to `@Microsoft.KeyVault(SecretUri=…)` references.

## Teardown

```pwsh
az group delete --name rg-stuffinabox-dev --yes --no-wait
```
