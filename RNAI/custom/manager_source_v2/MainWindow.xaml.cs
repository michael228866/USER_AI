using System.Globalization;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Threading;
using HomunculusManager.Models;
using HomunculusManager.Services;

namespace HomunculusManager;

public partial class MainWindow : Window
{
    private static readonly Brush Surface = Brush("#1F2937");
    private static readonly Brush Control = Brush("#374151");
    private static readonly Brush Primary = Brush("#8B5CF6");
    private static readonly Brush Muted = Brush("#9CA3AF");
    private static readonly Brush White = Brush("#F9FAFB");
    private static readonly Brush Warning = Brush("#F59E0B");
    private static readonly Brush Success = Brush("#10B981");
    private static readonly string[] Pages =
    ["總覽", "常用", "戰鬥", "安全", "進階", "名單", "Profiles", "技能管理"];

    private string configPath;
    private string profilesPath;
    private EditorSession session;
    private readonly List<Button> navButtons = [];
    private readonly List<Button> typeButtons = [];
    private readonly List<(WrapPanel Panel, bool ListCards)> responsiveGroups = [];
    private bool? lastHeaderStacked;
    private bool? lastSidebarNarrow;
    private TextBlock? timingPreview;
    private List<int>? friends;
    private List<int>? enemies;
    private List<int>? savedFriends;
    private List<int>? savedEnemies;
    private readonly Dictionary<int, string> profileDrafts = [];
    private readonly Dictionary<int, string> savedProfileNames = [];
    private bool persistWindowPlacement = true;
    private bool checkingUpdates;
    private UpdateInfo? availableUpdate;
    private bool restartingForUpdate;
    private bool ListsDirty => friends is not null && enemies is not null &&
        (!friends.SequenceEqual(savedFriends!) || !enemies.SequenceEqual(savedEnemies!));
    private bool ProfilesDirty => profileDrafts.Any(x => savedProfileNames.TryGetValue(x.Key, out var name) && x.Value != name);
    private string page = Pages[0];
    private string notice = "設定檔已載入。編輯後按「寫入 Lua 設定」。";

    internal MainWindow(string configPath, string profilesPath)
    {
        this.configPath = configPath;
        this.profilesPath = profilesPath;
        InitializeComponent();
        session = new EditorSession(configPath);
        SearchBox.Text = "";
        BuildNavigation();
        BuildTypes();
        RefreshHeader();
        RenderPage();
        SizeChanged += (_, _) => UpdateResponsiveLayout();
        ContentScroll.SizeChanged += (_, _) => UpdateResponsiveLayout();
        Loaded += async (_, _) =>
        {
            UpdateResponsiveLayout();
            if (persistWindowPlacement) await CheckForUpdatesAsync();
        };
        SourceInitialized += (_, _) =>
        {
            if (!persistWindowPlacement) return;
            var saved = WindowPlacementService.Load();
            if (saved is null) return;
            var bounds = WindowPlacementService.Resolve(saved, WindowPlacementService.Screens(this));
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = bounds.Left; Top = bounds.Top; Width = bounds.Width; Height = bounds.Height;
        };
        Closed += (_, _) => { if (persistWindowPlacement) WindowPlacementService.Save(this); };
        Closing += (_, e) =>
        {
            if (!restartingForUpdate && (session.IsDirty || ListsDirty || ProfilesDirty) && !Confirm("目前有未儲存修改，確定關閉嗎？")) e.Cancel = true;
        };
    }

    private static Brush Brush(string hex) => (Brush)new BrushConverter().ConvertFromString(hex)!;

    internal void DisablePlacementPersistence() => persistWindowPlacement = false;

    private async Task<bool> CheckForUpdatesAsync()
    {
        if (checkingUpdates) return false;
        checkingUpdates = true;
        UpdateButton.Content = "檢查中…";
        try
        {
            availableUpdate = await UpdateService.CheckAsync();
            UpdateButton.Content = availableUpdate is null ? "檢查更新" : "有新版可更新";
            UpdateButton.Background = availableUpdate is null ? Control : Success;
            UpdateButton.ToolTip = availableUpdate is null
                ? "目前沒有可用的更新；按此重新檢查"
                : $"GitHub 版本 {availableUpdate.Revision}；按此自動下載並更新";
            return true;
        }
        catch (Exception)
        {
            availableUpdate = null;
            UpdateButton.Content = "檢查更新";
            UpdateButton.ToolTip = "無法連線 GitHub；按此重試";
            return false;
        }
        finally { checkingUpdates = false; }
    }

    private async void UpdateButton_Click(object sender, RoutedEventArgs e)
    {
        if (checkingUpdates) return;
        if (availableUpdate is null)
        {
            var reached = await CheckForUpdatesAsync();
            if (availableUpdate is null)
            {
                MessageBox.Show(this, reached ? "目前已是最新版本。" : "無法連線 GitHub，請稍後再試。", "檢查更新",
                    MessageBoxButton.OK, reached ? MessageBoxImage.Information : MessageBoxImage.Warning);
                return;
            }
        }
        await InstallUpdateAsync(availableUpdate);
    }

