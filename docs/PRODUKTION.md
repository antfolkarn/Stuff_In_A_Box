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

- **Glömt lösenord saknas helt.** "Glömt?"-länken i
  `ClientApp/src/features/auth/LoginView.tsx` är död (`<a href="#">` utan onClick),
  och det finns **ingen återställning + ingen e-postinfrastruktur** i backend. En
  e-postregistrerad användare som glömmer lösenordet blir utelåst.
  - ⚠️ **Designkonflikt:** appen lagrar medvetet bara `SHA256(e-post)`
    (integritetsmodellen). Lösenordsåterställning kräver att man kan *mejla*
    användaren — alltså lagra/skicka till klartext-e-post. Medvetet vägval som måste
    tas: behåll noll-PII (ingen återställning möjlig) **eller** lägg till e-post +
    utskick för reset.
- **GDPR (relevant för svenskt bolag):** ingen **kontoradering** och ingen
  **dataexport** finns. Rätten till radering och dataportabilitet bör täckas.
  Rent backend-jobb, kräver ingen e-post — bra fristående nästa commit.
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

## 🔵 Skalning (bara om ni växer förbi en instans)

Allt nedan fungerar bra för **en server med beständig volym** (som docker-compose
ger nu):

- **SQLite → PostgreSQL** vid flera instanser (provider-switch finns redan i
  `Infrastructure/DependencyInjection.cs`).
- **Lokala bilduppladdningar → objektlagring** (Azure Blob/S3) — `IStorageService` är
  redan abstraherat, liten isolerad ändring.
- **SQLite-sårbarheten NU1903** (CVE-2025-6965) — ingen uppströms-fix än, låg risk,
  dokumenterad i [HANDOFF.md](../HANDOFF.md).
- **Felövervakning** — bara Serilog till fil idag; överväg Sentry/App Insights för
  fel + metrics.

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
