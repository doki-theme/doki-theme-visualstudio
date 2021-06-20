using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;

namespace doki_theme_visualstudio {
  internal enum AssetChangedStatus {
    same,
    different,
    lul_dunno
  }

  public class LocalAssetService {
    private readonly Dictionary<string, DateTime> _assetChecks;

    private static LocalAssetService? _instance;

    private LocalAssetService(Dictionary<string, DateTime> assetChecks) {
      _assetChecks = assetChecks;
    }

    public static LocalAssetService Instance =>
      _instance ?? throw new Exception("Expected local storage to be initialized!");


    public static void Init(Package package) {
      _instance ??= new LocalAssetService(ReadAssetChecks());
    }

    private static Dictionary<string, DateTime> ReadAssetChecks() {
      return ToolBox.RunSafelyWithResult(() => {
        var assetChecksFile = GetAssetsChecksFile();
        if (!File.Exists(assetChecksFile)) return new Dictionary<string, DateTime>();

        using var stream = File.OpenRead(assetChecksFile);
        using var jsonReader = new JsonTextReader(new StreamReader(stream));
        var jsonSerializer = JsonSerializer.Create();
        return jsonSerializer.Deserialize<Dictionary<string, DateTime>>(jsonReader);
      }, exception => {
        Console.Out.WriteLine("Unable to read asset checks for reasons " + exception.Message);
        return new Dictionary<string, DateTime>();
      });
    }

    private static string GetAssetsChecksFile() {
      return Path.Combine(LocalStorageService.Instance.GetAssetDirectory(), "assetChecks.json");
    }

    public async Task<bool> HasAssetChangedAsync(string localAssetPath, string remoteAssetUrl) {
      return !File.Exists(localAssetPath) ||
             await IsDifferentFromRemoteAsync(localAssetPath, remoteAssetUrl);
    }

    private async Task<bool> IsDifferentFromRemoteAsync(string localAssetPath, string remoteAssetUrl) {
      return !HasBeenCheckedToday(localAssetPath) &&
             await GetAssetStatusAsync(localAssetPath, remoteAssetUrl) == AssetChangedStatus.different;
    }

    private async Task<AssetChangedStatus> GetAssetStatusAsync(string localAssetPath, string remoteAssetUrl) {
      ThreadHelper.ThrowIfOnUIThread();
      return await ToolBox.RunSafelyWithResultAsync(async () => {
        var remoteChecksum = await GetRemoteChecksumAsync(remoteAssetUrl);
        WriteCheckedDate(localAssetPath);
        var localChecksum = GetLocalCheckSum(localAssetPath);
        if (remoteChecksum?.ToLower() == localChecksum.ToLower() && localChecksum.Trim().Length > 0) {
          return AssetChangedStatus.same;
        }

        await Console.Out.WriteLineAsync("Remote Asset different from local asset " +
                                         $"Local asset: {localAssetPath} " +
                                         $"is different from remote asset: {remoteAssetUrl} " +
                                         $"local asset checksum: {localChecksum} " +
                                         $"remote asset checksum: {remoteChecksum} ");
        return AssetChangedStatus.different;
      }, exception => {
        Console.Out.WriteLine("Unable to check asset status!", exception.Message);
        return AssetChangedStatus.lul_dunno;
      });
    }

    private void WriteCheckedDate(string localAssetPath) {
      ToolBox.RunSafely(() => {
        _assetChecks.Add(localAssetPath, DateTime.Now);
        JsonSerializer serializer = new JsonSerializer();
        serializer.NullValueHandling = NullValueHandling.Ignore;

        using StreamWriter sw = new StreamWriter(GetAssetsChecksFile());
        using JsonWriter writer = new JsonTextWriter(sw);
        serializer.Serialize(writer, _assetChecks);
      }, exception => { ActivityLog.LogWarning("Unable to save asset checks!", exception.Message); });
    }

    private static string GetLocalCheckSum(string localAssetPath) =>
      GetHash(MD5.Create(), File.OpenRead(localAssetPath));

    private static string GetHash(HashAlgorithm hashAlgorithm, Stream input) {
      byte[] md5Hash = hashAlgorithm.ComputeHash(input);
      var md5HexString = new StringBuilder();
      foreach (var t in md5Hash) {
        md5HexString.Append(t.ToString("x2"));
      }

      return md5HexString.ToString();
    }

    private static async Task<string?> GetRemoteChecksumAsync(string remoteAssetUrl) {
      using var webClient = new WebClient();
      return await webClient.DownloadStringTaskAsync(new Uri($"{remoteAssetUrl}.checksum.txt"));
    }

    private bool HasBeenCheckedToday(string localAssetPath) {
      if (!_assetChecks.ContainsKey(localAssetPath)) return false;

      var checkedDate = _assetChecks[localAssetPath];
      var meow = DateTime.Now;
      return (meow - checkedDate).TotalDays < 1.0;
    }
  }
}
