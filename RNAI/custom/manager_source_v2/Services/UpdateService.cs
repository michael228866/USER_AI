using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;

namespace HomunculusManager.Services;

internal sealed record UpdateInfo(string Commit, string PackageUrl, string? LuaPackageUrl)
{
    internal string Revision => Commit[..7];
}

internal static class UpdateService
{
    internal const string DownloadPage = "https://github.com/michael228866/USER_AI/releases/latest";
    private const string ApiUrl = "https://api.github.com/repos/michael228866/USER_AI/releases/latest";
    private const string CompareUrl = "https://api.github.com/repos/michael228866/USER_AI/compare/";
    private const string PackageName = "USER_AI_update.zip";
    private const string LuaPackageName = "USER_AI_lua.zip";
    // Records the installed build, since a Lua-only update leaves the older manager exe in place.
    private const string RevisionFile = "installed_revision.txt";
    private const string OldSuffix = ".old";
    // Changes under these paths need a new manager exe; anything else ships in the Lua package.
    private static readonly string[] ManagerPaths = ["RNAI/custom/manager_source_v2/", ".github/"];
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(8) };
    private static readonly HttpClient DownloadClient = new() { Timeout = TimeSpan.FromMinutes(10) };

    private static string BuildRevision =>
        typeof(UpdateService).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion.Split('+').LastOrDefault() ?? "local";

    internal static string CurrentRevision => ReadRevision(AppContext.BaseDirectory, BuildRevision);

    internal static string ReadRevision(string installDir, string fallback)
    {
        try
        {
            var path = Path.Combine(installDir, RevisionFile);
            var text = File.Exists(path) ? File.ReadAllText(path).Trim() : "";
            return IsCommit(text) ? text : fallback;
        }
        catch (IOException) { return fallback; }
        catch (UnauthorizedAccessException) { return fallback; }
    }

    private static bool IsCommit(string text) => text.Length == 40 && text.All(Uri.IsHexDigit);

    internal static bool HasUpdate(string tag, string revision, bool hasPackage) =>
        hasPackage && tag.StartsWith("build-", StringComparison.Ordinal) &&
        tag.Length == 46 && tag.AsSpan(6).ToString().All(Uri.IsHexDigit) &&
        !tag.AsSpan(6).Equals(revision.AsSpan(), StringComparison.OrdinalIgnoreCase);

    internal static bool TouchesManager(IEnumerable<string> changedFiles) =>
        changedFiles.Any(file => ManagerPaths.Any(prefix => file.StartsWith(prefix, StringComparison.Ordinal)));

    internal static async Task<UpdateInfo?> CheckAsync()
    {
        using var json = await GetJsonAsync(ApiUrl);
        if (json is null) return null;
        var root = json.RootElement;
        var tag = root.GetProperty("tag_name").GetString() ?? "";
        string? AssetUrl(string name)
        {
            var asset = root.GetProperty("assets").EnumerateArray()
                .FirstOrDefault(asset => asset.GetProperty("name").GetString() == name);
            return asset.ValueKind == JsonValueKind.Object ? asset.GetProperty("browser_download_url").GetString() : null;
        }
        var url = AssetUrl(PackageName);
        return HasUpdate(tag, CurrentRevision, url is not null) ? new UpdateInfo(tag[6..], url!, AssetUrl(LuaPackageName)) : null;
    }

    // True only when GitHub confirms no manager source changed between the installed and the new build.
    private static async Task<bool> CanSkipManagerAsync(UpdateInfo update)
    {
        var installed = CurrentRevision;
        if (update.LuaPackageUrl is null || !IsCommit(installed)) return false;
        try
        {
            using var json = await GetJsonAsync(CompareUrl + installed + "..." + update.Commit);
            if (json is null) return false;
            var root = json.RootElement;
            if (root.GetProperty("status").GetString() != "ahead") return false;
            var files = root.GetProperty("files").EnumerateArray()
                .Select(file => file.GetProperty("filename").GetString() ?? "").ToList();
            // The compare API lists at most 300 files; a full list is needed to rule out manager changes.
            return files.Count < 300 && !TouchesManager(files);
        }
        catch (Exception) { return false; }
    }

    private static async Task<JsonDocument?> GetJsonAsync(string url)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.ParseAdd("USER_AI-Manager/1.0");
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        using var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }

    // Downloads the update and copies it over the install folder. When the manager has not changed
    // only the Lua package is fetched and null is returned (no restart needed). Otherwise the running
    // manager, which cannot be overwritten, is renamed aside and replaced; returns the new manager's path.
    internal static async Task<string?> InstallAsync(UpdateInfo update, string installDir, IProgress<(long Read, long? Total)>? progress = null)
    {
        var luaOnly = await CanSkipManagerAsync(update);
        var work = Path.Combine(Path.GetTempPath(), "USER_AI_update_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(work);
        try
        {
            var zip = Path.Combine(work, "package.zip");
            using (var request = new HttpRequestMessage(HttpMethod.Get, luaOnly ? update.LuaPackageUrl : update.PackageUrl))
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
            var exe = Apply(packageRoot, installDir);
            File.WriteAllText(Path.Combine(installDir, RevisionFile), update.Commit);
            return exe;
        }
        finally
        {
            try { Directory.Delete(work, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }

    // Returns the replaced manager's path, or null when the package carries no manager.
    internal static string? Apply(string packageRoot, string installDir)
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
        if (exe.Source is null) return null;

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
