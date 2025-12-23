### Frontend Learning Plan (Vue3 + Vite + TypeScript + Vuetify)

This file is a **study + exercise tracker** for building the simplest local Ultimate Tic Tac Toe prototype (no backend yet).

---

### Current state (choices locked in)

- **Iteration 1**: local-only prototype with mocks (no backend, no SignalR)
- **Testing focus**: **unit tests for rules engine** (Vitest)
- **Goal UX**: playable on desktop + mobile, clear error messages, basic polish

Update this section whenever you change approach.

---

### Next up (current plan)

0) Setup frontend project with dependencies and baseline components  
1) Find appropriate Vuetify components for our case (board + lobby + feedback)

---

### TODO list (short-term, based on current plan)

#### 0) Setup frontend project

- [ ] Scaffold Vue3 + Vite + TypeScript project
- [ ] Install + wire: Vuetify, Vue Router, Pinia
- [ ] Add baseline pages + routes: `/` (Lobby), `/local` (LocalGame)
- [ ] Add baseline components (empty skeletons, no rules yet):
  - [ ] `AppShell.vue` (layout)
  - [ ] `LobbyPage.vue`
  - [ ] `LocalGamePage.vue`
  - [ ] `BigBoard.vue`, `MiniBoard.vue`, `CellButton.vue`
  - [ ] `GameStatusBar.vue`, `GameControls.vue`
- [ ] Tooling: ESLint + Prettier + Vitest + scripts (`lint`, `format`, `typecheck`, `test`)
- [ ] “Green baseline”: `npm run dev`, `npm run typecheck`, `npm test` all pass

#### 1) Pick Vuetify components (and lock UI approach)

- [ ] Decide board primitives:
  - [ ] Cell: `v-btn` (text vs `v-icon` for X/O)
  - [ ] Mini-board container: `v-sheet` vs `v-card`
  - [ ] Layout: `v-container` + `v-row`/`v-col` (or CSS grid) for 3×3
- [ ] Decide feedback primitives:
  - [ ] Illegal move / info: `v-snackbar`
  - [ ] Page-level warning/error: `v-alert`
- [ ] Decide lobby primitives:
  - [ ] Join code input: `v-text-field`
  - [ ] Actions: `v-btn`, optional `v-dialog` for “private room created”
- [ ] Build a tiny UI spike:
  - [ ] Render one mini-board (3×3) with all states (empty/X/O/disabled/highlighted)
  - [ ] Ensure it’s usable on mobile (tap targets, spacing)

---

### Target outcomes (definition of “done” for iteration 1)

- **Local Ultimate rules implemented** (including forced next mini-board, and “any board” when forced board is unplayable)
- **Rules engine is pure + unit tested**
- **UI is store-driven** (Pinia), minimal state in components
- **Vuetify layout** is responsive, clickable cells are clear, illegal moves show a reason
- **Mock lobby** exists (queue/create/join) to simulate future backend flows

---

### Tech stack to learn (minimum set)

- **Vue 3**: Composition API (`ref`, `computed`, `watch`, `defineProps`, `defineEmits`)
- **TypeScript**: unions, discriminated unions, readonly data modeling
- **Pinia**: store state/getters/actions, persistence (optional)
- **Vuetify**: grid/layout, buttons, snackbar, theming basics
- **Vitest**: unit testing pure functions

---

### Milestones & exercises (do in order)

#### Milestone 0 — Setup & discipline (½ day)

- [ ] Create Vue3 + Vite + TypeScript app
- [ ] Add Vuetify
- [ ] Add Vue Router with routes:
  - [ ] `/` (Lobby)
  - [ ] `/local` (Local prototype)
- [ ] Add scripts: `lint`, `format`, `typecheck`, `test`
- [ ] Configure ESLint + Prettier (strict, no `any`)

Deliverable: `npm run dev` and `npm test` work on a clean checkout.

#### Milestone 1 — Domain model + rules engine (1–2 days)

Create `src/domain/` with **only types + pure functions** (no Vue imports).

