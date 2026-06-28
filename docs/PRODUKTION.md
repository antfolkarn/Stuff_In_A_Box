# Produktion — StuffInABox

StuffInABox är **driftsatt i produktion på Azure App Service**, med all infrastruktur som
kod (Bicep) under [`infra/`](../infra/). Det här dokumentet beskriver den faktiska
prod-uppsättningen och vad som återstår.

_Senast uppdaterad: 2026-06-28._

---

## ✅ I produktion nu (Azure App Service)

- **URL:** <https://stuffinabox-andree.azurewebsites.net>
- **Hosting:** Azure App Service (Linux, .NET 10), SKU **F1 (gratis)** i `swedencentral`.
  Resursgrupp `rg-stuffinabox-prod`, subscription "Stuff in a Box" (personlig tenant).
  Provisioneras via Bicep (`infra/main.bicep`, subscription-scope). Deploy av kod separat
  (bygg SPA → `dotnet publish -r linux-x64` → zip-deploy).
- **Databas:** **Supabase PostgreSQL** (gratis-tier, Stockholm) via **session pooler**
  (`aws-1-eu-north-1.pooler.supabase.com:5432`, IPv4). `Database:Provider=postgres`;
  schemat migreras automatiskt vid start (`Migrate()`). Dev = SQLite (`EnsureCreated`).
  > ⚠️ Supabases *direkt*-host (`db.<ref>.supabase.co`) är IPv6-only → oåtkomlig från App
  > Service (som bara når utåt via IPv4). Använd **alltid** session-poolern.
- **Hemligheter:** **Azure Key Vault** (`kv-stuffinabox-andree`, RBAC). Appens
  system-assigned managed identity läser DB-sträng, JWT, Brevo-nyckel och OAuth-secrets via
  **Key Vault-referenser** — inget känsligt i kod, repo eller CI/CD-pipeline.
- **Inloggning:** e-post/lösenord **med e-postverifiering** (mjuk gating: overifierade
  blockeras från att skapa utrymmen/inbjudningar), **Google** och **Microsoft**.
- **E-post:** **Brevo SMTP** (gratis, 300/dygn) via utbytbar `SmtpEmailService` (MailKit)
  bakom `Email:Provider=smtp`. Skickar verifierings- och återställningsmejl.
- **CI/CD:** GitHub Actions (`.github/workflows/deploy.yml`) deployar via **OIDC** (ingen
  lagrad Azure-hemlighet). Engångs-behörighet: `infra/setup-github-oidc.ps1`. Pipelinen
  hanterar **inga** hemlighetsvärden — de bor i Key Vault.

## ⚠️ Återstår / att tänka på

- **Always-On / kallstart:** F1 saknar Always-On → appen kallstartar efter inaktivitet.
  Sätt `appServiceSku=B1` (~$13/mån) för att slippa det (krävs även för custom-domän-TLS).
- **Bildlagring:** kör på **lokal disk** på App Service (`Storage:Provider=local`) — instans-
  bunden, försvinner vid ominstallation. För durabilitet: Azure Blob (kräver en Blob-provider
  i appen — finns ej än; `storage.bicep` är förberedd) eller Cloudflare R2 (`R2StorageService`
  finns redan). Notera även: SkiaSharp på App Service Linux kan behöva extra systembibliotek
  för bildbearbetning — verifiera en uppladdning.
- **GitHub-pipeline:** sätt secrets/vars i repot (se `infra/github-oidc-setup.md`) för att
  aktivera auto-deploy vid push till `main`. (OIDC-behörigheten på Azure-sidan är klar.)
- **Legal:** fyll i `[Företagsnamn]`/`[kontakt@…]` i `legalContent.ts` + ev. juristgranskning.
- **Övervakning:** bara Serilog till fil idag; överväg Application Insights / Sentry.
- **Bredare rate limiting** (idag bara `/auth/*`) och **ETag/Cache-Control** — se nedan.

## 🟡 Funktionsluckor användare lär förvänta sig

- **Glömt lösenord — ✅ byggt.** Hela flödet finns (`forgot-password`/`reset-password`,
  reset-vy via `#reset=`-deeplink, engångs-token 1 h, återkallar sessioner). E-postkonton
  lagrar nu adressen i klartext (`UserIdentity.Email`) — det medvetna "noll-PII"-valet är
  alltså frångått för att kunna kontakta användare.
  - ⚠️ **Kvarstår:** koppla in en **riktig e-postleverantör.** Default `IEmailService`
    (`LoggingEmailService`) *loggar* bara länken — inga mejl skickas. Lägg en SMTP-/
    SendGrid-/Resend-/Supabase-implementation bakom `Email:Provider` och sätt `App:BaseUrl`
    (publik URL för länkarna). OAuth-konton lagrar fortfarande ingen e-post — vill man
    mejla dem måste e-post fångas från providern (separat jobb).
