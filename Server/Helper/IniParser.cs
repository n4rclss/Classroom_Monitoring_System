using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Helper
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    internal class IniParser
    {
        private Dictionary<string, Dictionary<string, string>> data;

        public IniParser(string filePath)
        {
            data = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            Load(filePath);
        }

        private void Load(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("INI file not found: " + filePath);

            string currentSection = "";

            foreach (string rawLine in File.ReadAllLines(filePath))
            {
                string line = rawLine.Trim();

                if (string.IsNullOrEmpty(line) || line.StartsWith(";") || line.StartsWith("#"))
                    continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    currentSection = line.Substring(1, line.Length - 2).Trim();

                    if (!data.ContainsKey(currentSection))
                        data[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }
                else if (line.Contains("="))
                {
                    int idx = line.IndexOf('=');
                    string key = line.Substring(0, idx).Trim();
                    string value = line.Substring(idx + 1).Trim();

                    if (!data.ContainsKey(currentSection))
                        data[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                    data[currentSection][key] = value;
                }
            }
        }

        public string Get(string section, string key)
        {
            if (data.ContainsKey(section) && data[section].ContainsKey(key))
                return data[section][key];

            throw new KeyNotFoundException($"Key '{key}' not found in section '{section}'.");
        }
    }

}
