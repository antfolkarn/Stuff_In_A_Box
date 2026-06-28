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

// --- Non-secret app config (read from env, or set as GitHub *variables*) ---
// NOTE: no secret VALUES here — all secrets live in Key Vault and are referenced at runtime.
param brevoSmtpUser = readEnvironmentVariable('SIB_BREVO_USER', 'b0375d001@smtp-brevo.com')
param emailFrom = readEnvironmentVariable('SIB_EMAIL_FROM', 'andree.antfolk@gmail.com')
param googleClientId = readEnvironmentVariable('SIB_GOOGLE_CLIENT_ID', '')
param microsoftClientId = readEnvironmentVariable('SIB_MICROSOFT_CLIENT_ID', '')
