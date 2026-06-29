# Handoff — StuffInABox

Snabb lägesbild för att fortsätta på en annan dator. För djupare info: [README.md](README.md) och [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

_Senast uppdaterad: 2026-06-29._

---

## Vad det är
Webapp (svenska + engelska) för att katalogisera fysisk förvaring (utrymmen → lådor → föremål), sök, QR-etiketter, **delning av utrymmen**. .NET 10 (Clean Architecture) + React/TypeScript (Vite). Lösningsfil: **`StuffInABox.slnx`** (modern XML-format; öppnas i Visual Studio 17.13+).

## Kör igång
```bash
# Backend (serverar även den byggda SPA:n) — http://localhost:5184
cd src/StuffInABox.Web && dotnet run

# Frontend i dev-läge (valfritt) — http://localhost:5173, proxar /api → 5184
cd src/StuffInABox.Web/ClientApp && npm install && npm run dev
```
- **Tester:** `dotnet test StuffInABox.slnx` — **108 gröna** (Domain 27, Application 30, Infrastructure 21, Web 30). Frontend: `cd …/ClientApp && npm test` (Vitest, 16 tester).
- **Bygg SPA till wwwroot:** `cd src/StuffInABox.Web/ClientApp && npm run build`.

> ℹ️ **Arbetet ligger på branch `prod-migration`** (ej mergad till `main`). Prod deployar från denna branch via GitHub Actions — se nedan.

## Produktion — LIVE 🟢

**URL:** <https://stuffinabox-andree.azurewebsites.net> · **Verifiera körande build:** `GET /version` → `{ version, commit, buildTimeUtc }`.

**Stack i prod:** Azure **App Service** (Linux, F1 Free, `rg-stuffinabox-prod`, prenumeration "Stuff in a Box" / gmail-kontot — **inte** Fabege) + **Supabase** Postgres (`eu-north-1`) + **Brevo** SMTP (verifierings-/reset-mejl) + **Staik** bildigenkänning. Bildlagring: lokal disk på instansen (R2-stöd finns men är inte påslaget). All infra är **Bicep** (`infra/`), secrets i **Key Vault** (`kv-stuffinabox-andree`) via managed-identity-referenser. Full bild: [docs/ARCHITECTURE.md §7](docs/ARCHITECTURE.md).

**CI/CD (GitHub Actions, OIDC):** push till `prod-migration` → `.github/workflows/deploy.yml` bygger SPA + `dotnet publish` → Bicep-deploy → `az webapp deploy --type zip`. Auth via OIDC (Entra-app `stuffinabox-github-deploy`, federated credential för `prod-migration`, Contributor på suben). GitHub-secrets/vars är satta (se `infra/github-oidc-setup.md`). **Verifiera alltid efter deploy att `/version` visar rätt commit.**

**Bildigenkänning = Staik** (`api.staik.se`, hostat, OpenAI-kompatibelt, gratis nyckel, modell `gemma4:31b`). Provider väljs med `ImageRecognition:Provider=staik`; nyckel i KV-secret `Staik-ApiKey`. Self-hostad Ollama (`gemma3:12b` via Tailscale Funnel + Caddy) finns kvar som **fallback men är avstängd** (inkl. autostart — Ollama-genvägen flyttad från Startup-mappen till `%LOCALAPPDATA%\StuffInABox-disabled-autostart\`). Flippa tillbaka: `imageRecognitionProvider='ollama'` i `infra/main.bicepparam` + deploy, och starta Ollama/Caddy/Funnel lokalt (se [HANDOFF-stuffinabox-ollama-selfhost.md](../../Dev/Repos/HANDOFF-stuffinabox-ollama-selfhost.md) i Dev-repos).

**Återstår / att tänka på:**
1. **F1 kallstarts-race:** ingen Always-On → första anropet efter idle kan vara trögt. Robust fix: uppgradera till **B1** (`appServiceSku='B1'` i `main.bicepparam`, ~$13/mån).
2. **Deploy från `main`:** vill man deploya från `main` också krävs ytterligare en federated credential för den branchen (eller merga `prod-migration`→`main`).
3. **Config:** verifiera OAuth-redirect-URI:er och CORS mot prod-domänen; juristgranska villkor/policy + fyll i platshållare (`[Företagsnamn]` m.m.).
4. **Städning:** snurra DB-lösenordet om det använts lokalt; rensa ev. testdata i Supabase.

> **Deploy-fällor (bet oss 2026-06-29, dokumenterade i ARCHITECTURE §7):** Windows-`tar -a -c -f x.zip` ger ett **tar-arkiv**, inte en zip → använd Python `zipfile`/`shutil.make_archive`. `azure/webapps-deploy@v3` matad med en mapp triggar en Oryx-build som hänger OneDeploy på F1 → använd `az webapp deploy --type zip` med förbyggd zip. En avbruten OneDeploy kan lämna ett föräldralöst Kudu-lås (`/home/site/locks/deployment/info.lock`) → "Another deployment is in progress"; rensas via Kudu VFS (`DELETE` med `If-Match: *`).

## Färdigt och fungerar
- Kärna: utrymmen/lådor/föremål (CRUD), sök, etiketter med QR, oföränderliga lådnummer (per ägare).
- **Delning av utrymmen:** ägaren skapar en delningslänk (`#invite=<token>`); inbjudna blir medlemmar som kan se/redigera lådor & föremål men inte hantera utrymmet. Auktorisering via `ISpaceAccessService` (ägare-eller-medlem, annars 403). Allt innehåll ägs av utrymmets ägare; lådnummer disambigueras med `spaceId`. Ägare kan återkalla länk/ta bort medlem, medlem kan lämna. Se README §Säkerhet + ARCHITECTURE §4b.
- **Flerspråk (sv/en):** lätt egen i18n (`src/…/ClientApp/src/i18n/`), webbläsardetektering med engelska som fallback, manuell växling i inställningarna. `<html lang>` sätts via JS (rör inte CSP-hashen).
- Auth: JWT + roterande refresh-token (HttpOnly-cookie) + Google/Apple OAuth (PKCE, kräver konfig).
- **Villkor & integritetspolicy:** tvåspråkiga sidor i `…/ClientApp/src/features/legal/` (`legalContent.ts` + `LegalView.tsx`), nåbara från login och Inställningar. **Platshållare `[Företagsnamn]`/`[kontakt@…]` ska fyllas i + juristgranskas.**
- **GDPR:** dataexport (`GET /api/v1/account/export`, JSON) + kontoradering (`DELETE /api/v1/account`) via Inställningar → "Konto & data". Radering kaskaderar allt (utrymmen/lådor/föremål + foton, medlemskap/inbjudningar, sessioner, reset-tokens, inställningar, identitet) och drar med delade utrymmen.
- **Glömt lösenord:** e-postkonton lagrar nu adressen i klartext (`UserIdentity.Email`) för att kunna mejla återställningslänk. `forgot-password` (alltid 200, läcker inte) + `reset-password` (engångs-token, 1 h, återkallar sessioner). E-post via `IEmailService` — **default loggar bara länken** (`LoggingEmailService`); koppla in riktig leverantör (SMTP/SendGrid/Resend/Supabase) bakom `Email:Provider`. Reset-vy via `#reset=<token>`-deeplink.
- Bilduppladdning (magic-byte + EXIF-strip via SkiaSharp). **Uppladdningar lagras utanför `wwwroot`** (annars raderas de av SPA-bygget). **Bildåtkomst är signerad:** `/uploads/{nyckel}` serveras via `PhotoEndpoints` och kräver en giltig HMAC-token (`?sig=`, tidsbegränsad via `Storage:UrlValidityMinutes`, default 6 h) — annars 403. Signering i `IPhotoUrlSigner`/`PhotoUrlSigner`, URL byggs i `LocalFileStorageService.GetUrl`.
- **N+1-frågorna fixade:** `GetSpaces`/`GetBoxesBySpace` räknar föremål per låda via en batch-fråga (`IItemRepository.GetCountsByBoxAsync`, en `GROUP BY` per ägare) i stället för en fråga per låda.
- **Bildigenkänning:** foto → `{ namn, taggar }` (föremål, färger, material, boktitlar), asynkront via `ImageRecognitionWorker`. Tre providers bakom `ImageRecognition:Provider`: `none`/`ollama`/`staik`. **Prod kör `staik`** (se Produktion ovan). Prompt + parsning delas i `VisionRecognition`; transporten skiljer per provider. Se "Bildigenkänning – providers" nedan.
- **Lägg till föremål – foto eller manuellt:** "Lägg till"-rutan (`addItem/AddItemSheet`) har en toggle. **Foto** (default) = bulkuppladdning, igenkänningen fyller i namn+taggar i bakgrunden. **Manuellt** = skriv namn + valfria taggar (chips), inget foto — använder `AddItemCommand`. När man är inne i ett **delat** utrymme defaultas destinationen nu rätt (lådan slås upp med `spaceId` så den inte krockar med en egen låda med samma nummer).
- **`/version`-endpoint + UI-footer:** visar körande commit + byggtid (commit bakas in via `SourceRevisionId` i ett MSBuild-target). Fångar "stale deploy".
- **Smeknamn:** användare kan sätta ett valfritt smeknamn (`UserSettings.DisplayName`, Inställningar → Smeknamn). I delningspanelen visas smeknamn → annars e-post → annars generisk etikett. Resolvas batchat i `GetSpaceMembersQueryHandler` (smeknamn från `IUserSettingsRepository.GetDisplayNamesAsync`, e-post från `IUserIdentityRepository.GetEmailsAsync`). Migration: `…_AddUserDisplayName.cs`. Frontend: fält i `SettingsView.tsx`, helper `features/space/memberDisplay.ts`.
- **Utloggat utseende låst till Pop + ljust:** oinloggat läge visar alltid Pop-designen i ljust läge, oavsett cachade prefs (branding på login/reset). Först vid inloggning laddas användarens prefs (`loadFromServer`). Logiken finns i `settingsStore.ts` (`initialAppearance`/`applyLoggedOut`, persist cachar bara när inloggad) + pre-paint-skriptet i `index.html` (kollar `sessionStorage['sib_token']`). **OBS:** ändras index.html-skriptet måste CSP-hashen uppdateras (se Gotchas). Tema-toggeln på login flippar fortfarande ljust/mörkt transient (sparas inte).
- **Inställningar i databasen (cross-device):** `UserSettings`-tabell + `GET/PUT /api/v1/settings`. Tema (ljus/mörk/system) + design + smeknamn sparas på kontot och följer användaren. Inställningssida via kugghjuls­ikonen i headern. `localStorage` används som cache; flimmerfri init i `index.html`.
- **Tre designs (token-nivå):** Standard, Atelier (varmt papper, Manrope/Spectral, skarpa hörn), Pop (lila, Plus Jakarta/Bricolage, runda 20px hörn + 2px-kanter). Växlas via `data-design` + `data-theme` på `<html>`. Form-språk tokeniserat: `--bw`, `--r-xl/lg/md/sm/chip` per design i `src/StuffInABox.Web/ClientApp/src/index.css`.
- Drift: Dockerfile + docker-compose, GitHub Actions CI, health checks (`/health`, `/health/ready`), Serilog (konsol + roterande fil).
- **Mobil-förberett:** API:t är versionerat (`/api/v1`, prefix i `ApiRoutes.cs`), fel returnerar maskinläsbara `code` i `ProblemDetails`, och auth stöder native-klienter — `X-Client: mobile` ger refresh-token i body (webben behåller HttpOnly-cookien). OAuth native-flöde/universal links/push är medvetet inte byggt än. Se [docs/PRODUKTION.md](docs/PRODUKTION.md).

## Design-trohet — alternativ 2 gjort (utrymmes-korten)
Designerna byter **färg, typsnitt och form** men matchade tidigare **inte** prototyperna fullt ut, för prototyperna har **design-specifikt komponentutförande** som de delade komponenterna inte återskapar.

**Gjort (alt. 2):** Utrymmes-korten på startsidan är nu lyfta för **Pop**: varje utrymme får egen färg ur 6-färgspaletten (`#6C4CF1, #FF5C49, #12B5A4, #FF9E1B, #E8489E, #2E8BFF`) via `:nth-child(6n+k)`, fylld bricka med vit ikon + mjuk färgad skugga, hörn-färgklick (`::before`), färg-matchad kod-chip och hover i kortets egen färg. Standard/Atelier är orörda. Kod: `SpaceCard` i [HomeView.tsx](src/StuffInABox.Web/ClientApp/src/features/home/HomeView.tsx) (hook-klasser `space-grid`/`space-card`/`space-card-icon`/`space-card-code`) + Pop-regler i [index.css](src/StuffInABox.Web/ClientApp/src/index.css).

**Återstår (om man vill gå längre):**
1. Bygg Pop troget överallt, sen Atelier — design-specifik komponentkod skärm för skärm: per-**låda**-färg i [BoxView.tsx](src/StuffInABox.Web/ClientApp/src/features/box/BoxView.tsx), display-typsnitt på lådnummer, färgade chips i listor/etiketter (stort jobb).
3. Behåll resten på token-nivån.

Referensprototyper: `design_handoff_stuffinabox/StuffInABox - Atelier.dc.html` och `… - Pop.dc.html` (originalet är `StuffInABox.dc.html`). Råpaket: `design/*.zip`.

## Bildigenkänning – providers
- **Prod = Staik** (`ImageRecognition:Provider=staik`): hostat OpenAI-kompatibelt vision-API, gratis nyckel, modell `gemma4:31b`. `StaikImageRecognitionService` POSTar fotot som base64-`data:`-URI till `/v1/chat/completions`. Nyckel i KV-secret `Staik-ApiKey`. **Staik tar bara base64 (PNG/JPEG/HEIC), inte fjärr-URL:er och inte WebP.**
- **Dev:** default är `none` (no-op). För lokal igenkänning kan man köra `ollama` (installera Ollama + `ollama pull gemma3:12b`, sätt `ImageRecognition__Provider=ollama`). Prompt/parsning är gemensam (`VisionRecognition`), så svaren ser likadana ut.
- **Ollama som prod-fallback** (self-hostad via Tailscale Funnel + Caddy) är avstängd; se Produktion ovan för hur man flippar tillbaka.

## Gotchas (viktigt)
- **CSP-hash för tema-skriptet:** inline-skriptet i `ClientApp/index.html` sätter tema+design före paint och tillåts av CSP via en SHA-256-hash i `src/StuffInABox.Web/Middleware/SecurityHeadersMiddleware.cs`. **Nuvarande hash: `sha256-3rsySJz2ymADKD1OT95TaKKPyHvxLoWFfbFzorU+xzU=`.** Ändras skriptet: bygg SPA, ta SHA-256 (base64) av exakt texten mellan `<script>`/`</script>` i `wwwroot/index.html`, och uppdatera konstanten — annars blockerar CSP:n skriptet (flimmer/fel tema).
- **`.slnx`**, inte `.sln`. `dotnet new sln` skapar `.slnx` som default på denna SDK.
- **API-bas är `/api/v1`** (`ApiRoutes.cs`) — frontend `client.ts` har `baseURL: '/api/v1'`; refresh-cookiens Path och OAuth-redirect-URIs är också `/api/v1/auth`. **Lägger du en endpoint: använd ALLTID `ApiRoutes.V1` i `MapGet`/`MapPost` (och i `Results.Created`-Location), aldrig en hårdkodad `"/api/..."`-sträng.** En endpoint på fel prefix matchar inget API, faller igenom till SPA-fallbacken (`MapFallbackToFile`) och returnerar `index.html` (HTTP 200, `text/html`) — frontend försöker tolka HTML som JSON och visar "ett oväntat fel inträffade", inte ett 404. (Detta bet `labels`/`search`/`recognize` som mappades på `/api/...` utan `/v1`; fixat + regressionstest i `ApiVersionRouteIntegrationTests`.) Kollar du snabbt: oautentiserat ska `…/v1/<rutt>` ge `401`, inte `200 text/html`.
- **SkiaSharp Linux-assets** finns med (annars kraschar bilduppladdning i container/CI).
- **Windows PowerShell 5.1** manglar icke-ASCII i `Invoke-RestMethod -Body` och `curl -F` (multipart) ger HTTP 000 — använd .NET `HttpClient` eller integrationstest för svenska/multipart.
- **NU1903 (SQLite, CVE-2025-6965):** `SQLitePCLRaw.lib.e_sqlite3 2.1.11` (transitivt via EF Core Sqlite) flaggas som high. **Ingen patchad version finns ännu** (advisory: `patched: None`, `<= 2.1.11`, 2.1.11 är senaste). Faktisk risk låg: appen kör enbart parametriserad EF Core/LINQ, ingen rå SQL; CI failar inte på varningen. **Beslut: vänta på uppström** — bumpa så fort SQLitePCLRaw släpper en fix.

## Var saker ligger
- Inställningar backend: `src/StuffInABox.Application/Settings/`, `…Domain/Entities/UserSettings.cs`, `…Web/Endpoints/SettingsEndpoints.cs`.
- Tema/design frontend: `…/ClientApp/src/store/settingsStore.ts`, `…/features/settings/SettingsView.tsx`, `…/api/settings.ts`, design-CSS i `…/src/index.css`.
- Flerspråk: `…/ClientApp/src/i18n/` (`messages.ts` = ordlista sv/en, `index.ts` = `useT`-hook + språkstore + detektering).
- Delning backend: `…Domain/Entities/SpaceMembership.cs` + `SpaceInvite.cs`, `…Application/Common/Access/SpaceAccessService.cs` (+ `ISpaceAccessService`), `…Application/Sharing/` (use cases), `…Web/Endpoints/InviteEndpoints.cs` + delnings-rutter i `SpaceEndpoints.cs`. Migration: `…Migrations/*_AddSpaceSharing.cs`.
- Delning frontend: `…/ClientApp/src/api/invites.ts`, `…/features/space/SharePanel.tsx`, `…/features/invite/InviteAcceptSheet.tsx`. `spaceId` trådas genom `api/boxes.ts` + `api/items.ts`; deeplink `#invite=` i `store/uiStore.ts`.
- GDPR backend: `…Application/Account/` (DeleteAccount-kommando + ExportAccount-query), `…Web/Endpoints/AccountEndpoints.cs`; repos fick `DeleteAllForOwner`/`DeleteAllForUser`-metoder. Frontend: konto-sektion i `SettingsView.tsx` + `api/account.ts`.
- Lösenordsåterställning backend: `…Domain/Entities/PasswordResetToken.cs` + `Email`-fält på `UserIdentity.cs`, `…Application/Common/Interfaces/IEmailService.cs`, `…Infrastructure/Email/LoggingEmailService.cs`, `forgot-/reset-password` i `…Web/Endpoints/AuthEndpoints.cs`. Migration: `…Migrations/*_AddEmailAndPasswordReset.cs`. Frontend: glömt-läge i `LoginView.tsx`, `features/auth/ResetPasswordView.tsx`, deeplink `#reset=` i `store/uiStore.ts`.
