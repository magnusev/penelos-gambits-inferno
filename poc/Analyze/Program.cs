var ours = System.IO.File.ReadAllBytes(@"C:\Repos\PenelosGambitsInfernoReborn\poc\rotation.cs");
Console.WriteLine($"Size: {ours.Length} bytes, first3: {ours[0]:X2} {ours[1]:X2} {ours[2]:X2}");

var text = System.IO.File.ReadAllText(@"C:\Repos\PenelosGambitsInfernoReborn\poc\rotation.cs");
var allText = text.Replace("\r", "");
var allLines = allText.Split('\n');
Console.WriteLine($"Lines: {allLines.Length}");

// Simulate the Inferno validator's naive string literal scanner
// It probably scans the raw file text for " characters and pairs them
var quotePositions = new List<int>();
for (int i = 0; i < allText.Length; i++)
    if (allText[i] == '"') quotePositions.Add(i);

Console.WriteLine($"Total '\"' characters: {quotePositions.Count}");

// Check paired quotes (0-1, 2-3, 4-5, etc.) for large gaps
Console.WriteLine("\nQuote pairs with gap > 100:");
for (int i = 0; i < quotePositions.Count - 1; i += 2)
{
    int gap = quotePositions[i + 1] - quotePositions[i] - 1;
    if (gap > 100)
    {
        int startPos = quotePositions[i];
        int lineNum = 0;
        int charCount = 0;
        for (int j = 0; j < allLines.Length; j++)
        {
            charCount += allLines[j].Length + 1;
            if (charCount > startPos) { lineNum = j + 1; break; }
        }
        string snippet = allText.Substring(startPos, Math.Min(60, allText.Length - startPos)).Replace("\n", "\\n");
        Console.WriteLine($"  Pair {i/2}: gap={gap} line~{lineNum} [{snippet}]");
    }
}

// Also check: what if the validator uses an odd/even mismatch?
// Check every consecutive pair
Console.WriteLine("\nConsecutive pairs with gap > 500:");
for (int i = 0; i < quotePositions.Count - 1; i++)
{
    int gap = quotePositions[i + 1] - quotePositions[i] - 1;
    if (gap > 500)
    {
        int startPos = quotePositions[i];
        int endPos = quotePositions[i + 1];
        int startLine = 0, endLine = 0;
        int charCount = 0;
        for (int j = 0; j < allLines.Length; j++)
        {
            charCount += allLines[j].Length + 1;
            if (startLine == 0 && charCount > startPos) startLine = j + 1;
            if (endLine == 0 && charCount > endPos) endLine = j + 1;
        }
        Console.WriteLine($"  idx {i}->{i+1}: gap={gap} lines {startLine}-{endLine}");
    }
}
