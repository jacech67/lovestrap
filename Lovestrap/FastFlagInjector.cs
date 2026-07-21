using System.Timers;

namespace Lovestrap
{
    /// <summary>
    /// Periodically merges an external "injected flags" file into the active FastFlag
    /// configuration. Every <see cref="RefreshInterval"/> the file is re-read; any newly
    /// added or changed flags are merged in, saved to the modifications folder, and pushed
    /// into any currently-deployed Roblox version folder so the next launch picks them up.
    /// </summary>
    public class FastFlagInjector
    {
        public static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(5);

        // drop-in file the user (or an external tool) can edit at any time
        public static string FilePath => Path.Combine(Paths.Modifications, "ClientSettings", "InjectedFlags.json");

        private readonly System.Timers.Timer _timer;
        private FileSystemWatcher? _watcher;
        private string _lastHash = "";

        public FastFlagInjector()
        {
            _timer = new System.Timers.Timer(RefreshInterval.TotalMilliseconds) { AutoReset = true };
            _timer.Elapsed += (_, _) => Refresh();
        }

        public void Start()
        {
            const string LOG_IDENT = "FastFlagInjector::Start";
            App.Logger.WriteLine(LOG_IDENT, $"Started, watching {FilePath} (fallback refresh every {RefreshInterval.TotalMinutes} min)");

            // run once immediately, then on the interval as a fallback
            Refresh();
            _timer.Start();

            // real-time: apply the moment the injected-flags file changes
            try
            {
                string dir = Path.GetDirectoryName(FilePath)!;
                Directory.CreateDirectory(dir);

                _watcher = new FileSystemWatcher(dir, Path.GetFileName(FilePath))
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
                    EnableRaisingEvents = true
                };

                FileSystemEventHandler onChange = (_, _) =>
                {
                    // brief debounce so we read after the writer finishes flushing
                    Thread.Sleep(150);
                    Refresh();
                };

                _watcher.Changed += onChange;
                _watcher.Created += onChange;
                _watcher.Renamed += (_, _) => { Thread.Sleep(150); Refresh(); };
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }

        public void Stop()
        {
            _timer.Stop();
            _watcher?.Dispose();
        }

        public void Refresh()
        {
            const string LOG_IDENT = "FastFlagInjector::Refresh";

            try
            {
                if (!File.Exists(FilePath))
                    return;

                string contents = File.ReadAllText(FilePath);

                // skip work if nothing changed since the last pass
                string hash = MD5Hash.FromString(contents);
                if (hash == _lastHash)
                    return;

                _lastHash = hash;

                var options = new JsonSerializerOptions
                {
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };

                var flags = JsonSerializer.Deserialize<Dictionary<string, object>>(contents, options);

                if (flags is null)
                    return;

                int changes = 0;

                foreach (var pair in flags)
                {
                    if (pair.Value is null)
                        continue;

                    string? value = pair.Value.ToString();

                    if (value is null)
                        continue;

                    // only write if it's new or different - no restrictions/validation
                    if (App.FastFlags.GetValue(pair.Key) != value)
                    {
                        App.FastFlags.SetValue(pair.Key, value);
                        changes++;
                    }
                }

                if (changes == 0)
                    return;

                App.Logger.WriteLine(LOG_IDENT, $"Injected/refreshed {changes} flag(s)");

                App.FastFlags.Save();

                PushToActiveVersion();
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }

        // copy the freshly-saved ClientAppSettings.json straight into the installed Roblox
        // version folder so an already-installed client uses the injected flags next launch
        private void PushToActiveVersion()
        {
            const string LOG_IDENT = "FastFlagInjector::PushToActiveVersion";

            try
            {
                string? guid = App.PlayerState.Prop.VersionGuid;

                if (String.IsNullOrEmpty(guid) || String.IsNullOrEmpty(Paths.Versions))
                    return;

                string source = App.FastFlags.FileLocation;
                string destDir = Path.Combine(Paths.Versions, guid, "ClientSettings");
                string dest = Path.Combine(destDir, "ClientAppSettings.json");

                if (!File.Exists(source))
                    return;

                Directory.CreateDirectory(destDir);
                Filesystem.AssertReadOnly(dest);
                File.Copy(source, dest, true);

                App.Logger.WriteLine(LOG_IDENT, $"Pushed injected flags to version {guid}");
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }
    }
}
