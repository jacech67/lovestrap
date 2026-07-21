using System.Reflection;
using System.Text.Json;

using Lovestrap;
using Lovestrap.Enums.FlagPresets;

static string Invoke(string methodName, params object?[] args)
{
    MethodInfo method = typeof(TextureAssetReplacementManager).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException($"Missing method {methodName}");
    return (string)(method.Invoke(null, args) ?? throw new InvalidOperationException($"{methodName} returned null"));
}

static JsonElement RulesFor(TextureMeshMode mode)
{
    using JsonDocument document = JsonDocument.Parse(Invoke("BuildProfileJson", mode));
    return document.RootElement.GetProperty("replacement_rules").Clone();
}

JsonElement blurry = RulesFor(TextureMeshMode.Blurry);
if (blurry.GetArrayLength() != 1 ||
    blurry[0].GetProperty("replace_ids")[0].GetInt64() != 7658055825 ||
    blurry[0].GetProperty("mode").GetString() != "local" ||
    !blurry[0].GetProperty("local_path").GetString()!.EndsWith("/Assets/BlurryTexture.png", StringComparison.Ordinal) ||
    blurry[0].TryGetProperty("with_id", out _))
    throw new InvalidOperationException("Blurry profile does not match supplied Fleasion config");

string blurryResource = typeof(TextureAssetReplacementManager).Assembly.GetManifestResourceNames()
    .Single(x => x.EndsWith("BlurryTexture.png", StringComparison.Ordinal));
using Stream blurryStream = typeof(TextureAssetReplacementManager).Assembly.GetManifestResourceStream(blurryResource)
    ?? throw new InvalidOperationException("Bundled blurry texture is missing");
if (blurryStream.Length == 0)
    throw new InvalidOperationException("Bundled blurry texture is empty");

JsonElement zero = RulesFor(TextureMeshMode.ZeroTextures);
if (zero.GetArrayLength() != 1 ||
    zero[0].GetProperty("replace_ids")[0].GetInt64() != 7658055825 ||
    zero[0].GetProperty("with_id").GetInt64() != 15403233827)
    throw new InvalidOperationException("ZeroTextures profile does not match supplied Fleasion config");

if (RulesFor(TextureMeshMode.Normal).GetArrayLength() != 0)
    throw new InvalidOperationException("Normal profile must not replace assets");

FastFlagManager fastFlags = new();
fastFlags.SetRtxMode(true);
if (fastFlags.GetPreset("Rendering.TextureQuality.OverrideEnabled") != "True" ||
    fastFlags.GetPreset("Rendering.TextureQuality.Level") != "3" ||
    fastFlags.GetPreset("Rendering.MSAA") != "4" ||
    fastFlags.GetPreset("Rendering.FRMQuality") != "21" ||
    fastFlags.GetPreset("Rendering.GraySky") is not null ||
    fastFlags.GetPreset("Rendering.PauseVoxelizer") is not null ||
    fastFlags.GetPreset("Rendering.DisableGrass.MaxDistance") is not null ||
    fastFlags.GetPreset("Rendering.MeshDetail.L12") is not null)
    throw new InvalidOperationException("RTX quality preset did not apply the expected maximum-quality settings");

fastFlags.SetRtxMode(false);
if (fastFlags.GetPreset("Rendering.TextureQuality.OverrideEnabled") is not null ||
    fastFlags.GetPreset("Rendering.TextureQuality.Level") is not null ||
    fastFlags.GetPreset("Rendering.MSAA") is not null ||
    fastFlags.GetPreset("Rendering.FRMQuality") is not null)
    throw new InvalidOperationException("RTX quality preset did not restore Roblox defaults when disabled");

fastFlags.SetValue(FastFlagManager.PresetFlags["Rendering.MeshDetail.L0"], "111");
fastFlags.SetValue(FastFlagManager.PresetFlags["Rendering.MeshDetail.L12"], "321");
fastFlags.SetValue(FastFlagManager.PresetFlags["Rendering.MeshDetail.L23"], "654");
fastFlags.SetValue(FastFlagManager.PresetFlags["Rendering.MeshDetail.L34"], "987");
fastFlags.SetValue(FastFlagManager.PresetFlags["Rendering.MSAA"], "4");
fastFlags.SetValue(FastFlagManager.PresetFlags["Rendering.FRMQuality"], "17");
fastFlags.SetValue(FastFlagManager.PresetFlags["Rendering.ManualFullscreen"], "True");
fastFlags.SetValue(FastFlagManager.PresetFlags["Rendering.DisableScaling"], "True");
Dictionary<string, string?> previousPerformanceValues = fastFlags.CapturePerformanceOptimizerSettings();
fastFlags.SetPerformanceOptimizer(true);

