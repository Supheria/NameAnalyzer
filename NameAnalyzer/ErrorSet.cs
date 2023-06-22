using System.Collections.Generic;
using System.Text;

namespace NameAnalyzer;

public class ErrorSet
{
    public bool HasNewToShow { get; set; }
    private readonly HashSet<string> _errors;

    public ErrorSet()
    {
        HasNewToShow = false;
        _errors = new();
    }

    public string Append(string message)
    {
        var number = _errors.Count;
        _errors.Add(message);
        if (number != _errors.Count)
            HasNewToShow = true;
        return ToString();
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        foreach (var error in _errors)
            sb.AppendLine(error);
        return sb.ToString();
    }
}