# ChatGPTExt — FIXES_TASKLOG
<!-- Append-only. Never rewrite history. -->

---

### TASK-ID: TASK-SETUP-01 — Project Bootstrap & Architecture Audit
- Status: IN-PROGRESS
- Started: 2026-03-06 23:15 (local)
- Last update: 2026-03-06 23:15 (local)

### Fragments

- [x] F01 — Architecture enforcement checklist + create FIXES_TASKLOG
  - Commit: no-commit
  - Evidence: build OK (0 errors, 0 warnings before this session)
  - Notes: Checklist complete. Findings logged below.

- [ ] F02 — Repo hygiene: delete 3 temp files, verify gitignore, git commit
  - Plan: Delete d1hdymqh.htz~, feikqqpo.fxd~, zat5gedo.mur~ from repo root; confirm not tracked; commit FIXES folder + tasklog
  - Preconditions: 0 build errors

### Resume pointer
- Resume from: F02
- Last known good state:
  - Build: OK (0 errors, 0 warnings)
  - Git: clean (no staged/unstaged changes)

---

## F01 — Architecture Enforcement Results

### PASS items
- [x] Single project — no cross-project dependency violations (N/A: single VSIX project)
- [x] No domain entities — private setter rule N/A
- [x] No repositories — persistence-only rule N/A
- [x] No empty catch blocks
- [x] No `.Result` / `.Wait()` anti-patterns
- [x] Async method naming correct (all async methods end with Async)

### FAIL items — must fix in Phase 1

**FAIL-01: Dead code in AskChatGptSelectionCommand.cs (lines 80–93)**
- `aiprompt` is built but never sent — `ui.SendAsync(prompt)` fires with the old dumb prompt instead
- The richer context-aware `aiprompt` is assembled after the fact and discarded
- Rule violated: dead/disconnected logic
- Fix in: TASK-FIX-01 / F01

**FAIL-02: Hardcoded model in ChatGptToolWindowControl.xaml.cs (line 49)**
- `const string model = "qwen2.5-coder:3b"` — ignores the ModelBox ComboBox in the UI
- ModelBox ComboBox lists gpt-5 / gpt-4o but is wired to nothing
- Rule violated: hardcoded values / dead UI control
- Fix in: TASK-FIX-01 / F02

**FAIL-03: FilePath always null in EditorContextService.cs (lines 70–84)**
- `string? filePath = null;` — IVsUserData GUID lookup is a stub, never populates FilePath
- Prompt sent to LLM always shows `File: ` with no value
- Rule violated: incomplete implementation presented as working
- Fix in: TASK-FIX-01 / F03

**FAIL-04: OpenAiClient ignores constructor model parameter (OpenAiClient.cs)**
- Constructor accepts `model` but uses `DefaultModel = "gpt-4o"` constant in the payload
- Rule violated: ignored constructor parameter
- Fix in: TASK-FIX-01 / F04

**FAIL-05: AskChatGptSelectionCommand calls ui.SendAsync before context is fetched**
- `ui.SendAsync(prompt)` fires at line ~67 with old dumb prompt
- `GetCurrentAsync` (line ~69) is called AFTER — so selection context is never in the prompt
- Combined with FAIL-01: full prompt pipeline is broken end-to-end
- Fix in: TASK-FIX-01 / F01 (same fix as FAIL-01)

---

### TASK-ID: TASK-FIX-01 — Fix broken prompt pipeline (Phase 1 core fixes)
- Status: PENDING
- Started: —
- Last update: —

### Fragments

- [ ] F01 — Fix AskChatGptSelectionCommand: reorder context fetch + send context-aware aiprompt
  - Plan: Move `GetCurrentAsync` before `ShowToolWindowAsync`; replace `ui.SendAsync(prompt)` with `ui.SendAsync(aiprompt)`; remove dead `prompt` variable
  - Preconditions: 0 build errors
  - Files: AskChatGptSelectionCommand.cs

- [ ] F02 — Wire ModelBox to client selection in ChatGptToolWindowControl
  - Plan: Remove hardcoded `const string model`; read ModelBox.SelectedItem at send time; instantiate correct client (Ollama vs OpenAI) based on selection
  - Preconditions: F01 done, 0 build errors
  - Files: ChatGptToolWindowControl.xaml.cs, ChatGptToolWindowControl.xaml

- [ ] F03 — Fix FilePath in EditorContextService using IVsPersistDocData moniker
  - Plan: Replace stub IVsUserData GUID lookup with `IVsPersistDocData.GetGuidEditorType` + `IVsRunningDocumentTable` to get correct file path
  - Preconditions: F02 done, 0 build errors
  - Files: Services/EditorContextService.cs

- [ ] F04 — Fix OpenAiClient to use constructor model parameter
  - Plan: Replace `DefaultModel` constant reference in payload with `_model` field
  - Preconditions: F03 done, 0 build errors
  - Files: OpenAiClient.cs

### Resume pointer
- Resume from: F01
- Last known good state:
  - Build: OK
  - Git: clean

---

### TASK-SETUP-01 update
- [x] F02 — Repo hygiene: temp files deleted, FIXES folder committed
  - Commit: 93458ec
  - Evidence: git clean, build OK

---

### TASK-FIX-01 — Multi-provider + broken pipeline fix
- Status: DONE
- Last update: 2026-03-07

### Fragments completed

- [x] F01 — LlmProvider enum + ProviderSettings defaults — Commit: bfdcbcc
- [x] F02 — ChatGptOptionsPage (Tools > Options > AI Assistant) — Commit: bfdcbcc
- [x] F03 — LlmClientFactory — Commit: bfdcbcc
- [x] F04 — Wire factory into tool window control, provider shown in status — Commit: bfdcbcc
- [x] F05 — Fix AskChatGptSelectionCommand prompt pipeline (FAIL-01+05) — Commit: bfdcbcc
- [x] F06 — OpenAiClient configurable baseUrl+model (FAIL-04) — Commit: bfdcbcc
- [x] F07 — OllamaClient configurable baseUrl — Commit: bfdcbcc
- [x] F08 — Build gate: 0 errors, 0 warnings, VSIX produced — Commit: bfdcbcc

### Open item deferred
- [ ] FAIL-03 — FilePath always null in EditorContextService -> TASK-FIX-02

### Resume pointer
- Next: TASK-FIX-02 — Fix FilePath in EditorContextService
- Build: OK | Git: clean (bfdcbcc)

---

### Provider quick-reference (Tools > Options > AI Assistant)

| Provider      | Base URL                                | Key needed |
|---------------|-----------------------------------------|------------|
| GitHub Models | https://models.inference.ai.azure.com   | GitHub PAT |
| OpenAI        | https://api.openai.com                  | Yes        |
| Groq          | https://api.groq.com/openai             | Yes (free) |
| OpenRouter    | https://openrouter.ai/api               | Yes (free) |
| Mistral       | https://api.mistral.ai                  | Yes (free) |
| Ollama        | http://localhost:11434                  | No         |
| LM Studio     | http://localhost:1234                   | No         |
