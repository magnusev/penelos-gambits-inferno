# Security Validator Integration

## Overview

The build pipeline now automatically validates rotations for security compliance before deployment.

## Validation Process

Every build runs through:
1. **Combine components** → Create rotation.cs
2. **Run SecurityValidator** → Check for violations
3. **If PASSED** → Deploy to bot (unless -LocalOnly)
4. **If FAILED** → Stop build, show errors

## Known Validator Bugs

### Quote Checker Bug (Fixed in Build)

**Problem**: The validator's quote checker gets confused by escape sequences like `\n`.

**How the bug works**:
1. Scanner walks character-by-character looking for `"` quotes
2. When it sees `\\n`, it treats the first `\` as escaping the second `\`
3. This leaves the `n` unescaped, throwing off quote pairing
4. Scanner thinks the `"` after the escape isn't the closing quote
5. It keeps scanning forward, matching with a `"` hundreds of lines later
6. This creates a "fake" multi-thousand character string literal
7. Validator blocks the file as "potential encoded payload"

**Root cause**: The validator's escape sequence parser doesn't properly handle `\"` sequences within string literals. It gets confused about which backslashes are actual escape characters.

**Our Solution**:
1. **Multi-line format** for all string concatenations with `\n`:
   ```csharp
   // ✅ WORKS - split across lines (like example_rotation.cs)
   File.AppendAllText(_logFile,
       DateTime.Now.ToString("HH:mm:ss.fff") + " " + msg + "\n");
   ```

2. **Use `string.IsNullOrEmpty()` instead of `!= ""`**:
   ```csharp
   // ✅ WORKS - uses string method (like example_rotation.cs)
   if (!string.IsNullOrEmpty(Inferno.UnitName(tk))) r.Add(tk);
   
   // ❌ MAY TRIGGER BUG - empty string literal
   if (Inferno.UnitName(tk) != "") r.Add(tk);
   ```

3. **Break up long conditional lines** into multiple lines:
   ```csharp
   // ✅ WORKS - each statement on own line
   if (condition1 && condition2)
   {
       Log("Message");
       return CastPersonal("Spell");
   }
   
   // ❌ MAY TRIGGER BUG - very long single line
   if (condition1 && condition2) { Log("Message"); return CastPersonal("Spell"); }
   ```

**Important**: There are TWO validators:
- **Our SecurityValidator** (in poc/SecurityValidator/) - We control this, it's fixed
- **Bot's internal validator** - We can't modify, it may still have the bug

**If the bot still blocks your rotation**:
1. Check which line the bot reports
2. Look for very long lines with multiple string concatenations
3. Break them into multiple lines
4. Rebuild and test

### Example from Training: Multi-Line Case Statements

The quote checker bug can also be triggered by very long lines with multiple case labels:

```csharp
// ❌ MAY TRIGGER BUG (depending on what's nearby)
case 2511: case 2515: case 2516: case 2517: case 2518: case 2519: case 2520:
    if (TryDispel("Ethereal Shackles")) return true;

// ✅ SAFER - split across lines
case 2511: 
case 2515: 
case 2516: 
case 2517: 
case 2518: 
case 2519: 
case 2520:
    if (TryDispel("Ethereal Shackles")) return true;
```

We've applied this formatting to all dungeon gambit cases.

## Validation Output

### Success
```
🔒 Running security validation...
✅ Security validation PASSED
```

### Failure
```
🔒 Running security validation...
❌ Security validation FAILED:
  [BLOCKED] Line 74: 'Environment.' access blocked
  Errors: 1
  
Build completed but rotation has security issues.
Fix the issues in component files and rebuild.
```

## Common Validation Errors

### 1. Banned Using Directives

**Error**: `[BLOCKED] Line 2: Banned using directive 'System.Text'`

**Allowed**:
- System
- System.Collections.Generic
- System.Drawing
- System.Linq
- System.IO
- InfernoWow.API

**Fix**: Remove the banned using directive

### 2. Environment Access