- **GDPR — ✅ byggt.** Kontoradering (`DELETE /account`) och dataexport
  (`GET /account/export`, JSON) finns, med UI under Inställningar → "Konto & data".
  Radering tar bort allt: utrymmen/lådor/föremål (+ foton), medlemskap/inbjudningar,
  sessioner, reset-tokens, inställningar och identiteten — och drar med delade
  utrymmen så medlemmar tappar åtkomst. (Kvarstår ev.: en *ångerfrist*/mjuk radering
  om ni vill, men hård radering uppfyller rätten till radering.)
- **Villkor & integritetspolicy — ✅ byggt.** Användarvillkor + Integritetspolicy finns
  som tvåspråkiga sidor (`features/legal/`), nåbara från login och Inställningar.
  Integritetspolicyn speglar appens faktiska databehandling.
  - ⚠️ **Kvarstår:** låt en jurist granska texterna och fyll i platshållarna
    `[Företagsnamn]` och `[kontakt@dindomän.se]` i `legalContent.ts`.

## 🟢 Design / UX att överväga

- **Medlemsnamn — ✅ byggt.** Användare kan sätta ett **smeknamn** (Inställningar →
  Smeknamn, `UserSettings.DisplayName`). I delningspanelen visas smeknamnet, annars
  e-postadressen, annars en generisk etikett. Ägaren ser alltså *vem* som gått med.
  (E-postkonton lagrar redan adressen; OAuth-konton utan vare sig smeknamn eller e-post
  faller tillbaka på den generiska etiketten.)
- **Designtrohet Atelier/Pop** — bara "alt. 2" (utrymmes-korten) är gjort; resten
  ligger på token-nivå. Dokumenterat i [HANDOFF.md](../HANDOFF.md). Inte en blockerare.
- **Backend-felmeddelanden** vid bilduppladdning är hårdkodad svenska
  (`UploadItemPhotoCommandHandler`) — i18n täcker bara frontend.

## 🟣 Effektivitet & fler gratis-förberedelser (hjälper både webb och mobil)

Lågrisk-förbättringar som inte försämrar webben:

- **N+1-frågorna — ✅ fixat.** `GetSpaces` och `GetBoxesBySpace` räknade tidigare
  föremål med en separat fråga per låda. De använder nu en batch-fråga
  (`IItemRepository.GetCountsByBoxAsync`, en `GROUP BY` per ägare) och summerar i minnet.
  Vinsten är **oberoende av vilken databas** som körs.
- **Bredare rate limiting** — idag bara på `/auth/*`; en generös global gräns skyddar
  hela API:t mot missbruk.
- **ETag / `Cache-Control` på GET-svar** — sparar bandbredd (särskilt mobil) via 304.
- **Exponera OpenAPI-specen i produktion (read-only)** — låter mobilutvecklare
  *generera* en typad API-klient istället för att handskriva den (idag bara i dev).
- **Felövervakning** — bara Serilog till fil idag; överväg Sentry/App Insights för
  fel + metrics.

Förmodligen onödigt nu (YAGNI): idempotency-nycklar för POST, "hantera enheter"-vy.

---

## 🔵 Skalning: databas & bildlagring (bara om ni växer förbi en instans)

> **Viktigt:** SQLite är förmodligen inte flaskhalsen — frågemönstret (N+1 ovan) är
> det. Migrera **inte** i förtid. Den verkliga förberedelsen finns redan:
> `IStorageService` (bilder) och EF Core provider-switch (databas) i
> `Infrastructure/DependencyInjection.cs`. Byt först när **(1)** ni behöver fler än en
> instans, eller **(2)** ni vill ha riktig backup/durabilitet utan att kopiera en fil.

### Databas

| Alternativ | Skalning | Effektivitet | Säkerhet | Pris | Kod-insats |
|---|---|---|---|---|---|
| **SQLite** (nu) | 1 instans (en skrivare) | Snabb läsning, lokal | Ingen nätverksyta (plus) | $0 | — |
| **Serverless Postgres** (Supabase / Neon) | Auto, scale-to-zero | Bra; kallstart vid noll-trafik | TLS + managed, backup ingår | Gratis-tier → betala vid trafik | Liten |
| **Managed Postgres** (Azure DB / RDS) | Vertikalt + läsrepliker | Stark, JSON + fulltext | TLS, roller, managed patchning | ~$15–30/mån floor | Liten |
| **Turso / libSQL** | Distribuerad/edge | Snabb, SQLite-dialekt | TLS, managed | Generös gratis-tier | Minst (SQLite-kompatibel) |
| **SQL Server / Azure SQL** | Bra | Stark | Stark | Dyrast (licens) | Liten |

