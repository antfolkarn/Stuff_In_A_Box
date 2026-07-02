# StuffInABox

> Ett sökbart register för fysisk förvaring. Lägg in vad som ligger i varje låda **en** gång — sök sedan istället för att öppna varje kartong i garaget, på vinden eller i förrådet.

StuffInABox löser ett *minnesproblem*, inte ett organisationsproblem. Varje låda har ett **permanent nummer** som du skriver på lådan. Vilket utrymme lådan står i är en separat, ändringsbar egenskap — att flytta en låda ändrar aldrig dess nummer. Föremål taggas brett (synonymer, kategori, material) så att en sökning på "täcke" hittar en låda märkt "Filtar".

Utrymmen kan **delas**: ägaren skapar en delningslänk och inbjudna användare kan se och redigera lådor och föremål — men bara ägaren och inbjudna medlemmar når innehållet (se [Säkerhet](#säkerhet)).

Gränssnittet finns på **svenska och engelska** och väljs automatiskt efter webbläsaren (engelska om svenska inte är förstaspråk), med manuell växling i inställningarna.

---

## Innehåll
- [Teknikstack](#teknikstack)
- [Projektstruktur](#projektstruktur)
- [Komma igång](#komma-igång)
- [Konfiguration](#konfiguration)
- [Testning](#testning)
- [Säkerhet](#säkerhet)
- [Arkitektur](#arkitektur)

---

## Teknikstack

| Del | Val |
|-----|-----|
| Backend | ASP.NET Core Minimal API (.NET 10), Clean Architecture |
| API | Versionerat under `/api/v1`; maskinläsbara felkoder; mobilvänlig auth (token i body via `X-Client: mobile`) |
| CQRS | MediatR 14 (Commands/Queries) + FluentValidation pipeline |
| Databas | EF Core 10 — SQLite (dev) / **PostgreSQL/Supabase** (prod), provider-switch via `Database:Provider` |
| Auth | JWT (15 min) + refresh-token (HttpOnly-cookie, 7 dagar); **e-postverifiering** (mjuk gating); OAuth (PKCE): **Google, Microsoft**, Apple (förberedd) |
| E-post | `IEmailService` — loggande (dev) eller **SMTP/MailKit** (prod, utbytbar; kör Brevo) för verifierings- och återställningsmejl |
| Bilder | `IStorageService` — lokal disk / **Cloudflare R2** (presignerade URL:er) + SkiaSharp (validering & EXIF-strip) |
| Igenkänning | **Bulk-fotouppladdning** med bakgrundsigenkänning (max 3 parallellt) via lokal vision-modell (Ollama, t.ex. `gemma3`) som fyller i namn + sökbara taggar; `ITaggingService` (tokenizer/Claude) |
| Loggning | Serilog → konsol **och** roterande dagsfil |
| Frontend | React 18 + TypeScript + Vite, React Query + Zustand |
| Tema & design | Ljust/mörkt läge + **sex designer** (Standard, Atelier, Pop, Nord, Console, Ledger; persisterat, följer kontot), respekterar OS-inställning |
| Språk | Svenska + engelska, webbläsardetektering (lätt egen i18n, inga beroenden) |
| Delning | Delningslänkar per utrymme + medlemskap (ägare-eller-medlem-auktorisering); medlemmar visas med smeknamn, annars e-post |
| Tester | xUnit + Moq (backend), WebApplicationFactory (integration) — **120+ tester** + Vitest (frontend) |
| Drift | **Azure App Service** via Bicep IaC ([`infra/`](infra/)), hemligheter i **Azure Key Vault**, **OIDC**-deploy med GitHub Actions; även Dockerfile + docker-compose, health checks |

---

## Projektstruktur

```
StuffInABox.slnx
├── src/
│   ├── StuffInABox.Domain/          # Entiteter, value objects, repository-gränssnitt (inga beroenden)
│   ├── StuffInABox.Application/      # Use cases (CQRS), DTOs, service-gränssnitt, validators
│   ├── StuffInABox.Infrastructure/  # EF Core, repo-implementationer, storage, tagging, imaging
│   └── StuffInABox.Web/             # Minimal API, auth, middleware, composition root
│       └── ClientApp/               # React + TypeScript SPA (byggs till wwwroot/)
└── tests/
    ├── StuffInABox.Domain.Tests/
    ├── StuffInABox.Application.Tests/
    ├── StuffInABox.Infrastructure.Tests/
    └── StuffInABox.Web.Tests/        # Integrationstester
```

Beroenderiktningen pekar alltid inåt: **Web → Infrastructure → Application → Domain**. Domain känner inte till något av de yttre lagren. Se [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

---

## Komma igång

### Förutsättningar
- **.NET 10 SDK**
- **Node.js 20+** och npm

### Kör i utvecklingsläge (två terminaler)

```bash
# Terminal 1 – backend (http://localhost:5184)
cd src/StuffInABox.Web
dotnet run

# Terminal 2 – frontend (http://localhost:5173, proxar /api → 5184)
cd src/StuffInABox.Web/ClientApp
npm install
npm run dev
```

Öppna http://localhost:5173. Databasen (`stuffinabox.db`) och migrationer skapas automatiskt vid första start.

### Produktionsbygge

```bash
# Bygg SPA:n in i wwwroot/ och kör som en enda app
cd src/StuffInABox.Web/ClientApp && npm run build
cd .. && dotnet run
```

ASP.NET serverar då både API:t och den byggda SPA:n från samma origin (`MapFallbackToFile`).

### Kör med Docker

```bash
# Sätt en riktig Jwt-secret i docker-compose.yml först (min 32 tecken)
docker compose up --build
```

Appen körs då på http://localhost:8080. Data, uppladdningar och loggar persisteras i volymen `sib-data`. `docker-compose.yml` innehåller även en valfri (utkommenterad) `ollama`-tjänst för lokal bildigenkänning.

**Health checks:** `GET /health` (liveness) och `GET /health/ready` (readiness, kollar databasen) — för load balancers / container-orkestrering.

---

## Produktion (Azure)

Appen är **driftsatt på Azure App Service** med all infrastruktur som kod (Bicep) under
[`infra/`](infra/) — live på <https://stuffinabox-andree.azurewebsites.net>.

- **Hosting:** App Service (Linux, F1) provisionerat via `infra/main.bicep` (subscription-scope).
- **Databas:** Supabase PostgreSQL via session pooler (IPv4).
- **Hemligheter:** **Azure Key Vault** — appens managed identity läser DB-sträng, JWT, e-post-
  och OAuth-secrets via Key Vault-referenser. Inget känsligt i kod, repo eller pipeline.
- **E-post:** Brevo SMTP. **Inloggning:** e-post (verifierad), Google, Microsoft.
- **CI/CD:** GitHub Actions med **OIDC** (`.github/workflows/deploy.yml`).

Detaljer i [docs/PRODUKTION.md](docs/PRODUKTION.md), [infra/README.md](infra/README.md) och
[infra/github-oidc-setup.md](infra/github-oidc-setup.md).

---

## Konfiguration

Alla värden ligger i `src/StuffInABox.Web/appsettings.json` (hemligheter hör hemma i user-secrets eller miljövariabler i produktion).

| Nyckel | Beskrivning |
|--------|-------------|
| `Jwt:Secret` | HMAC-nyckel (≥32 tecken). **Måste** sättas — finns i `appsettings.Development.json` för dev. |
| `App:BaseUrl` | Publik bas-URL för länkar i e-post (t.ex. återställning). Tom = härleds från requesten (sätt i produktion bakom proxy). |
| `Email:Provider` | `log` (default — loggar meddelandet så flöden funkar utan leverantör) eller `smtp` (utbytbar SMTP via MailKit; kör Brevo i prod). |
| `Email:Smtp:Host` / `:Port` / `:User` / `:Password` / `:From` / `:FromName` / `:UseSsl` | SMTP-uppgifter i `smtp`-läge — funkar med valfri leverantör (Brevo `smtp-relay.brevo.com:587`, Resend, Mailtrap …). |
| `Jwt:Issuer` / `Jwt:Audience` | JWT issuer/audience. |
| `Jwt:RefreshDays` | Livslängd för refresh-token (default 7). |
| `Database:Provider` | `sqlite` (default, dev) eller `postgres` (produktion, t.ex. Supabase). |
| `ConnectionStrings:Default` | Databasens connection string. För Postgres: använd `SSL Mode=VerifyFull` — appen pekar Npgsql på den buntade Supabase-root-CA:n (`certs/prod-ca-2021.crt`) automatiskt, så certet verifieras (MITM-skydd). |
| `Storage:Provider` | `local` (default, disk) eller `r2`/`s3` (Cloudflare R2 / S3-kompatibel objektlagring). |
| `Storage:LocalPath` | Mapp för uppladdade bilder i `local`-läge (tom = `wwwroot/uploads`). |
| `Storage:R2:AccountId` / `:AccessKey` / `:SecretKey` / `:Bucket` | R2-uppgifter i `r2`-läge (eller `Storage:R2:ServiceUrl` i stället för `AccountId`). |
| `Storage:UrlSigningKey` | HMAC-nyckel för signerade `local`-bild-URL:er. Tom = återanvänder `Jwt:Secret`. |
| `Storage:UrlValidityMinutes` | Giltighetstid för en signerad bild-URL (default 360 = 6 h). |
| `Tagging:Provider` | `tokenizer` (default) eller `claude`. |
| `Tagging:Claude:ApiKey` / `:Model` | Claude API-nyckel och modell (default `claude-haiku-4-5-20251001`). |
| `ImageRecognition:Provider` | `none` (default) eller `ollama` (lokal vision-modell som för-ifyller föremålsnamn). |
| `ImageRecognition:Ollama:BaseUrl` / `:Model` | Ollama-server (default `http://localhost:11434`) och modell (default `llava`). |
| `OAuth:Google:*` / `OAuth:Microsoft:*` / `OAuth:Apple:*` | Client-id/secret/redirect-URI per leverantör. Tomt = knappen ger ett vänligt felmeddelande. (Microsoft använder `common` = personliga + jobb-/skolkonton. I prod ligger secrets i Key Vault.) |
| `Cors:Origins` | Tillåtna origins för SPA:n. |
| `Logging:File:Path` | Sökväg för loggfil (default `logs/stuffinabox-.log`, roteras dagligen). |
| `RateLimiting:AuthPermitLimit` | Tillåtna `/auth/*`-anrop per minut och IP (default 10). |

### OAuth-inloggning lokalt (user-secrets)

Google-/Microsoft-inloggning kräver client-id + secret. Lägg dem i **user-secrets** (checkas aldrig in):

```bash
cd src/StuffInABox.Web
dotnet user-secrets set "OAuth:Google:ClientId" "<id>"
dotnet user-secrets set "OAuth:Google:ClientSecret" "<secret>"
# samma för OAuth:Microsoft:ClientId / :ClientSecret
```

Kör i **Development** (i Visual Studio: välj **`https`**-profilen i dropdownen bredvid Play — *inte* det bara projektnamnet, som kör Production utan user-secrets). Vid start loggas en rad `OAuth startup check — … Google ClientId set=True/False …` som direkt visar om nycklarna lästes in — kolla den först vid `#error=oauth_not_configured`.

**Om IDE:n inte kan läsa user-secrets** (sällsynt — sett när endpoint-säkerhet spärrar IDE-debuggerns barnprocess från `%APPDATA%\Microsoft\UserSecrets`): lägg samma OAuth-nycklar i en **git-ignorerad `src/StuffInABox.Web/appsettings.Local.json`**. Den ligger i projektmappen (alltid läsbar) och laddas sist i Development.

```json
{ "OAuth:Google:ClientId": "<id>", "OAuth:Google:ClientSecret": "<secret>" }
```

### Lokal bildigenkänning

När du lägger till ett föremål analyseras fotot och **namn + taggar** (föremål, färger, material, boktitlar) för-ifylls. Detta drivs av en lokal vision-modell via Ollama.

```bash
# Installera Ollama (https://ollama.com) och hämta en vision-modell
ollama pull llava            # eller t.ex. qwen2.5vl, llama3.2-vision, moondream
```

**På/av (toggle):**

| Läge | Standard | Slå av | Slå på |
|------|----------|--------|--------|
| `dotnet run` (Development) | **på** (`appsettings.Development.json` → `ImageRecognition:Provider = ollama`) | sätt `ImageRecognition__Provider=none` (miljövariabel) eller ändra dev-konfigen | — |
| Produktion (`appsettings.json`) | av (`none`) | — | sätt `ImageRecognition:Provider = ollama` |

```bash
# Tillfälligt av för en körning
ImageRecognition__Provider=none dotnet run        # bash
$env:ImageRecognition__Provider='none'; dotnet run  # PowerShell
```

Backenden POSTar fotot till Ollama (`http://localhost:11434`, modell via `ImageRecognition:Ollama:Model`) med en strikt svensk JSON-prompt. Allt sker lokalt, utan styckkostnad. Tjänsten följer "kastar aldrig"-kontraktet: om Ollama inte körs får du bara inga förslag — flödet fungerar ändå.

---

## Testning

```bash
dotnet test StuffInABox.slnx          # alla 108 backend-tester
```

| Lager | Antal | Vad testas |
|-------|------:|------------|
| Domain | 27 | Entitetsinvarianter, value object-validering |
| Application | 30 | Handler-logik, space-access-auktorisering, validering (inkl. **smeknamn**), service-orkestrering |
| Infrastructure | 19 | Repository-SQL (inkl. batch-räkning per låda), EF-config, bildbehandling, Claude-taggning, Ollama-igenkänning |
| Web | 30 | JWT, refresh-flöde (cookie + mobil-header), **lösenordsåterställning**, OAuth-start, rate limiting, bilduppladdning + **signerad bildåtkomst (403 utan token)**, **delning (åtkomstgräns + medlemsnamn)**, **GDPR export/radering**, **API-versionsrutter (v1, inte SPA-fallback)**, health checks, fel-mappning |

Utvecklat test-drivet: testet skrivs före implementationen.

### Frontend (Vitest)

```bash
cd src/StuffInABox.Web/ClientApp
npm test            # kör en gång
npm run test:watch  # watch-läge
```

Lätt enhetstestning (jsdom) av ren logik: i18n-översättning/fallback, deep-link-parsing
(`#invite=`/`#reset=`/`#box=`), medlemsvisning (smeknamn → e-post → fallback) och
utseende-logik (utloggad → Pop/ljust, inloggad → prefs). Fler komponenttester kan läggas
till med Testing Library.

---

## Säkerhet

- **Minimal PII.** För e-postinloggning sparas `SHA256(e-post)` (för uppslag) + BCrypt-hash av lösenordet, **samt e-postadressen i klartext** för att kunna kontakta användaren (t.ex. lösenordsåterställning). För OAuth sparas `(provider, sub)` samt e-postadressen från Google/Microsoft (så att admin kan identifiera kontot); Apple lämnar ingen e-post och sparas som enbart `(provider, sub)`.
- **Glömt lösenord.** `POST /auth/forgot-password` skickar en återställningslänk (alltid `200`, avslöjar aldrig om adressen finns). Återställnings-token lagras som hash, är engångs, går ut efter 1 timme; vid lyckad återställning återkallas alla sessioner. E-post skickas via `IEmailService` (logg-default; riktig leverantör bakom `Email:Provider`).
- **GDPR.** Användaren kan exportera all sin data (`GET /account/export`, JSON) och radera kontot (`DELETE /account`) — radering tar bort allt: utrymmen/lådor/föremål (+ foton), medlemskap/inbjudningar, sessioner, reset-tokens, inställningar och identiteten.
- **Refresh-tokens** lagras endast som SHA-256-hash, levereras i `HttpOnly; SameSite=Strict`-cookie, roteras vid varje förnyelse och kan återkallas vid utloggning.
- **Bilduppladdning** valideras via magic bytes (JPEG/PNG/WEBP), max 10 MB, och **EXIF strippas** genom om-kodning (skyddar mot GPS-läckage).
- **Bildåtkomst** är signerad: `/uploads/{nyckel}` serveras bara med en giltig, tidsbegränsad HMAC-token i query (`?sig=`) — annars `403`. Lagringsnycklarna är dessutom slumpade GUID:er. Modellen speglar presignerade URL:er i R2/S3 (planerad produktionslagring) och låter `<img>` ladda bilden utan Authorization-header.
- **Rate limiting** på alla `/auth/*` (per IP).
- **Säkerhetsheaders** (CSP, `X-Frame-Options: DENY`, `X-Content-Type-Options: nosniff`, m.fl.) + HSTS i produktion.
- **Åtkomstkontroll & delning**: all läsning och skrivning av lådor/föremål går genom `ISpaceAccessService`, som auktoriserar **ägare-eller-medlem** mot utrymmet och annars svarar **403**. Allt innehåll ägs av utrymmets ägare; inbjudna medlemmar kan redigera lådor och föremål men inte hantera utrymmet (byta namn/ikon, flytta lådor, ta bort, bjuda in). En extra kontroll verifierar att en låda faktiskt ligger i det auktoriserade utrymmet, så en medlem aldrig når ägarens andra, icke-delade utrymmen (lådnummer är per ägare och disambigueras med `spaceId`).
- **Delningslänkar**: slumpade, återkallningsbara tokens — ingen e-posthantering, matchar PII-modellen. Ägaren kan när som helst återkalla länken eller ta bort en medlem.
- **Medlemsnamn**: en medlem kan sätta ett valfritt **smeknamn** (`UserSettings.DisplayName`) som visas för utrymmets ägare. Saknas smeknamn visas medlemmens **e-postadress** för ägaren (om en sådan finns lagrad, dvs. e-postkonton) — den som inte vill exponera sin e-post sätter ett smeknamn. OAuth-konton utan smeknamn visas med en generisk etikett.

---

## Arkitektur

Se [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) för lagerdiagram, request-flöde genom MediatR-pipelinen, auth-flöden (lösenord, OAuth, refresh) och den asynkrona taggnings-workern.
