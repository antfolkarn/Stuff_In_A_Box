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
| Databas | EF Core 10 + SQLite (provider-switch för Postgres/SQL Server) |
| Auth | JWT (15 min) + refresh-token (HttpOnly-cookie, 7 dagar) + Google/Apple OAuth (PKCE) |
| Bilder | `IStorageService`-abstraktion (lokal disk nu, molnredo) + SkiaSharp (validering & EXIF-strip) |
| Taggning | `ITaggingService` — tokenizer (default) eller Claude API (feature-flagga) |
| Loggning | Serilog → konsol **och** roterande dagsfil |
| Frontend | React 18 + TypeScript + Vite, React Query + Zustand |
| Tema & design | Ljust/mörkt läge + tre designer (persisterat, följer kontot), respekterar OS-inställning |
| Språk | Svenska + engelska, webbläsardetektering (lätt egen i18n, inga beroenden) |
| Delning | Delningslänkar per utrymme + medlemskap (ägare-eller-medlem-auktorisering) |
| Tester | xUnit + Moq (backend), WebApplicationFactory (integration) — **91 tester** |
| Drift | Dockerfile (multi-stage) + docker-compose, health checks, GitHub Actions CI |

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

## Konfiguration

Alla värden ligger i `src/StuffInABox.Web/appsettings.json` (hemligheter hör hemma i user-secrets eller miljövariabler i produktion).

| Nyckel | Beskrivning |
|--------|-------------|
| `Jwt:Secret` | HMAC-nyckel (≥32 tecken). **Måste** sättas — finns i `appsettings.Development.json` för dev. |
| `Jwt:Issuer` / `Jwt:Audience` | JWT issuer/audience. |
| `Jwt:RefreshDays` | Livslängd för refresh-token (default 7). |
| `Database:Provider` | `sqlite` (default). Postgres/SQL Server finns förberett i `Infrastructure/DependencyInjection.cs`. |
| `ConnectionStrings:Default` | Databasens connection string. |
| `Storage:LocalPath` | Mapp för uppladdade bilder (tom = `wwwroot/uploads`). |
| `Tagging:Provider` | `tokenizer` (default) eller `claude`. |
| `Tagging:Claude:ApiKey` / `:Model` | Claude API-nyckel och modell (default `claude-haiku-4-5-20251001`). |
| `ImageRecognition:Provider` | `none` (default) eller `ollama` (lokal vision-modell som för-ifyller föremålsnamn). |
| `ImageRecognition:Ollama:BaseUrl` / `:Model` | Ollama-server (default `http://localhost:11434`) och modell (default `llava`). |
| `OAuth:Google:*` / `OAuth:Apple:*` | Client-id m.m. Tomt = knapparna ger ett vänligt felmeddelande. |
| `Cors:Origins` | Tillåtna origins för SPA:n. |
| `Logging:File:Path` | Sökväg för loggfil (default `logs/stuffinabox-.log`, roteras dagligen). |
| `RateLimiting:AuthPermitLimit` | Tillåtna `/auth/*`-anrop per minut och IP (default 10). |

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
dotnet test StuffInABox.slnx          # alla 91 backend-tester
```

| Lager | Antal | Vad testas |
|-------|------:|------------|
| Domain | 27 | Entitetsinvarianter, value object-validering |
| Application | 27 | Handler-logik, space-access-auktorisering, validering, service-orkestrering |
| Infrastructure | 18 | Repository-SQL, EF-config, bildbehandling, Claude-taggning, Ollama-igenkänning |
| Web | 19 | JWT, refresh-flöde (cookie + mobil-header), OAuth-start, rate limiting, bilduppladdning, **delning (åtkomstgräns)**, health checks, fel-mappning |

Utvecklat test-drivet: testet skrivs före implementationen.

---

## Säkerhet

- **Ingen PII lagras.** För e-postinloggning sparas `SHA256(e-post)` + BCrypt-hash av lösenordet. För OAuth sparas bara `(provider, sub)` — inget namn eller e-post.
- **Refresh-tokens** lagras endast som SHA-256-hash, levereras i `HttpOnly; SameSite=Strict`-cookie, roteras vid varje förnyelse och kan återkallas vid utloggning.
- **Bilduppladdning** valideras via magic bytes (JPEG/PNG/WEBP), max 10 MB, och **EXIF strippas** genom om-kodning (skyddar mot GPS-läckage).
- **Rate limiting** på alla `/auth/*` (per IP).
- **Säkerhetsheaders** (CSP, `X-Frame-Options: DENY`, `X-Content-Type-Options: nosniff`, m.fl.) + HSTS i produktion.
- **Åtkomstkontroll & delning**: all läsning och skrivning av lådor/föremål går genom `ISpaceAccessService`, som auktoriserar **ägare-eller-medlem** mot utrymmet och annars svarar **403**. Allt innehåll ägs av utrymmets ägare; inbjudna medlemmar kan redigera lådor och föremål men inte hantera utrymmet (byta namn/ikon, flytta lådor, ta bort, bjuda in). En extra kontroll verifierar att en låda faktiskt ligger i det auktoriserade utrymmet, så en medlem aldrig når ägarens andra, icke-delade utrymmen (lådnummer är per ägare och disambigueras med `spaceId`).
- **Delningslänkar**: slumpade, återkallningsbara tokens — ingen e-posthantering, matchar PII-modellen. Ägaren kan när som helst återkalla länken eller ta bort en medlem.

---

## Arkitektur

Se [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) för lagerdiagram, request-flöde genom MediatR-pipelinen, auth-flöden (lösenord, OAuth, refresh) och den asynkrona taggnings-workern.
