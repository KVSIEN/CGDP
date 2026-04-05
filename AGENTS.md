# CGDP Codex Guide

Keep context lean and task-focused.

## Scope
- Work primarily in `Assets/`, `ProjectSettings/`, and root docs that affect gameplay work.
- Read only what is needed for the current task.
- Prefer targeted search over opening whole files.

## Ignore By Default
- Do not read `Library/`, `Logs/`, `Temp/`, `obj/`, `Packages/`, or binary asset files unless the task explicitly requires it.
- Do not scan generated project files unless debugging build tooling.

## Search Strategy
- Use `rg` to find symbols, component names, and scene/prefab references before opening files.
- Open the smallest relevant slice of each file, not the whole codebase.
- When investigating Unity wiring, search prefabs/scenes for component names instead of reading raw YAML end to end.

## Unity Rules
- Use `[SerializeField] private` instead of widening visibility.
- Avoid `Find`, `FindObjectOfType`, and `SendMessage`; prefer serialized references.
- Cache component lookups in `Awake`/`Start`; avoid repeated `GetComponent` in frame loops.
- Avoid allocations in `Update`, `FixedUpdate`, and `LateUpdate`.
- Use the Input System already in the project; do not introduce the legacy `Input` API.

## Change Rules
- Make the smallest safe change that solves the request.
- Preserve existing architecture and naming unless a refactor is required.
- Update `FEATURES.md` only when user-facing behavior changes.
- Do not add packages, editor tooling, gizmos, or logging unless requested.

## Response Style
- Be concise.
- Summarize findings before details.
- Mention tests or verification run, and note if verification was not possible.
