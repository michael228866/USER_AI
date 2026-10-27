using System.Globalization;
using System.Windows;
using HomunculusManager.Services;

namespace HomunculusManager;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            var localConfig = Path.Combine(AppContext.BaseDirectory, "homunculus_config.lua");
            var nestedConfig = Path.Combine(AppContext.BaseDirectory, "RNAI", "custom", "homunculus_config.lua");
            var config = args.Length > 1 && args[0] is "--check" or "--self-test" or "--ui-smoke"
                ? args[1] : args.Length > 6 && args[0] == "--preview"
                    ? args[6] : File.Exists(localConfig) ? localConfig : nestedConfig;
            if (args.Length > 0 && args[0] == "--check")
            {
                LuaConfigService.Check(config);
                Console.WriteLine("PASS: nine homunculus profiles readable");
                return 0;
            }
            if (args.Length > 0 && args[0] == "--self-test")
            {
                SelfTests.Run(config);
                return 0;
            }
            if (args.Length == 3 && args[0] == "--export-map")
            {
                Documentation.ExportMap(args[1], args[2]);
                return 0;
            }
            var app = new Application { ShutdownMode = ShutdownMode.OnMainWindowClose };
            var window = new MainWindow(config, Path.Combine(Path.GetDirectoryName(config)!, "manager_profiles.json"));
            if (args.Length > 0 && args[0] is "--ui-smoke" or "--preview")
                window.DisablePlacementPersistence();
            if (args.Length > 0 && args[0] == "--ui-smoke")
            {
                Console.WriteLine($"PASS {window.SmokeTest()} UI checks");
                return 0;
            }
            if (args.Length > 1 && args[0] == "--preview")
            {
                if (args.Length > 5) window.SelectPageForPreview(args[5]);
                Preview.Save(window, args[1],
                    args.Length > 2 ? double.Parse(args[2], CultureInfo.InvariantCulture) : 1,
                    args.Length > 3 ? double.Parse(args[3], CultureInfo.InvariantCulture) : 1000,
                    args.Length > 4 ? double.Parse(args[4], CultureInfo.InvariantCulture) : 700);
                return 0;
            }
            app.Run(window);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            if (args.Length == 0) MessageBox.Show(ex.Message, "生命體 AI 管理器", MessageBoxButton.OK, MessageBoxImage.Error);
            return 1;
        }
    }
}
