using System.Text.RegularExpressions;
using UnityEngine;

public class SyntaxHighlighter
{
    private const string ColorString = "#CE9178";    // Terracotta
    private const string ColorComment = "#6A9955";   // Green
    private const string ColorKeyword = "#569CD6";   // Light Blue
    private const string ColorBuiltin = "#4EC9B0";   // Teal
    private const string ColorNumber = "#B5CEA8";    // Light Green

    public static string Highlight(string code)
    {
        string pattern = 
            $@"(?<String>""[^""\\]*(?:\\.[^""\\]*)*""|'[^'\\]*(?:\\.[^'\\]*)*')|" +
            $@"(?<Comment>#.*)|" +
            $@"\b(?<Keyword>if|elif|else|for|while|from|and|or)\b|" +
            $@"\b(?<Builtin>print|rangew)\b|" +
            $@"\b(?<Number>\d+(?:\.\d+)?)\b";

        return Regex.Replace(code, pattern, MatchEvaluator);
    }

    private static string MatchEvaluator(Match match)
    {   
        if (match.Groups["String"].Success)
            return $"<color={ColorString}>{match.Groups["String"].Value}</color>";
        
        if (match.Groups["Comment"].Success)
            return $"<color={ColorComment}>{match.Groups["Comment"].Value}</color>";
        
        if (match.Groups["Keyword"].Success)
            return $"<color={ColorKeyword}>{match.Groups["Keyword"].Value}</color>";
        
        if (match.Groups["Builtin"].Success)
            return $"<color={ColorBuiltin}>{match.Groups["Builtin"].Value}</color>";
        
        if (match.Groups["Number"].Success)
            return $"<color={ColorNumber}>{match.Groups["Number"].Value}</color>";
        
        return match.Value;
    }
}
