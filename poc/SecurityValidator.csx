// ═══════════════════════════════════════════════════════════════════════════
//  SecurityValidator.csx — Simulates the Inferno security validator
//  Run: dotnet-script SecurityValidator.csx
//  Or:  dotnet script SecurityValidator.csx
//  Or just: csi SecurityValidator.csx  (if you have Roslyn scripting)
//
//  This is a standalone C# script that checks the POC rotation.cs file
//  against all known Inferno security rules.
// ═══════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

var filePath = Path.Combine(Path.GetDirectoryName(Environment.GetCommandLineArgs().Skip(1).FirstOrDefault() ?? ".")  ?? ".", "..", "poc", "rotation.cs");

// Try a few possible paths
var candidates = new[]
{
    Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "poc", "rotation.cs")),
    Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "poc", "rotation.cs")),
    Path.GetFullPath("rotation.cs"),
};

string sourceFile = null;
foreach (var c in candidates)
{
    if (File.Exists(c)) { sourceFile = c; break; }
}

if (sourceFile == null)
{
    Console.WriteLine("ERROR: Could not find rotation.cs. Tried:");
    foreach (var c in candidates) Console.WriteLine("  " + c);
    return;
}

Console.WriteLine("Validating: " + sourceFile);
Console.WriteLine(new string('=', 70));

var lines = File.ReadAllLines(sourceFile);
int errors = 0;
int warnings = 0;

// ── Rule 1: Allowed using directives ────────────────────────────────
var allowedUsings = new HashSet<string>
{
    "System",
    "System.Collections.Generic",
    "System.Drawing",
    "System.Linq",
    "System.IO",
    "InfernoWow.API"
};

for (int i = 0; i < lines.Length; i++)
{
    var trimmed = lines[i].Trim();
    if (trimmed.StartsWith("using ") && trimmed.EndsWith(";") && !trimmed.Contains("("))
    {
        var ns = trimmed.Substring(6).TrimEnd(';').Trim();
        if (!allowedUsings.Contains(ns))
        {
            Console.WriteLine($"[BLOCKED] Line {i + 1}: Banned using directive '{ns}'");
            errors++;
        }
    }
}

// ── Rule 2: Allowed base classes ────────────────────────────────────
var classPattern = new Regex(@"class\s+(\w+)\s*:\s*(\w+)");
for (int i = 0; i < lines.Length; i++)
{
    var match = classPattern.Match(lines[i]);
    if (match.Success)
    {
        var baseClass = match.Groups[2].Value;
        if (baseClass != "Rotation" && baseClass != "Plugin")
        {
            Console.WriteLine($"[BLOCKED] Line {i + 1}: Class inherits from '{baseClass}'. Allowed: Rotation, Plugin");
            errors++;
        }
    }
}

// ── Rule 3: One class per file ──────────────────────────────────────
var classDecls = new List<(int line, string name)>();
var classDeclPattern = new Regex(@"(?:public|private|internal|protected)?\s*(?:static\s+)?class\s+(\w+)");
for (int i = 0; i < lines.Length; i++)
{
    var trimmed = lines[i].Trim();
    if (trimmed.StartsWith("//")) continue; // skip comments
    var match = classDeclPattern.Match(trimmed);
    if (match.Success)
    {
        classDecls.Add((i + 1, match.Groups[1].Value));
    }
}
if (classDecls.Count > 1)
{
    Console.WriteLine($"[BLOCKED] Multiple classes defined: {string.Join(", ", classDecls.Select(c => c.name))}");
    errors++;
}
else if (classDecls.Count == 1)
{
    Console.WriteLine($"[OK] Single class: {classDecls[0].name}");
}

// ── Rule 4: No long string literals ─────────────────────────────────
var stringLitPattern = new Regex("\"([^\"\\\\]|\\\\.)*\"");
for (int i = 0; i < lines.Length; i++)
{
    var matches = stringLitPattern.Matches(lines[i]);
    foreach (Match m in matches)
    {
        if (m.Value.Length > 2000)
        {
            Console.WriteLine($"[BLOCKED] Line {i + 1}: Extremely long string literal ({m.Value.Length} chars)");
            errors++;
        }
    }
}

// ── Rule 5: Banned namespace references ─────────────────────────────
var bannedNamespaces = new[]
{
    "System.Diagnostics", "System.Text", "System.Net",
    "System.Net.Http", "System.Threading", "System.Threading.Tasks",
    "InfernoWow.Modules"
};

for (int i = 0; i < lines.Length; i++)
{
    var trimmed = lines[i].Trim();
    if (trimmed.StartsWith("//")) continue;
    foreach (var banned in bannedNamespaces)
    {
        if (trimmed.Contains(banned + ".") || trimmed.Contains(banned + ";") || trimmed.Contains(banned + " "))
        {
            // Skip false positives like "System.Environment.TickCount" being flagged for "System.Text"
            // The banned check is for the namespace reference, not substring
            if (banned == "System.Text" && trimmed.Contains("System.Text")) { }
            else if (banned == "System.Net" && !trimmed.Contains("System.Net")) continue;
            
            Console.WriteLine($"[BLOCKED] Line {i + 1}: Banned namespace reference '{banned}' in: {trimmed.Substring(0, Math.Min(trimmed.Length, 80))}");
            errors++;
        }
    }
}

// ── Rule 6: No namespace declarations ───────────────────────────────
for (int i = 0; i < lines.Length; i++)
{
    var trimmed = lines[i].Trim();
    if (trimmed.StartsWith("//")) continue;
    if (Regex.IsMatch(trimmed, @"^namespace\s+"))
    {
        Console.WriteLine($"[BLOCKED] Line {i + 1}: Namespace declaration found: {trimmed}");
        errors++;
    }
}

// ── Additional checks (warnings) ────────────────────────────────────

// Check for Stopwatch usage
for (int i = 0; i < lines.Length; i++)
{
    if (lines[i].Contains("Stopwatch"))
    {
        Console.WriteLine($"[WARNING] Line {i + 1}: Stopwatch usage detected (requires System.Diagnostics)");
        warnings++;
    }
}

// Check for async/await
for (int i = 0; i < lines.Length; i++)
{
    if (Regex.IsMatch(lines[i], @"\basync\b") || Regex.IsMatch(lines[i], @"\bawait\b"))
    {
        Console.WriteLine($"[WARNING] Line {i + 1}: async/await usage detected (requires System.Threading.Tasks)");
        warnings++;
    }
}

// ── Summary ─────────────────────────────────────────────────────────
Console.WriteLine(new string('=', 70));
Console.WriteLine($"Lines scanned: {lines.Length}");
Console.WriteLine($"Errors: {errors}");
Console.WriteLine($"Warnings: {warnings}");

if (errors == 0)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("✓ PASSED — File is compliant with Inferno security validator");
}
else
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("✗ FAILED — File has security violations");
}
Console.ResetColor();
