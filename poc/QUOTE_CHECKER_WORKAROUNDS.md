# Quote Checker Bug - Complete Workaround Guide

## ✅ FINAL SOLUTION

**The build system now handles this automatically!**

The `BuildRotation.ps1` script:
1. **Strips all `//` comments** from the output (main fix)
2. **Applies code formatting rules** (multi-line strings, split loops, etc.)

**You don't need to manually apply workarounds** - just write clean, commented code in source files and let the build process handle the quote checker quirks!

---

## The Problem

The bot's internal security validator has a **buggy quote checker** that miscounts where strings start and end. When it gets confused, it thinks there's a "very long string literal" (thousands of characters) and blocks your rotation.

## Root Cause

The quote checker walks through your code character-by-character looking for `"` quotes. It has bugs in how it handles:
1. Escape sequences (`\n`, `\"`, etc.)
2. Empty string literals (`""`)
3. Long lines with multiple string concatenations

When it mismatches quotes, it creates a "phantom" super-long string spanning hundreds of lines, triggering the "extremely long string literal" security block.

## All Known Workarounds

### 1. **Strip Comments During Build** (PRIMARY FIX)

The build script now automatically strips all `//` comments from the output. This eliminates the majority of quote checker issues.

**Source file** (keeps comments for readability):
```csharp
// -- Queue System --
// Matches old ActionQueuer.QueueAction: don't overwrite if already queued
private bool CastOnFocus(string unit, string macro) 
{ 
    if (_queuedAction != null) return false;
    Inferno.Cast("focus_" + unit); 
    _queuedAction = macro; 
    return true; 
}
```

**Build output** (comments removed):
```csharp
private bool CastOnFocus(string unit, string macro) 
{ 
    if (_queuedAction != null) return false;
    Inferno.Cast("focus_" + unit); 
    _queuedAction = macro; 
    return true; 
}
```

---

### 2. Multi-Line Format for `\n` Strings

**❌ TRIGGERS BUG**:
```csharp
File.AppendAllText(_logFile, DateTime.Now.ToString("HH:mm:ss.fff") + " " + msg + "\n");
```

**✅ WORKS**:
```csharp
File.AppendAllText(_logFile,
    DateTime.Now.ToString("HH:mm:ss.fff") + " " + msg + "\n");
```

**Why**: The multi-line format prevents the parser from misinterpreting the `\n` escape sequence.

---

### 2. Multi-Line Format for `\n` Strings

**❌ TRIGGERS BUG**:
```csharp
File.AppendAllText(_logFile, DateTime.Now.ToString("HH:mm:ss.fff") + " " + msg + "\n");
```

**✅ WORKS**:
```csharp
File.AppendAllText(_logFile,
    DateTime.Now.ToString("HH:mm:ss.fff") + " " + msg + "\n");
```

**Why**: The multi-line format prevents the parser from misinterpreting the `\n` escape sequence.

---

### 3. Use `string.IsNullOrEmpty()` Instead of `!= ""`

**❌ TRIGGERS BUG**:
```csharp
if (Inferno.UnitName(tk) != "") r.Add(tk);
```

**✅ WORKS**:
```csharp
if (!string.IsNullOrEmpty(Inferno.UnitName(tk))) r.Add(tk);
```

**Why**: Empty string literals (`""`) confuse the quote counter. Using the `string` method avoids empty strings entirely.

---

### 4. Break Up For-Loops with String Concatenation

**❌ TRIGGERS BUG**:
```csharp
for (int i = 1; i <= 28; i++) Macros.Add("focus_raid" + i, "/focus raid" + i);
```

**✅ WORKS**:
```csharp
for (int i = 1; i <= 28; i++)
{
    Macros.Add("focus_raid" + i, "/focus raid" + i);
}
```

**Why**: Single-line for loops with string literals create dense quote patterns that confuse the parser.

---

### 5. Extract Long String Literals to Variables

**❌ TRIGGERS BUG**:
```csharp
CustomFunctions.Add("HasHealthstone", "return GetItemCount(5512) > 0 and 1 or 0");
```

