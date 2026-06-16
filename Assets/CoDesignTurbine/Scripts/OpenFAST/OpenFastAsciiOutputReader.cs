using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace CoDesignTurbine.OpenFAST
{
    public static class OpenFastAsciiOutputReader
    {
        private static readonly char[] Separators = { ' ', '\t', ',' };

        public static OpenFastOutputSeries Read(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("OpenFAST output path is empty.");
            }

            if (!File.Exists(path))
            {
                throw new FileNotFoundException("OpenFAST output file was not found.", path);
            }

            string[] lines = File.ReadAllLines(path);
            int headerIndex = FindHeaderLine(lines);
            if (headerIndex < 0)
            {
                throw new InvalidDataException("Could not find an OpenFAST ASCII output header line beginning with Time.");
            }

            string[] headers = Split(lines[headerIndex]);
            string[] units = headerIndex + 1 < lines.Length ? Split(lines[headerIndex + 1]) : new string[headers.Length];
            List<float>[] columns = new List<float>[headers.Length];
            for (int i = 0; i < columns.Length; i++)
            {
                columns[i] = new List<float>();
            }

            for (int lineIndex = headerIndex + 2; lineIndex < lines.Length; lineIndex++)
            {
                string[] tokens = Split(lines[lineIndex]);
                if (tokens.Length < headers.Length)
                {
                    continue;
                }

                float firstValue;
                if (!TryParseFloat(tokens[0], out firstValue))
                {
                    continue;
                }

                for (int col = 0; col < headers.Length; col++)
                {
                    float value;
                    columns[col].Add(TryParseFloat(tokens[col], out value) ? value : float.NaN);
                }
            }

            OpenFastOutputSeries series = new OpenFastOutputSeries
            {
                sourcePath = path,
                channelNames = headers,
                units = units,
                timeSeconds = columns.Length > 0 ? columns[0].ToArray() : new float[0]
            };

            for (int col = 0; col < headers.Length; col++)
            {
                series.AddChannel(headers[col], columns[col].ToArray());
            }

            return series;
        }

        private static int FindHeaderLine(string[] lines)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                string[] tokens = Split(lines[i]);
                if (tokens.Length > 0 && string.Equals(tokens[0], "Time", StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        private static string[] Split(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return new string[0];
            }

            return line.Trim().Split(Separators, StringSplitOptions.RemoveEmptyEntries);
        }

        private static bool TryParseFloat(string token, out float value)
        {
            token = token.Trim().Replace('D', 'E').Replace('d', 'e');
            return float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
    }
}

