using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace CoDesignTurbine.OpenFAST
{
    public class OpenFastRunResult
    {
        public int exitCode;
        public string runRoot;
        public string fstPath;
        public string outputPath;
        public string standardOutput;
        public string standardError;
        public OpenFastOutputSeries outputSeries;

        public bool Succeeded
        {
            get { return exitCode == 0; }
        }
    }

    public static class OpenFastRunUtility
    {
        public static OpenFastRunResult Run(OpenFastCaseConfig config, string runLabel, IList<OpenFastParameterEdit> edits, bool loadOutput)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            string sourceRoot = config.ResolveCaseRoot();
            if (!Directory.Exists(sourceRoot))
            {
                throw new DirectoryNotFoundException("OpenFAST case root was not found: " + sourceRoot);
            }

            string runRoot = sourceRoot;
            if (config.copyCaseBeforeRun)
            {
                string label = string.IsNullOrWhiteSpace(runLabel) ? config.runLabel : runLabel;
                if (config.timestampRunFolders)
                {
                    label += "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                }

                runRoot = Path.Combine(config.ResolveGeneratedRunsRoot(), SanitizeFileName(label));
                CopyDirectory(sourceRoot, runRoot);
            }

            if (edits != null)
            {
                for (int i = 0; i < edits.Count; i++)
                {
                    OpenFastParameterPatcher.ApplyEdit(runRoot, edits[i]);
                }
            }

            string fstPath = config.ResolveFstPath(runRoot);
            string outputPath = config.ResolveOutputPath(runRoot);
            if (!File.Exists(fstPath))
            {
                throw new FileNotFoundException("OpenFAST .fst file was not found.", fstPath);
            }

            string executable = config.ResolveExecutable();
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = Quote(fstPath),
                WorkingDirectory = Path.GetDirectoryName(fstPath),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            Stopwatch stopwatch = Stopwatch.StartNew();
            using (Process process = new Process())
            {
                StringBuilder stdout = new StringBuilder();
                StringBuilder stderr = new StringBuilder();
                process.StartInfo = startInfo;
                process.OutputDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        stdout.AppendLine(args.Data);
                    }
                };
                process.ErrorDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        stderr.AppendLine(args.Data);
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
                stopwatch.Stop();

                OpenFastRunResult result = new OpenFastRunResult
                {
                    exitCode = process.ExitCode,
                    runRoot = runRoot,
                    fstPath = fstPath,
                    outputPath = outputPath,
                    standardOutput = stdout.ToString(),
                    standardError = stderr.ToString()
                };

                Debug.Log("OpenFAST finished in " + stopwatch.Elapsed.TotalSeconds.ToString("0.00") + " s with exit code " + result.exitCode + ".");

                if (loadOutput && result.Succeeded)
                {
                    if (!File.Exists(outputPath))
                    {
                        Debug.LogWarning("OpenFAST finished, but the configured output file was not found: " + outputPath);
                    }
                    else
                    {
                        result.outputSeries = OpenFastAsciiOutputReader.Read(outputPath);
                    }
                }

                return result;
            }
        }

        public static string Quote(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return "\"\"";
            }

            return "\"" + path.Replace("\"", "\\\"") + "\"";
        }

        public static string SanitizeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "run";
            }

            char[] invalid = Path.GetInvalidFileNameChars();
            string result = value;
            for (int i = 0; i < invalid.Length; i++)
            {
                result = result.Replace(invalid[i], '_');
            }

            return result.Replace(' ', '_');
        }

        private static void CopyDirectory(string sourceDirectory, string targetDirectory)
        {
            Directory.CreateDirectory(targetDirectory);

            foreach (string directory in Directory.GetDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
            {
                string relative = directory.Substring(sourceDirectory.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                Directory.CreateDirectory(Path.Combine(targetDirectory, relative));
            }

            foreach (string file in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
            {
                string relative = file.Substring(sourceDirectory.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string destination = Path.Combine(targetDirectory, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.Copy(file, destination, true);
            }
        }
    }
}
