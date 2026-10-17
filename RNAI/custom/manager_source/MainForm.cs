using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Drawing.Drawing2D;

namespace HomunculusManager;

internal sealed class MainForm : Form
{
    private static readonly Color Ink = Color.FromArgb(35, 42, 69);
    private static readonly Color Muted = Color.FromArgb(105, 111, 135);
    private static readonly Color Accent = Color.FromArgb(111, 66, 218);
    private static readonly Color Canvas = Color.FromArgb(248, 247, 253);
    private static readonly Color[] TypeColors =
    [Color.FromArgb(226, 85, 151), Color.FromArgb(238, 137, 54), Color.FromArgb(40, 160, 192), Color.FromArgb(126, 86, 223)];
    private readonly string configPath = Path.Combine(AppContext.BaseDirectory, "homunculus_config.lua");
    private readonly string profilesPath = Path.Combine(AppContext.BaseDirectory, "manager_profiles.json");
    private readonly Button[] typeButtons = new Button[Settings.Types.Length];
    private readonly Button[] profileButtons = new Button[3];
    private readonly Dictionary<string, int> values = [];
    private readonly Dictionary<string, Button> settingButtons = [];
    private readonly Label status = new() { AutoSize = true, Text = "請選擇生命體", ForeColor = Muted };
    private bool loading;
    private bool dirty;
    private int loadedIndex = -1;
    private int selectedType;
    private int selectedProfile;