    private async Task InstallUpdateAsync(UpdateInfo update)
    {
        var installDir = AppContext.BaseDirectory;
        if (!File.Exists(Path.Combine(installDir, "AI.lua")))
        {
            MessageBox.Show(this, "管理器不在 USER_AI 資料夾內，無法自動更新；將開啟下載頁。", "檢查更新", MessageBoxButton.OK, MessageBoxImage.Warning);
            try { Process.Start(new ProcessStartInfo(UpdateService.DownloadPage) { UseShellExecute = true }); } catch (Exception) { }
            return;
        }
        var dirty = session.IsDirty || ListsDirty || ProfilesDirty;
        if (!Confirm($"發現新版 {update.Revision}，要立即下載並更新嗎？\n\n" +
                     "更新會覆蓋 AI 程式檔與管理器，不會動到你的生命體設定、朋友／黑名單與自訂配置。\n" +
                     "完成後管理器會自動重新開啟，遊戲內請重新召喚生命體以套用新 AI。" +
                     (dirty ? "\n\n注意：目前有未儲存的修改，更新後會遺失。" : "")))
            return;

        checkingUpdates = true;
        UpdateButton.IsEnabled = false;
        UpdateButton.Content = "下載更新中…";
        try
        {
            var progress = new Progress<(long Read, long? Total)>(p =>
                UpdateButton.Content = p.Total is > 0
                    ? $"下載更新中 {p.Read * 100 / p.Total.Value}%（{p.Read / 1048576.0:0.0}/{p.Total.Value / 1048576.0:0.0} MB）"
                    : $"下載更新中 {p.Read / 1048576.0:0.0} MB");
            var exe = await UpdateService.InstallAsync(update, installDir, progress);
            UpdateButton.Content = "安裝更新中…";
            restartingForUpdate = true;
            UpdateService.Restart(exe);
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "自動更新失敗：" + ex.Message, "檢查更新", MessageBoxButton.OK, MessageBoxImage.Warning);
            UpdateButton.Content = "有新版可更新";
        }
        finally
        {
            checkingUpdates = false;
            UpdateButton.IsEnabled = true;
        }
    }

    private void BuildNavigation()
    {
        NavigationPanel.Children.Clear();
        navButtons.Clear();
        foreach (var name in Pages)
        {
            var button = new Button
            {
                Content = name,
                Height = 34, Margin = new Thickness(0, 0, 0, 4),
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Style = (Style)FindResource("ModernButton")
            };
            button.Click += (_, _) => { page = name; SearchBox.Text = ""; RenderPage(); RefreshNavigation(); };
            NavigationPanel.Children.Add(button);
            navButtons.Add(button);
        }
        RefreshNavigation();
    }

    private void RefreshNavigation()
    {
        for (var i = 0; i < Pages.Length; i++)
        {
            navButtons[i].Background = Pages[i] == page ? Primary : Brushes.Transparent;
            navButtons[i].Foreground = Pages[i] == page ? White : Muted;
        }
    }

    private void BuildTypes()
    {
        TypePanel.Children.Clear();
        typeButtons.Clear();
        foreach (var type in SettingsCatalog.Types)
        {
            var key = type.Key;
            var button = new Button
            {
                Height = 64, Margin = new Thickness(0, 0, 4, 0), Padding = new Thickness(4, 4, 4, 4),
                Style = (Style)FindResource("ModernButton"),
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Content = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = type.ShortName, FontSize = 14, FontWeight = FontWeights.SemiBold,
                            TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center },
                        new TextBlock { Text = type.Key, FontSize = 11, Foreground = Muted,
                            TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 2, 0, 0) }
                    }
                }
            };
            button.Click += (_, _) =>
            {
                try
                {
                    if (!session.Switch(key, () => Confirm("目前有未儲存修改，切換生命體會放棄這些修改。繼續嗎？"))) return;
                    notice = $"已載入 {type.Name}。";
                    RefreshHeader();
                    RenderPage();
                    RefreshTypes();
                }
                catch (Exception ex) { ShowError(ex); }
            };
            TypePanel.Children.Add(button);
            typeButtons.Add(button);
        }
        RefreshTypes();
    }

    private void RefreshTypes()
    {
        for (var i = 0; i < typeButtons.Count; i++)
        {
            var active = SettingsCatalog.Types[i].Key == session.Type;
            typeButtons[i].Background = active ? Primary : Surface;
            typeButtons[i].BorderBrush = active ? Brush("#A78BFA") : Control;
            typeButtons[i].BorderThickness = active ? new Thickness(1) : new Thickness(0);
        }
    }

    private void RefreshHeader()
    {
        CurrentTypeText.Text = SettingsCatalog.Types.Single(x => x.Key == session.Type).Name;
        LoadStateText.Text = "設定檔已載入";
        DirtyBadge.Text = session.ChangedCount == 0 && !ListsDirty && !ProfilesDirty ? "沒有未儲存修改" :
            $"{session.ChangedCount} 項設定修改{(ListsDirty ? " · 名單待儲存" : "")}{(ProfilesDirty ? " · 配置名稱待儲存" : "")}";
        DirtyBadge.Foreground = session.IsDirty || ListsDirty || ProfilesDirty ? Warning : Muted;
        StatusText.Text = notice;
        StatusText.Foreground = notice.StartsWith("操作失敗", StringComparison.Ordinal) ? Brush("#EF4444") : Muted;
        ResetButton.IsEnabled = session.IsDirty;
        SaveButton.IsEnabled = session.IsDirty;
    }

    private void RenderPage()
    {
        PageStack.Children.Clear();
        responsiveGroups.Clear();
        var query = SearchBox.Text.Trim();
        if (query.Length > 0)
        {
            PageTitle.Text = "搜尋設定";
            PageSubtitle.Text = $"搜尋「{query}」的中文名稱、Lua Key 和用途。";
            var matches = SettingsCatalog.ForType(session.Type).Where(x =>
                x.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                x.Key.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                x.Description.Contains(query, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (matches.Length == 0) PageStack.Children.Add(NoticeCard("沒有符合的設定", "請嘗試其他名稱或 Lua Key。"));
            else AddSettingsGroup(matches);
            UpdateResponsiveLayout();
            return;
        }
        PageTitle.Text = page;
        PageSubtitle.Text = page switch
        {
            "總覽" => "目前生命體與設定狀態，一眼掌握。",
            "常用" => "跟隨、搜尋、攻速與常用延遲。",
            "戰鬥" => "攻擊模式、反擊、黑名單開關與技能使用方式。",
            "安全" => "目標排除、優先順序與快捷鍵。",
            "進階" => "卡住恢復、走位與其他進階參數。",
            "技能管理" => "技能控制與現有技能表。",
            "名單" => "管理朋友與黑名單 ID。",
            "Profiles" => "管理自訂 1～3。儲存配置與寫入 Lua 是兩個獨立動作。",
            _ => "尚未提供安全編輯的欄位與設定檔資訊。"
        };
        switch (page)
        {
            case "總覽": RenderOverview(); break;
            case "Profiles": RenderProfiles(); break;
            case "名單": RenderLists(); break;
            case "進階": RenderAdvanced(); break;
            case "技能管理": RenderSkills(); break;
            default:
                AddSettingsGroup(FieldsForPage(page));
                break;
        }
        UpdateResponsiveLayout();
        ContentScroll.ScrollToTop();
    }

    private void AddSettingsGroup(IEnumerable<SettingDefinition> fields)
    {
        var panel = new WrapPanel();
        foreach (var field in fields) panel.Children.Add(SettingCard(field));
        responsiveGroups.Add((panel, false));
        PageStack.Children.Add(panel);
    }

    private IEnumerable<SettingDefinition> FieldsForPage(string name)
    {
        var common = new HashSet<string>
        {
            "FollowDis", "MoveDelay", "RadiusAggr", "AutoReturnDelay", "SentryRadius", "PlayerASPD", "ASPD",
            "DanceMinSP", "SkillMinSP", "DanceActivationDelay", "DanceCombatDelay"
        };
        var battle = new HashSet<string>
        {
            "AttackMonstersEnabled", "CooperativeMode", "ContinuousAttackMode", "FollowMode", "AltT_ToggleMode",
            "SentryMode", "AutoCounterAttack", "MVPMode", "SkillMode", "DanceAttackMode",
            "AutoBlacklist", "BlacklistPriority", "BlacklistOnly"
        };
        return SettingsCatalog.ForType(session.Type).Where(field => name switch
        {
            "常用" => common.Contains(field.Key),
            "戰鬥" => battle.Contains(field.Key),
            "安全" => field.Parent is "AttackAllExclude" or "HotkeySettings",
            "進階" => !common.Contains(field.Key) && !battle.Contains(field.Key) &&
                field.Parent is not ("AttackAllExclude" or "HotkeySettings"),
            _ => false
        }).OrderBy(field => name == "常用" && (field.Key is "PlayerASPD" or "ASPD") ? 0 : 1);
    }

    private void UpdateResponsiveLayout()
    {
        if (ContentScroll is null || HeaderGrid is null) return;
        var narrowWindow = ActualWidth > 0 && ActualWidth < 900;
        if (lastSidebarNarrow != narrowWindow)
        {
            SidebarColumn.Width = new GridLength(narrowWindow ? 160 : 180);
            lastSidebarNarrow = narrowWindow;
        }
        var stackedHeader = ActualWidth > 0 && ActualWidth < 950;
        if (lastHeaderStacked != stackedHeader)
        {
            foreach (var control in new UIElement[] { SearchBox, SearchHint })
            {
                Grid.SetRow(control, 1);
                Grid.SetColumn(control, stackedHeader ? 0 : 1);
                Grid.SetColumnSpan(control, stackedHeader ? 2 : 1);
            }
            SearchBox.Margin = stackedHeader ? new Thickness(0, 9, 0, 0) : new Thickness(0, 4, 0, 0);
            SearchHint.Margin = stackedHeader ? new Thickness(12, 9, 0, 0) : new Thickness(12, 4, 0, 0);
            lastHeaderStacked = stackedHeader;
        }
        var viewport = ContentScroll.ViewportWidth > 0 ? ContentScroll.ViewportWidth : ContentScroll.ActualWidth;
        var available = Math.Max(450, viewport - 64);
        foreach (var (panel, listCards) in responsiveGroups)
        {
            var columns = available >= (listCards ? 690 : 850) ? 2 : 1;
            var width = (available - columns * 12) / columns;
            foreach (FrameworkElement card in panel.Children)
            {
                card.Width = width;
                card.Margin = new Thickness(0, 0, 12, 12);
            }
        }
    }

    internal void SelectPageForPreview(string name)
    {
        if (!Pages.Contains(name)) throw new ArgumentException("未知頁面", nameof(name));
        page = name;
        RenderPage();
        RefreshNavigation();
    }

    internal int SmokeTest()
    {
        var checks = 0;
        WindowStartupLocation = WindowStartupLocation.Manual;
        Left = -10000;
        Top = -10000;
        Show();
        var monitors = WindowPlacementService.Screens(this);
        if (monitors.Count == 0 || monitors.All(x => x.Width <= 0 || x.Height <= 0))
            throw new Exception("無法取得可見螢幕工作區");
        checks++;
        foreach (var size in new[] { (800d, 600d), (1000d, 700d), (1200d, 800d) })
        {
            Width = size.Item1;
            Height = size.Item2;
            Dispatcher.Invoke(() => { }, DispatcherPriority.Render);
            UpdateLayout();
            if (ContentScroll.ActualWidth <= 0 || ContentScroll.ActualHeight <= 0)
                throw new Exception("主內容區未顯示");
            if (ContentScroll.ExtentWidth > ContentScroll.ViewportWidth + 1)
                throw new Exception($"{size.Item1}×{size.Item2} 出現外層橫向溢出");
            var saveRight = SaveButton.TranslatePoint(new Point(SaveButton.ActualWidth, 0), this).X;
            if (saveRight > ActualWidth + 1) throw new Exception("儲存按鈕被裁切");
            var lastNavBottom = navButtons[^1].TranslatePoint(new Point(0, navButtons[^1].ActualHeight), this).Y;
            if (lastNavBottom > ActualHeight - 20) throw new Exception("側欄功能被裁切");
            checks++;
        }
        foreach (var type in SettingsCatalog.Types)
        {
            var button = typeButtons[Array.FindIndex(SettingsCatalog.Types, x => x.Key == type.Key)];
            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            if (session.Type != type.Key) throw new Exception("生命體選擇失敗：" + type.Key);
            var assigned = new[] { "常用", "戰鬥", "安全", "進階" }.SelectMany(FieldsForPage).Select(x => x.Key).ToArray();
            var expected = SettingsCatalog.ForType(type.Key).Select(x => x.Key).ToArray();
            if (assigned.Length != expected.Length || assigned.Distinct().Count() != expected.Length ||
                expected.Except(assigned).Any()) throw new Exception("原版六頁欄位有遺漏或重複：" + type.Key);
            checks++;
        }
        foreach (var name in Pages)
        {
            SelectPageForPreview(name);
            if (PageStack.Children.Count == 0) throw new Exception("頁面為空：" + name);
            foreach (var size in new[] { (800d, 600d), (1000d, 700d), (1200d, 800d) })
            {
                Width = size.Item1; Height = size.Item2;
                Dispatcher.Invoke(() => { }, DispatcherPriority.Render);
                UpdateLayout();
                if (ContentScroll.ExtentWidth > ContentScroll.ViewportWidth + 1)
                    throw new Exception($"{name} 在 {size.Item1}×{size.Item2} 橫向溢出");
                foreach (var (panel, _) in responsiveGroups)
                foreach (Border settingTile in panel.Children)
                {
                    if (settingTile.Child is not Grid grid || grid.Children.Count < 2 || grid.Children[1] is not WrapPanel controls) continue;
                    var editorRight = controls.TranslatePoint(new Point(controls.ActualWidth, 0), settingTile).X;
                    if (editorRight > settingTile.ActualWidth + 1)
                        throw new Exception($"{name} 在 {size.Item1}×{size.Item2} 控制項超出卡片");
                }
                checks++;
            }
        }
        SearchBox.Text = "AttackTimeoutMs";
        if (PageStack.Children.Count != 1) throw new Exception("搜尋結果不正確");
        checks++;
        SearchBox.Text = "";
        session.Switch("lif", () => true);
        RefreshTypes();
        SelectPageForPreview("常用");
        var aspdCard = (Border)((WrapPanel)PageStack.Children[0]).Children[0];
        var aspdEditor = (WrapPanel)((Grid)aspdCard.Child).Children[1];
        ((TextBox)aspdEditor.Children[0]).Text = "190";
        if (session.Values["PlayerASPD"] != 190 || timingPreview?.Text.Contains("不影響") != true)
            throw new Exception("舊版 ASPD 說明未即時顯示");
        checks++;
        session.Reset();
        SelectPageForPreview("戰鬥");
        var card = (Border)((WrapPanel)PageStack.Children[0]).Children[0];
        var editor = (WrapPanel)((Grid)card.Child).Children[1];
        var toggle = (CheckBox)editor.Children[1];
        toggle.IsChecked = !toggle.IsChecked;
        toggle.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
        if (!session.IsDirty) throw new Exception("UI 編輯未標記未儲存");
        checks++;
        session.Reset();
        RenderPage();
        if (session.IsDirty) throw new Exception("UI 重設後仍有未儲存修改");
        checks++;
        Close();
        return checks;
    }

    private void RenderOverview()
    {
        var type = SettingsCatalog.Types.Single(x => x.Key == session.Type);
        PageStack.Children.Add(NoticeCard(type.Name,
            $"目前載入 {SettingsCatalog.ForType(session.Type).Count()} 個可編輯欄位。{session.ChangedCount} 項修改尚未寫入 Lua。"));
        PageStack.Children.Add(NoticeCard("資料安全",
            "寫入時只替換已修改欄位的數值，保留其他生命體、註解、編碼與換行。"));
        PageStack.Children.Add(NoticeCard("原版功能",
            "包含常用、戰鬥、安全、進階、名單與 Profiles。新介面按操作目的重新分類。"));
    }

    private void LoadLists()
    {
        var dir = Path.GetDirectoryName(configPath)!;
        friends = ListService.Read(Path.Combine(dir, "friends_list.lua"), "friends_data").ToList();
        enemies = ListService.Read(Path.Combine(dir, "enemies_list.lua"), "enemies_data").ToList();
        savedFriends = [.. friends];
        savedEnemies = [.. enemies];
    }

    private void RenderLists()
    {
        try
        {
            if (friends is null || enemies is null) LoadLists();
            var listPanel = new WrapPanel();
            listPanel.Children.Add(ListCard("朋友列表", friends!, "friends_list.lua", "friends_data"));
            listPanel.Children.Add(ListCard("黑名單", enemies!, "enemies_list.lua", "enemies_data"));
            responsiveGroups.Add((listPanel, true));
            PageStack.Children.Add(listPanel);
        }
        catch (Exception ex) { PageStack.Children.Add(NoticeCard("名單無法讀取", ex.Message)); }
    }

    private Border ListCard(string title, List<int> ids, string fileName, string variable)
    {
        var card = Card();
        var body = new StackPanel();
        body.Children.Add(new TextBlock { Text = $"{title} · {ids.Count} 個 ID", FontSize = 16, FontWeight = FontWeights.SemiBold });
        body.Children.Add(new TextBlock { Text = fileName + " · 所有生命體共用", Foreground = Muted, Margin = new Thickness(0, 5, 0, 10) });
        var list = new ListBox { ItemsSource = ids, Height = 140, Background = Brush("#111827"), Foreground = White,
            BorderBrush = Control, BorderThickness = new Thickness(1) };
        body.Children.Add(list);
        var input = new TextBox { Width = 145, Style = (Style)FindResource("ModernTextBox"), ToolTip = "輸入正整數 ID" };
        var row = new WrapPanel { Margin = new Thickness(0, 12, 0, 0) };
        row.Children.Add(input);
        foreach (var (label, action) in new (string, Action)[]
        {
            ("新增", () => { if (!int.TryParse(input.Text, out var id) || id <= 0 || ids.Contains(id))
                { notice = "ID 必須是未存在的正整數。"; RefreshHeader(); return; } ids.Add(id); RenderPage(); RefreshHeader(); }),
            ("移除選取", () => { if (list.SelectedItem is int id) { ids.Remove(id); RenderPage(); RefreshHeader(); } }),
            ("清空", () => { if (ids.Count > 0 && Confirm($"確定清空{title}？")) { ids.Clear(); RenderPage(); RefreshHeader(); } }),
            ("儲存名單", () => {
                if (!Confirm($"確定寫入 {fileName}？")) return;
                try { var path = Path.Combine(Path.GetDirectoryName(configPath)!, fileName);
                    ListService.Save(path, variable, ids, variable == "friends_data" ? savedFriends : savedEnemies);
                    if (variable == "friends_data") savedFriends = [.. ids]; else savedEnemies = [.. ids];
                    notice = $"已儲存{title}。"; RefreshHeader(); }
                catch (Exception ex) { ShowError(ex); }
            }),
            ("重新載入", () => { if (ListsDirty && !Confirm("放棄尚未儲存的名單修改？")) return;
                try { LoadLists(); RenderPage(); RefreshHeader(); } catch (Exception ex) { ShowError(ex); } })
        })
        {
            var button = ActionButton(label, action);
            button.Margin = new Thickness(8, 0, 0, 0);
            row.Children.Add(button);
        }
        body.Children.Add(row);
        card.Child = body;
        return card;
    }

    private void RenderSkills()
    {
        PageStack.Children.Add(NoticeCard("技能清單", "可逐一開關自動施放，並修改技能等級與延遲。修改會直接寫入 Lua；id=0 是普通攻擊。"));
        try
        {
            var skills = SkillService.Read(session.Document.Text, session.Type);
            var unsupported = skills.Where(x => !SkillService.IsHomunculusSkill(x.Id)).Select(x => x.Id).ToArray();
            if (unsupported.Length > 0)
                PageStack.Children.Add(NoticeCard("舊設定含非生命體技能",
                    $"技能 ID {string.Join("、", unsupported)} 屬於傭兵技能，已從生命體技能編輯區排除。原 Lua 內容未變更。"));
            foreach (var skill in skills.Where(x => SkillService.IsHomunculusSkill(x.Id)))
            {
                var card = Card();
                var row = new WrapPanel();
                row.Children.Add(new TextBlock { Text = $"{SkillName(skill.Id)} ({skill.Id}) · 等級 {skill.Level}", Width = 230,
                    VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.SemiBold });
                if (skill.Id > 0)
                {
                    var maxLevel = SkillService.MaxLevel(skill.Id);
                    var autoEnabled = SkillService.ReadAutoEnabled(session.Document.Text, session.Type, skill.Id, skill.Level);
                    if (autoEnabled is bool enabled)
                    {
                        var toggle = ActionButton(enabled ? "自動施放：開啟" : "自動施放：關閉", () =>
                        {
                            if (session.IsDirty) { notice = "請先儲存或重設其他 Lua 設定，再更新技能開關。"; RefreshHeader(); return; }
                            if (!Confirm($"確定{(enabled ? "關閉" : "開啟")}技能 {skill.Id} 的自動施放？")) return;
                            try
                            {
                                SkillService.SaveAutoEnabled(configPath, session.Type, skill.Id, skill.Level, enabled, !enabled);
                                session.ReloadFromDisk();
                                notice = $"技能 {skill.Id} 自動施放已{(enabled ? "關閉" : "開啟")}。";
                                RefreshHeader(); RenderPage();
                            }
                            catch (Exception ex) { ShowError(ex); }
                        });
                        toggle.Margin = new Thickness(0, 0, 12, 0);
                        row.Children.Add(toggle);
                    }
                    row.Children.Add(new TextBlock { Text = "等級", Foreground = Muted,
                        VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 7, 0) });
                    var levelInput = new TextBox { Text = skill.Level.ToString(CultureInfo.InvariantCulture), Width = 65,
                        Style = (Style)FindResource("ModernTextBox"), ToolTip = $"1–{maxLevel}；不能高於遊戲中習得等級" };
                    row.Children.Add(levelInput);
                    var saveLevel = ActionButton("更新等級", () =>
                    {
                        if (!int.TryParse(levelInput.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var level) || level < 1 || level > maxLevel)
                        { notice = $"技能等級需為 1–{maxLevel}。"; RefreshHeader(); return; }
                        if (session.IsDirty) { notice = "請先儲存或重設其他 Lua 設定，再更新技能等級。"; RefreshHeader(); return; }
                        if (!Confirm($"確定將技能 {skill.Id} 的等級設為 {level}？請確認遊戲中已習得該等級。")) return;
                        try { SkillService.SaveLevel(configPath, session.Type, skill.Index, skill.Id, skill.Level, level);
                            session.ReloadFromDisk(); notice = $"技能 {skill.Id} 等級已更新為 {level}。"; RefreshHeader(); RenderPage(); }
                        catch (Exception ex) { ShowError(ex); }
                    });
                    saveLevel.Margin = new Thickness(8, 0, 18, 0);
                    row.Children.Add(saveLevel);
                }
                row.Children.Add(new TextBlock { Text = "延遲", Foreground = Muted,
                    VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 7, 0) });
                var input = new TextBox { Text = skill.Delay.ToString(CultureInfo.InvariantCulture), Width = 110,
                    Style = (Style)FindResource("ModernTextBox"), ToolTip = "0–600000 毫秒" };
                row.Children.Add(input);
                var save = ActionButton("更新延遲", () =>
                {
                    if (!int.TryParse(input.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var delay) || delay is < 0 or > 600000)
                    { notice = "技能延遲需為 0–600000 毫秒。"; RefreshHeader(); return; }
                    if (session.IsDirty) { notice = "請先儲存或重設其他 Lua 設定，再更新技能延遲。"; RefreshHeader(); return; }
                    if (!Confirm($"確定將技能 {skill.Id} 的延遲設為 {delay} 毫秒？")) return;
                    try { SkillService.SaveDelay(configPath, session.Type, skill.Index, skill.Id, skill.Level, delay);
                        session.ReloadFromDisk(); notice = $"技能 {skill.Id} 延遲已更新。"; RefreshHeader(); RenderPage(); }
                    catch (Exception ex) { ShowError(ex); }
                });
                save.Margin = new Thickness(10, 0, 0, 0);
                row.Children.Add(save);
                card.Child = row;
                PageStack.Children.Add(card);
            }
            PageStack.Children.Add(CodeCard("SkillConfig", LuaConfigService.ExtractBlock(session.Document.Text, session.Type, "SkillConfig")));
        }
        catch (Exception ex) { PageStack.Children.Add(NoticeCard("技能讀取失敗", ex.Message)); }
    }

    private static string SkillName(int id) => id switch
    {
        0 => "普通攻擊", 8001 => "治癒之手", 8002 => "輕捷移動", 8006 => "防禦力",
        8009 => "月光", 8010 => "橫越速度", 8011 => "緊急迴避",
        8013 => "善變", 8014 => "混亂祈福",
        8018 => "召喚軍團", 8019 => "麻痺針", 8020 => "毒霧", 8021 => "止痛劑",
        8022 => "再生之光", 8023 => "超速強化", 8024 => "抹消切割", 8025 => "異星斬",
        8026 => "寂靜微風", 8027 => "變換姿態", 8028 => "音速爪", 8029 => "銀脈衝刺",
        8030 => "午夜狂熱", 8031 => "鋼鐵之角", 8032 => "黃金之踵", 8033 => "石牆",
        8034 => "聖光之柱", 8035 => "攻擊模式", 8036 => "燃火破擊", 8037 => "連續破擊連擊",
        8038 => "永恆迅捷連擊", 8039 => "岩漿流動", 8040 => "花崗岩護甲", 8041 => "熔岩滑動",
        8042 => "火山碎屑", 8043 => "火山灰", _ => "技能"
    };

    private void RenderAdvanced()
    {
        PageStack.Children.Add(NoticeCard("設定檔位置", configPath));
        PageStack.Children.Add(NoticeCard("重要提示", "部分舊欄位在目前 Lua 腳本中找不到直接引用。此處保留原始設定語意，實際效果仍需遊戲內驗證。"));
        AddSettingsGroup(FieldsForPage("進階"));
        var unmanaged = LuaConfigService.GetUnmanagedFields(session.Document.Text, session.Type);
        PageStack.Children.Add(CodeCard("其他頂層欄位（唯讀）", unmanaged.Length == 0 ? "無" : unmanaged));
    }

    private void RenderProfiles()
    {
        PageStack.Children.Add(ActionButton("重新載入 Profiles", () =>
        {
            if (ProfilesDirty && !Confirm("放棄尚未儲存的配置名稱？")) return;
            profileDrafts.Clear(); savedProfileNames.Clear();
            notice = "Profiles 已從磁碟重新載入。";
            RenderPage(); RefreshHeader();
        }));
        for (var slot = 1; slot <= 3; slot++)
        {
            var selected = slot;
            bool saved;
            try { saved = ProfileService.IsSaved(profilesPath, slot); }
            catch (Exception ex) { PageStack.Children.Add(NoticeCard($"自訂{slot} 讀取失敗", ex.Message)); continue; }
            var card = Card();
            var body = new StackPanel();
            body.Children.Add(new TextBlock { Text = $"自訂{slot}", FontSize = 16, FontWeight = FontWeights.SemiBold });
            if (!savedProfileNames.ContainsKey(slot)) savedProfileNames[slot] = ProfileService.Name(profilesPath, slot);
            if (!profileDrafts.ContainsKey(slot)) profileDrafts[slot] = savedProfileNames[slot];
            var name = new TextBox { Text = profileDrafts[slot], MaxLength = 40, Width = 240,
                HorizontalAlignment = HorizontalAlignment.Left, Style = (Style)FindResource("ModernTextBox"),
                ToolTip = "配置名稱，1–40 字元", Margin = new Thickness(0, 9, 0, 0) };
            name.TextChanged += (_, _) => { profileDrafts[selected] = name.Text; RefreshHeader(); };
            body.Children.Add(name);
            body.Children.Add(new TextBlock
            {
                Text = saved ? "已儲存配置" : "尚未儲存", Foreground = saved ? Success : Muted,
                Margin = new Thickness(0, 5, 0, 13)
            });
            body.Children.Add(new TextBlock
            {
                Text = "載入只更新目前編輯畫面；「套用並寫入 Lua」會另外要求確認。",
                Foreground = Muted, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 16)
            });
            var actions = new WrapPanel();
            var load = ActionButton("載入配置", () => LoadProfile(selected));
            load.IsEnabled = saved;
            actions.Children.Add(load);
            var store = ActionButton("儲存目前編輯為配置", () => SaveProfile(selected, name.Text));
            store.Margin = new Thickness(8, 0, 0, 0);
            actions.Children.Add(store);
            var apply = ActionButton("套用並寫入 Lua", () => ApplyProfile(selected));
            apply.IsEnabled = saved;
            apply.Margin = new Thickness(8, 0, 0, 0);
            actions.Children.Add(apply);
            var delete = ActionButton("刪除配置", () =>
            {
                if (!Confirm($"確定刪除自訂{selected}？")) return;
                try { ProfileService.Delete(profilesPath, selected); savedProfileNames[selected] = $"自訂{selected}";
                    profileDrafts[selected] = savedProfileNames[selected]; notice = $"已刪除自訂{selected}。"; RefreshHeader(); RenderPage(); }
                catch (Exception ex) { ShowError(ex); }
            });
            delete.IsEnabled = saved;
            delete.Margin = new Thickness(8, 0, 0, 0);
            actions.Children.Add(delete);
            body.Children.Add(actions);
            card.Child = body;
            PageStack.Children.Add(card);
        }
    }

    private Border SettingCard(SettingDefinition field)
    {
        var card = Card();
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4, GridUnitType.Star) });
        var left = new StackPanel { Margin = new Thickness(0, 0, 18, 0), VerticalAlignment = VerticalAlignment.Center };
        left.Children.Add(new TextBlock { Text = field.Name, FontSize = 15, FontWeight = FontWeights.SemiBold });
        left.Children.Add(new TextBlock { Text = field.Description, Foreground = Muted, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 5, 0, 0) });
        left.Children.Add(new TextBlock
        {
            Text = $"{field.Key}  ·  範圍 {field.Min}–{field.Max}{(field.Unit.Length > 0 ? " " + field.Unit : "")}",
            FontSize = 11, Foreground = Brush("#6B7280"), Margin = new Thickness(0, 8, 0, 0), TextWrapping = TextWrapping.Wrap
        });
        var changed = new TextBlock { Foreground = Warning, FontSize = 11, Margin = new Thickness(0, 5, 0, 0) };
        left.Children.Add(changed);
        if (field.Key == "PlayerASPD")
        {
            timingPreview = new TextBlock { Foreground = Success, TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 8, 0, 0), FontSize = 12 };
            left.Children.Add(timingPreview);
            UpdateTimingPreview();
        }
        grid.Children.Add(left);
        var editor = new WrapPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(editor, 1);
        grid.Children.Add(editor);
        bool? lastStacked = null;
        void AdaptCard(double width)
        {
            var stacked = width < 720;
            if (lastStacked == stacked) return;
            lastStacked = stacked;
            grid.RowDefinitions.Clear();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            if (stacked) grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.ColumnDefinitions[0].Width = stacked ? new GridLength(1, GridUnitType.Star) : new GridLength(3, GridUnitType.Star);
            grid.ColumnDefinitions[1].Width = stacked ? new GridLength(0) : new GridLength(4, GridUnitType.Star);
            Grid.SetRow(editor, stacked ? 1 : 0);
            Grid.SetColumn(editor, stacked ? 0 : 1);
            editor.HorizontalAlignment = stacked ? HorizontalAlignment.Left : HorizontalAlignment.Right;
            editor.Margin = stacked ? new Thickness(0, 14, 0, 0) : new Thickness(0);
        }
        card.SizeChanged += (_, _) => AdaptCard(card.ActualWidth);
        void UpdateState()
        {
            changed.Text = session.IsChanged(field.Key) ? "● 已修改" : "";
            UpdateTimingPreview();
            RefreshHeader();
        }
        var value = session.Values[field.Key];
        if (field.Kind == SettingKind.Toggle)
        {
            var label = new TextBlock { Text = field.DisplayValue(value), Foreground = value == 1 ? Success : Muted,
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 12, 0), MinWidth = 40 };
            var toggle = new CheckBox { Style = (Style)FindResource("ToggleStyle"), IsChecked = value == 1 };
            toggle.Click += (_, _) =>
            {
                var next = toggle.IsChecked == true ? 1 : 0;
                session.Set(field.Key, next);
                label.Text = field.DisplayValue(next);
                label.Foreground = next == 1 ? Success : Muted;
                UpdateState();
            };
            editor.Children.Add(label);
            editor.Children.Add(toggle);
        }
        else if (field.Kind == SettingKind.Choice)
        {
            for (var option = field.Min; option <= field.Max; option++)
            {
                var selected = option;
                var button = ActionButton(field.DisplayValue(option), () =>
                {
                    session.Set(field.Key, selected);
                    foreach (Button peer in editor.Children) peer.Background = (int)peer.Tag == selected ? Primary : Control;
                    UpdateState();
                });
                button.Tag = option;
                button.Background = option == value ? Primary : Control;
                button.Margin = new Thickness(option == field.Min ? 0 : 5, 0, 0, 0);
                button.ToolTip = $"Lua 值 {option}";
                editor.Children.Add(button);
            }
        }
        else
        {
            var input = new TextBox
            {
                Width = 100, Text = (value / (decimal)field.Scale).ToString(CultureInfo.InvariantCulture), TextAlignment = TextAlignment.Right,
                Style = (Style)FindResource("ModernTextBox"), ToolTip = $"允許範圍 {field.Min}–{field.Max}"
            };
            var adjusting = false;
            void CommitText(bool revertInvalid)
            {
                if (adjusting) return;
                var validNumber = decimal.TryParse(input.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed);
                var scaled = validNumber ? parsed * field.Scale : -1;
                if (!validNumber || scaled != decimal.Truncate(scaled) || scaled < field.Min || scaled > field.Max)
                {
                    input.BorderBrush = Brush("#EF4444");
                    if (revertInvalid)
                    {
                        notice = $"{field.Name} 需介於 {field.Min}–{field.Max}。原值已保留。";
                        adjusting = true;
                        input.Text = (session.Values[field.Key] / (decimal)field.Scale).ToString(CultureInfo.InvariantCulture);
                        adjusting = false;
                        RefreshHeader();
                    }
                    return;
                }
                input.BorderBrush = Brush("#4B5563");
                session.Set(field.Key, (int)scaled);
                UpdateState();
            }
            input.TextChanged += (_, _) => CommitText(false);
            input.LostFocus += (_, _) => CommitText(true);
            input.KeyDown += (_, e) => { if (e.Key == System.Windows.Input.Key.Enter) Keyboard.ClearFocus(); };
            editor.Children.Add(input);
            if (field.Unit.Length > 0)
                editor.Children.Add(new TextBlock { Text = field.Unit, Foreground = Muted, VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(9, 0, 0, 0) });
        }
        card.Child = grid;
        changed.Text = session.IsChanged(field.Key) ? "● 已修改" : "";
        return card;
    }

    private void UpdateTimingPreview()
    {
        if (timingPreview is null) return;
        timingPreview.Text = "此舊欄位不影響 RO3 原版舞步；原版使用 ASPD 與 DanceEnabled。";
    }

    private static Border Card() => new()
    {
        Background = Surface, BorderBrush = Control, BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(14), Padding = new Thickness(19),
        Margin = new Thickness(0, 0, 0, 11)
    };

    private static Border NoticeCard(string title, string description)
    {
        var card = Card();
        card.Child = new StackPanel
        {
            Children =
            {
                new TextBlock { Text = title, FontSize = 15, FontWeight = FontWeights.SemiBold },
                new TextBlock { Text = description, Foreground = Muted, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 7, 0, 0) }
            }
        };
        return card;
    }

    private static Border CodeCard(string title, string code)
    {
        var card = Card();
        card.Child = new StackPanel
        {
            Children =
            {
                new TextBlock { Text = title, FontSize = 15, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 10) },
                new TextBox { Text = code, IsReadOnly = true, TextWrapping = TextWrapping.NoWrap,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    MaxHeight = 230, Background = Brush("#111827"), Foreground = White, BorderThickness = new Thickness(0),
                    FontFamily = new FontFamily("Consolas"), Padding = new Thickness(10) }
            }
        };
        return card;
    }

    private Button ActionButton(string label, Action action)
    {
        var button = new Button { Content = label, Style = (Style)FindResource("ModernButton"), Background = Control };
        button.Click += (_, _) => action();
        return button;
    }

    private void LoadProfile(int slot)
    {
        try
        {
            var patch = ProfileService.Load(profilesPath, slot);
            if (!session.ApplyProfile(patch, () => Confirm("載入配置會取代目前未儲存修改。繼續嗎？"))) return;
            notice = $"已載入自訂{slot}，尚未寫入 Lua。";
            RefreshHeader();
            RenderPage();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void SaveProfile(int slot, string name)
    {
        try
        {
            if (ProfileService.IsSaved(profilesPath, slot) && !Confirm($"自訂{slot} 已存在，確定覆蓋嗎？")) return;
            ProfileService.Save(profilesPath, slot, session.Values, name);
            savedProfileNames[slot] = ProfileService.Name(profilesPath, slot);
            profileDrafts[slot] = savedProfileNames[slot];
            notice = $"已儲存自訂{slot}；Lua 設定檔尚未變動。";
            RefreshHeader();
            RenderPage();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void ApplyProfile(int slot)
    {
        try
        {
            var patch = ProfileService.Load(profilesPath, slot);
            if (!session.ApplyProfile(patch, () => Confirm("套用配置會取代目前未儲存修改。繼續嗎？"))) return;
            RefreshHeader();
            RenderPage();
            SaveLua();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void SaveLua()
    {
        if (!session.IsDirty) return;
        if (!Confirm($"確定將 {session.ChangedCount} 項修改寫入 {SettingsCatalog.Types.Single(x => x.Key == session.Type).Name} 的 Lua 設定嗎？")) return;
        try
        {
            session.Save();
            notice = "儲存成功。";
            RefreshHeader();
            RenderPage();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private bool Confirm(string text) => MessageBox.Show(this, text, "確認操作", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    private void ShowError(Exception ex)
    {
        notice = "操作失敗：" + ex.Message;
        RefreshHeader();
        MessageBox.Show(this, ex.Message, "生命體 AI 管理器", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (SearchHint is not null) SearchHint.Visibility = SearchBox.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        if (PageStack is not null && session is not null) RenderPage();
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        if (session.IsDirty && !Confirm("確定放棄目前編輯，恢復已載入的設定嗎？")) return;
        session.Reset();
        notice = "已重設目前編輯內容。";
        RefreshHeader();
        RenderPage();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e) => SaveLua();
}
