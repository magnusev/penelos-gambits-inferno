// ═══════════════════════════════════════════════════════════════════════════
//  SecurityValidator.cs — Simulates the Inferno security validator
//  Build & run: dotnet run --project poc/SecurityValidator
//
//  Checks poc/rotation.cs against all known Inferno security constraints.
// ═══════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

class SecurityValidator
{
    static int Main(string[] args)
    {
        // Find rotation.cs relative to this project
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string sourceFile = null;

        var candidates = new[]
        {
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "poc", "rotation.cs")),
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "rotation.cs")),
            Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "poc", "rotation.cs")),
            Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "rotation.cs")),
        };

        foreach (var c in candidates)
        {
            if (File.Exists(c)) { sourceFile = c; break; }
        }

        if (args.Length > 0 && File.Exists(args[0]))
            sourceFile = args[0];

        if (sourceFile == null)
        {
            Console.WriteLine("ERROR: Could not find rotation.cs. Tried:");
            foreach (var c in candidates) Console.WriteLine("  " + c);
            return 1;
        }

        Console.WriteLine("Validating: " + sourceFile);
        Console.WriteLine(new string('=', 70));

        var lines = File.ReadAllLines(sourceFile);
        int errors = 0;
        int warnings = 0;
        int passes = 0;

        // ── Rule 1: Allowed using directives ────────────────────────
        var allowedUsings = new HashSet<string>
        {
            "System",
            "System.Collections.Generic",
            "System.Drawing",
            "System.Linq",
            "System.IO",
            "InfernoWow.API"
        };

        bool anyBadUsing = false;
        for (int i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (trimmed.StartsWith("using ") && trimmed.EndsWith(";") && !trimmed.Contains("("))
            {
                var ns = trimmed.Substring(6).TrimEnd(';').Trim();
                if (!allowedUsings.Contains(ns))
                {
                    Console.WriteLine($"  [BLOCKED] Line {i + 1}: Banned using directive '{ns}'");
                    errors++;
                    anyBadUsing = true;
                }
            }
        }
        if (!anyBadUsing) { Console.WriteLine("  [PASS] Using directives — all allowed"); passes++; }

        // ── Rule 2: Allowed base classes ────────────────────────────
        bool anyBadBase = false;
        var classInheritPattern = new Regex(@"\bclass\s+(\w+)\s*:\s*(\w+)");
        for (int i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (trimmed.StartsWith("//")) continue;
            var match = classInheritPattern.Match(trimmed);
            if (match.Success)
            {
                var baseClass = match.Groups[2].Value;
                if (baseClass != "Rotation" && baseClass != "Plugin")
                {
                    Console.WriteLine($"  [BLOCKED] Line {i + 1}: Class '{match.Groups[1].Value}' inherits from '{baseClass}'. Allowed: Rotation, Plugin");
                    errors++;
                    anyBadBase = true;
                }
            }
        }
        if (!anyBadBase) { Console.WriteLine("  [PASS] Base classes — only Rotation/Plugin"); passes++; }

        // ── Rule 3: One class per file ──────────────────────────────
        // IMPORTANT: The real Inferno validator does NOT skip comments.
        // It naively pattern-matches 'class <word>' even inside // comments.
        var classDecls = new List<(int line, string name)>();
        var classDeclPattern = new Regex(@"\bclass\s+(\w+)");
        for (int i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            // Do NOT skip comments — the real validator matches in comments too
            var match = classDeclPattern.Match(trimmed);
            if (match.Success)
            {
                classDecls.Add((i + 1, match.Groups[1].Value));
            }
        }
        if (classDecls.Count > 1)
        {
            Console.WriteLine($"  [BLOCKED] Multiple classes defined: {string.Join(", ", classDecls.Select(c => c.name))} ({classDecls.Count} total)");
            errors++;
        }
        else if (classDecls.Count == 1)
        {
            Console.WriteLine($"  [PASS] Single class: {classDecls[0].name}"); passes++;
        }
        else
        {
            Console.WriteLine($"  [WARNING] No class found"); warnings++;
        }

        // ── Rule 4: No long string literals ─────────────────────────
        bool anyLongString = false;
        var stringLitPattern = new Regex("\"([^\"\\\\]|\\\\.)*\"");
        for (int i = 0; i < lines.Length; i++)
        {
            var matches = stringLitPattern.Matches(lines[i]);
            foreach (Match m in matches)
            {
                if (m.Value.Length > 2000)
                {
                    Console.WriteLine($"  [BLOCKED] Line {i + 1}: Extremely long string literal ({m.Value.Length} chars)");
                    errors++;
                    anyLongString = true;
                }
            }
        }
        if (!anyLongString) { Console.WriteLine("  [PASS] No long string literals (all < 2000 chars)"); passes++; }

        // ── Rule 5: Banned namespace references in code ─────────────
        var bannedNamespaces = new[]
        {
            "System.Diagnostics",
            "System.Net.Http",
            "System.Net.WebSockets",
            "System.Threading.Tasks"
        };
        bool anyBannedRef = false;
        for (int i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            // Do NOT skip comments — real validator scans everything
            foreach (var banned in bannedNamespaces)
            {
                if (trimmed.Contains(banned))
                {
                    Console.WriteLine($"  [BLOCKED] Line {i + 1}: Banned namespace reference '{banned}'");
                    errors++;
                    anyBannedRef = true;
                }
            }
        }
        // Also check for Stopwatch (System.Diagnostics type)
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains("Stopwatch"))
            {
                Console.WriteLine($"  [BLOCKED] Line {i + 1}: Stopwatch requires System.Diagnostics");
                errors++;
                anyBannedRef = true;
            }
        }
        if (!anyBannedRef) { Console.WriteLine("  [PASS] No banned namespace references"); passes++; }

        // ── Rule 5b: Environment access blocked ─────────────────────
        // The real validator blocks ALL 'Environment.' references (even in comments)
        bool anyEnvAccess = false;
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains("Environment."))
            {
                Console.WriteLine($"  [BLOCKED] Line {i + 1}: 'Environment.' access blocked — matched pattern");
                errors++;
                anyEnvAccess = true;
            }
        }
        if (!anyEnvAccess) { Console.WriteLine("  [PASS] No blocked 'Environment.' access"); passes++; }

        // ── Rule 6: No unexpected namespace declarations ────────────
        bool anyBadNamespace = false;
        for (int i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (trimmed.StartsWith("//")) continue;
            if (Regex.IsMatch(trimmed, @"^namespace\s+"))
            {
                // InfernoWow.Modules is the required namespace for rotations
                if (trimmed == "namespace InfernoWow.Modules") continue;
                Console.WriteLine($"  [BLOCKED] Line {i + 1}: Namespace declaration: {trimmed}");
                errors++;
                anyBadNamespace = true;
            }
        }
        if (!anyBadNamespace) { Console.WriteLine("  [PASS] Namespace (InfernoWow.Modules or none)"); passes++; }

        // ── Check: async/await ──────────────────────────────────────
        bool anyAsync = false;
        for (int i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (trimmed.StartsWith("//")) continue;
            if (Regex.IsMatch(trimmed, @"\basync\b") || Regex.IsMatch(trimmed, @"\bawait\b"))
            {
                Console.WriteLine($"  [BLOCKED] Line {i + 1}: async/await requires System.Threading.Tasks");
                errors++;
                anyAsync = true;
            }
        }
        if (!anyAsync) { Console.WriteLine("  [PASS] No async/await"); passes++; }

        // ── Check: Value tuple usage (test if they exist) ───────────
        bool usesTuples = false;
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains("(int") && lines[i].Contains("Func<"))
            {
                usesTuples = true;
                break;
            }
        }
        if (usesTuples)
        {
            Console.WriteLine("  [INFO] Value tuples are used — requires C# 7+. Verify Inferno runtime supports them.");
            Console.WriteLine("         If not, fallback to if-chain or nested private class.");
            warnings++;
        }

        // ── Check: DateTime-based throttle usage ─────────────────────
        bool usesDateTimeThrottle = false;
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains("DateTime.UtcNow"))
            {
                usesDateTimeThrottle = true;
                break;
            }
        }
        if (usesDateTimeThrottle)
        {
            Console.WriteLine("  [PASS] Uses DateTime.UtcNow for throttling — no banned imports needed");
            passes++;
        }

        // ── Check: File I/O (allowed via System.IO) ────────────────
        bool usesFileIO = false;
        for (int i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (trimmed.StartsWith("//")) continue;
            if (trimmed.Contains("File.") || trimmed.Contains("StreamWriter"))
            {
                usesFileIO = true;
                break;
            }
        }
        if (usesFileIO)
        {
            Console.WriteLine("  [PASS] Uses System.IO File operations — allowed"); passes++;
        }

        // ── Check: Pattern completeness ─────────────────────────────
        var requiredPatterns = new Dictionary<string, string>
        {
            { "Spellbook.Add", "Spell registration in Initialize()" },
            { "Macros.Add", "Macro registration in Initialize()" },
            { "override void Initialize", "Initialize() override" },
            { "override bool CombatTick", "CombatTick() override" },
            { "GetGroupMembers", "Group member detection (solo/party/raid)" },
            { "InRaid()", "Raid detection" },
            { "InParty()", "Party detection" },
            { "_queuedAction", "Action queue pattern (focus→queue→cast)" },
            { "focus_", "Focus macro pattern" },
            { "DateTime.UtcNow", "Throttle via DateTime (no banned imports)" },
            { "Func<bool>", "Functional conditions (Func<bool>)" },
            { "OrderBy", "LINQ-based selector (replaces FilterChainSelector)" },
            { "FirstOrDefault", "LINQ selector terminal" },
        };

        Console.WriteLine();
        Console.WriteLine("  Pattern checks:");
        foreach (var kv in requiredPatterns)
        {
            bool found = lines.Any(l => l.Contains(kv.Key));
            if (found)
            {
                Console.WriteLine($"    [PASS] {kv.Value}");
                passes++;
            }
            else
            {
                Console.WriteLine($"    [MISSING] {kv.Value} (expected '{kv.Key}')");
                warnings++;
            }
        }

        // ── Summary ─────────────────────────────────────────────────
        Console.WriteLine();
        Console.WriteLine(new string('=', 70));
        Console.WriteLine($"  Lines scanned:  {lines.Length}");
        Console.WriteLine($"  Passes:         {passes}");
        Console.WriteLine($"  Errors:         {errors}");
        Console.WriteLine($"  Warnings/Info:  {warnings}");
        Console.WriteLine();

        if (errors == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  ✓ PASSED — rotation.cs is compliant with Inferno security validator");
            Console.ResetColor();
            return 0;
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ✗ FAILED — rotation.cs has security violations");
            Console.ResetColor();
            return 1;
        }
    }
}
