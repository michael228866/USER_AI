using System.Text.Json;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;

namespace HomunculusManager.Services;

internal sealed record WindowPlacementState(double Left, double Top, double Width, double Height);

internal static class WindowPlacementService
{
    private delegate bool MonitorCallback(nint monitor, nint dc, ref NativeRect rect, nint data);
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct MonitorInfo
    {
        public int Size;
        public NativeRect Monitor;
        public NativeRect Work;
        public int Flags;
    }
    [DllImport("user32.dll")] private static extern bool EnumDisplayMonitors(nint dc, nint clip, MonitorCallback callback, nint data);
    [DllImport("user32.dll", CharSet = CharSet.Auto)] private static extern bool GetMonitorInfo(nint monitor, ref MonitorInfo info);
    private static readonly string StatePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "HomunculusAiManager", "window.json");

    internal static WindowPlacementState? Load(string? path = null)
    {
        try
        {
            var source = path ?? StatePath;
            return File.Exists(source)
                ? JsonSerializer.Deserialize<WindowPlacementState>(File.ReadAllText(source)) : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }
    }

    internal static void Save(Window window)
    {
        try
        {
            var bounds = window.WindowState == WindowState.Normal ?
                new Rect(window.Left, window.Top, window.Width, window.Height) : window.RestoreBounds;
            if (!IsFinite(bounds)) return;
            Write(StatePath, new WindowPlacementState(bounds.Left, bounds.Top, bounds.Width, bounds.Height));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }

    internal static void Write(string path, WindowPlacementState state)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temp, JsonSerializer.Serialize(state));
            File.Move(temp, path, true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }

    internal static IReadOnlyList<Rect> Screens(Window window)
    {
        var transform = PresentationSource.FromVisual(window)?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
        var monitors = new List<(NativeRect Work, bool Primary)>();
        EnumDisplayMonitors(0, 0, (nint monitor, nint dc, ref NativeRect rect, nint data) =>
        {
            var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
            if (GetMonitorInfo(monitor, ref info)) monitors.Add((info.Work, (info.Flags & 1) != 0));
            return true;
        }, 0);
        var screens = monitors.OrderByDescending(s => s.Primary)
            .Select(s =>
            {
                var topLeft = transform.Transform(new Point(s.Work.Left, s.Work.Top));
                var bottomRight = transform.Transform(new Point(s.Work.Right, s.Work.Bottom));
                return new Rect(topLeft, bottomRight);
            }).Where(IsFinite).ToArray();
        return screens.Length > 0 ? screens : [SystemParameters.WorkArea];
    }

    internal static Rect Resolve(WindowPlacementState? saved, IReadOnlyList<Rect> screens,
        double minWidth = 800, double minHeight = 600, double defaultWidth = 1000, double defaultHeight = 700)
    {
        if (screens.Count == 0) throw new ArgumentException("沒有可用螢幕", nameof(screens));
        var valid = saved is not null && double.IsFinite(saved.Left) && double.IsFinite(saved.Top) &&
            double.IsFinite(saved.Width) && double.IsFinite(saved.Height) &&
            saved.Width >= minWidth && saved.Height >= minHeight;
        var requested = valid ? new Rect(saved!.Left, saved.Top, saved.Width, saved.Height) : Rect.Empty;
        var screen = valid
            ? screens.FirstOrDefault(area => Intersection(requested, area).Width >= 64 && Intersection(requested, area).Height >= 64)
            : Rect.Empty;
        if (screen.IsEmpty || screen.Width <= 0 || screen.Height <= 0) screen = screens[0];
        var width = Math.Max(minWidth, Math.Min(valid ? requested.Width : defaultWidth, screen.Width));
        var height = Math.Max(minHeight, Math.Min(valid ? requested.Height : defaultHeight, screen.Height));
        var left = valid && Intersection(requested, screen).Width >= 64
            ? Clamp(requested.Left, screen.Left, screen.Right - width)
            : screen.Left + (screen.Width - width) / 2;
        var top = valid && Intersection(requested, screen).Height >= 64
            ? Clamp(requested.Top, screen.Top, screen.Bottom - height)
            : screen.Top + (screen.Height - height) / 2;
        return new Rect(left, top, width, height);
    }

    private static Rect Intersection(Rect a, Rect b) { a.Intersect(b); return a; }
    private static double Clamp(double value, double min, double max) => max < min ? min : Math.Clamp(value, min, max);
    private static bool IsFinite(Rect rect) =>
        double.IsFinite(rect.Left) && double.IsFinite(rect.Top) && double.IsFinite(rect.Width) && double.IsFinite(rect.Height) &&
        rect.Width > 0 && rect.Height > 0;
}
