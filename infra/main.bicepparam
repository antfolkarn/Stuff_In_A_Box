using './main.bicep'

// --- Non-secret settings (safe to commit) ---
param environmentName = 'prod'
param location = 'swedencentral'
// Globally unique — becomes <appName>.azurewebsites.net.
param appName = 'stuffinabox-andree'
param appServiceSku = 'F1' // Free tier; switch to 'B1' for Always-On / real use
param databaseProvider = 'postgres'
param keyVaultName = 'kv-stuffinabox-andree'

// Key Vault + secrets are a one-time Owner bootstrap; normal deploys leave these off.
param enableKeyVault = false
param enablePostgres = false
param enableStorage = false

// Admin app host (StuffInABox.Admin.Web) — separate App Service, off by default. Turn on and
// fill the three params below to deploy it. TenantId/ClientId come from entra/admin-app.bicep.
param enableAdminApp = false
param adminAppName = 'stuffinabox-admin-andree'
param adminAzureAdTenantId = readEnvironmentVariable('SIB_ADMIN_TENANT_ID', '')
param adminAzureAdClientId = readEnvironmentVariable('SIB_ADMIN_CLIENT_ID', '')

// --- Non-secret app config (read from env, or set as GitHub *variables*) ---
// NOTE: no secret VALUES here — all secrets live in Key Vault and are referenced at runtime.
param brevoSmtpUser = readEnvironmentVariable('SIB_BREVO_USER', 'b0375d001@smtp-brevo.com')
param emailFrom = readEnvironmentVariable('SIB_EMAIL_FROM', 'andree.antfolk@gmail.com')
param googleClientId = readEnvironmentVariable('SIB_GOOGLE_CLIENT_ID', '')
param microsoftClientId = readEnvironmentVariable('SIB_MICROSOFT_CLIENT_ID', '')

// --- Image recognition. Currently the hosted Staik API (free tier); the API key
//     lives in Key Vault (secret 'Staik-ApiKey'), only non-secret config here.
//     The Ollama params below are kept so we can flip back to the self-hosted
//     model by changing `imageRecognitionProvider` to 'ollama'. ---
param imageRecognitionProvider = 'staik'
param staikBaseUrl = 'https://api.staik.se'
param staikModel = 'gemma4:31b'
param staikTimeoutSeconds = 180
param ollamaBaseUrl = 'https://antfalk.tail3037e5.ts.net'
param ollamaModel = 'gemma3:12b'
param ollamaTimeoutSeconds = 180