**✅ WORKS**:
```csharp
string hasHealthstoneCode = "return GetItemCount(5512) > 0 and 1 or 0";
CustomFunctions.Add("HasHealthstone", hasHealthstoneCode);
```

**Why**: Embedded code strings (Lua, etc.) can contain patterns that confuse the parser. Extracting to a variable splits the quotes.

---

### 6. Break Up Long Lines with Multiple Strings

**❌ TRIGGERS BUG**:
```csharp
if (condition) { Log("Casting Divine Toll on " + t + " (" + HealthPct(t) + "%)"); return CastOnFocus(t, "cast_dt"); }
```

**✅ WORKS**:
```csharp
if (condition)
{ 
    Log("Casting Divine Toll on " + t + " (" + HealthPct(t) + "%)"); 
    return CastOnFocus(t, "cast_dt"); 
}
```

**Why**: Long lines with many `"..."` patterns in close proximity confuse the parser's quote pairing logic.

---

### 7. Split Multi-Case Labels Across Lines

**❌ MAY TRIGGER BUG**:
```csharp
case 2511: case 2515: case 2516: case 2517: case 2518: case 2519: case 2520:
    if (TryDispel("Ethereal Shackles")) return true;
```

**✅ SAFER**:
```csharp
case 2511: 
case 2515: 
case 2516: 
case 2517: 
case 2518: 
case 2519: 
case 2520:
    if (TryDispel("Ethereal Shackles")) return true;
```

**Why**: Very long lines, especially with keywords + string literals, can trigger miscounts.

---

## General Rules

### ✅ DO:
- Split string concatenations across multiple lines
- Use `string.IsNullOrEmpty()` instead of `== ""` or `!= ""`
- Break up long if-statements into multi-line format
- Keep individual lines under 150 characters
- Split case labels to one per line

### ❌ DON'T:
- Use `!= ""` or `== ""` for string comparisons
- Put multiple statements with string literals on one line
- Create very long lines with nested string concatenations
- Mix `\n` escape sequences with other strings on the same line

---

## How to Debug Quote Checker Issues

If the bot blocks your rotation with "Extremely long string literal" on line X:

### Step 1: Check the Reported Line

The line number is usually WRONG — the actual problem is earlier in the file where the quote mismatch starts.

### Step 2: Search Backwards for Patterns

From the reported line, search backwards for:
- `!= ""` or `== ""`
- `\n` on a single line with other strings
- Very long lines (>150 chars) with multiple `"..."` patterns

### Step 3: Apply Workarounds

Replace each pattern with the ✅ WORKS version above.

### Step 4: Rebuild and Test

```powershell
.\Build\BuildRotation.ps1 -Class YourClass -ClassName YourClassName
```

---

## Example: Real Bug We Fixed

**Error Message**:
```
[Security BLOCKED] Line 160: Extremely long string literal (2371 chars)
```

**Line 160** in output:
```csharp
160 |     else { r.Add("player"); }
```

This line is fine! The problem was **earlier in the file**.

**Lines 147 and 157** (the real culprit):
```csharp
147 |             if (Inferno.UnitName(tk) != "") r.Add(tk);  // ❌ BUG HERE
157 |             if (Inferno.UnitName(tk) != "") r.Add(tk);  // ❌ BUG HERE
```

**The Fix**:
```csharp
147 |             if (!string.IsNullOrEmpty(Inferno.UnitName(tk))) r.Add(tk);  // ✅ FIXED
157 |             if (!string.IsNullOrEmpty(Inferno.UnitName(tk))) r.Add(tk);  // ✅ FIXED
```

After this change, the validator passed!

---

## Summary

The quote checker bug is annoying but manageable. By following these patterns (all copied from the working `example_rotation.cs`), you can avoid triggering it:

1. ✅ Multi-line `\n` strings
2. ✅ `string.IsNullOrEmpty()` instead of `== ""` / `!= ""`
3. ✅ Split long lines with multiple strings
4. ✅ One case label per line

**All these patterns are now built into the Components/** — future classes will automatically avoid the bug! 🎉





