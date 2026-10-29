using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;

namespace HomunculusManager.Services;

internal sealed record UpdateInfo(string Revision, string PackageUrl);

internal static class UpdateService
{
    internal const string DownloadPage = "https://github.com/michael228866/USER_AI/releases/latest";
    private const string ApiUrl = "https://api.github.com/repos/michael228866/USER_AI/releases/latest";
    private const string PackageName = "USER_AI_update.zip";
    private const string OldSuffix = ".old";
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(8) };
    private static readonly HttpClient DownloadClient = new() { Timeout = TimeSpan.FromMinutes(10) };

    internal static string CurrentRevision =>
        typeof(UpdateService).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion.Split('+').LastOrDefault() ?? "local";

    internal static bool HasUpdate(string tag, string revision, bool hasPackage) =>
        hasPackage && tag.StartsWith("build-", StringComparison.Ordinal) &&
        tag.Length == 46 && tag.AsSpan(6).ToString().All(Uri.IsHexDigit) &&
        !tag.AsSpan(6).Equals(revision.AsSpan(), StringComparison.OrdinalIgnoreCase);

    internal static async Task<UpdateInfo?> CheckAsync()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, ApiUrl);
        request.Headers.UserAgent.ParseAdd("USER_AI-Manager/1.0");
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        using var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var json = await JsonDocument.ParseAsync(stream);
        var root = json.RootElement;
        var tag = root.GetProperty("tag_name").GetString() ?? "";
        var package = root.GetProperty("assets").EnumerateArray()
            .FirstOrDefault(asset => asset.GetProperty("name").GetString() == PackageName);
        var url = package.ValueKind == JsonValueKind.Object
            ? package.GetProperty("browser_download_url").GetString() : null;
        return HasUpdate(tag, CurrentRevision, url is not null) ? new UpdateInfo(tag[6..13], url!) : null;
    }

    // Downloads the update package and copies it over the install folder. The running
    // manager cannot be overwritten, so it is renamed aside and replaced; returns the
    // path of the new manager to restart into.
    internal static async Task<string> InstallAsync(UpdateInfo update, string installDir, IProgress<(long Read, long? Total)>? progress = null)
    {
        var work = Path.Combine(Path.GetTempPath(), "USER_AI_update_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(work);
        try
        {
            var zip = Path.Combine(work, PackageName);
            using (var request = new HttpRequestMessage(HttpMethod.Get, update.PackageUrl))
            {
                request.Headers.UserAgent.ParseAdd("USER_AI-Manager/1.0");
                using var response = await DownloadClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                await using var source = await response.Content.ReadAsStreamAsync();
                await using var target = File.Create(zip);
                var total = response.Content.Headers.ContentLength;
                var buffer = new byte[81920];
                long read = 0;
                int count;
                while ((count = await source.ReadAsync(buffer)) > 0)
                {
                    await target.WriteAsync(buffer.AsMemory(0, count));
                    read += count;
                    progress?.Report((read, total));
                }
            }
            var extracted = Path.Combine(work, "extracted");
            ZipFile.ExtractToDirectory(zip, extracted);
            var packageRoot = Path.Combine(extracted, "USER_AI");
            if (!Directory.Exists(packageRoot)) throw new InvalidDataException("更新包格式不正確：缺少 USER_AI 資料夾");
            return Apply(packageRoot, installDir, Environment.ProcessPath);
        }
        finally
        {
            try { Directory.Delete(work, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }

    internal static string Apply(string packageRoot, string installDir, string? runningExe)
    {
        var root = Path.GetFullPath(installDir).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var files = Directory.GetFiles(packageRoot, "*", SearchOption.AllDirectories)
            .Select(file => (Source: file, Target: Path.GetFullPath(Path.Combine(root, Path.GetRelativePath(packageRoot, file)))))
            .ToList();
        if (files.Any(file => !file.Target.StartsWith(root, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidDataException("更新包含有安裝資料夾以外的路徑");
        var exe = files.FirstOrDefault(file => file.Target.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));

        foreach (var file in files.Where(file => file != exe))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file.Target)!);
            File.Copy(file.Source, file.Target, true);
        }
        if (exe.Source is null) return runningExe ?? "";

        var old = exe.Target + OldSuffix;
        var replacingSelf = File.Exists(exe.Target);
        if (replacingSelf) File.Move(exe.Target, old, true);
        try { File.Copy(exe.Source, exe.Target, true); }
        catch
        {
            if (replacingSelf) File.Move(old, exe.Target, true);
            throw;
        }
        return exe.Target;
    }

    // The previous manager is left behind as *.old while it is still running; remove it on the next start.
    internal static void CleanupOldVersion()
    {
        try
        {
            foreach (var old in Directory.GetFiles(AppContext.BaseDirectory, "*.exe" + OldSuffix))
                File.Delete(old);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    internal static void Restart(string exe) =>
        Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(exe) });
}
