# Vägen till produktion — StuffInABox

Lägesbild över vad som återstår innan appen kan driftsättas, grundat på en
genomgång av config, auth, `Program.cs`, `docker-compose.yml` och login-flödet.

Appen är **funktionellt komplett och välbyggd**: Clean Architecture, 108 backend-tester
(+ Vitest på frontend), säkerhetsheaders, HSTS, rate limiting och health checks finns
redan. Det som återstår är mest **drift-config** och ett par **funktionsluckor**.

_Senast uppdaterad: 2026-06-23._

---

## 🔴 Måste fixas före produktion (config, inte kod)

Inga kodändringar — men appen startar inte / fungerar inte korrekt utan dessa:

- **Riktig `Jwt:Secret`** — ✅ wiring klar: `docker-compose.yml` läser `Jwt__Secret`
  från `.env` (gitignorerad), och samma namn sätts som ACA-hemlighet. **Kvarstår:** sätt
  ett riktigt värde (`openssl rand -base64 32`).
- **CORS + OAuth-redirect för produktionsdomänen** — `appsettings.json` har
  `Cors:Origins = http://localhost:5173` och OAuth-redirect-URI:er pekar på
  `localhost:7094`. Måste bytas till den riktiga domänen.
- **OAuth-nycklar** — Google/Apple `ClientId`/`Secret` är tomma → de knapparna ger
  bara ett felmeddelande tills de konfigureras. (E-post/lösenord fungerar utan dem.)
- **Backup** — ✅ i praktiken löst av stack-valet: Supabase har automatisk DB-backup,
  R2 är durabelt. Loggar går till stdout i ACA. (Inget `/data`-volym-beroende kvar i
  prod-stacken.)

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

### Beslut / status — ✅ GENOMFÖRT (branch `prod-migration`)

Eftersom hosten blev **Azure Container Apps** (stateless, flyktigt filsystem) gjordes
flytten nu i stället för senare. Allt nedan är **byggt och verifierat live mot Supabase**.

- **Databas → Supabase (serverless PostgreSQL), `eu-north-1`/Stockholm.** ✅ Byggt:
  `Npgsql` + `Database:Provider=postgres` aktiverat. Dev förblir SQLite (`EnsureCreated`);
  prod kör Postgres (`Migrate`). En Postgres-migrationsuppsättning (`InitialCreate` +
  `EnableRowLevelSecurity`). Schemat är **applicerat och verifierat mot riktig PG17**.
- **Säkerhet på DB:n:** **RLS påslaget på alla tabeller** — Supabase exponerar
  public-schemat via PostgREST/anon-nyckeln, och RLS utan policies nekar den vägen
  (appen kör som ägare → kringgår RLS). Security-advisorn rapporterar inga fel.
- **Verifierad TLS:** `SSL Mode=VerifyFull` + buntad Supabase root-CA
  (`certs/prod-ca-2021.crt`, auto-injicerad av DI) — krypterat *och* MITM-skyddat.
- **Anslutning:** Session pooler (`aws-1-eu-north-1.pooler.supabase.com:5432`) = gratis
  IPv4, **inget IPv4-tillägg behövs**.
- **Bilder → Cloudflare R2.** ✅ Byggt: `R2StorageService : IStorageService` (AWSSDK.S3,
  presignerade URL:er) bakom `Storage:Provider=r2`. R2 har **noll egress-avgift** —
  stor besparing för en bild-tung app. Dev = lokal disk. *(Återstår: skapa R2-bucket +
  API-token och sätta nycklarna.)*
- **SQLite-sårbarheten NU1903** (CVE-2025-6965) är därmed **löst i prod** (Postgres).

**Återstår för deploy (steg D):** bygg image → push till registry → skapa Container App
(Sweden Central) → sätt hemligheter (`Jwt__Secret`, `ConnectionStrings__Default`,
`Storage__Provider=r2` + `Storage__R2__*`, `ASPNETCORE_ENVIRONMENT=Production`) →
CI-deploy via GitHub Actions. Se [HANDOFF.md §Produktionssättning](../HANDOFF.md).

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
