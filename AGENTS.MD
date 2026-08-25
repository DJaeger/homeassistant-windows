# AI Agent Reference for Home Assistant Windows Companion

---

## Token Efficiency Rules (CRITICAL — Read First)

These rules apply to **every response** without exception:

1. **Output minimal prose.** Bullet points only. No introductory sentences, no filler, no "Great question!", no "As requested".
2. **No walkthrough unless explicitly asked.** Never create or update `walkthrough.md` unless the user writes "walkthrough" or "summary" in their request.
3. **No implementation plan unless complex.** Skip planning artifacts for simple tweaks, single-file edits, bug fixes, or minor features. Plan only for major architectural changes.
4. **Short change summaries only.** After making changes, output ≤5 bullet points describing *what* changed and *why* — never a line-by-line description.
5. **No repeating file content.** Never echo back code you just wrote or edited. Reference filenames with links instead.
6. **No tool-call narration.** Do not describe what tool you are about to call. Just call it.
7. **Targeted file reads only.** Use `grep_search` or `view_file` with `StartLine`/`EndLine` to read only the relevant section. Never view an entire large file unless strictly necessary.
8. **Use the manifest first.** Before exploring the codebase, read `project_manifest.json` and `project_connections.json`. Do not walk directories recursively.
9. **Parallel tool calls.** Fire all independent tool calls in a single block. Never sequence calls that can run simultaneously.
10. **No re-summarizing artifacts.** After creating or updating an artifact, do NOT restate its contents — just link to it and note any open questions.
11. **Skip trivial confirmations.** Do not ask "Would you like me to proceed?" for obvious next steps. Just do them.
12. **No closing pleasantries.** End your response after the change summary. No "Let me know if you have questions!" etc.
13. **Omit unchanged layers.** If a change only affects the viewmodel, do not mention views or core layers unless they are also modified.
14. **Reuse subagents.** Send follow-up tasks to an existing idle subagent instead of spawning a new one.

---

## Project Manifest & Connection Map (Single Source of Truth)

Read these before exploring the codebase — they cover the entire file tree, endpoints, models, and feature flows:

- **Manifest**: `project_manifest.json` — classes, properties, methods, namespaces, file tree.
- **Connections**: `project_connections.json` — file-level mapping for feature flows (e.g. settings, sensors, notifications, commands).

---

## Codebase Architecture

The project is a WinUI 3 desktop application structured in three projects:

| Layer | Project | Path | Purpose |
|---|---|---|---|
| **App** | `HAWindowsCompanion.App` | `src/HAWindowsCompanion.App/` | UI/UX layer, ViewModels, Views, Navigation, Window subclassing |
| **Core** | `HAWindowsCompanion.Core` | `src/HAWindowsCompanion.Core/` | Domain interfaces, models, helper utilities |
| **Infrastructure** | `HAWindowsCompanion.Infrastructure` | `src/HAWindowsCompanion.Infrastructure/` | API clients, sensors implementation, WebSocket command dispatcher, local persistence, authentication |

### Key Locations

- App entry & DI configuration: `src/HAWindowsCompanion.App/App.xaml.cs`
- ViewModels: `src/HAWindowsCompanion.App/ViewModels/`
- Views/Pages: `src/HAWindowsCompanion.App/Views/`
- Sensors: `src/HAWindowsCompanion.Infrastructure/Sensors/`
- Command Handlers: `src/HAWindowsCompanion.Infrastructure/Commands/`

---

## CLI Commands

| Task | Command | Dir |
|---|---|---|
| Restore dependencies | `dotnet restore HAWindowsCompanion.sln` | Root |
| Build solution | `dotnet build HAWindowsCompanion.sln -c Release -p:Platform=x64` | Root |
| Package application | `python build_release.py` | Root |
| Regenerate manifest | `python scripts/generate_manifest.py` | Root |

---

## Coding Rules

### WinUI 3 / MVVM
- Use **CommunityToolkit.Mvvm** for ViewModels (`ObservableObject`, `ObservableProperty`, `RelayCommand`).
- Bind properties using `{x:Bind}` in XAML wherever possible.
- Pages should resolve their ViewModels through constructor DI configured in `App.xaml.cs`.

### Quality & Hygiene
- **No silent failures:** Avoid empty `catch` blocks. All caught exceptions should be logged using `_logger` or handled/reported to the UI.
- **Background threads:** Keep resource-intensive operations (such as WMI queries or large I/O tasks) off the UI thread via `Task.Run`.
- **Resource cleanup:** Ensure temporary resources, `ServiceProvider` instances, and WMI objects (`ManagementObjectSearcher`/`ManagementObject`) are properly disposed using `using` statements/declarations.
- **Languages:** Default to English (EN) for all code, logs, and user interface elements.

---

## Language Rules

- **Codebase & Documentation**: All program code, documentation files, READMEs, and inline code comments must always be written in English (EN), unless explicitly requested otherwise.
- **AI Chat Interactions**: The AI must respond in English (EN) by default, unless the user explicitly requests another language.
