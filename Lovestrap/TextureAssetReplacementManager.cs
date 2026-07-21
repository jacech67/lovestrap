using System.ComponentModel;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using System.Windows;

using Lovestrap.Enums.FlagPresets;

namespace Lovestrap
{
    /// <summary>
    /// Stages Lovestrap's texture presets for the official Fleasion companion.
    /// Fleasion owns the local asset proxy; Lovestrap only writes its documented
    /// profile format, downloads the pinned official release and starts it before Roblox.
    /// </summary>
    public static class TextureAssetReplacementManager
    {
        private const string LOG_IDENT = "TextureAssetReplacementManager";
        private const string ProfileName = "Lovestrap Texture Preset";
        private const string FleasionVersion = "2.3.0";
        private const string FleasionDownloadUrl = "https://github.com/fleasion/Fleasion/releases/download/v2.3.0/Fleasion-v2.3.0-Windows.exe";
        private const string FleasionSha256 = "E16001A8D80AF46D306D1112BB0B577BC8E7900F393316AA384A082AD9AE778A";

        private static string FleasionConfigDirectory => Path.Combine(Paths.LocalAppData, "FleasionNT");
        private static string FleasionConfigsDirectory => Path.Combine(FleasionConfigDirectory, "configs");
        private static string FleasionSettingsPath => Path.Combine(FleasionConfigDirectory, "settings.json");
        private static string FleasionProfilePath => Path.Combine(FleasionConfigsDirectory, $"{ProfileName}.json");
        private static string FleasionDirectory => Path.Combine(Paths.Integrations, "Fleasion");
        private static string FleasionExecutablePath => Path.Combine(FleasionDirectory, $"Fleasion-v{FleasionVersion}-Windows.exe");
        private static string BlurryTexturePath => Path.Combine(FleasionDirectory, "Assets", "BlurryTexture.png");
        private static string AppliedModePath => Path.Combine(Paths.Base, "TextureAssetPreset.applied");

        public static async Task<bool> PrepareForLaunchAsync(TextureMeshMode mode, CancellationToken token)
        {
            // A fresh installation in Normal mode does not create or start any companion files.
            if (mode == TextureMeshMode.Normal &&
                !File.Exists(AppliedModePath) &&
                !File.Exists(FleasionProfilePath))
                return true;

            // Never touch the cache, active Fleasion profile or Roblox process while a game is open.
            // The saved choice remains queued for the next clean launch instead.
            if (IsRobloxRunning())
            {
                App.Logger.WriteLine(LOG_IDENT, $"Roblox is already running; '{mode}' remains queued for the next launch");
                return true;
            }

            try
            {
                if (mode == TextureMeshMode.Blurry)
                    ExtractBlurryTexture();

                WriteFleasionProfile(mode);
                MergeSafeFleasionSettings();

                if (mode != TextureMeshMode.Normal)
                {
                    if (!await EnsureFleasionExecutableAsync(token))
                        return false;

                    if (!await EnsureFleasionRunningAsync(token))
                        return false;
                }

                string? appliedMode = File.Exists(AppliedModePath)
                    ? File.ReadAllText(AppliedModePath).Trim()
                    : null;

                string appliedSignature = GetAppliedSignature(mode);
                if (!String.Equals(appliedMode, appliedSignature, StringComparison.Ordinal))
                {
                    DeleteRobloxAssetCache();
                    File.WriteAllText(AppliedModePath, appliedSignature);
                }

                return true;
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT, ex);
                return false;
            }
        }

        internal static string BuildProfileJson(TextureMeshMode mode)
        {
            var rules = new JsonArray();

            if (mode == TextureMeshMode.Blurry)
            {
                rules.Add(new JsonObject
                {
                    ["name"] = "blurry",
                    ["replace_ids"] = new JsonArray(7658055825),
                    ["mode"] = "local",
                    ["enabled"] = true,
                    ["local_path"] = Path.GetFullPath(BlurryTexturePath).Replace('\\', '/')
                });
            }
            else if (mode == TextureMeshMode.ZeroTextures)
            {
                rules.Add(BuildRule("No Textures", new JsonArray(7658055825), 15403233827));
            }

            return new JsonObject { ["replacement_rules"] = rules }.ToJsonString();
        }

        internal static string MergeSettingsJson(string? existingJson)
        {
            JsonObject settings;

            try
            {
                settings = String.IsNullOrWhiteSpace(existingJson)
                    ? new JsonObject()
                    : JsonNode.Parse(existingJson)?.AsObject() ?? new JsonObject();
            }
            catch (JsonException)
            {
                settings = new JsonObject();
            }

            var enabledConfigs = settings["enabled_configs"] as JsonArray ?? new JsonArray();
            if (!enabledConfigs.Any(x => String.Equals(x?.GetValue<string>(), ProfileName, StringComparison.OrdinalIgnoreCase)))
                enabledConfigs.Add(ProfileName);

            settings["enabled_configs"] = enabledConfigs;
            settings["last_config"] = ProfileName;
            settings["proxy_features_enabled"] = true;

            // Lovestrap handles a one-time cache clear only between launches. These settings
            // prevent Fleasion from terminating Roblox or deleting cache on its own.
            settings["clear_cache_on_launch"] = false;
            settings["auto_delete_cache_on_exit"] = false;
            settings["run_on_boot"] = false;
            settings["open_dashboard_on_launch"] = false;

            return settings.ToJsonString();
        }