### Bildlagring (separat fråga)

| Alternativ | Skalning | Säkerhet | Pris (nyckel: **egress**) | Kod-insats |
|---|---|---|---|---|
| **Lokal disk** (nu) | 1 instans + volym | Filrättigheter + signerade `/uploads`-URL:er (HMAC-token, 403 utan) | $0 | — |
| **Cloudflare R2** | Obegränsad | Signerade URL:er, TLS | Billig lagring, **ingen egress-avgift** ✅ | Liten |
| **Backblaze B2** | Obegränsad | TLS, signerade URL:er | Billigast lagring | Liten |
| **Azure Blob / S3** | Obegränsad | Moget, IAM/SAS | Lagring billig men **egress kostar** | Liten |

### Beslut / status — ✅ DRIFTSATT

Hosten blev **Azure App Service** (inte Container Apps) — enklast för en .NET-monolit som
serverar sin egen SPA. Se "[I produktion nu](#-i-produktion-nu-azure-app-service)" överst för
den faktiska uppsättningen. Sammanfattat:

- **Databas → Supabase PostgreSQL** (`eu-north-1`/Stockholm), live via **session pooler**
  (IPv4). `Npgsql` + `Database:Provider=postgres`; schemat migreras vid start. Dev = SQLite.
  Migrationsuppsättning: `InitialCreate`, `EnableRowLevelSecurity`, `AddItemEnrichmentStatus`,
  `AddEmailVerification`.
- **Säkerhet på DB:n:** **RLS påslaget på alla tabeller** (nekar PostgREST/anon-vägen; appen
  kör som ägare och kringgår RLS).
- **TLS:** `SSL Mode=Require;Trust Server Certificate=true` mot poolern. (Poolerns cert kedjar
  inte till den buntade `prod-ca-2021.crt`, så `VerifyFull` används inte mot poolern — den
  CA:n gäller direkt-anslutningen, som vi ändå inte kan nå pga IPv6.)
- **Bilder:** lokal disk i prod just nu. `R2StorageService` (Cloudflare R2, presignerade
  URL:er, noll egress) finns färdig bakom `Storage:Provider=r2` när durabel lagring behövs.
- **SQLite-sårbarheten NU1903** (CVE-2025-6965) är **löst i prod** (Postgres).
- **Hemligheter** i Azure Key Vault (se överst), **inte** som ACA-/container-secrets.

---

## 📱 Flera klienter (Android / iOS mot samma backend)

Kärnan är redan mobilvänlig — ett rent REST/JSON-API som native-appar kan prata med
direkt. CORS spelar ingen roll för native-klienter. Följande **förberedelser är
gjorda** så att en mobilstart blir smidig utan att försämra webben:

- **API-versionering** — allt ligger nu under `/api/v1/...` (prefix centraliserat i
  `ApiRoutes.cs`). En framtida `v2` blir en avgränsad ändring utan att bryta webben.
- **Felkoder** — `GlobalExceptionHandler` returnerar ett stabilt, maskinläsbart
  `code` (`not_found`, `forbidden`, `validation_error`, …) i `ProblemDetails`, så varje
  klient översätter felen själv. Registreringskonflikt returnerar `code: "email_taken"`.
- **Token-baserad refresh** — webben använder fortfarande HttpOnly-cookien (refresh-token
  läcker aldrig till JavaScript). Native-klienter signalerar med headern `X-Client: mobile`
  och får då refresh-token **i svarsbody** (login/register/refresh) att lagra i
  Keychain/Keystore, och förnyar via headern `X-Refresh-Token`. Säkrat med test.

**Medvetet INTE byggt i förväg** (görs tillsammans med appen, kräver en riktig klient):

- **OAuth native-flöde** (behöver appens redirect-schema / universal link).
- **Universal Links / App Links** så delningslänkar (`/#invite=<token>`) öppnar appen —
  kräver appens bundle-id:n + att backend serverar `apple-app-site-association` /
  `assetlinks.json`.
- **Push-notiser** (APNs/FCM) — ny backend-infra, bara om det önskas.

---

## Rekommenderad ordning härifrån

Allt under "I produktion nu" är driftsatt och verifierat. Återstående, i prioritet:

1. **Aktivera GitHub-pipelinen** — sätt secrets/vars enligt `infra/github-oidc-setup.md`,
   så deploy sker automatiskt vid push till `main` (OIDC-behörigheten är redan klar).
2. **F1 → B1** om kallstarter stör (krävs även för egen domän + TLS).
3. **Durabel bildlagring** (Azure Blob eller R2) innan appen blir bild-tung.
4. **Legal-platshållare** i `legalContent.ts` + ev. juristgranskning.
5. Polish/skalning (övervakning, bredare rate limiting, ETag) efter behov.
