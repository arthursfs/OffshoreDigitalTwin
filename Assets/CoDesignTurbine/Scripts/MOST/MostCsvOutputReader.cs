using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace CoDesignTurbine.MOST
{
    public static class MostCsvOutputReader
    {
        public static MostOutputSeries Read(string path, string timeChannel)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("MOST CSV path is empty.");
            }

            if (!File.Exists(path))
            {
                throw new FileNotFoundException("MOST CSV file was not found.", path);
            }

            string[] lines = File.ReadAllLines(path);
            if (lines.Length < 2)
            {
                throw new InvalidDataException("MOST CSV must contain a header and at least one data row.");
            }

            string[] headers = SplitCsvLine(lines[0]);
            List<float>[] columns = new List<float>[headers.Length];
            for (int i = 0; i < columns.Length; i++)
            {
                headers[i] = headers[i].Trim();
                columns[i] = new List<float>();
            }

            for (int lineIndex = 1; lineIndex < lines.Length; lineIndex++)
            {
                if (string.IsNullOrWhiteSpace(lines[lineIndex]))
                {
                    continue;
                }

                string[] tokens = SplitCsvLine(lines[lineIndex]);
                if (tokens.Length < headers.Length)
                {
                    continue;
                }

                for (int col = 0; col < headers.Length; col++)
                {
                    float value;
                    columns[col].Add(TryParseFloat(tokens[col], out value) ? value : float.NaN);
                }
            }

            MostOutputSeries series = new MostOutputSeries
            {
                sourcePath = path,
                channelNames = headers
            };

            for (int col = 0; col < headers.Length; col++)
            {
                float[] values = columns[col].ToArray();
                series.AddChannel(headers[col], values);
                if (string.Equals(headers[col], timeChannel, StringComparison.OrdinalIgnoreCase))
                {
                    series.timeSeconds = values;
                }
            }

            if (series.timeSeconds == null && headers.Length > 0)
            {
                series.timeSeconds = columns[0].ToArray();
            }

            return series;
        }

        private static string[] SplitCsvLine(string line)
        {
            return line.Split(',');
        }

        private static bool TryParseFloat(string token, out float value)
        {
            token = token.Trim().Replace('D', 'E').Replace('d', 'e');
            return float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
    }
}