        private static JsonObject BuildRule(string name, JsonArray ids, long? replacementId)
        {
            var rule = new JsonObject
            {
                ["name"] = name,
                ["replace_ids"] = ids,
                ["mode"] = "id",
                ["enabled"] = true
            };

            if (replacementId.HasValue)
                rule["with_id"] = replacementId.Value;

            return rule;
        }

        private static string GetAppliedSignature(TextureMeshMode mode) => mode switch
        {
            TextureMeshMode.Blurry => "Blurry-local-v1",
            TextureMeshMode.ZeroTextures => "ZeroTextures-id-v1",
            _ => "Normal-v1"
        };

        private static void WriteFleasionProfile(TextureMeshMode mode)
        {
            Directory.CreateDirectory(FleasionConfigsDirectory);
            File.WriteAllText(FleasionProfilePath, BuildProfileJson(mode));
            App.Logger.WriteLine(LOG_IDENT, $"Prepared Fleasion profile '{ProfileName}' for mode '{mode}'");
        }

        private static void ExtractBlurryTexture()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(BlurryTexturePath)!);

            using Stream source = Resource.GetStream("BlurryTexture.png");
            using var destination = new FileStream(BlurryTexturePath, FileMode.Create, FileAccess.Write, FileShare.Read);
            source.CopyTo(destination);

            App.Logger.WriteLine(LOG_IDENT, $"Extracted built-in blurry texture to '{BlurryTexturePath}'");
        }

        private static void MergeSafeFleasionSettings()
        {
            Directory.CreateDirectory(FleasionConfigDirectory);
            string? existing = File.Exists(FleasionSettingsPath) ? File.ReadAllText(FleasionSettingsPath) : null;
            File.WriteAllText(FleasionSettingsPath, MergeSettingsJson(existing));
        }

        private static async Task<bool> EnsureFleasionExecutableAsync(CancellationToken token)
        {
            if (File.Exists(FleasionExecutablePath) && VerifySha256(FleasionExecutablePath))
                return true;

            Directory.CreateDirectory(FleasionDirectory);
            string temporaryPath = FleasionExecutablePath + ".download";

            try
            {
                using var response = await App.HttpClient.GetAsync(FleasionDownloadUrl, HttpCompletionOption.ResponseHeadersRead, token);
                response.EnsureSuccessStatusCode();

                await using (Stream source = await response.Content.ReadAsStreamAsync(token))
                await using (var destination = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    await source.CopyToAsync(destination, token);

                if (!VerifySha256(temporaryPath))
                {
                    File.Delete(temporaryPath);
                    App.Logger.WriteLine(LOG_IDENT, "Official Fleasion download failed SHA-256 verification");
                    return false;
                }

                File.Move(temporaryPath, FleasionExecutablePath, true);
                return true;
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException or OperationCanceledException)
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);

                App.Logger.WriteException(LOG_IDENT, ex);
                return false;
            }
        }

        private static bool VerifySha256(string path)
        {
            using var stream = File.OpenRead(path);
            using var sha256 = SHA256.Create();
            string actual = Convert.ToHexString(sha256.ComputeHash(stream));
            return String.Equals(actual, FleasionSha256, StringComparison.OrdinalIgnoreCase);
        }

        private static async Task<bool> EnsureFleasionRunningAsync(CancellationToken token)
        {
            if (!IsFleasionRunning())
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = FleasionExecutablePath,
                        WorkingDirectory = FleasionDirectory,
                        UseShellExecute = true,
                        Verb = "runas"
                    });
                }
                catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
                {
                    App.Logger.WriteLine(LOG_IDENT, "Fleasion administrator prompt was cancelled");
                    return false;
                }
            }

            // Do not start Roblox until the local companion is actually listening.
            DateTime deadline = DateTime.UtcNow.AddSeconds(30);
            while (DateTime.UtcNow < deadline && !token.IsCancellationRequested)
            {
                if (await IsLocalProxyReadyAsync())
                    return true;

                await Task.Delay(500, token);
            }

            App.Logger.WriteLine(LOG_IDENT, "Fleasion did not become ready within 30 seconds");
            return false;
        }

        private static async Task<bool> IsLocalProxyReadyAsync()
        {
            try
            {
                using var client = new TcpClient();
                using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
                await client.ConnectAsync(IPAddress.Loopback, 443, timeout.Token);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsFleasionRunning() => Process.GetProcesses()
            .Any(x => x.ProcessName.StartsWith("Fleasion", StringComparison.OrdinalIgnoreCase));

        private static bool IsRobloxRunning() => Process.GetProcessesByName("RobloxPlayerBeta").Any();

        private static void DeleteRobloxAssetCache()
        {
            if (IsRobloxRunning())
                return;

            foreach (string path in new[]
            {
                Path.Combine(Paths.LocalAppData, "Roblox", "rbx-storage.db"),
                Path.Combine(Paths.LocalAppData, "RobloxPCGDK", "rbx-storage.db")
            })
            {
                if (!File.Exists(path))
                    continue;

                File.Delete(path);
                App.Logger.WriteLine(LOG_IDENT, $"Cleared inactive Roblox asset cache '{path}'");
            }
        }
    }
}
