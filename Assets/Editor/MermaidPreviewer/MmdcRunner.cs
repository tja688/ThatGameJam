using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace MermaidPreviewer
{
    internal sealed class MmdcRunner
    {
        private const string TempFolderName = "UnityMermaidPreview";

        public bool TryRender(string mermaidSource, out Texture2D texture, out string errorMessage)
        {
            texture = null;
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(mermaidSource))
            {
                errorMessage = "Mermaid source is empty.";
                return false;
            }

            var tempDir = Path.Combine(Path.GetTempPath(), TempFolderName);
            Directory.CreateDirectory(tempDir);

            var inputPath = Path.Combine(tempDir, $"input_{Guid.NewGuid():N}.mmd");
            var outputPath = Path.Combine(tempDir, $"output_{Guid.NewGuid():N}.png");

            try
            {
                File.WriteAllText(inputPath, mermaidSource, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to write temp file: {ex.Message}";
                return false;
            }

            var mmdcPath = MermaidPreviewerPrefs.MmdcPath;
            var puppeteerConfig = MermaidPreviewerPrefs.PuppeteerConfigPath;
            var args = BuildArgs(inputPath, outputPath, puppeteerConfig);

            var startInfo = new ProcessStartInfo
            {
                FileName = mmdcPath,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            try
            {
                using var process = new Process { StartInfo = startInfo };
                process.Start();

                var stdOut = process.StandardOutput.ReadToEnd();
                var stdErr = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    errorMessage = BuildProcessError(process.ExitCode, stdErr, stdOut);
                    Debug.LogError($"mmdc failed: {errorMessage}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to run mmdc: {ex.Message}";
                Debug.LogError(errorMessage);
                return false;
            }

            if (!File.Exists(outputPath))
            {
                errorMessage = "mmdc did not produce an output image.";
                Debug.LogError(errorMessage);
                return false;
            }

            try
            {
                var bytes = File.ReadAllBytes(outputPath);
                texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(bytes))
                {
                    errorMessage = "Failed to load rendered image.";
                    Debug.LogError(errorMessage);
                    UnityEngine.Object.DestroyImmediate(texture);
                    texture = null;
                    return false;
                }

                texture.name = "MermaidPreview";
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to read rendered image: {ex.Message}";
                Debug.LogError(errorMessage);
                return false;
            }
            finally
            {
                TryDeleteFile(inputPath);
                TryDeleteFile(outputPath);
            }

            return true;
        }

        private static string BuildArgs(string inputPath, string outputPath, string puppeteerConfigPath)
        {
            var builder = new StringBuilder();
            builder.Append("-i ").Append(Quote(inputPath)).Append(' ');
            builder.Append("-o ").Append(Quote(outputPath));

            if (!string.IsNullOrWhiteSpace(puppeteerConfigPath))
            {
                builder.Append(' ').Append("-p ").Append(Quote(puppeteerConfigPath));
            }

            return builder.ToString();
        }

        private static string Quote(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "\"\"";
            }

            if (value.IndexOf('"') >= 0)
            {
                value = value.Replace("\"", "\\\"");
            }

            return $"\"{value}\"";
        }

        private static string BuildProcessError(int exitCode, string stdErr, string stdOut)
        {
            if (!string.IsNullOrWhiteSpace(stdErr))
            {
                return $"mmdc exit code {exitCode}: {stdErr}";
            }

            if (!string.IsNullOrWhiteSpace(stdOut))
            {
                return $"mmdc exit code {exitCode}: {stdOut}";
            }

            return $"mmdc exit code {exitCode}.";
        }

        private static void TryDeleteFile(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // Ignore cleanup failures.
            }
        }
    }
}