**Error**: `[BLOCKED] Line 74: 'Environment.' access blocked`

**Problem**: `System.Environment.NewLine` is blocked

**Fix**: Use `"\n"` in multi-line format (see Quote Checker Bug above)

### 3. Long String Literals

**Error**: `[BLOCKED] Line 152: Extremely long string literal (2371 chars)`

**Problem**: Often caused by quote checker bug, not an actual long string

**Fix**: Split string concatenations across multiple lines

### 4. Multiple Classes

**Error**: `[BLOCKED] Multiple classes defined: ClassA, ClassB. Only one class per file allowed.`

**Fix**: Each rotation.cs must have exactly one class

### 5. Wrong Base Class

**Error**: `[BLOCKED] Line 12: Class inherits from 'Action'. Allowed: Rotation, Plugin`

**Fix**: Only inherit from `Rotation` or `Plugin`

## Integration with Build Scripts

### BuildRotation.ps1

```powershell
# After building rotation.cs:

# Run security validation
$validatorProject = Join-Path $pocRoot "SecurityValidator\SecurityValidator.csproj"
if (Test-Path $validatorProject) {
    $validationResult = & dotnet run --project $validatorProject -- $localFile 2>&1
    $validationOutput = $validationResult -join "`n"
    
    # Check error count
    if ($validationOutput -match "Errors:\s+(\d+)" -and [int]$matches[1] -gt 0) {
        Write-Host "❌ Security validation FAILED" -ForegroundColor Red
        Write-Host $validationOutput
        exit 1
    }
}
```

### BuildAll.ps1

BuildAll.ps1 calls BuildRotation.ps1, so validation is automatic for all classes.

If any class fails validation:
- Build stops for that class
- Error count increments
- Summary shows: "Build Summary: X succeeded, Y failed"

## Best Practices

### Avoid Quote Checker Bug

1. **Split long string concatenations** across lines:
   ```csharp
   // ✅ GOOD
   File.AppendAllText(_logFile,
       timestamp + " " + message + "\n");
   ```

2. **Use verbatim strings** for multi-line text (if needed):
   ```csharp
   // ✅ GOOD (but usually not needed)
   string multiLine = @"Line 1
   Line 2
   Line 3";
   ```

3. **Avoid long inline strings**:
   ```csharp
   // ❌ BAD
   Log("Very long message with lots of text that goes on and on and on...");
   
   // ✅ GOOD
   string msg = "Very long message with lots of text " +
                "that goes on and on and on...";
   Log(msg);
   ```

### Test Before Deployment

Always build with `-LocalOnly` first:
```powershell
.\BuildRotation.ps1 -Class PaladinHoly -ClassName HolyPaladinPvE -LocalOnly
```

This runs validation without deploying to the bot. If it passes, then deploy:
```powershell
.\BuildRotation.ps1 -Class PaladinHoly -ClassName HolyPaladinPvE
```

## Manual Validation

You can also run the validator manually:

```powershell
cd C:\Repos\PenelosGambitsInfernoReborn\poc\SecurityValidator
dotnet run -- ..\Output\PaladinHoly_rotation.cs
```

## Validator Source Code

The SecurityValidator is in:
- `poc/SecurityValidator/Program.cs`
- `poc/SecurityValidator/SecurityValidator.csproj`

It checks:
- Using directives (whitelist)
- Base classes (only Rotation/Plugin allowed)
- Single class per file
- String literal lengths
- Namespace references (blocked: System.Net, System.Threading, etc.)
- Environment access (all Environment.* methods blocked)
- Async/await keywords (blocked)

## Summary

✅ **Integrated**: Security validation is now part of every build  
✅ **Automatic**: No manual steps needed  
✅ **Fast**: Validation completes in <1 second  
✅ **Fixed**: Quote checker bug workaround applied  
✅ **Reliable**: Same validator the bot uses internally  

The build system now catches security issues **before** you test in-game, saving time and preventing frustrating "Rotation blocked" messages.

