using System.Xml.Linq;

namespace Lovestrap
{
    /// <summary>
    /// Reads and writes Roblox's global in-game settings file
    /// (%LOCALAPPDATA%\Roblox\GlobalBasicSettings_*.xml, the UserGameSettings block).
    /// Each Set writes immediately; the file can be locked read-only so Roblox
    /// won't overwrite the chosen values.
    /// </summary>
    public static class RobloxGlobalSettings
    {
        private static string RobloxDir => Path.Combine(Paths.LocalAppData, "Roblox");

        public static string FilePath
        {
            get
            {
                try
                {
                    if (Directory.Exists(RobloxDir))
                    {
                        // pick the highest-numbered GlobalBasicSettings_*.xml if present
                        var existing = Directory.GetFiles(RobloxDir, "GlobalBasicSettings_*.xml")
                            .OrderByDescending(x => x)
                            .FirstOrDefault();

                        if (existing is not null)
                            return existing;
                    }
                }
                catch { }

                return Path.Combine(RobloxDir, "GlobalBasicSettings_13.xml");
            }
        }

        public static bool IsReadOnly
        {
            get
            {
                try { return File.Exists(FilePath) && File.GetAttributes(FilePath).HasFlag(FileAttributes.ReadOnly); }
                catch { return false; }
            }
        }

        public static void SetReadOnly(bool value)
        {
            try
            {
                if (!File.Exists(FilePath))
                    EnsureFile();

                var attrs = File.GetAttributes(FilePath);
                File.SetAttributes(FilePath, value ? attrs | FileAttributes.ReadOnly : attrs & ~FileAttributes.ReadOnly);
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("RobloxGlobalSettings::SetReadOnly", ex);
            }
        }

        private static XDocument Load()
        {
            try
            {
                if (File.Exists(FilePath))
                    return XDocument.Load(FilePath);
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("RobloxGlobalSettings::Load", ex);
            }

            // minimal skeleton Roblox will accept/merge
            return new XDocument(
                new XElement("roblox",
                    new XAttribute("version", 4),
                    new XElement("Item",
                        new XAttribute("class", "UserGameSettings"),
                        new XElement("Properties"))));
        }

        private static XElement? Properties(XDocument doc) =>
            doc.Root?.Elements("Item")
                .FirstOrDefault(x => (string?)x.Attribute("class") == "UserGameSettings")?
                .Element("Properties");

        private static void EnsureFile()
        {
            Directory.CreateDirectory(RobloxDir);
            if (!File.Exists(FilePath))
                Save(Load());
        }

        private static void Save(XDocument doc)
        {
            bool wasReadOnly = IsReadOnly;

            try
            {
                Directory.CreateDirectory(RobloxDir);

                if (wasReadOnly && File.Exists(FilePath))
                    File.SetAttributes(FilePath, File.GetAttributes(FilePath) & ~FileAttributes.ReadOnly);

                doc.Save(FilePath);

                if (wasReadOnly)
                    File.SetAttributes(FilePath, File.GetAttributes(FilePath) | FileAttributes.ReadOnly);
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("RobloxGlobalSettings::Save", ex);
            }
        }

        public static string? GetProperty(string name)
        {
            var props = Properties(Load());
            return props?.Elements().FirstOrDefault(x => (string?)x.Attribute("name") == name)?.Value;
        }

        /// <summary>Sets a simple scalar property (token/int/float/bool), creating it if needed.</summary>
        public static void SetProperty(string elementType, string name, string value)
        {
            var doc = Load();
            var props = Properties(doc);
            if (props is null)
                return;

            var node = props.Elements().FirstOrDefault(x => (string?)x.Attribute("name") == name);

            if (node is null)
            {
                node = new XElement(elementType, new XAttribute("name", name));
                props.Add(node);
            }

            node.Value = value;
            Save(doc);
        }

        public static void RemoveProperty(string name)
        {
            var doc = Load();
            var props = Properties(doc);
            if (props is null)
                return;

            var node = props.Elements().FirstOrDefault(x => (string?)x.Attribute("name") == name);
            if (node is null)
                return;

            node.Remove();
            Save(doc);
        }

        /// <summary>Sets a Vector2 property (used for mouse sensitivity).</summary>
        public static void SetVector2(string name, float value)
        {
            var doc = Load();
            var props = Properties(doc);
            if (props is null)
                return;

            var node = props.Elements().FirstOrDefault(x => (string?)x.Attribute("name") == name);

            if (node is null)
            {
                node = new XElement("Vector2", new XAttribute("name", name));
                props.Add(node);
            }

            node.RemoveNodes();
            node.Add(new XElement("X", value), new XElement("Y", value));
            Save(doc);
        }

        public static float? GetVector2X(string name)
        {
            var node = Properties(Load())?.Elements().FirstOrDefault(x => (string?)x.Attribute("name") == name);
            if (node?.Element("X")?.Value is string s && float.TryParse(s, out float v))
                return v;
            return null;
        }
    }
}
