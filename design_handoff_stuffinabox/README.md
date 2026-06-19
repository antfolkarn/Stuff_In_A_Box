# Handoff: StuffInABox

## Overview
StuffInABox is a web app for keeping a **searchable index of physical storage**. The core insight from the product brief: this solves a *memory* problem, not an *organization* problem. People put things in boxes in a garage/attic/basement and then forget what is where. The app is a "library catalog" layered on top of physical boxes — you register what goes in a box once, then later you **search** instead of opening every box.

Key principles that drive the design:
- **Search is the hero.** A global search bar is always present; results tell you exactly where a thing is (which box number) without opening anything.
- **Low friction to add.** Adding an item must be faster than skipping it: photo → auto-recognition pre-fills the name → confirm destination → save (with "save & next" to register several in a row).
- **A physical number system the user applies themselves.** Every box has a **global, permanent number** (#1, #2, #3 … across the whole home). The user writes that number on the box. Location (which space the box sits in) is a *separate, changeable* property — moving a box between spaces never changes its number.
- **Broad tagging.** Each item carries related tags/synonyms so searching "täcke" finds a "Filtar" (blanket), and "hammare" leads you to the toolbox. New items are auto-tagged.
- **Printable labels with QR codes.** Labels (big number + code + contents + QR) can be printed, cut out, and taped to boxes. Scanning the QR opens that box in the app.

The UI language is Swedish.

## About the Design Files
The file in this bundle (`StuffInABox.dc.html`) is a **design reference created in HTML** — a working prototype showing the intended look and behavior. It is **not production code to copy directly.** It is authored in a bespoke HTML component format (a "Design Component" with inline-styled templates and a logic class) that will not exist in your codebase.

Your task is to **recreate this design in the target codebase's existing environment** (React, Vue, SwiftUI, native, etc.) using its established patterns, component library, and state management. If no environment exists yet, choose the most appropriate framework for the project and implement the design there.

The prototype uses two external libraries you should map to your stack's equivalents:
- **Tabler Icons** (webfont) for all iconography — swap for your icon set; specific glyph names are listed per component below.
- **qrcode-generator** (npm `qrcode-generator`) to render QR codes to data URLs — use any QR library.
- Fonts: **Hanken Grotesk** (UI text) and **IBM Plex Mono** (numbers, codes, labels). Both on Google Fonts.

## Fidelity
**High-fidelity (hifi).** Final colors, typography, spacing, and interactions are specified. Recreate the UI faithfully using your codebase's existing libraries and patterns. Exact hex values, sizes, and copy are given below.

---

## Screens / Views
The app is a single-page experience with a top app bar (search + add + logout) and a `main` area that swaps between views based on state. There is also a login gate before the app, and a modal bottom-area sheet for adding items.

Layout container: `main` is `max-width: 1080px; margin: 0 auto; padding: 28px 24px 80px`. The header is sticky.

### 0. Login (gate)
- **Purpose**: Authenticate before entering the app. Prototype auto-succeeds with any method.
- **Layout**: Full-viewport centered column (`min-height:100vh; display:flex; align-items:center; justify-content:center; padding:24px`). Card max-width **392px**.
- **Components**:
  - Brand lockup (centered): 48×48 rounded-13px accent tile with `ti-stack-2-filled` icon (white, 26px); title "StuffInABox" (19px/600); mono eyebrow "INDEX FÖR FYSISK FÖRVARING" (10px, letter-spacing 0.14em, uppercase, #9AA0A8).
  - Card: white, `border:1px solid rgba(20,24,30,0.1)`, `border-radius:18px`, `box-shadow:0 8px 30px rgba(20,24,30,0.07)`, `padding:28px`.
  - Title (20px/600) + subtitle (13.5px, #6B7078). Copy depends on mode — login: "Logga in" / "Välkommen tillbaka till ditt register."; signup: "Skapa konto" / "Börja hålla reda på var allt finns."
  - **Google button**: white, `border:1px solid rgba(20,24,30,0.16)`, 46px tall, `border-radius:11px`, `ti-brand-google` icon + "Fortsätt med Google".
  - **Apple button**: `background:#16181C`, white text, 46px, `ti-brand-apple-filled` + "Fortsätt med Apple".
  - Divider with centered label "eller med e-post" (#9AA0A8, 12px; 1px hairlines either side).
  - Email + password fields (46px tall, `border-radius:11px`, 1px border `rgba(20,24,30,0.14)`, 15px text). Password label row includes "Glömt?" link in login mode (accent color).
  - Submit button: full-width, 48px, accent background, white, `border-radius:12px`, shadow `0 2px 8px` of 28% accent. Label "Logga in" / "Skapa konto".
  - Mode switch line at bottom: "Har du inget konto? **Skapa konto**" / "Har du redan ett konto? **Logga in**" (link in accent).
  - Legal microcopy under card (11.5px, #9AA0A8): "Genom att fortsätta godkänner du våra villkor och vår integritetspolicy."
- **Behavior**: Any of the three methods sets `authed = true`. If the page was opened via a QR deep link (`#box=N`), after auth it navigates to that box.

### 1. App header (persistent once authed)
- **Layout**: Sticky, `background:rgba(243,244,245,0.82)` with `backdrop-filter:blur(14px)`, bottom hairline. Inner row `max-width:1080px; padding:13px 24px; display:flex; align-items:center; gap:18px; flex-wrap:wrap`.
- **Components**:
  - Brand button (left): 32×32 accent tile (`ti-stack-2-filled`, white 18px) + stacked "StuffInABox" (15.5px/600) and mono eyebrow. Clicking goes home.
  - **Search input** (flex, max-width 540px): 42px tall, `ti-search` icon left (#9AA0A8), `border-radius:11px`, white, 1px border `rgba(20,24,30,0.12)`. Placeholder "Sök – t.ex. täcke, verktyg, vinter…". When non-empty, a clear (`ti-x`) button appears at right.
  - **"Lägg till" button**: accent background, white, 42px, `ti-plus` + "Lägg till".
  - **Logout button**: white, 1px border, 42px, `padding:4px 13px 4px 4px`. Contains a 32×32 `#F1F2F4` avatar tile showing the user's initial (IBM Plex Mono, 14px/600) + a label "Logga ut" with a `ti-logout` icon (16px). The text label makes it clearly a logout action.

### 2. Home / Overview (`view: 'home'`)
- **Purpose**: See all storage spaces.
- **Layout**: Header row (title block left, action buttons right, `flex-wrap`). Below: responsive grid `repeat(auto-fill, minmax(252px, 1fr)); gap:14px`.
- **Components**:
  - H1 "Mina utrymmen" (24px/600, letter-spacing -0.02em). Subtitle = totals line, e.g. "3 utrymmen · 10 lådor · 31 föremål i registret" (14px, #6B7078).
  - Right actions: **"Etiketter"** button (`ti-printer`, opens labels view, all boxes) and **"Nytt utrymme"** button (`ti-plus`, toggles inline add-space form). Both: white, 38px, `border-radius:10px`, 1px border, 13.5px/500.
  - **Add-space form** (shown when toggled): white card, accent-tinted border, `border-radius:13px`, `padding:14px`. Row 1: 38×38 accent-tinted tile showing the chosen icon, name input (placeholder "Namn, t.ex. Vinden eller Förråd"), "Spara" button (accent), cancel `ti-x`. Row 2 (above a hairline): mono label "VÄLJ IKON" + a wrap of 16 icon buttons (40×40, `border-radius:10px`); the selected icon has an accent border + accent-tinted background. Icon set (Tabler): `ti-box, ti-home, ti-car, ti-stairs, ti-door, ti-building-warehouse, ti-tools, ti-archive, ti-books, ti-fridge, ti-plant-2, ti-bike, ti-christmas-tree, ti-paint, ti-shirt, ti-ball-basketball`.
  - **Space card**: white, 1px border `rgba(20,24,30,0.10)`, `border-radius:16px`, `padding:18px`, column with `gap:14px`. Top row: 42×42 `#F1F2F4` tile with the space icon (21px, #6B7078) + an optional mono code chip (e.g. "GAR") at right. Then name (17px/600) + mono meta "5 lådor · 17 föremål" (12px, #8B9098). Hover: border darkens, shadow `0 8px 26px rgba(20,24,30,0.09)`, `translateY(-2px)`.

### 3. Space view (`view: 'space'`)
- **Purpose**: See the boxes inside one space; change the space icon; jump to printing labels for this space.
- **Layout**: Header row (editable icon button, title block, "Etiketter" button) then optional icon-picker panel, then box grid `repeat(auto-fill, minmax(168px, 1fr)); gap:13px`.
- **Components**:
  - **Editable icon button**: 54×54 white tile, 1px border, current space icon (26px). A 21×21 accent badge with `ti-pencil` sits at the bottom-right corner to signal it's editable. Title "Ändra ikon". Click toggles the icon picker.
  - Title block: H1 space name (24px/600) + optional mono code chip; meta line "N lådor · M föremål".
  - **"Etiketter" button** (right): white, 38px, `ti-printer` — opens labels view filtered to this space.
  - **Icon-picker panel** (when editing): same 16-icon grid as add-space; label "VÄLJ IKON FÖR {space name}". Picking sets the space icon and closes the panel.
  - **Box card**: white, 1px border, `border-radius:14px`, `padding:15px`, `min-height:128px`, column. Top row: 38×38 accent-tinted tile with the **box number** (IBM Plex Mono, 18px/600, accent) + optional mono code chip "BOX-001" (#B7BCC2). Bottom: box label (14.5px/500) + mono item count (11.5px, #9AA0A8). Hover lift as elsewhere.
  - **"Ny låda" tile**: dashed 1.5px border, `ti-plus` + "Ny låda", accent on hover. Creates a new box with the next global number and opens it.

### 4. Box view (`view: 'box'`)
- **Purpose**: See/confirm what's in a box, move the box to another space, add items, print this box's label.
- **Layout**: Header row (large number tile, title block, "Märk lådan" pill). Then a "Plats" (location) control row. Then items grid `repeat(auto-fill, minmax(244px, 1fr)); gap:10px`. Then a large "add item" button.
- **Components**:
  - **Number tile**: 62×62 accent background, white box number (IBM Plex Mono, 30px/600), `border-radius:16px`, shadow `0 3px 10px` of 35% accent.
  - Title block: H1 box label (23px/600) + meta "N föremål".
  - **"Märk lådan" pill**: white, dashed border, `ti-tag` icon + "Märk lådan:" + the tag the user should write (e.g. "#8" in IBM Plex Mono 15px/600). This is the number to physically write on the box.
  - **Location ("Plats") row**: `ti-map-pin` + label "Plats" + a `<select>` of all spaces (current selected). Changing it **moves the box to that space; the number stays the same**. Helper microcopy: "Numret #N följer lådan om du flyttar den." On the right of this row: **"Etikett för denna låda"** button (`ti-printer`) → labels view filtered to just this box.
  - **Item card**: white, 1px border, `border-radius:12px`, `padding:11px`, row. Optional 46×46 photo placeholder (diagonal-hatch background, `ti-photo`) + item name (14.5px/500) + a wrap of up to 4 tag chips (10.5px, #8B9098, `#F1F2F4` pill).
  - **Empty state** (no items): centered card, `ti-package`, "Tom låda" / "Registrera det första du lägger i."
  - **Add-item button**: accent, 46px, `border-radius:12px`, `ti-camera-plus` + "Lägg till en sak i lådan".

### 5. Add-item sheet (modal, `addOpen`)
- **Purpose**: Register a new item with minimal friction.
- **Layout**: Fixed overlay `inset:0; z-index:60; background:rgba(16,18,22,0.34); backdrop-filter:blur(3px); display:flex; align-items:flex-start; justify-content:center; padding:5vh 16px 24px; overflow-y:auto`. The sheet sits near the **top** of the viewport (~5vh down), not the bottom. Sheet: white, max-width 460px, `border-radius:18px`, `box-shadow:0 18px 50px rgba(0,0,0,0.28)`, `max-height:90vh; overflow-y:auto`. Enter animation: fade + slide up (`translateY(10px)→0`, 0.24s `cubic-bezier(.2,.8,.2,1)`).
- **Components**:
  - Sticky header: "Lägg till en sak" (17px/600) + mono subtitle "Snabbare än att strunta i det"; close `ti-x` button (34×34, `#F1F2F4`).
  - **Photo zone** (148px tall, `border-radius:14px`):
    - *Idle*: dashed border, `ti-camera` (30px), "Ta ett foto", "Vi känner igen vad det är åt dig".
    - *Analyzing*: hatch background, a horizontal **scan line** (2px, accent, glowing) animating top↔bottom (`@keyframes`, 1.1s ease-in-out infinite), spinning `ti-loader-2`, "Analyserar bild…". (~1.05s simulated.)
    - *Done*: green-tinted hatch, 40px green (`#27500A`) check circle, "Igenkänt: **{guess}**", a "Ta om" link. The guessed name pre-fills the name field if empty.
  - **Name field**: mono label "VAD ÄR DET?" + input (46px, 16px/500). Helper with `ti-sparkles`: "Taggas automatiskt med relaterade ord så den blir lätt att hitta".
  - **Destination**: mono label "VAR LÄGGER DU DEN?" + two selects in a row — space select (flex) and box select (138px, "Box #N · label"). Changing space resets the box to that space's first box.
  - **"Tillagda nu"** chips (after first save in a session): green pills (`#EAF3DE`/`#27500A`) with `ti-check` + item name.
  - Sticky footer: two buttons — **"Spara & nästa"** (white outline, keeps sheet open, clears name/photo, pushes to recents) and **"Klart"** (accent, saves and closes).
- **Behavior on save**: Append `{name, tags}` to the chosen box; immediately attach quick tokenized tags from the name, then asynchronously enrich with broader tags (synonyms/category/material/use). Requires name + space + box.

### 6. Search results (`query` non-empty — overrides any view)
- **Purpose**: Find where things are without opening boxes; matches on item names **and tags**, plus box labels and space names.
- **Layout**: H1 "Sökresultat" + mono count; subtitle "Söker även på relaterade ord – var sakerna finns, utan att öppna en låda." Results are **grouped** into up to three sections, each with a mono uppercase section label: **Utrymmen** (spaces), **Lådor** (boxes), **Föremål** (items). `display:flex; flex-direction:column; gap:22px` between groups, `gap:9px` within.
- **Result rows** (white, 1px border, `border-radius:13px`, `padding:13px 14px`, hover lift):
  - *Space row*: 38×38 `#F1F2F4` tile w/ space icon, name, "Utrymme · N lådor", `ti-arrow-right`.
  - *Box row*: 38×38 accent-tinted tile w/ box number, box title, "Space · Box #N", and if it matched because of contents, an accent sub-line "Innehåller {item}".
  - *Item row*: 38×38 tile w/ `ti-photo`, item name, and **if matched via a tag** an accent pill with `ti-sparkles` + the matched term (the "why it matched" signal). Right side: a prominent **"LÅDA / {number}"** stat tile (accent-tinted, mono) + `ti-arrow-right`. Clicking jumps to that box.
- **Empty state**: centered card, `ti-search-off`, "Inga träffar", "Inget i registret matchar "{query}"."

### 7. Labels view (`view: 'labels'`) — printable
- **Purpose**: Print labels to physically tape on boxes. Reachable from Home (all), Space view (that space), Box view (that box) — all land here; the filter controls what's shown/printed.
- **Layout (screen)**: Header row — back link "Mina utrymmen", H1 "Etiketter", instructional subtitle ("Skriv ut, klipp längs den streckade kanten och tejpa på lådan. Numret är nyckeln till registret."), and a **"Skriv ut (N)"** button (accent) where N is the filtered count. Then a **filter row** (`.sib-noprint`): `ti-filter` "Filter" + a wrap of pill buttons — **"Alla"** (+ total count) and one pill per space (icon + name + box count); selected pill has accent border + accent-tinted bg. When filtered to a single box, an extra removable accent pill "Endast Box #N" with `ti-x`. Then the label grid `repeat(auto-fill, minmax(248px, 1fr)); gap:14px`.
- **Label card** (`.sib-label`): white, **dashed** 1.5px border, `border-radius:12px`, `padding:18px`, `min-height:188px`, column with `gap:12px`.
  - Top row: left column = 62×62 accent number tile (mono 34px/600 box number) + mono code "#N" below; right = a **QR code** in a 74×74 white box with 1px border (`border-radius:9px`, 5px padding) — 64×64 image, `image-rendering:pixelated`. While generating, show a faint `ti-qrcode` placeholder.
  - Middle (above a hairline): box title (15px/600) + space name (right, 13px #6B7078); then contents preview (12px #6B7078) = up to 6 item names joined by " · ", or "Tom låda".
  - Footer row: mono "SKANNA FÖR ATT ÖPPNA I APPEN" with `ti-qrcode` (9px, #9AA0A8) + mono item count.
- **QR contents**: a deep link `‹app-url›#box=N`. Opening the app with that hash sets a pending box; after auth it navigates to box N.
- **Print CSS** (`@media print`): hide header and `.sib-noprint`; white background; `main` padding 0 / max-width none; force the label grid to **2 columns**, `gap:0`; labels `break-inside:avoid` with a solid 1px dashed-look cut border, no radius, no shadow; `@page { margin:12mm }`.

---

## Interactions & Behavior
- **View switching** is state-driven (`view`), but a non-empty `query` overrides everything to show search results.
- **Search** is case-insensitive substring matching over: space names; box labels; item names; **item tags**. A box also appears if one of its items matches ("Innehåller {item}"). Item rows show *why* they matched when the hit was via a tag.
- **Add item**: photo step is a ~1.05s simulated recognition that pre-fills the name from a random plausible guess; real implementation should call an image-recognition service. "Spara & nästa" keeps the sheet open and accumulates a "Tillagda nu" recents list; "Klart" closes.
- **Auto-tagging**: on save, tokenized tags from the name are attached synchronously; then a broader set of 4–7 Swedish tags (synonyms, parent category, material, use) is fetched asynchronously and merged onto that item. In the prototype this uses an LLM completion; in production use your tagging/embedding service. Tagging must not block the save.
- **Move box**: changing the "Plats" select moves the box object to the target space's collection. **The box number is global and never changes on move** — this was a deliberate fix (numbering used to be per-space and collided on move).
- **New box** gets `max(all box numbers) + 1`.
- **Labels filter**: selecting a space pill filters to that space; the single-box filter (from Box view) is additionally removable. Print count reflects the filter.
- **QR deep link**: `#box=N` on load → store pending box → after login, open that box.
- **Animations**: view transitions fade in (`@keyframes`, ~0.18s). Sheet slides up (0.24s). Scan line loops during analysis. Hover on cards: `translateY(-1px/-2px)` + elevated shadow.

## State Management
Top-level state (names from the prototype's logic class):
- `authed`, `authMode` ('login'|'signup'), `email`, `password`, `user`, `pendingBox` (from QR deep link).
- `view`: 'home' | 'space' | 'box' | 'labels'. `spaceId`, `boxNum` (currently selected). `query` (search overrides view).
- `spaces`: `[{ id, name, code, icon, boxes: [{ num, label, items: [{ name, tags: string[] }] }] }]`. **`num` is globally unique and permanent.** `code` is a 3-letter uppercase derived from the name.
- `addingSpace`, `newSpaceName`, `newSpaceIcon`; `editingIcon` (space-view icon picker open).
- `addOpen` + `add: { photo, analyzing, name, suggested, spaceId, boxNum, recent: [] }`.
- `qr`: map of box number → QR data URL (generated lazily, cached). `labelFilter: { spaceId, boxNum }`.

Derived per render: totals; space cards; box cards; flattened items for current box; grouped search results (spaces/boxes/items) with match reasons; label cards (filtered); filter pills with counts; add-sheet space/box option lists.

## Design Tokens
**Colors**
- App background: `#F3F4F5`
- Surface / cards: `#FFFFFF`
- Primary text: `#16181C`
- Secondary text: `#6B7078`
- Tertiary / muted: `#8B9098`, `#9AA0A8`
- Faint / placeholder: `#B7BCC2`, `#D2D6DB`
- Neutral tile fill: `#F1F2F4`; hover fill `#F4F5F6`
- Hairline border: `rgba(20,24,30,0.08–0.16)` (varies by emphasis)
- **Accent (primary, tweakable)**: `#2F63E6` (default). Alternatives shipped as options: `#1F8A5B`, `#B4530F`, `#6A4CFF`. Accent tints use CSS `color-mix(in srgb, accent X%, #fff)` — commonly 9%, 10%, 11%, 22%, 30–35%.
- Success: text/icon `#27500A`, chip bg `#EAF3DE`
- Apple button: `#16181C`
- Diagonal hatch (photo placeholders): `repeating-linear-gradient(45deg, rgba(20,24,30,0.05) 0 6–7px, transparent 6–7px 12–14px)` over `#EEF0F2`.

**Typography**
- UI font: **Hanken Grotesk** (400/500/600/700).
- Mono font: **IBM Plex Mono** (400/500/600) — used for numbers, codes, eyebrows/labels, counts.
- Sizes seen: H1 23–24px/600 (letter-spacing -0.02em); section H1 21px; card titles 15–17px/600; body 13.5–15px; meta 11.5–13px; mono eyebrows 9–11px with letter-spacing 0.1–0.14em, uppercase. Big number tiles: 18px (box card), 30px (box header), 34px (label).

**Spacing / radii / shadows**
- Page padding 24–28px; card padding 11–18px; gaps 7–22px.
- Radii: chips/pills 999px; small tiles 8–11px; inputs/buttons 10–12px; cards 12–16px; brand tiles 9–13px.
- Shadows: resting `0 1px 2px rgba(20,24,30,0.04)`; hover `0 6–8px 22–26px rgba(20,24,30,0.08–0.09)`; accent button `0 2px 8px color-mix(accent 28%)`; modal `0 18px 50px rgba(0,0,0,0.28)`.
- Control heights: search/add/logout 42px; secondary buttons 38–40px; inputs 46px; primary CTAs 46–48px.

## Assets
- **Icons**: Tabler Icons (webfont in the prototype). Glyphs used include: `ti-stack-2-filled, ti-stack-2, ti-search, ti-x, ti-plus, ti-logout, ti-printer, ti-filter, ti-chevron-right, ti-chevron-down, ti-arrow-right, ti-arrow-left, ti-pencil, ti-map-pin, ti-info-circle, ti-tag, ti-package, ti-photo, ti-camera, ti-camera-plus, ti-loader-2, ti-check, ti-sparkles, ti-qrcode, ti-search-off, ti-brand-google, ti-brand-apple-filled`, plus the space-icon set listed in §2.
- **Fonts**: Hanken Grotesk + IBM Plex Mono (Google Fonts).
- **QR codes**: generated at runtime from each box's deep link; no static asset.
- **Photos**: the prototype uses hatch placeholders. Real implementation should store user-taken photos and show thumbnails.
- No bitmap/logo assets — the brand mark is the `ti-stack-2-filled` glyph on an accent tile.

## Tweakable props (in the prototype)
The root component exposes three props worth preserving as configuration:
- `accent` (color) — primary accent, default `#2F63E6`.
- `showCodes` (boolean, default true) — show/hide the mono code chips (space codes, box codes).
- `showItemPhotos` (boolean, default true) — show/hide item photo thumbnails.

## Files
- `StuffInABox.dc.html` — the complete design reference (all views, login, add sheet, labels). This is the single source of truth for layout, copy, and behavior.
