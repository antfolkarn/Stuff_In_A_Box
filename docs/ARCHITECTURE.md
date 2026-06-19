# Arkitektur — StuffInABox

Det här dokumentet beskriver nuläget: lagerindelning, beroenderegler, hur ett
request flödar genom systemet, samt auth-, taggnings- och lagringsmekanismerna.

---

## 1. Clean Architecture — lager och beroenderiktning

Beroenden pekar **alltid inåt**. Domain är kärnan och känner inte till något yttre
lager. Web är composition root och kopplar ihop allt.

```mermaid
flowchart TD
    subgraph Web["StuffInABox.Web — Presentation / Composition root"]
        EP["Minimal API-endpoints<br/>Auth · OAuth · Spaces · Boxes · Items · Search · Labels"]
        MW["Middleware<br/>GlobalExceptionHandler · SecurityHeaders · Serilog"]
        AUTH["Auth<br/>JwtTokenService · TokenIssuer · OAuthService · CurrentUserService"]
        SPA["ClientApp (React SPA, byggs till wwwroot/)"]
    end

    subgraph Infra["StuffInABox.Infrastructure"]
        EF["EF Core · AppDbContext · Repositories · Migrations"]
        STORE["LocalFileStorageService"]
        IMG["SkiaImageProcessor"]
        TAG["TokenizerTaggingService · ClaudeTaggingService"]
        WORKER["TagEnrichmentWorker (BackgroundService)"]
    end

    subgraph App["StuffInABox.Application"]
        UC["Use cases (CQRS via MediatR)<br/>Commands · Queries · Handlers · Validators · DTOs"]
        BEH["ValidationBehavior (pipeline)"]
        IF["Service-gränssnitt<br/>IStorageService · ITaggingService · IImageProcessor<br/>ICurrentUserService · IEnrichmentQueue"]
    end

    subgraph Domain["StuffInABox.Domain — kärna, inga beroenden"]
        ENT["Entiteter<br/>Space · Box · Item · UserIdentity · RefreshToken"]
        VO["Value objects<br/>UserId · BoxNumber · SpaceCode"]
        REPO["Repository-gränssnitt"]
        EXC["Domänundantag"]
    end

    Web --> App
    Web --> Infra
    Infra --> App
    App --> Domain
    Infra --> Domain

    EP --> UC
    UC --> IF
    UC --> REPO
    EF -. implementerar .-> REPO
    STORE -. implementerar .-> IF
    IMG -. implementerar .-> IF
    TAG -. implementerar .-> IF
```

**Regeln i praktiken:** ett use case (Application) anropar bara gränssnitt
(`IBoxRepository`, `IStorageService`, …). De konkreta implementationerna lever i
Infrastructure och kopplas in via dependency injection i `Program.cs` /
`Infrastructure/DependencyInjection.cs`.

---

## 2. Request-flöde (command via MediatR)

Exempel: lägg till ett föremål (`POST /api/boxes/{n}/items`).

```mermaid
sequenceDiagram
    actor U as Webbläsare (SPA)
    participant EP as ItemEndpoints
    participant M as MediatR
    participant V as ValidationBehavior
    participant H as AddItemCommandHandler
    participant R as IItemRepository (EF Core)
    participant Q as IEnrichmentQueue
    participant W as TagEnrichmentWorker

    U->>EP: POST /api/boxes/8/items { name }
    Note over EP: JWT valideras, CurrentUserService ger UserId
    EP->>M: Send(AddItemCommand)
    M->>V: pipeline
    V->>V: FluentValidation (annars 400)
    V->>H: handler
    H->>R: lägg till föremål (snabba tokenizer-taggar)
    H->>Q: EnqueueEnrichment(itemId, namn)  (fire-and-forget)
    H-->>EP: AddItemResult
    EP-->>U: 201 Created
    Note over W: separat tråd
    Q-->>W: läs kö
    W->>R: MergeTags(breda taggar)
```

Snabba taggar (tokenisering av namnet) sätts synkront så sparet aldrig blockeras.
Bredare taggar (synonymer/kategori/material) berikas asynkront av workern.

Fel hanteras centralt av `GlobalExceptionHandler` som mappar undantag till HTTP-status:
`ValidationException`/`InvalidImageException` → 400, `NotFoundException` → 404,
`ForbiddenException` → 403, `UnauthorizedAccessException` → 401, övrigt → 500.

---

## 3. Autentisering

### 3a. Lösenord + refresh-token

```mermaid
sequenceDiagram
    actor U as SPA
    participant A as AuthEndpoints
    participant UR as IUserIdentityRepository
    participant TR as IRefreshTokenRepository
    participant J as JwtTokenService

    U->>A: POST /api/auth/login { email, password }
    A->>UR: FindAsync("email", SHA256(email))
    A->>A: BCrypt.Verify(password, hash)
    A->>J: GenerateAccessToken(userId)  (15 min)
    A->>TR: spara SHA256(refreshToken)  (7 dagar)
    A-->>U: { token } + Set-Cookie sib_refresh (HttpOnly, Strict)

    Note over U: 15 min senare → access-token utgånget
    U->>A: POST /api/auth/refresh  (cookie skickas)
    A->>TR: slå upp hash, kontrollera aktiv
    A->>TR: revoke gammal + spara ny (rotation)
    A-->>U: { nytt token } + ny cookie
```

