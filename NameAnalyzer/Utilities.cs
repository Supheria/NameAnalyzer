using System.Diagnostics;

namespace NameAnalyzer;

public static class Utilities
{
    public static void OpenFileOrFolder(string path)
    {
        using var process = new Process
        {
            StartInfo = new()
            {
                FileName = path,
                UseShellExecute = true
            }
        };
        _ = process.Start();
    }
}