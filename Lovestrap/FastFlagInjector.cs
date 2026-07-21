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
        private CancellationTokenSource? _debounceCancellation;
        private readonly SemaphoreSlim _refreshLock = new(1, 1);
        private readonly HashSet<string> _injectedKeys = new(StringComparer.Ordinal);
        private string _lastHash = "";

        public FastFlagInjector()
        {
            _timer = new System.Timers.Timer(RefreshInterval.TotalMilliseconds) { AutoReset = true };
            _timer.Elapsed += (_, _) => Refresh();
        }

        public void Start()
        {
            const string LOG_IDENT = "FastFlagInjector::Start";

            // The settings toggle and startup path can both call Start. Never leave duplicate
            // watchers running, since they would process and save every external edit twice.
            Stop();

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

                FileSystemEventHandler onChange = (_, _) => QueueRefresh();

                _watcher.Changed += onChange;
                _watcher.Created += onChange;
                _watcher.Deleted += onChange;
                _watcher.Renamed += (_, _) => QueueRefresh();
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }

        public void Stop()
        {
            _timer.Stop();
            _debounceCancellation?.Cancel();
            _debounceCancellation?.Dispose();
            _debounceCancellation = null;
            _watcher?.Dispose();
            _watcher = null;
        }

        private async void QueueRefresh()
        {
            _debounceCancellation?.Cancel();
            _debounceCancellation?.Dispose();
            _debounceCancellation = new CancellationTokenSource();

            try
            {
                // Editors commonly replace a file through several rapid rename/write events.
                await Task.Delay(250, _debounceCancellation.Token);
                await RefreshAsync();
            }
            catch (OperationCanceledException)
            {
                // A newer filesystem event superseded this refresh.
            }
        }

        public void Refresh() => RefreshAsync().GetAwaiter().GetResult();

        private async Task RefreshAsync()
        {
            const string LOG_IDENT = "FastFlagInjector::Refresh";

            await _refreshLock.WaitAsync();

            try
            {
                if (!File.Exists(FilePath))
                {
                    if (_injectedKeys.Count == 0)
                        return;

                    foreach (string key in _injectedKeys)
                        App.FastFlags.SetValue(key, null);

                    _injectedKeys.Clear();
                    _lastHash = "";
                    App.FastFlags.Save();
                    App.Logger.WriteLine(LOG_IDENT, "External FastFlag file was removed; cleared its injected flags");
                    return;
                }

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
                var newInjectedKeys = new HashSet<string>(StringComparer.Ordinal);

                foreach (var pair in flags)
                {
                    if (pair.Value is null)
                        continue;

                    string? value = pair.Value.ToString();

                    if (value is null)
                        continue;

                    newInjectedKeys.Add(pair.Key);

                    // only write if it's new or different - no restrictions/validation
                    if (App.FastFlags.GetValue(pair.Key) != value)
                    {
                        App.FastFlags.SetValue(pair.Key, value);
                        changes++;
                    }
                }

                // Removing a key from InjectedFlags.json removes only flags previously owned by
                // that external file; regular presets and editor entries remain untouched.
                foreach (string removedKey in _injectedKeys.Except(newInjectedKeys).ToArray())
                {
                    App.FastFlags.SetValue(removedKey, null);
                    changes++;
                }

                _injectedKeys.Clear();
                _injectedKeys.UnionWith(newInjectedKeys);

                if (changes == 0)
                    return;

                App.Logger.WriteLine(LOG_IDENT, $"Injected/refreshed {changes} flag(s)");

                App.FastFlags.Save();
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT, ex);
            }
            finally
            {
                _refreshLock.Release();
            }
        }

    }
}
