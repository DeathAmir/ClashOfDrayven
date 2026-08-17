using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Media;
using System.Text.Json;

namespace ClashOfDrayven;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Theme.Initialize();
        SoundBank.Initialize();
        var state = GameState.Load();
        using (var splash = new SplashForm()) splash.ShowDialog();
        Application.Run(new GameForm(state));
    }
}

internal enum Currency { Gold, Elixir, Gems }

internal sealed record BuildingDefinition(
    string Id, string Name, Currency Currency, int BaseCost, int MaxLevel,
    string Description, Color Body, Color Roof);

internal sealed record UnitDefinition(
    string Id, string Name, Currency Currency, int Cost, string Description,
    Color Body, Color Accent);

internal sealed class PlacedBuilding
{
    public Guid InstanceId { get; set; } = Guid.NewGuid();
    public string DefinitionId { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
    public int Level { get; set; } = 1;
}

internal sealed class ClanInfo
{
    public string Name { get; set; } = "";
    public string Tag { get; set; } = "";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

internal sealed class GameSave
{
    public int Gold { get; set; } = 7000;
    public int Elixir { get; set; } = 7000;
    public int Gems { get; set; } = 250;
    public List<PlacedBuilding> Buildings { get; set; } = new();
    public Dictionary<string, int> Units { get; set; } = new();
    public ClanInfo? Clan { get; set; }
    public DateTime LastSavedUtc { get; set; } = DateTime.UtcNow;
}

internal sealed class GameState
{
    public static readonly BuildingDefinition[] BuildingCatalog =
    [
        new("townhall", "Drayven Hall", Currency.Gold, 0, 5, "The heart of your stronghold.", Color.FromArgb(205,150,74), Color.FromArgb(116,57,44)),
        new("goldmine", "Gold Mine", Currency.Gold, 850, 8, "Produces gold over time.", Color.FromArgb(92,105,117), Color.FromArgb(242,183,47)),
        new("elixircollector", "Elixir Pump", Currency.Gold, 900, 8, "Produces elixir over time.", Color.FromArgb(104,79,139), Color.FromArgb(225,94,225)),
        new("barracks", "Barracks", Currency.Elixir, 1200, 7, "Unlocks and trains battle units.", Color.FromArgb(171,91,58), Color.FromArgb(96,51,40)),
        new("cannon", "Cannon", Currency.Gold, 1100, 8, "A sturdy defensive weapon.", Color.FromArgb(80,83,88), Color.FromArgb(47,49,55)),
        new("wall", "Stone Wall", Currency.Gold, 180, 10, "Cheap protection for your base.", Color.FromArgb(127,119,104), Color.FromArgb(164,151,125))
    ];

    public static readonly UnitDefinition[] UnitCatalog =
    [
        new("vanguard", "Vanguard", Currency.Elixir, 250, "Reliable frontline fighter.", Color.FromArgb(207,154,92), Color.FromArgb(60,117,185)),
        new("ranger", "Ranger", Currency.Elixir, 360, "Fast ranged attacker.", Color.FromArgb(217,164,144), Color.FromArgb(104,66,132)),
        new("brute", "Brute", Currency.Elixir, 800, "Heavy unit with high durability.", Color.FromArgb(142,106,88), Color.FromArgb(63,75,95)),
        new("stormcaller", "Stormcaller", Currency.Gems, 25, "Premium caster with burst damage.", Color.FromArgb(101,161,194), Color.FromArgb(239,202,72))
    ];

    private readonly string _savePath;
    public GameSave SaveData { get; private set; }

    private GameState(GameSave save, string savePath)
    {
        SaveData = save;
        _savePath = savePath;
        EnsureDefaults();
    }

