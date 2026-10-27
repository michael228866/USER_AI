using System.Globalization;
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
    ["總覽", "戰鬥行為", "目標過濾", "距離與時間", "技能管理", "自訂配置", "進階設定"];

    private readonly string configPath;
    private readonly string profilesPath;
    private readonly EditorSession session;
    private readonly List<Button> navButtons = [];
    private readonly List<Button> typeButtons = [];
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
        Closing += (_, e) =>
        {
            if (session.IsDirty && !Confirm("目前有未儲存修改，確定關閉嗎？")) e.Cancel = true;
        };
    }

    private static Brush Brush(string hex) => (Brush)new BrushConverter().ConvertFromString(hex)!;

    private void BuildNavigation()
    {
        NavigationPanel.Children.Clear();
        navButtons.Clear();
        foreach (var name in Pages)
        {
            var button = new Button
            {
                Content = name, Height = 42, Margin = new Thickness(0, 0, 0, 7),
                HorizontalContentAlignment = HorizontalAlignment.Left,
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
                Height = 58, Margin = new Thickness(0, 0, 10, 0),
                Style = (Style)FindResource("ModernButton"),
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Content = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = type.ShortName, FontSize = 14, FontWeight = FontWeights.SemiBold },
                        new TextBlock { Text = type.Key, FontSize = 11, Foreground = Muted, Margin = new Thickness(0, 2, 0, 0) }
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
        DirtyBadge.Text = session.ChangedCount == 0 ? "沒有未儲存修改" : $"{session.ChangedCount} 項未儲存修改";
        DirtyBadge.Foreground = session.IsDirty ? Warning : Muted;
        StatusText.Text = notice;
        StatusText.Foreground = notice.StartsWith("操作失敗", StringComparison.Ordinal) ? Brush("#EF4444") : Muted;
        ResetButton.IsEnabled = session.IsDirty;
        SaveButton.IsEnabled = session.IsDirty;
    }

    private void RenderPage()
    {
        PageStack.Children.Clear();
        var query = SearchBox.Text.Trim();
        if (query.Length > 0)
        {
            PageTitle.Text = "搜尋設定";
            PageSubtitle.Text = $"搜尋「{query}」的中文名稱、Lua Key 和用途。";
            var matches = SettingsCatalog.All.Where(x =>
                x.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                x.Key.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                x.Description.Contains(query, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (matches.Length == 0) PageStack.Children.Add(NoticeCard("沒有符合的設定", "請嘗試其他名稱或 Lua Key。"));
            else foreach (var field in matches) PageStack.Children.Add(SettingCard(field));
            return;
        }
        PageTitle.Text = page;
        PageSubtitle.Text = page switch
        {
            "總覽" => "目前生命體與設定狀態，一眼掌握。",
            "戰鬥行為" => "攻擊、反擊、跟隨與哨戒模式。",
            "目標過濾" => "選擇生命體可以攻擊或應排除的對象。",
            "距離與時間" => "搜尋距離、攻速與逾時參數。",
            "技能管理" => "安全可編輯的技能 SP 門檻，以及唯讀的技能清單。",
            "自訂配置" => "管理自訂 1～3。儲存配置與寫入 Lua 是兩個獨立動作。",
            _ => "尚未提供安全編輯的欄位與設定檔資訊。"
        };
        switch (page)
        {
            case "總覽": RenderOverview(); break;
            case "自訂配置": RenderProfiles(); break;
            case "進階設定": RenderAdvanced(); break;
            case "技能管理": RenderSkills(); break;
            default:
                foreach (var field in SettingsCatalog.All.Where(x => x.Section == page))
                    PageStack.Children.Add(SettingCard(field));
                break;
        }
        ContentScroll.ScrollToTop();
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
        foreach (var size in new[] { (900d, 600d), (1100d, 760d) })
        {
            Width = size.Item1;
            Height = size.Item2;
            Dispatcher.Invoke(() => { }, DispatcherPriority.Render);
            UpdateLayout();
            if (ContentScroll.ActualWidth <= 0 || ContentScroll.ActualHeight <= 0)
                throw new Exception("主內容區未顯示");
            checks++;
        }
        foreach (var type in SettingsCatalog.Types)
        {
            var button = typeButtons[Array.FindIndex(SettingsCatalog.Types, x => x.Key == type.Key)];
            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            if (session.Type != type.Key) throw new Exception("生命體選擇失敗：" + type.Key);
            checks++;
        }
        foreach (var name in Pages)
        {
            SelectPageForPreview(name);
            UpdateLayout();
            if (PageStack.Children.Count == 0) throw new Exception("頁面為空：" + name);
            checks++;
        }
        SearchBox.Text = "AttackTimeoutMs";
        if (PageStack.Children.Count != 1) throw new Exception("搜尋結果不正確");
        checks++;
        SearchBox.Text = "";
        Close();
        return checks;
    }

    private void RenderOverview()
    {
        var type = SettingsCatalog.Types.Single(x => x.Key == session.Type);
        PageStack.Children.Add(NoticeCard(type.Name,
            $"目前載入 {SettingsCatalog.All.Length} 個可編輯欄位。{session.ChangedCount} 項修改尚未寫入 Lua。"));
        PageStack.Children.Add(NoticeCard("資料安全",
            "寫入時只替換已修改欄位的數值，保留其他生命體、註解、編碼與換行；原檔另存 .manager.bak 備份。"));
        PageStack.Children.Add(NoticeCard("技能與其他欄位",
            "SkillConfig、Skill 與未支援的 Lua 欄位可在技能管理、進階設定頁唯讀檢視。"));
    }

    private void RenderSkills()
    {
        PageStack.Children.Add(SettingCard(SettingsCatalog.Find("SkillMinSP")));
        PageStack.Children.Add(NoticeCard("唯讀技能結構", "技能表包含巢狀 Lua table，目前僅提供唯讀瀏覽；不會修改技能語法。"));
        try
        {
            PageStack.Children.Add(CodeCard("SkillConfig", LuaConfigService.ExtractBlock(session.Document.Text, session.Type, "SkillConfig")));
            PageStack.Children.Add(CodeCard("Skill", LuaConfigService.ExtractBlock(session.Document.Text, session.Type, "Skill")));
        }
        catch (Exception ex) { PageStack.Children.Add(NoticeCard("技能讀取失敗", ex.Message)); }
    }

    private void RenderAdvanced()
    {
        PageStack.Children.Add(NoticeCard("設定檔位置", configPath));
        PageStack.Children.Add(NoticeCard("重要提示", "部分舊欄位在目前 Lua 腳本中找不到直接引用。此處保留原始設定語意，實際效果仍需遊戲內驗證。"));
        var unmanaged = LuaConfigService.GetUnmanagedFields(session.Document.Text, session.Type);
        PageStack.Children.Add(CodeCard("其他頂層欄位（唯讀）", unmanaged.Length == 0 ? "無" : unmanaged));
        PageStack.Children.Add(CodeCard("HotkeySettings（唯讀）", LuaConfigService.ExtractBlock(session.Document.Text, session.Type, "HotkeySettings")));
    }

    private void RenderProfiles()
    {
        for (var slot = 1; slot <= 3; slot++)
        {
            var selected = slot;
            bool saved;
            try { saved = ProfileService.IsSaved(profilesPath, slot); }
            catch (Exception ex) { PageStack.Children.Add(NoticeCard($"自訂{slot} 讀取失敗", ex.Message)); continue; }
            var card = Card();
            var body = new StackPanel();
            body.Children.Add(new TextBlock { Text = $"自訂{slot}", FontSize = 16, FontWeight = FontWeights.SemiBold });
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
            var store = ActionButton("儲存目前編輯為配置", () => SaveProfile(selected));
            store.Margin = new Thickness(8, 0, 0, 0);
            actions.Children.Add(store);
            var apply = ActionButton("套用並寫入 Lua", () => ApplyProfile(selected));
            apply.IsEnabled = saved;
            apply.Margin = new Thickness(8, 0, 0, 0);
            actions.Children.Add(apply);
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
        grid.Children.Add(left);
        var editor = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(editor, 1);
        grid.Children.Add(editor);
        void UpdateState()
        {
            changed.Text = session.IsChanged(field.Key) ? "● 已修改" : "";
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
                Width = 100, Text = value.ToString(CultureInfo.InvariantCulture), TextAlignment = TextAlignment.Right,
                Style = (Style)FindResource("ModernTextBox"), ToolTip = $"允許範圍 {field.Min}–{field.Max}"
            };
            var adjusting = false;
            void CommitText(bool revertInvalid)
            {
                if (adjusting) return;
                if (!int.TryParse(input.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var next) || next < field.Min || next > field.Max)
                {
                    input.BorderBrush = Brush("#EF4444");
                    if (revertInvalid)
                    {
                        notice = $"{field.Name} 需介於 {field.Min}–{field.Max}。原值已保留。";
                        adjusting = true;
                        input.Text = session.Values[field.Key].ToString(CultureInfo.InvariantCulture);
                        adjusting = false;
                        RefreshHeader();
                    }
                    return;
                }
                input.BorderBrush = Brush("#4B5563");
                session.Set(field.Key, next);
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

    private void SaveProfile(int slot)
    {
        try
        {
            if (ProfileService.IsSaved(profilesPath, slot) && !Confirm($"自訂{slot} 已存在，確定覆蓋嗎？")) return;
            ProfileService.Save(profilesPath, slot, session.Values);
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
            notice = "儲存成功。原設定已備份為 homunculus_config.lua.manager.bak。";
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
        MessageBox.Show(this, ex.Message, "生命體設定器", MessageBoxButton.OK, MessageBoxImage.Error);
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