if (fastFlags.GetPreset("Rendering.MeshDetail.L0") != "111" ||
    fastFlags.GetPreset("Rendering.MeshDetail.L12") != "321" ||
    fastFlags.GetPreset("Rendering.MeshDetail.L23") != "654" ||
    fastFlags.GetPreset("Rendering.MeshDetail.L34") != "987" ||
    fastFlags.GetPreset("Rendering.MSAA") != "1" ||
    fastFlags.GetPreset("Rendering.TextureQuality.Level") != "0" ||
    fastFlags.GetPreset("Rendering.FRMQuality") != "17" ||
    fastFlags.GetPreset("Rendering.ManualFullscreen") != "False" ||
    fastFlags.GetPreset("Rendering.DisableScaling") is not null ||
    fastFlags.GetPreset("Rendering.GraySky") != "True" ||
    fastFlags.GetPreset("Rendering.PauseVoxelizer") != "True" ||
    fastFlags.GetPreset("Rendering.DisableGrass.MaxDistance") != "0" ||
    fastFlags.GetPreset("Rendering.DisableGrass.MinDistance") != "0" ||
    fastFlags.GetPreset("Rendering.GrassMovement") != "0")
    throw new InvalidOperationException("Performance Optimizer changed render distance or missed an FPS setting");

fastFlags.SetPerformanceOptimizer(false, previousPerformanceValues);
if (fastFlags.GetPreset("Rendering.MeshDetail.L0") != "111" ||
    fastFlags.GetPreset("Rendering.MeshDetail.L12") != "321" ||
    fastFlags.GetPreset("Rendering.MeshDetail.L23") != "654" ||
    fastFlags.GetPreset("Rendering.MeshDetail.L34") != "987" ||
    fastFlags.GetPreset("Rendering.MSAA") != "4" ||
    fastFlags.GetPreset("Rendering.ManualFullscreen") != "True" ||
    fastFlags.GetPreset("Rendering.DisableScaling") != "True" ||
    fastFlags.GetPreset("Rendering.GraySky") is not null ||
    fastFlags.GetPreset("Rendering.FRMQuality") != "17" ||
    fastFlags.GetPreset("Rendering.PauseVoxelizer") is not null)
    throw new InvalidOperationException("Performance Optimizer did not restore previous values");

var defaultSettings = new Lovestrap.Models.Persistable.Settings();
if (!defaultSettings.RobloxUpgradesEnabled ||
    defaultSettings.UseStaticRobloxVersionDirectory ||
    defaultSettings.CloseRobloxCrashHandler ||
    defaultSettings.PerformanceOptimizer ||
    defaultSettings.PerformanceOptimizerFrameCap != 240 ||
    defaultSettings.RobloxChannel != "production")
    throw new InvalidOperationException("Roblox deployment settings do not have safe defaults");

string lockedDirectory = Path.Combine(Path.GetTempPath(), $"LovestrapUpgradeSmoke-{Guid.NewGuid():N}");
Directory.CreateDirectory(Path.Combine(lockedDirectory, "ssl"));
string lockedFile = Path.Combine(lockedDirectory, "ssl", "cacert.pem");
File.WriteAllText(lockedFile, "test certificate");
File.SetAttributes(lockedFile, File.GetAttributes(lockedFile) | FileAttributes.ReadOnly);

Type filesystemType = typeof(TextureAssetReplacementManager).Assembly.GetType("Lovestrap.Utility.Filesystem")
    ?? throw new InvalidOperationException("Filesystem helper type is missing");
MethodInfo deleteDirectory = filesystemType.GetMethod("DeleteDirectory", BindingFlags.NonPublic | BindingFlags.Static)
    ?? throw new InvalidOperationException("Read-only-safe directory deletion helper is missing");
deleteDirectory.Invoke(null, new object[] { lockedDirectory });

if (Directory.Exists(lockedDirectory))
    throw new InvalidOperationException("Read-only Roblox version directory was not deleted");

string merged = Invoke("MergeSettingsJson", "{\"theme\":\"Dark\",\"enabled_configs\":[\"Personal\"]}");
using JsonDocument settings = JsonDocument.Parse(merged);
JsonElement root = settings.RootElement;
if (root.GetProperty("theme").GetString() != "Dark" ||
    root.GetProperty("clear_cache_on_launch").GetBoolean() ||
    root.GetProperty("auto_delete_cache_on_exit").GetBoolean() ||
    !root.GetProperty("enabled_configs").EnumerateArray().Any(x => x.GetString() == "Lovestrap Texture Preset"))
    throw new InvalidOperationException("Fleasion settings merge failed");

Console.WriteLine("Texture, RTX, performance and Roblox upgrade smoke tests passed.");
