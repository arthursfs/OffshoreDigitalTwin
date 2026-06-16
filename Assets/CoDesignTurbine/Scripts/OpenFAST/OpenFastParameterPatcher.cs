using System;
using System.Collections.Generic;
using System.IO;

namespace CoDesignTurbine.OpenFAST
{
    [Serializable]
    public class OpenFastParameterEdit
    {
        public string relativeFilePath;
        public string parameterName;
        public string value;
    }

    [Serializable]
    public class OpenFastParameterSweep
    {
        public string relativeFilePath;
        public string parameterName;
        public string[] values;
    }

    public static class OpenFastParameterPatcher
    {
        private static readonly char[] Separators = { ' ', '\t' };

        public static void ApplyEdit(string caseRoot, OpenFastParameterEdit edit)
        {
            if (edit == null || string.IsNullOrWhiteSpace(edit.relativeFilePath) || string.IsNullOrWhiteSpace(edit.parameterName))
            {
                return;
            }

            string path = Path.Combine(caseRoot, OpenFastCaseConfig.NormalizeRelativePath(edit.relativeFilePath));
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("OpenFAST parameter edit target was not found.", path);
            }

            string[] lines = File.ReadAllLines(path);
            bool replaced = false;
            for (int i = 0; i < lines.Length; i++)
            {
                string replacement;
                if (TryPatchLine(lines[i], edit.parameterName, edit.value, out replacement))
                {
                    lines[i] = replacement;
                    replaced = true;
                    break;
                }
            }

            if (!replaced)
            {
                throw new InvalidDataException("Could not find parameter '" + edit.parameterName + "' in " + path);
            }

            File.WriteAllLines(path, lines);
        }

        public static List<OpenFastParameterEdit> BuildEdits(IList<OpenFastParameterSweep> sweeps, int[] indices)
        {
            List<OpenFastParameterEdit> edits = new List<OpenFastParameterEdit>();
            if (sweeps == null || indices == null)
            {
                return edits;
            }

            for (int i = 0; i < sweeps.Count && i < indices.Length; i++)
            {
                OpenFastParameterSweep sweep = sweeps[i];
                if (sweep == null || sweep.values == null || sweep.values.Length == 0)
                {
                    continue;
                }

                int valueIndex = Math.Max(0, Math.Min(indices[i], sweep.values.Length - 1));
                edits.Add(new OpenFastParameterEdit
                {
                    relativeFilePath = sweep.relativeFilePath,
                    parameterName = sweep.parameterName,
                    value = sweep.values[valueIndex]
                });
            }

            return edits;
        }

        public static string DescribeEdits(IList<OpenFastParameterEdit> edits)
        {
            if (edits == null || edits.Count == 0)
            {
                return "base";
            }

            List<string> pieces = new List<string>();
            for (int i = 0; i < edits.Count; i++)
            {
                OpenFastParameterEdit edit = edits[i];
                if (edit == null)
                {
                    continue;
                }

                pieces.Add(edit.parameterName + "=" + edit.value);
            }

            return string.Join("_", pieces.ToArray());
        }

        private static bool TryPatchLine(string line, string parameterName, string value, out string replacement)
        {
            replacement = line;
            string trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith("!") || trimmed.StartsWith("#"))
            {
                return false;
            }

            string[] tokens = trimmed.Split(Separators, StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Length >= 2 && string.Equals(tokens[1], parameterName, StringComparison.OrdinalIgnoreCase))
            {
                int firstTokenStart = line.IndexOf(tokens[0], StringComparison.Ordinal);
                int firstTokenEnd = firstTokenStart + tokens[0].Length;
                replacement = line.Substring(0, firstTokenStart) + value + line.Substring(firstTokenEnd);
                return true;
            }

            int equals = line.IndexOf('=');
            if (equals >= 0)
            {
                string left = line.Substring(0, equals).Trim();
                if (string.Equals(left, parameterName, StringComparison.OrdinalIgnoreCase))
                {
                    replacement = line.Substring(0, equals + 1) + " " + value;
                    return true;
                }
            }

            return false;
        }
    }
}

