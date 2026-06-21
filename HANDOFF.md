# Handoff — StuffInABox

Snabb lägesbild för att fortsätta på en annan dator. För djupare info: [README.md](README.md) och [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

_Senast uppdaterad: 2026-06-20._

---

## Vad det är
Svensk webapp för att katalogisera fysisk förvaring (utrymmen → lådor → föremål), sök, QR-etiketter. .NET 10 (Clean Architecture) + React/TypeScript (Vite). Lösningsfil: **`StuffInABox.slnx`** (modern XML-format; öppnas i Visual Studio 17.13+).

## Kör igång
```bash
# Backend (serverar även den byggda SPA:n) — http://localhost:5184
cd src/StuffInABox.Web && dotnet run

# Frontend i dev-läge (valfritt) — http://localhost:5173, proxar /api → 5184
cd src/StuffInABox.Web/ClientApp && npm install && npm run dev
```
- **Tester:** `dotnet test StuffInABox.slnx` — **86 gröna** (Domain 27, Application 25, Infrastructure 18, Web 16).
- **Bygg SPA till wwwroot:** `cd src/StuffInABox.Web/ClientApp && npm run build`.

## Färdigt och fungerar
- Kärna: utrymmen/lådor/föremål (CRUD), sök, etiketter med QR, globala oföränderliga lådnummer, dataisolering per användare.
- Auth: JWT + roterande refresh-token (HttpOnly-cookie) + Google/Apple OAuth (PKCE, kräver konfig).
- Bilduppladdning (magic-byte + EXIF-strip via SkiaSharp). **Uppladdningar lagras utanför `wwwroot`** (annars raderas de av SPA-bygget).
- **Bildigenkänning (Ollama, lokal):** foto → `{ namn, taggar }` (föremål, färger, material, boktitlar). På som default i dev (`ImageRecognition:Provider=ollama`). Se "Ollama" nedan.
- **Inställningar i databasen (cross-device):** `UserSettings`-tabell + `GET/PUT /api/settings`. Tema (ljus/mörk/system) + design sparas på kontot och följer användaren. Inställningssida via kugghjuls­ikonen i headern. `localStorage` används som cache; flimmerfri init i `index.html`.
- **Tre designs (token-nivå):** Standard, Atelier (varmt papper, Manrope/Spectral, skarpa hörn), Pop (lila, Plus Jakarta/Bricolage, runda 20px hörn + 2px-kanter). Växlas via `data-design` + `data-theme` på `<html>`. Form-språk tokeniserat: `--bw`, `--r-xl/lg/md/sm/chip` per design i `src/StuffInABox.Web/ClientApp/src/index.css`.
- Drift: Dockerfile + docker-compose, GitHub Actions CI, health checks (`/health`, `/health/ready`), Serilog (konsol + roterande fil).

## ⚠️ ÖPPEN PUNKT — design-trohet (här var vi)
Designerna byter **färg, typsnitt och form** men matchar **inte** prototyperna fullt ut, för prototyperna har **design-specifikt komponentutförande** som de delade komponenterna inte återskapar. Konkret för **Pop** (se skärmdump-jämförelse):
- **Varje utrymme/låda får egen färg** ur en 6-färgspalett. I prototypen görs det med CSS `.pop-rot:nth-child(6n+k){--c:…}` och färgerna är: `#6C4CF1, #FF5C49, #12B5A4, #FF9E1B, #E8489E, #2E8BFF`. Komponenter använder `var(--c, var(--accent))`.
- Ikonbrickan är **fylld med kortets färg + vit ikon + större**; Standard har neutrala grå brickor. Plus en mjuk **färgklick i hörnet**, färgad kod-chip, display-typsnitt på siffror.
- Atelier har sitt eget utförande (eget formspråk).

**Beslut som behövs (frågan som ställdes):** hur troget ska det matchas?
1. Bygg Pop troget, sen Atelier — design-specifik komponentkod, skärm för skärm (stort jobb).
2. Lyft bara utrymmes-korten (per-utrymme-färg + större fyllda vit-ikon-brickor + hörnklick i Pop).
3. Behåll token-nivån.

Referensprototyper: `design_handoff_stuffinabox/StuffInABox - Atelier.dc.html` och `… - Pop.dc.html` (originalet är `StuffInABox.dc.html`). Råpaket: `design/*.zip`.

## Ollama (bildigenkänning)
- Installerat på **denna** dator (winget: `Ollama.Ollama`, modell `llava` hämtad). På en annan dator: installera Ollama + `ollama pull llava`, **eller** stäng av med `ImageRecognition__Provider=none` (bash) / `$env:ImageRecognition__Provider='none'` (PowerShell) före `dotnet run`.

## Gotchas (viktigt)
- **CSP-hash för tema-skriptet:** inline-skriptet i `ClientApp/index.html` sätter tema+design före paint och tillåts av CSP via en SHA-256-hash i `src/StuffInABox.Web/Middleware/SecurityHeadersMiddleware.cs`. **Nuvarande hash: `sha256-44oUjcpwRxvRj5LHU+Rw2hWiyR5rFJQOISEkuLZAE4U=`.** Ändras skriptet: bygg SPA, ta SHA-256 (base64) av exakt texten mellan `<script>`/`</script>` i `wwwroot/index.html`, och uppdatera konstanten — annars blockerar CSP:n skriptet (flimmer/fel tema).
- **`.slnx`**, inte `.sln`. `dotnet new sln` skapar `.slnx` som default på denna SDK.
- **SkiaSharp Linux-assets** finns med (annars kraschar bilduppladdning i container/CI).
- **Windows PowerShell 5.1** manglar icke-ASCII i `Invoke-RestMethod -Body` och `curl -F` (multipart) ger HTTP 000 — använd .NET `HttpClient` eller integrationstest för svenska/multipart.

## Var saker ligger (nytt sen design-arbetet)
- Inställningar backend: `src/StuffInABox.Application/Settings/`, `…Domain/Entities/UserSettings.cs`, `…Web/Endpoints/SettingsEndpoints.cs`.
- Tema/design frontend: `…/ClientApp/src/store/settingsStore.ts`, `…/features/settings/SettingsView.tsx`, `…/api/settings.ts`, design-CSS i `…/src/index.css`.
