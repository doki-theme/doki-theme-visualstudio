using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace doki_theme_visualstudio {
  internal enum AssetChangedStatus {
    same,
    different,
    lul_dunno
  }

  public static class LocalAssetService {
    public static async Task<bool> HasAssetChangedAsync(string localAssetPath, string remoteAssetUrl) {
      return !File.Exists(localAssetPath) ||
             await IsDifferentFromRemoteAsync(localAssetPath, remoteAssetUrl);
    }

    private static async Task<bool> IsDifferentFromRemoteAsync(string localAssetPath, string remoteAssetUrl) {
      return !HasBeenCheckedToday(localAssetPath) &&
             await GetAssetStatusAsync(localAssetPath, remoteAssetUrl) == AssetChangedStatus.different;
    }

    private static async Task<AssetChangedStatus> GetAssetStatusAsync(string localAssetPath, string remoteAssetUrl) {
      ThreadHelper.ThrowIfOnUIThread();
      return await ToolBox.RunSafelyWithResultAsync(async () => {
        var remoteChecksum = await GetRemoteChecksumAsync(remoteAssetUrl);
        var localChecksum = GetLocalCheckSum(localAssetPath);
        if (remoteChecksum?.ToLower() == localChecksum?.ToLower() && localChecksum != null) {
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

    // todo: this
    private static bool HasBeenCheckedToday(string localAssetPath) {
      return false;
    }
  }
}