Frontendens axios-interceptor fångar 401, anropar `/refresh` en gång och kör om
det ursprungliga anropet. Vid sidladdning återställs sessionen tyst från cookien.

### 3b. OAuth (Google / Apple, Authorization Code + PKCE)

```mermaid
sequenceDiagram
    actor U as Webbläsare
    participant O as OAuthEndpoints
    participant P as Leverantör (Google/Apple)
    participant UR as IUserIdentityRepository

    U->>O: GET /api/auth/google/start
    O->>O: skapa state + PKCE verifier/challenge
    O-->>U: 302 → leverantörens authorize-URL<br/>(cookie sib_oauth = state:verifier)
    U->>P: loggar in & godkänner
    P-->>U: 302 → /api/auth/google/callback?code&state
    U->>O: callback (cookie följer med)
    O->>O: validera state, byt code+verifier mot id_token
    O->>O: läs `sub` ur id_token (ingen PII)
    O->>UR: slå upp/skapa UserIdentity(provider, sub)
    O-->>U: 302 → /#token=JWT  (+ refresh-cookie)
```

SPA:n läser access-token ur URL-fragmentet vid laddning och rensar det ur historiken.
Apple-client-secret signeras on-the-fly med ES256 från konfigurerad .p8-nyckel.

---

## 4. Domänmodell

```mermaid
classDiagram
    class Space {
        +Guid Id
        +UserId OwnerId
        +string Name
        +SpaceCode Code
        +string Icon
    }
    class Box {
        +BoxNumber Number
        +Guid SpaceId
        +UserId OwnerId
        +string Label
        +MoveTo(spaceId)
        +UpdateLabel(label)
    }
    class Item {
        +Guid Id
        +BoxNumber BoxNumber
        +UserId OwnerId
        +string Name
        +string? PhotoStorageKey
        +IReadOnlyList~string~ Tags
        +Rename / SetPhoto / Replace-/MergeTags
    }
    class UserIdentity {
        +Guid InternalUserId
        +string Provider
        +string ExternalId
        +string? PasswordHash
    }
    class RefreshToken {
        +Guid UserId
        +string TokenHash
        +ExpiresAt / RevokedAt
        +Issue / Revoke / IsActive
    }

    Space "1" --> "*" Box : innehåller
    Box "1" --> "*" Item : innehåller
```

`BoxNumber` är globalt unikt och oföränderligt — composite-nyckel `(Number, OwnerId)`
i databasen. Att flytta en låda ändrar bara `SpaceId`. `SpaceCode` härleds från namnet
(3 versaler, svenska tecken normaliseras: å/ä→a, ö→o).

---

## 5. Tvärgående mekanismer

| Mekanism | Var | Not |
|----------|-----|-----|
| **Loggning** | `Program.cs` (Serilog) | Konsol + roterande dagsfil (`logs/stuffinabox-.log`), request-loggning. |
| **Databas-swap** | `Infrastructure/DependencyInjection.cs` | `Database:Provider`-switch; entitetskonfig undviker provider-specifika typer. |
| **Bildlagring** | `IStorageService` | Lokal disk nu; byts mot t.ex. Azure Blob utan schemaändring (nyckel, inte URL, lagras). |
| **Taggning** | `ITaggingService` | Tokenizer default; Claude API bakom `Tagging:Provider`-flagga. |
| **Bakgrundsjobb** | `TagEnrichmentWorker` | In-process `Channel<T>` + `IHostedService`; kastar aldrig, blockerar aldrig sparet. |
| **Tema** | `ClientApp` `themeStore` | Ljust/mörkt via CSS-variabler och `data-theme`, persisterat i `localStorage`. Flimmerfritt: en liten inline-init i `index.html` sätter temat före första paint, tillåten av CSP via sin SHA-256-hash (ingen `'unsafe-inline'` för skript). Ändras skriptet måste hashen i `SecurityHeadersMiddleware` räknas om. |

---

## 6. Frontend (React SPA)

```
ClientApp/src/
├── api/        # axios-klient (JWT-interceptor, 401→refresh) + en fil per resurs
├── store/      # Zustand: authStore · uiStore · themeStore
├── features/   # auth · home · space · box · addItem · search · labels
├── shared/     # AppHeader · SpaceIconPicker · useQrCode
└── App.tsx     # vy-växling (state-driven; query åsidosätter vy)
```

- **Server-state**: React Query (cache, bakgrunds-refetch, invalidering vid mutationer).
- **UI/auth/tema-state**: Zustand.
- **Routing**: tillståndsdriven (ingen URL-router); `#box=N` är en QR-deeplänk och
  `#token=…` tas emot från OAuth-callbacken.
