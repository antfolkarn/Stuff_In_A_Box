# Vägen till produktion — StuffInABox

Lägesbild över vad som återstår innan appen kan driftsättas, grundat på en
genomgång av config, auth, `Program.cs`, `docker-compose.yml` och login-flödet.

Appen är **funktionellt komplett och välbyggd**: Clean Architecture, 90 tester,
säkerhetsheaders, HSTS, rate limiting och health checks finns redan. Det som
återstår är mest **drift-config**, ett par **funktionsluckor** och **GDPR**.

_Senast uppdaterad: 2026-06-21._

---

## 🔴 Måste fixas före produktion (config, inte kod)

Inga kodändringar — men appen startar inte / fungerar inte korrekt utan dessa:

- **Riktig `Jwt:Secret`** — `docker-compose.yml` har platshållaren
  `change-me-to-a-real-secret-min-32-characters`. Sätt en riktig (≥32 tecken) via
  miljövariabel / secret store.
- **CORS + OAuth-redirect för produktionsdomänen** — `appsettings.json` har
  `Cors:Origins = http://localhost:5173` och OAuth-redirect-URI:er pekar på
  `localhost:7094`. Måste bytas till den riktiga domänen.
- **OAuth-nycklar** — Google/Apple `ClientId`/`Secret` är tomma → de knapparna ger
  bara ett felmeddelande tills de konfigureras. (E-post/lösenord fungerar utan dem.)
- **Backup av `/data`-volymen** — databas, uppladdningar och loggar ligger där.
  Ingen automatisk backup finns; sätt upp snapshot/cron.

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
- **Villkor & integritetspolicy** — login-skärmen hänvisar till "våra villkor och
  vår integritetspolicy" men det finns inga sidor/länkar bakom. Behövs riktiga texter.

## 🟢 Design / UX att överväga

- **Medlemmar visas som GUID** i delningspanelen
  (`ClientApp/src/features/space/SharePanel.tsx`) — "Medlem" + de första 8 tecknen av
  user-id. Ägaren kan inte se *vem* som gått med. Eftersom ingen e-post/namn lagras
  skulle detta kräva ett valfritt **visningsnamn/smeknamn**.
- **Designtrohet Atelier/Pop** — bara "alt. 2" (utrymmes-korten) är gjort; resten
  ligger på token-nivå. Dokumenterat i [HANDOFF.md](../HANDOFF.md). Inte en blockerare.
- **Backend-felmeddelanden** vid bilduppladdning är hårdkodad svenska
  (`UploadItemPhotoCommandHandler`) — i18n täcker bara frontend.

## 🟣 Effektivitet & fler gratis-förberedelser (hjälper både webb och mobil)

Lågrisk-förbättringar som inte försämrar webben:

- **Fixa N+1-frågorna** (störst effekt) — `GetSpacesQueryHandler` loopar utrymmen →
  lådor → en separat fråga per låda för föremål. Samma mönster i `Search`/`Labels`.
  Det här är den verkliga effektivitetsvinsten, **oberoende av vilken databas** som
  körs. Bör göras snart.
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
| **Lokal disk** (nu) | 1 instans + volym | Filrättigheter, `/uploads` saknar åtkomstkontroll | $0 | — |
| **Cloudflare R2** | Obegränsad | Signerade URL:er, TLS | Billig lagring, **ingen egress-avgift** ✅ | Liten |
| **Backblaze B2** | Obegränsad | TLS, signerade URL:er | Billigast lagring | Liten |
| **Azure Blob / S3** | Obegränsad | Moget, IAM/SAS | Lagring billig men **egress kostar** | Liten |

### Beslut / rekommendation

- **Databas → Supabase (serverless PostgreSQL).** ✅ *Vald väg.* Gratis vid låg trafik,
  skalar när det behövs, TLS + automatisk backup utan drift, och EF Core-providern är
  redan förberedd. (Neon är ett likvärdigt serverless-Postgres-alternativ; Turso är
  frestande för minimal kodändring men Postgres är mer beprövat med EF Core.)
- **Bilder → Cloudflare R2.** För en bild-tung app är **egress** (utgående trafik när
  bilder visas) den stora kostnaden — R2 har **noll egress-avgift**, markant billigare
  än S3/Azure Blob vid frekvent bildvisning. Backblaze B2 är näst billigast.
- **SQLite-sårbarheten NU1903** (CVE-2025-6965) löser sig på köpet vid byte till
  Postgres; tills dess: låg risk, dokumenterad i [HANDOFF.md](../HANDOFF.md).

**Migrationsskiss när tröskeln nås:**
1. Skapa ett Supabase-projekt → sätt `Database:Provider=postgres` +
   `ConnectionStrings:Default` (installera `Npgsql.EntityFrameworkCore.PostgreSQL`,
   avkommentera grenen i `DependencyInjection.cs`).
2. Kör `dotnet ef database update` mot Postgres (migrationerna är providerneutrala —
   verifiera att inget SQLite-specifikt smugit sig in).
3. Lägg till en `R2StorageService : IStorageService` (S3-kompatibelt API) och
   registrera den bakom en `Storage:Provider`-flagga; behåll lokal disk som default.
4. Flytta befintliga bilder från volymen till R2 (engångs-kopiering).

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

## Rekommenderad ordning

1. **Config-blockerarna** (Jwt-secret, CORS/redirect, OAuth, backup) — krävs, snabbt.
2. **Besluta e-post-frågan** → lås upp lösenordsåterställning (annars är
   e-postinloggning bräcklig).
3. **GDPR: kontoradering + export** — viktigt för svenska användare.
4. Resten är polish/skalning som kan komma efter lansering.