    public static GameState Load()
    {
        string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IrAutoX", "ClashOfDrayven");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "save.json");
        GameSave save = new();
        try
        {
            if (File.Exists(path))
            {
                save = JsonSerializer.Deserialize<GameSave>(File.ReadAllText(path), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new GameSave();
            }
        }
        catch { save = new GameSave(); }
        var state = new GameState(save, path);
        state.ApplyOfflineProduction();
        state.Save();
        return state;
    }

    private void EnsureDefaults()
    {
        SaveData.Buildings ??= new List<PlacedBuilding>();
        SaveData.Units ??= new Dictionary<string, int>();
        if (!SaveData.Buildings.Any(b => b.DefinitionId == "townhall"))
        {
            SaveData.Buildings.Add(new PlacedBuilding { DefinitionId = "townhall", X = 9, Y = 9, Level = 1 });
        }
        foreach (var unit in UnitCatalog)
            SaveData.Units.TryAdd(unit.Id, 0);
    }

    private void ApplyOfflineProduction()
    {
        var elapsed = DateTime.UtcNow - SaveData.LastSavedUtc;
        int seconds = (int)Math.Clamp(elapsed.TotalSeconds, 0, 60 * 60 * 8);
        if (seconds <= 0) return;
        int goldRate = SaveData.Buildings.Where(b => b.DefinitionId == "goldmine").Sum(b => 2 + b.Level * 2);
        int elixirRate = SaveData.Buildings.Where(b => b.DefinitionId == "elixircollector").Sum(b => 2 + b.Level * 2);
        SaveData.Gold = Math.Min(9999999, SaveData.Gold + goldRate * seconds / 3);
        SaveData.Elixir = Math.Min(9999999, SaveData.Elixir + elixirRate * seconds / 3);
    }

    public void TickProduction()
    {
        int gold = SaveData.Buildings.Where(b => b.DefinitionId == "goldmine").Sum(b => 1 + b.Level);
        int elixir = SaveData.Buildings.Where(b => b.DefinitionId == "elixircollector").Sum(b => 1 + b.Level);
        SaveData.Gold = Math.Min(9999999, SaveData.Gold + gold);
        SaveData.Elixir = Math.Min(9999999, SaveData.Elixir + elixir);
        Save();
    }

    public BuildingDefinition? Definition(string id) => BuildingCatalog.FirstOrDefault(x => x.Id == id);
    public UnitDefinition? Unit(string id) => UnitCatalog.FirstOrDefault(x => x.Id == id);

    public bool Occupied(int x, int y) => SaveData.Buildings.Any(b => b.X == x && b.Y == y);

    public bool TryBuild(BuildingDefinition def, int x, int y, out string message)
    {
        if (def.Id == "townhall") { message = "Only one Drayven Hall is allowed."; return false; }
        if (x < 0 || x >= 20 || y < 0 || y >= 20) { message = "Choose a tile inside the village."; return false; }
        if (Occupied(x, y)) { message = "That tile is occupied."; return false; }
        if (!TrySpend(def.Currency, def.BaseCost)) { message = $"Not enough {def.Currency}."; return false; }
        SaveData.Buildings.Add(new PlacedBuilding { DefinitionId = def.Id, X = x, Y = y, Level = 1 });
        Save();
        message = $"{def.Name} constructed.";
        return true;
    }

    public int UpgradeCost(PlacedBuilding building)
    {
        var def = Definition(building.DefinitionId)!;
        int baseCost = Math.Max(250, def.BaseCost);
        return (int)Math.Round(baseCost * (0.85 + building.Level * 0.85));
    }

    public bool TryUpgrade(PlacedBuilding building, out string message)
    {
        var def = Definition(building.DefinitionId)!;
        if (building.Level >= def.MaxLevel) { message = "Maximum level reached."; return false; }
        int cost = UpgradeCost(building);
        if (!TrySpend(def.Currency, cost)) { message = $"Need {cost:N0} {def.Currency}."; return false; }
        building.Level++;
        Save();
        message = $"{def.Name} upgraded to level {building.Level}.";
        return true;
    }

    public bool TryRecruit(UnitDefinition unit, out string message)
    {
        bool hasBarracks = SaveData.Buildings.Any(b => b.DefinitionId == "barracks");
        if (!hasBarracks) { message = "Build a Barracks first."; return false; }
        if (!TrySpend(unit.Currency, unit.Cost)) { message = $"Not enough {unit.Currency}."; return false; }
        SaveData.Units[unit.Id] = SaveData.Units.GetValueOrDefault(unit.Id) + 1;
        Save();
        message = $"{unit.Name} recruited.";
        return true;
    }

    public bool TryCreateClan(string name, string tag, out string message)
    {
        if (SaveData.Clan is not null) { message = "You already belong to a clan."; return false; }
        name = name.Trim(); tag = tag.Trim().ToUpperInvariant();
        if (name.Length is < 3 or > 20) { message = "Clan name must be 3-20 characters."; return false; }
        if (tag.Length is < 2 or > 6) { message = "Clan tag must be 2-6 characters."; return false; }
        const int fee = 1000;
        if (!TrySpend(Currency.Gold, fee)) { message = "Creating a clan costs 1,000 Gold."; return false; }
        SaveData.Clan = new ClanInfo { Name = name, Tag = tag };
        Save();
        message = $"Clan {name} created!";
        return true;
    }

    private bool TrySpend(Currency currency, int amount)
    {
        if (amount <= 0) return true;
        switch (currency)
        {
            case Currency.Gold when SaveData.Gold >= amount: SaveData.Gold -= amount; return true;
            case Currency.Elixir when SaveData.Elixir >= amount: SaveData.Elixir -= amount; return true;
            case Currency.Gems when SaveData.Gems >= amount: SaveData.Gems -= amount; return true;
            default: return false;
        }
    }

    public void Save()
    {
        try
        {
            SaveData.LastSavedUtc = DateTime.UtcNow;
            File.WriteAllText(_savePath, JsonSerializer.Serialize(SaveData, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }
}

internal static class Theme
{
    private static PrivateFontCollection? _privateFonts;
    public static FontFamily Family { get; private set; } = FontFamily.GenericSansSerif;

    public static readonly Color Background = Color.FromArgb(20, 34, 42);
    public static readonly Color Panel = Color.FromArgb(33, 48, 58);
    public static readonly Color Panel2 = Color.FromArgb(49, 66, 74);
    public static readonly Color Gold = Color.FromArgb(248, 196, 52);
    public static readonly Color Elixir = Color.FromArgb(219, 84, 216);
    public static readonly Color Gems = Color.FromArgb(52, 218, 152);
    public static readonly Color Text = Color.FromArgb(248, 244, 225);

    public static void Initialize()
    {
        string custom = Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts", "Supercell-Magic_5.ttf");
        if (File.Exists(custom))
        {
            try
            {
                _privateFonts = new PrivateFontCollection();
                _privateFonts.AddFontFile(custom);
                if (_privateFonts.Families.Length > 0) { Family = _privateFonts.Families[0]; return; }
            }
            catch { }
        }
        try { Family = new FontFamily("Segoe UI Black"); }
        catch { Family = FontFamily.GenericSansSerif; }
    }

    public static Font Font(float px, FontStyle style = FontStyle.Regular) => new(Family, px, style, GraphicsUnit.Pixel);

    public static void RoundedRect(Graphics g, Brush brush, RectangleF rect, float radius)
    {
        using var path = RoundedPath(rect, radius);
        g.FillPath(brush, path);
    }

    public static void RoundedOutline(Graphics g, Pen pen, RectangleF rect, float radius)
    {
        using var path = RoundedPath(rect, radius);
        g.DrawPath(pen, path);
    }

    private static GraphicsPath RoundedPath(RectangleF r, float radius)
    {
        float d = radius * 2;
        var p = new GraphicsPath();
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    public static string CurrencySymbol(Currency currency) => currency switch
    {
        Currency.Gold => "G",
        Currency.Elixir => "E",
        Currency.Gems => "D",
        _ => "$"
    };

    public static Color CurrencyColor(Currency currency) => currency switch
    {
        Currency.Gold => Gold,
        Currency.Elixir => Elixir,
        Currency.Gems => Gems,
        _ => Text
    };
}

internal static class SoundBank
{
    private static string _dir = "";
    private static readonly Dictionary<string, string> Files = new();

    public static void Initialize()
    {
        try
        {
            _dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IrAutoX", "ClashOfDrayven", "audio");
            Directory.CreateDirectory(_dir);
            EnsureTone("click", 510, 0.055, 0.22, false);
            EnsureTone("build", 145, 0.15, 0.33, true);
            EnsureTone("coin", 820, 0.11, 0.22, false);
        }
        catch { }
    }

    private static void EnsureTone(string name, double freq, double seconds, double volume, bool noisy)
    {
        string path = Path.Combine(_dir, name + ".wav");
        Files[name] = path;
        if (File.Exists(path)) return;
        const int sampleRate = 22050;
        int count = Math.Max(1, (int)(sampleRate * seconds));
        using var fs = File.Create(path);
        using var bw = new BinaryWriter(fs);
        int dataBytes = count * 2;
        bw.Write("RIFF"u8.ToArray()); bw.Write(36 + dataBytes); bw.Write("WAVE"u8.ToArray());
        bw.Write("fmt "u8.ToArray()); bw.Write(16); bw.Write((short)1); bw.Write((short)1); bw.Write(sampleRate); bw.Write(sampleRate * 2); bw.Write((short)2); bw.Write((short)16);
        bw.Write("data"u8.ToArray()); bw.Write(dataBytes);
        var random = new Random(713 + name.Length);
        for (int i = 0; i < count; i++)
        {
            double t = i / (double)sampleRate;
            double env = Math.Pow(1.0 - i / (double)count, 1.7);
            double wave = Math.Sin(Math.PI * 2 * freq * t) + 0.23 * Math.Sin(Math.PI * 4 * freq * t);
            if (noisy) wave += (random.NextDouble() * 2 - 1) * 0.28;
            short sample = (short)Math.Clamp(wave * env * volume * short.MaxValue, short.MinValue, short.MaxValue);
            bw.Write(sample);
        }
    }

    public static void Play(string name)
    {
        try { if (Files.TryGetValue(name, out var path) && File.Exists(path)) new SoundPlayer(path).Play(); }
        catch { }
    }
}

internal sealed class SplashForm : Form
{
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 1750 };
    private float _progress;
    private readonly System.Windows.Forms.Timer _progressTimer = new() { Interval = 28 };

    public SplashForm()
    {
        DoubleBuffered = true;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(960, 540);
        BackColor = Theme.Background;
        _timer.Tick += (_, _) => { _timer.Stop(); Close(); };
        _progressTimer.Tick += (_, _) => { _progress = Math.Min(1f, _progress + .018f); Invalidate(); };
        Shown += (_, _) => { _timer.Start(); _progressTimer.Start(); };
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var bg = new LinearGradientBrush(ClientRectangle, Color.FromArgb(12, 31, 43), Color.FromArgb(37, 79, 76), 35f);
        g.FillRectangle(bg, ClientRectangle);
        for (int i = 0; i < 12; i++)
        {
            int x = 70 + (i * 113) % 860;
            int y = 75 + (i * 67) % 360;
            using var b = new SolidBrush(Color.FromArgb(13, 255, 255, 255));
            g.FillEllipse(b, x, y, 8 + i % 3 * 4, 8 + i % 3 * 4);
        }
        using var irFont = Theme.Font(74);
        using var subFont = Theme.Font(26);
        using var smallFont = Theme.Font(15);
        DrawCentered(g, "IrAutoX", irFont, Brushes.White, new RectangleF(0, 180, Width, 100));
        using var gold = new SolidBrush(Theme.Gold);
        DrawCentered(g, "CLASH OF DRAYVEN", subFont, gold, new RectangleF(0, 270, Width, 52));
        using var soft = new SolidBrush(Color.FromArgb(210, 235, 235, 230));
        DrawCentered(g, "Forging your stronghold...", smallFont, soft, new RectangleF(0, 345, Width, 35));
        var bar = new RectangleF(215, 410, 530, 16);
        using var barBg = new SolidBrush(Color.FromArgb(80, 255, 255, 255));
        Theme.RoundedRect(g, barBg, bar, 8);
        Theme.RoundedRect(g, gold, new RectangleF(bar.X, bar.Y, bar.Width * _progress, bar.Height), 8);
    }

    private static void DrawCentered(Graphics g, string text, Font font, Brush brush, RectangleF rect)
    {
        var size = g.MeasureString(text, font);
        g.DrawString(text, font, brush, rect.X + (rect.Width - size.Width) / 2, rect.Y + (rect.Height - size.Height) / 2);
    }
}

internal sealed class GameForm : Form
{
    private const int GridSize = 20;
    private const float TileW = 62f;
    private const float TileH = 31f;
    private readonly GameState _state;
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 1000 };
    private readonly Dictionary<string, Image> _textures = new();
    private BuildingDefinition? _buildMode;
    private PlacedBuilding? _selected;
    private string _toast = "Welcome, Chief. Build your first defenses.";
    private DateTime _toastUntil = DateTime.UtcNow.AddSeconds(5);
    private RectangleF _upgradeButton;
    private RectangleF _clanButton;
    private readonly List<(RectangleF Rect, BuildingDefinition Def)> _buildCards = new();
    private readonly List<(RectangleF Rect, UnitDefinition Unit)> _unitCards = new();

    public GameForm(GameState state)
    {
        _state = state;
        Text = "Clash Of Drayven - IrAutoX";
        ClientSize = new Size(1380, 820);
        MinimumSize = new Size(1120, 700);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Theme.Background;
        DoubleBuffered = true;
        KeyPreview = true;
        LoadTextures();
        _timer.Tick += (_, _) => { _state.TickProduction(); Invalidate(); };
        _timer.Start();
        MouseDown += OnGameMouseDown;
        KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) { _buildMode = null; _selected = null; Toast("Selection cleared."); } };
        FormClosing += (_, _) => _state.Save();
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
            foreach (var image in _textures.Values) image.Dispose();
        }
        base.Dispose(disposing);
    }