    internal MainForm()
    {
        Text = "生命體設定器";
        ClientSize = new Size(900, 790);
        MinimumSize = new Size(850, 600);
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Microsoft JhengHei UI", 10);
        BackColor = Canvas;

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Margin = Padding.Empty, Padding = Padding.Empty };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 221));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
        Controls.Add(layout);

        var header = new Panel { Dock = DockStyle.Fill, BackColor = Accent, Margin = Padding.Empty };
        header.Paint += (_, e) =>
        {
            using var brush = new LinearGradientBrush(header.ClientRectangle, Color.FromArgb(81, 68, 194),
                Color.FromArgb(177, 71, 185), LinearGradientMode.Horizontal);
            e.Graphics.FillRectangle(brush, header.ClientRectangle);
        };
        header.Controls.Add(new Label
        {
            Text = "生命體設定器", Location = new Point(26, 15), AutoSize = true,
            Font = new Font(Font.FontFamily, 18, FontStyle.Bold), ForeColor = Color.White,
            BackColor = Color.Transparent
        });
        header.Controls.Add(new Label
        {
            Text = "點按鈕切換或輸入數值；部分舊欄位在 AI 腳本中找不到直接引用", Location = new Point(28, 52),
            AutoSize = true, ForeColor = Color.FromArgb(237, 230, 255),
            BackColor = Color.Transparent, Font = new Font(Font.FontFamily, 9)
        });
        header.Controls.Add(new Label { Text = "選擇生命體", Location = new Point(28, 86), AutoSize = true,
            ForeColor = Color.White, BackColor = Color.Transparent, Font = new Font(Font, FontStyle.Bold) });
        for (var i = 0; i < typeButtons.Length; i++)
        {
            var index = i;
            var button = MakeButton(Settings.Types[i].Label, false);
            button.Location = new Point(26 + i * 212, 109);
            button.Size = new Size(200, 38);
            button.Click += (_, _) => LoadSelected(index);
            typeButtons[i] = button;
            header.Controls.Add(button);
        }
        header.ClientSizeChanged += (_, _) =>
        {
            var width = (header.ClientSize.Width - 82) / 4;
            for (var i = 0; i < typeButtons.Length; i++)
            {
                typeButtons[i].Width = width;
                typeButtons[i].Left = 26 + i * (width + 10);
            }
        };
        header.Controls.Add(new Label { Text = "自訂配置", Location = new Point(28, 172), AutoSize = true,
            ForeColor = Color.White, BackColor = Color.Transparent, Font = new Font(Font, FontStyle.Bold) });
        for (var i = 0; i < profileButtons.Length; i++)
        {
            var index = i;
            var button = MakeButton($"自訂{i + 1}", false);
            button.Location = new Point(121 + i * 93, 164);
            button.Size = new Size(84, 35);
            button.Click += (_, _) => { selectedProfile = index; RefreshSelectionButtons(); };
            profileButtons[i] = button;
            header.Controls.Add(button);
        }
        var loadProfile = MakeButton("載入配置", false);
        loadProfile.Location = new Point(435, 164);
        loadProfile.Click += (_, _) => LoadProfile();
        header.Controls.Add(loadProfile);
        var saveProfile = MakeButton("儲存配置", false);
        saveProfile.Location = new Point(544, 164);
        saveProfile.Click += (_, _) => SaveProfile();
        header.Controls.Add(saveProfile);
        layout.Controls.Add(header, 0, 0);

        var footer = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Margin = Padding.Empty };
        footer.Paint += (_, e) => e.Graphics.DrawLine(Pens.Gainsboro, 0, 0, footer.Width, 0);
        var save = MakeButton("儲存生命體設定", true);
        save.Size = new Size(180, 40);
        save.Location = new Point(26, 14);
        save.Click += (_, _) => Save();
        footer.Controls.Add(save);
        status.Location = new Point(224, 25);
        footer.Controls.Add(status);
        layout.Controls.Add(footer, 0, 2);

        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Canvas,
            Padding = new Padding(22, 18, 22, 18), Margin = Padding.Empty };
        var cards = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = Padding.Empty
        };
        cards.Controls.Add(CreateCard("01  戰鬥行為", "攻擊與跟隨模式", Settings.Fields.Take(7), Color.FromArgb(233, 80, 135)));
        cards.Controls.Add(CreateCard("02  排除與目標", "控制 AI 選擇攻擊對象", Settings.Fields.Skip(7).Take(7), Color.FromArgb(41, 171, 164)));
        cards.Controls.Add(CreateCard("03  距離與時間", "調整索敵、速度與逾時", Settings.Fields.Skip(14), Color.FromArgb(245, 151, 55)));
        scroll.Controls.Add(cards);
        scroll.ClientSizeChanged += (_, _) =>
        {
            foreach (Control card in cards.Controls) card.Width = Math.Max(760, scroll.ClientSize.Width - 48);
        };
        layout.Controls.Add(scroll, 0, 1);

        LoadSelected(0);
    }

    private static Button MakeButton(string text, bool primary)
    {
        var button = new Button
        {
            Text = text, Size = new Size(text.Length > 4 ? 96 : 86, 34),
            FlatStyle = FlatStyle.Flat, BackColor = primary ? Accent : Color.White,
            ForeColor = primary ? Color.White : Ink, Font = new Font("Microsoft JhengHei UI", 9, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = primary ? Color.FromArgb(86, 43, 182) : Color.FromArgb(238, 231, 255);
        return button;
    }

    private void RefreshSelectionButtons()
    {
        for (var i = 0; i < typeButtons.Length; i++)
        {
            typeButtons[i].BackColor = i == selectedType ? TypeColors[i] : Color.White;
            typeButtons[i].ForeColor = i == selectedType ? Color.White : Ink;
        }
        for (var i = 0; i < profileButtons.Length; i++)
        {
            profileButtons[i].BackColor = i == selectedProfile ? Color.FromArgb(255, 204, 77) : Color.White;
            profileButtons[i].ForeColor = Ink;
        }
    }

    private Panel CreateCard(string title, string subtitle, IEnumerable<Setting> fields, Color color)
    {
        var entries = fields.ToArray();
        var card = new Panel
        {
            Width = 800, Height = 83 + entries.Length * 88, BackColor = Color.White,
            Margin = new Padding(0, 0, 0, 16), Padding = new Padding(20)
        };
        card.Paint += (_, e) =>
        {
            ControlPaint.DrawBorder(e.Graphics, card.ClientRectangle, Color.FromArgb(225, 227, 238), ButtonBorderStyle.Solid);
            using var brush = new SolidBrush(color);
            e.Graphics.FillRectangle(brush, 0, 0, 6, card.Height);
        };
        card.Controls.Add(new Label
        {
            Text = title, Location = new Point(24, 15), AutoSize = true,
            Font = new Font(Font.FontFamily, 12, FontStyle.Bold), ForeColor = color
        });
        card.Controls.Add(new Label
        {
            Text = subtitle, Location = new Point(24, 43), AutoSize = true,
            Font = new Font(Font.FontFamily, 9), ForeColor = Muted
        });
        for (var i = 0; i < entries.Length; i++)
        {
            var field = entries[i];
            var y = 82 + i * 88;
            card.Controls.Add(new Label
            {
                Text = field.Label, Location = new Point(25, y + 23), Width = 260,
                ForeColor = Ink, Font = new Font(Font, FontStyle.Bold)
            });
            var button = new Button
            {
                Location = new Point(290, y + 5), Width = 470, Height = 70,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 3, 8, 3),
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(250, 248, 255),
                ForeColor = Ink, Cursor = Cursors.Hand, Font = new Font(Font.FontFamily, 9)
            };
            button.FlatAppearance.BorderColor = color;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(242, 235, 255);
            button.Click += (_, _) => ChangeSetting(field);
            card.Controls.Add(button);
            settingButtons[field.Key] = button;
        }
        return card;
    }

    private void RefreshSettingButtons()
    {
        foreach (var field in Settings.Fields)
        {
            var value = values.GetValueOrDefault(field.Key);
            settingButtons[field.Key].Text = $"目前 {value}  ·  {field.Description}";
        }
    }

    private void ChangeSetting(Setting field)
    {
        if (!values.TryGetValue(field.Key, out var current)) return;
        int? next = field.Max <= 3
            ? current >= field.Max ? field.Min : current + 1
            : AskNumber(field, current);
        if (next is null || next == current) return;
        values[field.Key] = next.Value;
        dirty = true;
        RefreshSettingButtons();
    }

    private int? AskNumber(Setting field, int current)
    {
        using var dialog = new Form
        {
            Text = field.Label, ClientSize = new Size(420, 175), FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent, MaximizeBox = false, MinimizeBox = false,
            Font = Font, BackColor = Color.White
        };
        dialog.Controls.Add(new Label { Text = field.Description, Location = new Point(20, 18), Size = new Size(380, 42), ForeColor = Ink });
        var number = new NumericUpDown
        {
            Minimum = field.Min, Maximum = field.Max, Value = current,
            Location = new Point(20, 75), Width = 190
        };
        dialog.Controls.Add(number);
        var accept = MakeButton("確定", true);
        accept.Location = new Point(205, 122);
        accept.DialogResult = DialogResult.OK;
        dialog.Controls.Add(accept);
        var cancel = MakeButton("取消", false);
        cancel.Location = new Point(305, 122);
        cancel.DialogResult = DialogResult.Cancel;
        dialog.Controls.Add(cancel);
        dialog.AcceptButton = accept;
        dialog.CancelButton = cancel;
        return dialog.ShowDialog(this) == DialogResult.OK ? (int)number.Value : null;
    }

    private void LoadSelected(int index)
    {
        if (loading || index < 0 || index >= Settings.Types.Length) return;
        if (dirty && loadedIndex >= 0 && index != loadedIndex &&
            MessageBox.Show(this, "目前的修改尚未儲存，確定切換生命體？", "未儲存的修改",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;
        try
        {
            var type = Settings.Types[index].Key;
            var values = ConfigEditor.Read(File.ReadAllText(configPath), type);
            loading = true;
            foreach (var field in Settings.Fields) this.values[field.Key] = values[field.Key];
            RefreshSettingButtons();
            selectedType = index;
            loadedIndex = index;
            dirty = false;
            RefreshSelectionButtons();
            status.Text = "已載入「" + Settings.Types[index].Label + "」";
        }
        catch (Exception ex)
        {
            status.Text = "載入失敗";
            MessageBox.Show(this, ex.Message, "設定檔錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { loading = false; }
    }

    private void Save()
    {
        if (loadedIndex < 0) return;
        try
        {
            var type = Settings.Types[selectedType].Key;
            var source = File.ReadAllText(configPath);
            var updated = ConfigEditor.Update(source, type, values);
            ConfigEditor.Save(configPath, updated);
            dirty = false;
            status.Text = "已儲存，備份為 homunculus_config.lua.manager.bak";
        }
        catch (Exception ex)
        {
            status.Text = "儲存失敗";
            MessageBox.Show(this, ex.Message, "儲存失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static string ProfileGroup(Setting field) => field.Key == "AttackMonstersEnabled" || field.Parent is not null
        ? "exclude" : field.Key is "RadiusAggr" or "FollowDis" or "SentryRadius" or "PlayerASPD" or "SkillMinSP" or "AttackTimeoutMs" or "AltT_ToggleMode"
            ? "parameters" : "combat";

    private void LoadProfile()
    {
        try
        {
            var root = JsonNode.Parse(File.ReadAllText(profilesPath)) ?? throw new FormatException("配置檔為空");
            var name = $"自訂{selectedProfile + 1}";
            var profile = root["profiles"]?[name] ?? throw new FormatException("找不到配置：" + name);
            if (profile["saved"]?.GetValue<bool>() != true) throw new FormatException(name + " 尚未儲存");
            foreach (var field in Settings.Fields)
            {
                var node = profile[ProfileGroup(field)]?[field.Key];
                if (node is null) continue;
                var value = field.Key == "AttackTimeoutMs" ? (int)Math.Round(node.GetValue<double>() * 1000) : node.GetValue<int>();
                if (value < field.Min || value > field.Max) throw new FormatException(field.Label + " 超出範圍");
                values[field.Key] = value;
            }
            RefreshSettingButtons();
            dirty = true;
            status.Text = "已載入 " + name + "；按「儲存設定」才會寫入生命體設定";
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "載入配置失敗", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void SaveProfile()
    {
        try
        {
            var root = JsonNode.Parse(File.ReadAllText(profilesPath)) ?? throw new FormatException("配置檔為空");
            var name = $"自訂{selectedProfile + 1}";
            var profile = root["profiles"]?[name] ?? throw new FormatException("找不到配置：" + name);
            foreach (var field in Settings.Fields)
            {
                var group = profile[ProfileGroup(field)] ?? throw new FormatException("缺少配置群組：" + ProfileGroup(field));
                if (field.Key == "AttackTimeoutMs")
                    group[field.Key] = (double)values[field.Key] / 1000;
                else
                    group[field.Key] = values[field.Key];
            }
            profile["saved"] = true;
            var temp = profilesPath + ".manager.tmp";
            try
            {
                File.WriteAllText(temp, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
                File.Replace(temp, profilesPath, profilesPath + ".manager.bak");
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
            status.Text = "已儲存 " + name;
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "儲存配置失敗", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}
