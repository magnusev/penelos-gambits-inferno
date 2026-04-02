var ours = System.IO.File.ReadAllBytes(@"C:\Repos\PenelosGambitsInfernoReborn\poc\rotation.cs");
var example = System.IO.File.ReadAllBytes(@"C:\Repos\PenelosGambitsInfernoReborn\PenelosGambits\examples\Paladin Protection\rotation.cs");
Console.WriteLine($"Ours: {ours.Length} bytes, first3: {ours[0]:X2} {ours[1]:X2} {ours[2]:X2}");
Console.WriteLine($"Example: {example.Length} bytes, first3: {example[0]:X2} {example[1]:X2} {example[2]:X2}");

// Count quotes in our file
var text = System.IO.File.ReadAllText(@"C:\Repos\PenelosGambitsInfernoReborn\poc\rotation.cs");
var lines = text.Split('\n');
Console.WriteLine($"Lines: {lines.Length}");

// Find longest span between quotes (simulating a naive validator)
int maxSpan = 0; int maxSpanLine = 0;
for (int i = 0; i < lines.Length; i++)
{
    var line = lines[i];
    // Find all quote positions
    for (int j = 0; j < line.Length; j++)
    {
        if (line[j] == '"')
        {
            // find matching close quote
            int k = j + 1;
            while (k < line.Length && line[k] != '"') k++;
            if (k < line.Length)
            {
                int span = k - j - 1;
                if (span > maxSpan) { maxSpan = span; maxSpanLine = i + 1; }
                j = k; // skip past closing quote
            }
        }
    }
}
Console.WriteLine($"Max string literal on single line: {maxSpan} chars at line {maxSpanLine}");

// Now simulate cross-line: find all quote positions in the entire file
var allText = text.Replace("\r", "");
var quotePositions = new System.Collections.Generic.List<int>();
for (int i = 0; i < allText.Length; i++)
    if (allText[i] == '"') quotePositions.Add(i);
Console.WriteLine($"Total quote chars: {quotePositions.Count}");

// Check for any pair with large gap
int maxGap = 0; int maxGapIdx = 0;
for (int i = 0; i < quotePositions.Count - 1; i += 2)
{
    int gap = quotePositions[i + 1] - quotePositions[i] - 1;
    if (gap > maxGap) { maxGap = gap; maxGapIdx = i; }
}
Console.WriteLine($"Max gap between quote pairs: {maxGap} chars");
if (maxGap > 100)
{
    int startPos = quotePositions[maxGapIdx];
    // Find which line this is on
    int lineNum = 0;
    int charCount = 0;
    var allLines = allText.Split('\n');
    for (int i = 0; i < allLines.Length; i++)
    {
        charCount += allLines[i].Length + 1;
        if (charCount > startPos) { lineNum = i + 1; break; }
    }
    Console.WriteLine($"  Starting at line ~{lineNum}, offset {startPos}");
    Console.WriteLine($"  Content near start: [{allText.Substring(startPos, Math.Min(80, allText.Length - startPos))}]");
}