    private void LoadTextures()
    {
        string dir = Path.Combine(AppContext.BaseDirectory, "Assets", "External");
        if (!Directory.Exists(dir)) return;
        foreach (string id in GameState.BuildingCatalog.Select(x => x.Id))
        {
            string? file = Directory.EnumerateFiles(dir).FirstOrDefault(p => Path.GetFileNameWithoutExtension(p).Equals(id, StringComparison.OrdinalIgnoreCase));
            if (file is null) continue;
            try { _textures[id] = Image.FromFile(file); } catch { }
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        using var sky = new LinearGradientBrush(ClientRectangle, Color.FromArgb(113, 179, 197), Color.FromArgb(31, 73, 72), 90f);
        g.FillRectangle(sky, ClientRectangle);
        DrawVillage(g);
        DrawHud(g);
        DrawBottomPanel(g);
        DrawRightPanel(g);
        DrawToast(g);
        if (_buildMode is not null) DrawBuildHint(g);
    }

    private (float X, float Y) Origin => (Math.Max(420, ClientSize.Width * .51f), 125f);

    private PointF TileToScreen(int x, int y)
    {
        var (ox, oy) = Origin;
        return new PointF(ox + (x - y) * TileW / 2f, oy + (x + y) * TileH / 2f);
    }

    private (int X, int Y) ScreenToTile(Point p)
    {
        var (ox, oy) = Origin;
        float sx = p.X - ox;
        float sy = p.Y - oy;
        float x = (sy / (TileH / 2f) + sx / (TileW / 2f)) / 2f;
        float y = (sy / (TileH / 2f) - sx / (TileW / 2f)) / 2f;
        return ((int)Math.Round(x), (int)Math.Round(y));
    }

    private void DrawVillage(Graphics g)
    {
        for (int sum = 0; sum <= (GridSize - 1) * 2; sum++)
        {
            for (int x = 0; x < GridSize; x++)
            {
                int y = sum - x;
                if (y < 0 || y >= GridSize) continue;
                var c = TileToScreen(x, y);
                var pts = new[]
                {
                    new PointF(c.X, c.Y - TileH/2), new PointF(c.X + TileW/2, c.Y),
                    new PointF(c.X, c.Y + TileH/2), new PointF(c.X - TileW/2, c.Y)
                };
                bool dark = (x + y) % 2 == 0;
                using var grass = new SolidBrush(dark ? Color.FromArgb(91,157,73) : Color.FromArgb(99,169,79));
                g.FillPolygon(grass, pts);
                using var line = new Pen(Color.FromArgb(45,74,56), 1f);
                g.DrawPolygon(line, pts);
            }
        }

        foreach (var b in _state.SaveData.Buildings.OrderBy(b => b.X + b.Y).ThenBy(b => b.Y))
            DrawBuilding(g, b, ReferenceEquals(b, _selected));
    }

    private void DrawBuilding(Graphics g, PlacedBuilding building, bool selected)
    {
        var def = _state.Definition(building.DefinitionId);
        if (def is null) return;
        var c = TileToScreen(building.X, building.Y);
        if (selected)
        {
            using var sel = new Pen(Color.White, 3);
            g.DrawEllipse(sel, c.X - 32, c.Y - 17, 64, 34);
        }
        using var shadow = new SolidBrush(Color.FromArgb(70, 0, 0, 0));
        g.FillEllipse(shadow, c.X - 28, c.Y - 7, 58, 25);

        if (_textures.TryGetValue(def.Id, out var image))
        {
            float maxH = def.Id == "wall" ? 44 : 78;
            float scale = Math.Min(80f / image.Width, maxH / image.Height);
            float w = image.Width * scale;
            float h = image.Height * scale;
            g.DrawImage(image, c.X - w / 2, c.Y - h + 8, w, h);
        }
        else
        {
            DrawProceduralBuilding(g, def, building.Level, c);
        }

        using var levelFont = Theme.Font(11);
        var badge = new RectangleF(c.X + 16, c.Y - 61, 31, 20);
        using var badgeBrush = new SolidBrush(Color.FromArgb(220, 30, 42, 48));
        Theme.RoundedRect(g, badgeBrush, badge, 8);
        g.DrawString("L" + building.Level, levelFont, Brushes.White, badge.X + 5, badge.Y + 3);
    }

    private static void DrawProceduralBuilding(Graphics g, BuildingDefinition def, int level, PointF c)
    {
        if (def.Id == "wall")
        {
            using var b = new SolidBrush(def.Body);
            g.FillPolygon(b, new[] { new PointF(c.X-25,c.Y-17), new PointF(c.X,c.Y-29), new PointF(c.X+25,c.Y-17), new PointF(c.X,c.Y-4) });
            using var p = new Pen(Color.FromArgb(75,55,45), 2);
            g.DrawPolygon(p, new[] { new PointF(c.X-25,c.Y-17), new PointF(c.X,c.Y-29), new PointF(c.X+25,c.Y-17), new PointF(c.X,c.Y-4) });
            return;
        }
        if (def.Id == "cannon")
        {
            using var baseBrush = new SolidBrush(Color.FromArgb(112,96,78));
            g.FillEllipse(baseBrush, c.X - 24, c.Y - 24, 48, 25);
            using var metal = new SolidBrush(def.Body);
            g.FillRectangle(metal, c.X - 7, c.Y - 46, 14, 34);
            g.FillEllipse(Brushes.Black, c.X - 10, c.Y - 51, 20, 12);
            return;
        }
        float h = 36 + Math.Min(16, level * 2);
        using var body = new SolidBrush(def.Body);
        using var roof = new SolidBrush(def.Roof);
        g.FillRectangle(body, c.X - 25, c.Y - h, 50, h - 5);
        g.FillPolygon(roof, new[] { new PointF(c.X - 32,c.Y-h+4), new PointF(c.X,c.Y-h-22), new PointF(c.X+32,c.Y-h+4) });
        using var edge = new Pen(Color.FromArgb(95,55,40), 2);
        g.DrawRectangle(edge, c.X - 25, c.Y - h, 50, h - 5);
        if (def.Id == "goldmine")
        {
            using var gold = new SolidBrush(Theme.Gold);
            g.FillEllipse(gold, c.X - 13, c.Y - h + 12, 26, 20);
        }
        else if (def.Id == "elixircollector")
        {
            using var elixir = new SolidBrush(Theme.Elixir);
            g.FillEllipse(elixir, c.X - 12, c.Y - h + 9, 24, 24);
        }
        else if (def.Id == "barracks")
        {
            using var flag = new SolidBrush(Color.FromArgb(226,84,68));
            g.FillRectangle(Brushes.DimGray, c.X + 18, c.Y - h - 30, 3, 35);
            g.FillPolygon(flag, new[] { new PointF(c.X+21,c.Y-h-29), new PointF(c.X+43,c.Y-h-21), new PointF(c.X+21,c.Y-h-14) });
        }
    }

    private void DrawHud(Graphics g)
    {
        using var titleFont = Theme.Font(24);
        using var smallFont = Theme.Font(14);
        var titleBox = new RectangleF(22, 20, 285, 70);
        using var titleBg = new SolidBrush(Color.FromArgb(215, 25, 40, 48));
        Theme.RoundedRect(g, titleBg, titleBox, 18);
        g.DrawString("CLASH OF DRAYVEN", titleFont, Brushes.White, 38, 31);
        using var subtitle = new SolidBrush(Color.FromArgb(210,230,230,224));
        g.DrawString("IrAutoX stronghold", smallFont, subtitle, 40, 62);

        float chipX = Math.Max(330, Width - 680);
        DrawResourceChip(g, new RectangleF(chipX, 20, 190, 54), Currency.Gold, _state.SaveData.Gold);
        DrawResourceChip(g, new RectangleF(chipX + 198, 20, 190, 54), Currency.Elixir, _state.SaveData.Elixir);
        DrawResourceChip(g, new RectangleF(chipX + 396, 20, 155, 54), Currency.Gems, _state.SaveData.Gems);

        _clanButton = new RectangleF(22, 104, 215, 52);
        using var clanBg = new SolidBrush(Color.FromArgb(215, 55, 70, 82));
        Theme.RoundedRect(g, clanBg, _clanButton, 14);
        using var clanFont = Theme.Font(16);
        string clanText = _state.SaveData.Clan is null ? "CREATE CLAN" : $"{_state.SaveData.Clan.Name}  [{_state.SaveData.Clan.Tag}]";
        g.DrawString(clanText, clanFont, Brushes.White, _clanButton.X + 15, _clanButton.Y + 16);
    }

    private static void DrawResourceChip(Graphics g, RectangleF rect, Currency currency, int amount)
    {
        using var chipBg = new SolidBrush(Color.FromArgb(225, 31, 45, 54));
        Theme.RoundedRect(g, chipBg, rect, 16);
        var circle = new RectangleF(rect.X + 8, rect.Y + 8, 38, 38);
        using var cb = new SolidBrush(Theme.CurrencyColor(currency));
        g.FillEllipse(cb, circle);
        using var symbolFont = Theme.Font(19);
        using var valueFont = Theme.Font(17);
        var symbol = Theme.CurrencySymbol(currency);
        var ss = g.MeasureString(symbol, symbolFont);
        g.DrawString(symbol, symbolFont, Brushes.White, circle.X + (circle.Width - ss.Width)/2, circle.Y + 7);
        g.DrawString(amount.ToString("N0"), valueFont, Brushes.White, rect.X + 55, rect.Y + 17);
    }

    private void DrawBottomPanel(Graphics g)
    {
        float panelH = 172;
        var panel = new RectangleF(0, Height - panelH, Width, panelH);
        using var panelBrush = new SolidBrush(Color.FromArgb(238, 23, 34, 40));
        g.FillRectangle(panelBrush, panel);
        using var labelFont = Theme.Font(14);
        using var labelBrush = new SolidBrush(Color.FromArgb(215,235,235,225));
        g.DrawString("BUILD", labelFont, labelBrush, 18, panel.Y + 10);
        _buildCards.Clear();
        float x = 18;
        float y = panel.Y + 36;
        foreach (var def in GameState.BuildingCatalog.Where(d => d.Id != "townhall"))
        {
            var rect = new RectangleF(x, y, 150, 116);
            bool active = _buildMode?.Id == def.Id;
            DrawBuildCard(g, rect, def, active);
            _buildCards.Add((rect, def));
            x += 158;
        }
    }

    private static void DrawBuildCard(Graphics g, RectangleF rect, BuildingDefinition def, bool active)
    {
        using var bg = new SolidBrush(active ? Color.FromArgb(80,133,101) : Color.FromArgb(53,68,76));
        Theme.RoundedRect(g, bg, rect, 14);
        using var outline = new Pen(active ? Theme.Gold : Color.FromArgb(78,92,100), active ? 3 : 1);
        Theme.RoundedOutline(g, outline, rect, 14);
        using var icon = new SolidBrush(def.Body);
        g.FillEllipse(icon, rect.X + 12, rect.Y + 12, 42, 42);
        using var nameFont = Theme.Font(13);
        using var costFont = Theme.Font(12);
        g.DrawString(def.Name, nameFont, Brushes.White, rect.X + 10, rect.Y + 62);
        using var c = new SolidBrush(Theme.CurrencyColor(def.Currency));
        g.FillEllipse(c, rect.X + 11, rect.Y + 91, 18, 18);
        g.DrawString(def.BaseCost.ToString("N0"), costFont, Brushes.White, rect.X + 34, rect.Y + 91);
    }

    private void DrawRightPanel(Graphics g)
    {
        float w = 260;
        float x = Width - w - 18;
        float y = 92;
        float h = Height - 285;
        if (h < 300) return;
        var rect = new RectangleF(x, y, w, h);
        using var panel = new SolidBrush(Color.FromArgb(224, 29, 42, 49));
        Theme.RoundedRect(g, panel, rect, 18);
        using var headerFont = Theme.Font(19);
        using var bodyFont = Theme.Font(13);
        using var smallFont = Theme.Font(12);
        if (_selected is not null)
        {
            var def = _state.Definition(_selected.DefinitionId)!;
            g.DrawString(def.Name, headerFont, Brushes.White, x + 18, y + 20);
            using var gold = new SolidBrush(Theme.Gold);
            g.DrawString($"Level {_selected.Level} / {def.MaxLevel}", bodyFont, gold, x + 18, y + 52);
            using var desc = new SolidBrush(Color.FromArgb(220,230,229,219));
            DrawWrapped(g, def.Description, bodyFont, desc, new RectangleF(x+18,y+82,w-36,60));
            int cost = _state.UpgradeCost(_selected);
            _upgradeButton = new RectangleF(x + 18, y + 148, w - 36, 54);
            using var upBg = new SolidBrush(_selected.Level < def.MaxLevel ? Color.FromArgb(68,151,81) : Color.FromArgb(82,88,91));
            Theme.RoundedRect(g, upBg, _upgradeButton, 14);
            string up = _selected.Level < def.MaxLevel ? $"UPGRADE  {cost:N0} {Theme.CurrencySymbol(def.Currency)}" : "MAX LEVEL";
            g.DrawString(up, bodyFont, Brushes.White, _upgradeButton.X + 16, _upgradeButton.Y + 17);
            using var tip = new SolidBrush(Color.FromArgb(170,225,225,216));
            g.DrawString("Tip: press ESC to clear selection.", smallFont, tip, x + 18, y + 216);
            _unitCards.Clear();
        }
        else
        {
            _upgradeButton = RectangleF.Empty;
            g.DrawString("ARMY", headerFont, Brushes.White, x + 18, y + 20);
            using var hint = new SolidBrush(Color.FromArgb(200,229,229,219));
            g.DrawString("Recruit units after building a Barracks.", smallFont, hint, x + 18, y + 53);
            _unitCards.Clear();
            float uy = y + 88;
            foreach (var unit in GameState.UnitCatalog)
            {
                var urect = new RectangleF(x + 14, uy, w - 28, 70);
                DrawUnitCard(g, urect, unit);
                _unitCards.Add((urect, unit));
                uy += 78;
                if (uy > rect.Bottom - 70) break;
            }
        }
    }

    private void DrawUnitCard(Graphics g, RectangleF rect, UnitDefinition unit)
    {
        using var bg = new SolidBrush(Color.FromArgb(58,73,82));
        Theme.RoundedRect(g, bg, rect, 13);
        using var body = new SolidBrush(unit.Body);
        g.FillEllipse(body, rect.X + 10, rect.Y + 10, 48, 48);
        using var accent = new Pen(unit.Accent, 4);
        g.DrawArc(accent, rect.X + 16, rect.Y + 16, 36, 36, 195, 155);
        using var nameFont = Theme.Font(13);
        using var small = Theme.Font(11);
        g.DrawString(unit.Name, nameFont, Brushes.White, rect.X + 67, rect.Y + 10);
        int count = _state.SaveData.Units.GetValueOrDefault(unit.Id);
        using var currency = new SolidBrush(Theme.CurrencyColor(unit.Currency));
        g.DrawString($"Owned {count}   •   {unit.Cost:N0} {Theme.CurrencySymbol(unit.Currency)}", small, currency, rect.X + 67, rect.Y + 37);
    }

    private void DrawToast(Graphics g)
    {
        if (DateTime.UtcNow > _toastUntil) return;
        using var font = Theme.Font(14);
        var size = g.MeasureString(_toast, font);
        var rect = new RectangleF((Width - size.Width - 42)/2, 88, size.Width + 42, 48);
        using var bg = new SolidBrush(Color.FromArgb(228, 17, 27, 32));
        Theme.RoundedRect(g, bg, rect, 16);
        g.DrawString(_toast, font, Brushes.White, rect.X + 21, rect.Y + 15);
    }

    private void DrawBuildHint(Graphics g)
    {
        using var font = Theme.Font(13);
        string text = $"Place {_buildMode!.Name}: click an empty grass tile • ESC cancels";
        var size = g.MeasureString(text, font);
        var rect = new RectangleF((Width-size.Width-30)/2, Height-213, size.Width+30, 38);
        using var bg = new SolidBrush(Color.FromArgb(230, 45, 78, 58));
        Theme.RoundedRect(g, bg, rect, 12);
        g.DrawString(text, font, Brushes.White, rect.X+15, rect.Y+11);
    }

    private static void DrawWrapped(Graphics g, string text, Font font, Brush brush, RectangleF rect)
    {
        using var format = new StringFormat { Trimming = StringTrimming.EllipsisWord };
        g.DrawString(text, font, brush, rect, format);
    }

    private void OnGameMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        if (_clanButton.Contains(e.Location))
        {
            SoundBank.Play("click");
            using var dialog = new ClanDialog(_state);
            dialog.ShowDialog(this);
            Toast(_state.SaveData.Clan is null ? "Clan creation menu closed." : $"Clan {_state.SaveData.Clan.Name} ready.");
            Invalidate();
            return;
        }

        foreach (var card in _buildCards)
        {
            if (!card.Rect.Contains(e.Location)) continue;
            SoundBank.Play("click");
            _buildMode = card.Def;
            _selected = null;
            Toast($"Choose a tile for {card.Def.Name}.");
            Invalidate();
            return;
        }

        foreach (var card in _unitCards)
        {
            if (!card.Rect.Contains(e.Location)) continue;
            if (_state.TryRecruit(card.Unit, out string msg)) SoundBank.Play("coin"); else SoundBank.Play("click");
            Toast(msg);
            Invalidate();
            return;
        }

        if (!_upgradeButton.IsEmpty && _upgradeButton.Contains(e.Location) && _selected is not null)
        {
            if (_state.TryUpgrade(_selected, out string msg)) SoundBank.Play("build"); else SoundBank.Play("click");
            Toast(msg);
            Invalidate();
            return;
        }

        var tile = ScreenToTile(e.Location);
        if (tile.X >= 0 && tile.X < GridSize && tile.Y >= 0 && tile.Y < GridSize)
        {
            if (_buildMode is not null)
            {
                if (_state.TryBuild(_buildMode, tile.X, tile.Y, out string msg)) { SoundBank.Play("build"); _buildMode = null; }
                else SoundBank.Play("click");
                Toast(msg);
                Invalidate();
                return;
            }
            var found = _state.SaveData.Buildings.OrderByDescending(b => b.X + b.Y).FirstOrDefault(b => b.X == tile.X && b.Y == tile.Y);
            _selected = found;
            if (found is not null) SoundBank.Play("click");
            Invalidate();
        }
    }

