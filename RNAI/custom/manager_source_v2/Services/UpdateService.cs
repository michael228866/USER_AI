using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;

namespace HomunculusManager.Services;

internal static class UpdateService
{
    internal const string DownloadPage = "https://github.com/michael228866/USER_AI/releases/latest";
    private const string ApiUrl = "https://api.github.com/repos/michael228866/USER_AI/releases/latest";
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(8) };

    internal static string CurrentRevision =>
        typeof(UpdateService).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion.Split('+').LastOrDefault() ?? "local";

    internal static bool HasUpdate(string tag, string revision, bool hasPackage) =>
        hasPackage && tag.StartsWith("build-", StringComparison.Ordinal) &&
        tag.Length == 46 && tag.AsSpan(6).ToString().All(Uri.IsHexDigit) &&
        !tag.AsSpan(6).Equals(revision.AsSpan(), StringComparison.OrdinalIgnoreCase);

    internal static async Task<string?> CheckAsync()
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
        var hasPackage = root.GetProperty("assets").EnumerateArray().Any(asset =>
            asset.GetProperty("name").GetString() == "USER_AI_update.zip");
        return HasUpdate(tag, CurrentRevision, hasPackage) ? tag[6..13] : null;
    }
}
