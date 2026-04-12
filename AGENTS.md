# Memory Bank: Operational Protocol (Lean)

I am an expert software engineer. My memory resets between sessions. I rely ENTIRELY on my Memory Bank.

## Handshake Contract

### Phase 0: Context Handshake
1. Detect `memory-bank/` candidates.
2. Resolve `MB_ROOT` before any `mb-*` command.
3. Full reload on first access for `MB_ROOT`: read all files under `<MB_ROOT>/memory-bank/` except `proposals/completed/`, `proposals/abandoned/`, `proposals/archived/`.
4. Emit marker: `[Memory Bank: Active - <ProjectName> @ <MB_ROOT>]` and track `Last Full Reload`.
5. Same-session turns may use cached mode (`context.md` + `proposals/active/` + fingerprint check) and emit `[Memory Bank: Cached - ...]`; fallback to full reload on mismatch/uncertainty.

### MB_ROOT Resolution Rules
- `@mb_plan`: explicit path -> task anchors -> memory-bank discovery recommendation -> active files -> cwd fallback.
- Non-plan `mb-*`: explicit path -> active proposal discovery (`**/memory-bank/proposals/active/proposal_*.md`).
- Non-plan ambiguity handling: if >1 active proposal Memory Bank exists, ask exactly one question; if 0, STOP and suggest `@mb_plan`.

### Scope Lock
- Read/write Memory Bank files only inside `<MB_ROOT>/memory-bank/` unless the user explicitly requests cross-project synchronization.
- Before first write, announce:
  - `[Memory Bank: Active - <ProjectName> @ <MB_ROOT>]`
  - `Target Memory Bank: <MB_ROOT>/memory-bank/`
  - `Reason: <explicit path | inferred from anchors | active proposal discovery | inferred from active files | cwd fallback>`

## Routing Layer (No Command Duplication)

AGENTS is router + policy only.

- Activate explicit `mb-*` skills for command execution details.
- Do not embed command workflow sections for `@mb_plan`, `@mb_act`, or `@mb_done*` inside AGENTS.
- Keep canonical command behavior in skill files (`integrations/skills/mb-*/SKILL.md` or `integrations/superpowers/skills/mb-*/SKILL.md`).

### Supported Command Set

| Command | Alias | Primary skill |
|:--|:--|:--|
| `@mb_init` | `mb_inicializovat` | `mb-init` |
| `@mb_plan` | `mb_navrhnout` | `mb-plan` |
| `@mb_review` | `mb_oponentura` | `mb-review` |
| `@mb_act` | `mb_implementovat` | `mb-act` |
| `@mb_done` | `mb_hotovo` | `mb-done` |
| `@mb_abort` | `mb_zrusit` | `mb-abort` |
| `@mb_sync` | `mb_synchronizovat` | `mb-sync` |
| `@mb_scan` | `mb_skenovat` | `mb-scan` |
| `@mb_state` | `mb_stav` | `mb-state` |
| `@mb_git_message` | `mb_zprava` | `mb-git-message` |
| `@mb_git_commit` | `mb_potvrdit` | `mb-git-commit` |
| `@mb_done_git_commit` | `mb_hotovo_commit` | `mb-done-git-commit` |

## Workflow Contracts That Must Hold

- Phase is derived from `memory-bank/proposals/active/` (`ACTIVE_WORK` when non-empty, otherwise `IDLE`).
- Proposal lifecycle is filesystem-driven (`active` -> `completed`/`abandoned`).
- Code remains source of truth over docs/proposal.
- Memory Bank files are maintained in Czech.

## 🎨 Diagram Rules

I must follow these rules when creating diagrams:

1. **Mermaid Only:** Use Mermaid for all diagrams.
2. **Syntax Safety:** Enclose text with brackets `()` or `[]` in quotes to prevent syntax errors (e.g., `id["Node (Details)"]`).

## 🔗 Linking Rules

I must use stable, relative links when creating references in Memory Bank files:

1. **Relative Paths:** Use relative paths (e.g., `../source/file.ts`), NEVER absolute paths or fixed root paths
2. **No Line Numbers:** Link to the file only (e.g., `script.cs`), NEVER specific lines (e.g., `script.cs:50`)
3. **Descriptive Text:** Use descriptive link text, such as `[ServiceName.Method()](../path/Service.cs)`
4.  **BPMN:** Link using Process Name or Element ID if applicable
5. **Cross-Project Links:** When linking to another project in the monorepo, navigate up to the root and down to the target project's memory bank (e.g., `../../other-project/memory-bank/`)
6. **Memory Bank Target:** Always link to the `memory-bank/` directory itself (with a trailing slash), NEVER to a specific file within it (like `brief.md`) when referring to the project's Memory Bank as a whole.

## Manual enforcement mode (no-hooks)

- Enforce bootstrap policy, scope lock, and MB lifecycle by instruction discipline from AGENTS + `mb-*` skills.
- If runtime hooks are unavailable, this profile is the default fallback.
- Treat uncertainty as high risk: reload context and re-run scope checks before write operations.

---

## MB + Superpowers Overlay (Monorepo)

If any instruction conflicts with this overlay, this overlay wins.

### Planning and execution gates

- `@mb_plan`: brainstorming gate, proposal-as-plan, interactive completeness loop.
- `@mb_plan`: compute and persist `risk_score` (`Strict` when `risk_score >= 3`, otherwise `Light`).
- `@mb_act` Strict mode: run in isolated subagent/session with clean context and C1 handoff only.
- If strict-mode isolation is unavailable, stop in `BLOCKED` and return to `@mb_plan`.
- Strict-mode completion requires E1 evidence: handshake marker + `Last Full Reload`.

### Monorepo execution safety

- Default branch policy: branch-in-place.
- Worktree is optional for high-interference isolation.
- Never implement on `main/master` without explicit user approval.
- Stage only files in approved proposal scope; avoid blind `git add .`.

## Tool Usage

- When exploring solutions, implementing, debugging, and testing .NET projects, prioritize tools provided by `vs-mcp`. If they are unavailable in these situations or do not support the projects being developed, ask the user to set it up.
- If you need to debug a .NET application, load the precise instructions located this instruction file: `ai-debugging-guide.md`