    private void Toast(string text)
    {
        _toast = text;
        _toastUntil = DateTime.UtcNow.AddSeconds(3.5);
        Invalidate();
    }
}

internal sealed class ClanDialog : Form
{
    private readonly GameState _state;
    private readonly TextBox _name = new();
    private readonly TextBox _tag = new();
    private readonly Label _status = new();

    public ClanDialog(GameState state)
    {
        _state = state;
        Text = "Clan Hall";
        ClientSize = new Size(520, 350);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Theme.Panel;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BuildUi();
    }

    private void BuildUi()
    {
        var title = new Label { Text = "CLAN HALL", ForeColor = Color.White, Font = Theme.Font(27), AutoSize = true, Left = 28, Top = 24 };
        Controls.Add(title);
        if (_state.SaveData.Clan is not null)
        {
            var c = _state.SaveData.Clan;
            Controls.Add(new Label { Text = $"{c.Name}   [{c.Tag}]", ForeColor = Theme.Gold, Font = Theme.Font(19), AutoSize = true, Left = 30, Top = 100 });
            Controls.Add(new Label { Text = "Simple clan foundation is active. Members and clan wars come in a later milestone.", ForeColor = Theme.Text, Font = Theme.Font(12), AutoSize = false, Left = 30, Top = 150, Width = 450, Height = 80 });
            return;
        }

        AddLabel("Clan name", 30, 92);
        ConfigureBox(_name, 30, 118, 300);
        AddLabel("Tag", 350, 92);
        ConfigureBox(_tag, 350, 118, 130);
        _name.MaxLength = 20; _tag.MaxLength = 6;

        var fee = new Label { Text = "Creation fee: 1,000 Gold", ForeColor = Theme.Gold, Font = Theme.Font(12), AutoSize = true, Left = 30, Top = 180 };
        Controls.Add(fee);
        var create = new Button { Text = "CREATE CLAN", Left = 30, Top = 220, Width = 220, Height = 58, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(68,151,81), ForeColor = Color.White, Font = Theme.Font(14) };
        create.FlatAppearance.BorderSize = 0;
        create.Click += (_, _) =>
        {
            SoundBank.Play("click");
            if (_state.TryCreateClan(_name.Text, _tag.Text, out string msg)) { _status.ForeColor = Theme.Gems; _status.Text = msg; SoundBank.Play("coin"); }
            else { _status.ForeColor = Color.FromArgb(255,145,120); _status.Text = msg; }
        };
        Controls.Add(create);
        _status.AutoSize = false; _status.Left = 270; _status.Top = 220; _status.Width = 210; _status.Height = 80; _status.Font = Theme.Font(11); _status.ForeColor = Theme.Text;
        Controls.Add(_status);
    }

    private void AddLabel(string text, int x, int y) => Controls.Add(new Label { Text = text, ForeColor = Theme.Text, Font = Theme.Font(12), AutoSize = true, Left = x, Top = y });

    private static void ConfigureBox(TextBox box, int x, int y, int width)
    {
        box.Left = x; box.Top = y; box.Width = width; box.Height = 38; box.Font = Theme.Font(13); box.BackColor = Color.FromArgb(64,78,86); box.ForeColor = Color.White; box.BorderStyle = BorderStyle.FixedSingle;
    }
}
