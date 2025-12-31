using System.Diagnostics;

namespace SpeakStoreLocate.ApiService.Utilities;

public static class FfmpegAudioTranscoder
{
    public static async Task<byte[]> TranscodeToWavPcm16kMonoAsync(byte[] inputAudioBytes, CancellationToken cancellationToken = default)
    {
        if (inputAudioBytes == null) throw new ArgumentNullException(nameof(inputAudioBytes));

        // We use temp files to keep implementation simple and reliable across platforms.
        var tempDir = Path.Combine(Path.GetTempPath(), "speakstorelocate-audio", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var inputPath = Path.Combine(tempDir, "input");
        var outputPath = Path.Combine(tempDir, "output.wav");

        try
        {
            await File.WriteAllBytesAsync(inputPath, inputAudioBytes, cancellationToken);

            // Normalize to mono 16kHz PCM (s16le) WAV.
            // -hide_banner / -loglevel error keeps logs clean; errors are captured via stderr.
            // -y overwrites output if it exists.
            var args = $"-hide_banner -loglevel error -y -i \"{inputPath}\" -ac 1 -ar 16000 -c:a pcm_s16le -f wav \"{outputPath}\"";

            var (exitCode, stderr) = await RunProcessAsync("ffmpeg", args, cancellationToken);
            if (exitCode != 0)
            {
                throw new InvalidOperationException($"ffmpeg failed (exit code {exitCode}). {Truncate(stderr, 2000)}");
            }

            if (!File.Exists(outputPath))
            {
                throw new InvalidOperationException("ffmpeg did not produce an output file.");
            }

            return await File.ReadAllBytesAsync(outputPath, cancellationToken);
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            }
            catch
            {
                // best-effort cleanup
            }
        }
    }

    private static async Task<(int ExitCode, string Stderr)> RunProcessAsync(string fileName, string arguments, CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = new Process { StartInfo = psi };

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to start process '{fileName}'. Is it installed and on PATH?", ex);
        }

        var stderrTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync(cancellationToken);
        var stderr = await stderrTask;

        return (process.ExitCode, stderr ?? string.Empty);
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Length <= maxLength) return value;
        return value.Substring(0, maxLength) + "…(truncated)";
    }
}