- [ ] Types
  - [ ] `Player = 'X' | 'O'`
  - [ ] `Cell = Player | null`
  - [ ] `Move = { by: Player; mini: number; cell: number }`
  - [ ] `MiniStatus = 'InProgress' | 'Draw' | { kind: 'Won'; winner: Player }`
  - [ ] `GameStatus = 'InProgress' | 'Draw' | { kind: 'Won'; winner: Player }`
  - [ ] `GameState` contains:
    - [ ] `cells` (9 mini-boards × 9 cells)
    - [ ] `turn`
    - [ ] `nextAllowedMini: number | 'any'`
    - [ ] `miniStatus[9]`
    - [ ] `status`
    - [ ] `lastMove?`

- [ ] Pure functions
  - [ ] `createNewGame(): GameState`
  - [ ] `validateMove(state, move): { ok: true } | { ok: false; reason: string }`
  - [ ] `applyMove(state, move): GameState` (assumes valid move)
  - [ ] `getMiniStatus(miniCells): MiniStatus`
  - [ ] `getGameStatus(miniStatus): GameStatus`
  - [ ] `getNextAllowedMini(state, move): number | 'any'`

- [ ] Unit tests (Vitest)
  - [ ] First move legal anywhere
  - [ ] Not-your-turn rejected
  - [ ] Occupied cell rejected
  - [ ] Forced next mini-board computed correctly
  - [ ] If forced mini-board is won/drawn/full → next is `'any'`
  - [ ] Mini-board win detection (rows/cols/diags)
  - [ ] Big-board win detection based on mini winners
  - [ ] Draw cases (mini draw + overall draw)

Deliverable: `npm test` green; tests cover edge cases.

#### Milestone 2 — Pinia store (½–1 day)

- [ ] Create `useLocalGameStore`
  - [ ] `state`: `game`, `error?`
  - [ ] `getters`: turn, allowed mini-board, status, “is cell clickable”
  - [ ] `actions`: `newGame()`, `tryMove(mini, cell)`
- [ ] Optional: persisted state for refresh survival (keep it simple)

Deliverable: store API is stable and UI doesn’t re-implement rules.

#### Milestone 3 — UI components (1–2 days)

- [ ] `CellButton.vue`
  - [ ] Typed props, typed click emit
  - [ ] Disabled + selected + hover states
- [ ] `MiniBoard.vue` (3×3)
  - [ ] Highlights when it is allowed (or when any is allowed)
  - [ ] Shows mini-board result (won/draw)
- [ ] `BigBoard.vue` (3×3 of mini boards)
  - [ ] Highlights last move
- [ ] `GameStatusBar.vue` (turn/status/allowed board)
- [ ] `GameControls.vue` (New Game / Reset)
- [ ] UX
  - [ ] Illegal move → show reason in `v-snackbar`
  - [ ] Mobile friendly sizing (tappable cells)

Deliverable: playable local game, clear feedback, looks decent.

#### Milestone 4 — Mock Lobby (½–1 day)

Build a local-only simulation of future backend flows (no SignalR yet).

- [ ] `Lobby` screen:
  - [ ] “Play (queue)” → mock delay → navigate to `/local`
  - [ ] “Create private room” → generate join code, display it
  - [ ] “Join private room” → input join code, validate in mock store
- [ ] `src/mocks/rooms.ts`: in-memory lists + async delays

Deliverable: end-to-end UX from lobby → local game, with “rooms-like” concepts.

---

### Architecture constraints (rules you follow while practicing)

- **Domain is framework-free**: `src/domain/**` has no Vue imports.
- **No duplicated rules**: UI asks store; store asks domain.
- **Typed boundaries**: types are explicit at domain/store boundaries.
- **Error UX always exists**: no silent failure.

---

### Optional stretch goals (only after iteration 1 works)

- [ ] Keyboard navigation (tab + enter/space)
- [ ] Visual hints: show forced mini-board, show blocked mini-boards
- [ ] Add a “review mode” (step through move history)
- [ ] Add simple bot (random legal move) behind a toggle

