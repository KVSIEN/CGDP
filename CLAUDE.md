# Unity Project

## Project
- Unity URP, C#
- Source: `Assets/`, `ProjectSettings/`

## Reading
- Only read `.cs`, `.unity`, `.asset`, `.prefab`, `.inputactions`, `ProjectSettings/`
- Never read `Library/`, `Temp/`, `Logs/`, `obj/`, `Packages/`, or binary assets
- Grep for component names in scenes/prefabs; do not read raw YAML in full

## MonoBehaviour
- No `Find`, `FindObjectOfType`, or `SendMessage` — use serialized references
- `[SerializeField] private` over `public`
- Cache `GetComponent` in `Awake`; never call it in `Update`
- Use `TryGetComponent` when result may be null

## Performance
- No allocations in `Update`/`FixedUpdate`/`LateUpdate` — no LINQ, string concat, or `new` collections
- `CompareTag` over string `==` for tags
- Pool frequently spawned objects
- Coroutines for async flows; no UniTask/async unless already in project

## Architecture
- Game logic in plain C# classes; MonoBehaviours handle lifecycle and wiring only
- ScriptableObjects for shared data; no static singletons unless already present
- Use Input System (`InputSystem_Actions`); no legacy `Input` class

## Code Style
- No `#region` unless already in file
- No `Debug.Log` unless asked
- Early returns over nested conditionals
- No empty Unity event stubs
- No `using` directives for unused namespaces
- No editor scripts, custom inspectors, gizmos, or Package Manager changes unless asked

## Feature Tracking
- `FEATURES.md` in the project root lists all implemented features in plain English
- Update it whenever a feature is added, changed, or removed
- Keep entries non-technical — describe what it does, not how
