# Proof of Concept: Functional Style Inferno Rotation

## What this tests

This POC creates a minimal rotation that casts **Flash of Light on the lowest HP team member**, implemented in the functional single-class style required by the Inferno security validator.

### Migration Guide Constraints Tested

| # | Constraint | How Tested |
|---|---|---|
| 1 | Only allowed `using` directives | `rotation.cs` uses only the 6 allowed usings |
| 2 | Single class inheriting from `Rotation` | `FlashOfLightPOC : Rotation` — no other classes |
| 3 | No namespace declarations | Class is at top level |
| 4 | No `System.Diagnostics` (Stopwatch) | Throttle uses `Environment.TickCount` instead |
| 5 | No custom inheritance | All logic in methods, lambdas, `Func<bool>` — no custom base classes |
| 6 | No long string literals | All strings are short |
| 7 | Value tuples for gambits | `(int pri, string name, Func<bool> cond, Func<bool> exec)` |
| 8 | Functional conditions | `IsInCombat()`, `IsSpellReady()` — replace `Condition` interface |
| 9 | Functional selectors | `LowestAllyUnder()`, `GetAllyWithDebuff()` — replace `ISelector`/`FilterChainSelector` |
| 10 | Functional actions | `CastOnFocus()`, `ProcessQueue()` — replace `PersonalAction`/`FriendlyTargetedAction` |
| 11 | Group detection (solo/party/raid) | `GetGroupMembers()` — replaces `Group`/`PartyGroup`/`RaidGroup`/`Solo` |
| 12 | ActionQueuer pattern | `_queuedAction` field + `ProcessQueue()` at top of tick |
| 13 | Dungeon-specific gambits | `GetDungeonGambits(mapId)` with Proving Grounds Aqua Bomb dispel |
| 14 | Consume() pattern | `ThrottleRestart()` called inside action lambda (not condition) |
| 15 | Logging without System.Diagnostics | `Log()` uses `Inferno.PrintMessage()` + `File.AppendAllText()` |
| 16 | Settings | `LoadSettings()` with slider + checkbox |

## Files

| File | Purpose |
|---|---|
| `rotation.cs` | **The actual output** — the single-file rotation for Inferno |
| `CompileCheck/` | Project that compiles `rotation.cs` against Inferno API stubs |
| `CompileCheck/InfernoStubs.cs` | Minimal stubs for `Rotation`, `Inferno`, `Setting` |
| `SecurityValidator/` | Console app that checks `rotation.cs` against all security rules |

## How to validate

### 1. Compile check (does it build?)
```powershell
dotnet build poc/CompileCheck/CompileCheck.csproj
```

### 2. Security check (does it pass the validator?)
```powershell
dotnet run --project poc/SecurityValidator/SecurityValidator.csproj
```

### 3. Both
```powershell
dotnet build poc/CompileCheck/CompileCheck.csproj; dotnet run --project poc/SecurityValidator/SecurityValidator.csproj
```

## Expected output

```
  [PASS] Using directives — all allowed
  [PASS] Base classes — only Rotation/Plugin
  [PASS] Single class: FlashOfLightPOC
  [PASS] No long string literals (all < 2000 chars)
  [PASS] No banned namespace references
  [PASS] No namespace declarations
  [PASS] No async/await
  [PASS] Uses Environment.TickCount (replaces Stopwatch) — no banned imports needed
  [PASS] Uses System.IO File operations — allowed
  ...
  ✓ PASSED — rotation.cs is compliant with Inferno security validator
```
