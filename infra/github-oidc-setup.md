# Letting GitHub Actions deploy to Azure (OIDC)

Deployment is split into three workflows, each triggered by a push to **`main`** that
touches its area (plus manual `workflow_dispatch`):

| Workflow | Runs when these change | Does |
|---|---|---|
| `.github/workflows/infra.yml` | `infra/**` | deploys the Bicep (provisions **both** apps) |
| `.github/workflows/deploy-web.yml` | `src/StuffInABox.Web/**` or a shared project (`Domain`/`Application`/`Infrastructure`) | builds the SPA + publishes the consumer app |
| `.github/workflows/deploy-admin.yml` | `src/StuffInABox.Admin.Web/**` or a shared project | publishes the admin app |

They all authenticate to Azure with **GitHub OIDC federated credentials** — a trust between
your GitHub repo and an Entra app that has a role on your subscription. **No Azure password is
ever stored in GitHub.**

> First-time order: run **infra** first (it creates the App Services), then the code
> workflows. After that they're independent — a code-only change redeploys just that app.

## 1. Prerequisite

You need an Azure **subscription** (the `Standardkatalog` tenant has none yet — create a
free one at <https://azure.microsoft.com/free>). Then sign in locally as an account that
can create app registrations and assign subscription roles (Owner, or Contributor +
User Access Administrator).

## 2. Run the setup (once)

```pwsh
az login
./infra/setup-github-oidc.ps1 `
  -GitHubRepo "<owner>/<repo>" `
  -SubscriptionId "<your-subscription-guid>"
# add -IncludeUserAccessAdmin only if you plan to enable the Key Vault module
```

This creates the Entra app + service principal, a federated credential trusting
`repo:<owner>/<repo>:ref:refs/heads/main`, grants **Contributor** on the subscription,
and prints the values to put into GitHub.

> Doing it by hand instead? The same three steps are `az ad app create`, then
> `az ad app federated-credential create` (subject `repo:<owner>/<repo>:ref:refs/heads/main`,
> audience `api://AzureADTokenExchange`), then `az role assignment create --role Contributor`.

## 3. Configure GitHub

Repo → **Settings → Secrets and variables → Actions**.

**The app secrets are NOT here** — they live in Key Vault (`Db-Connection`, `Jwt-Secret`,
`Email-Smtp-Password`, `OAuth-Google-ClientSecret`, `OAuth-Microsoft-ClientSecret`) and the
app reads them at runtime via its managed identity. The pipeline only authenticates to
Azure; it never handles secret values.

**Secrets** (sensitive) — only the OIDC identifiers:

| Name | Value |
|---|---|
| `AZURE_CLIENT_ID` | app id printed by the script |
| `AZURE_TENANT_ID` | tenant id printed by the script |
| `AZURE_SUBSCRIPTION_ID` | your subscription guid |

**Variables** (non-sensitive):

| Name | Example |
|---|---|
| `AZURE_LOCATION` | `swedencentral` |
| `AZURE_ENV` | `prod` |
| `AZURE_APP_NAME` | `stuffinabox-andree` (globally unique) |
| `AZURE_APP_SKU` | `F1` |
| `AZURE_KEY_VAULT` | `kv-stuffinabox-andree` |
| `SIB_BREVO_USER` | `b0375d001@smtp-brevo.com` |
| `SIB_EMAIL_FROM` | `andree.antfolk@gmail.com` |
| `SIB_GOOGLE_CLIENT_ID` | Google OAuth client id (public) |
| `SIB_MICROSOFT_CLIENT_ID` | Microsoft OAuth client id (public) |

**Admin app** (only needed if you deploy it — see `infra/README.md` → "Admin app"):

| Name | Example |
|---|---|
| `AZURE_ENABLE_ADMIN_APP` | `true` |
| `AZURE_ADMIN_APP_NAME` | `stuffinabox-admin-andree` (globally unique) |
| `AZURE_ADMIN_TENANT_ID` | tenant the admin sign-in is locked to |
| `AZURE_ADMIN_CLIENT_ID` | client id from `entra/admin-app.bicep` |

## 4. Deploy

Push to `main` (or run a workflow manually from the **Actions** tab). Each workflow only
fires for changes in its area (see the table at the top), so an infra change deploys infra,
a web-code change redeploys the web app, and an admin-code change redeploys the admin app. A
change that touches a shared project (`Domain`/`Application`/`Infrastructure`) redeploys both
apps. On the very first setup, run **infra** first so the App Services exist.

## 5. After the first deploy

Add the site's callback to Google Cloud Console → your OAuth client → Authorized redirect
URIs: `https://<AZURE_APP_NAME>.azurewebsites.net/api/v1/auth/google/callback`.

## Security notes

- The trust is scoped to **one repo + the `main` branch** — other repos/branches can't use
  it. To allow more (e.g. a `staging` branch or a GitHub Environment with approvals), add
  another federated credential with the matching `subject`.
- **Contributor** is enough for the default deploy. Only enabling the Key Vault module needs
  the extra **User Access Administrator** (because it creates a role assignment). Prefer the
  least privilege you actually need.
