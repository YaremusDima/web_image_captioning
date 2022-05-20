using System.Diagnostics;

namespace Worker;

public static class Runner
{
    public class Result
    {
        public string Output { get; init; } = null!;
        public string Error { get; init; } = null!;
    }

    public static Result GetCaption(string imageFilePath)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "c:/Users/yarem/AppData/Local/Programs/Python/Python310/python.exe",
                Arguments = $"../model/generate.py --path_jpg {imageFilePath}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return new Result { Output = output, Error = error };
    }
}