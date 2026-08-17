using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClashOfDrayven
{
    internal sealed class DrySplashForm : Form
    {
        private readonly Timer _timer = new Timer { Interval = 24 };
        private float _p;
        private readonly Image _hero;

        public DrySplashForm()
        {
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1000, 560);
            BackColor = DryTheme.Night;
            _hero = AssetLibrary.Find("town", "hall") ?? AssetLibrary.Find("castle") ?? AssetLibrary.Find("building");
            _timer.Tick += delegate
            {
                _p += 0.013f;
                if (_p >= 1f) { _timer.Stop(); Close(); }
                Invalidate();
            };
            Shown += delegate { _timer.Start(); };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var bg = new LinearGradientBrush(ClientRectangle, Color.FromArgb(10, 28, 37), Color.FromArgb(42, 83, 70), 22f))
                g.FillRectangle(bg, ClientRectangle);
            for (int i = 0; i < 16; i++)
            {
                var a = 18 + i * 2;
                using (var b = new SolidBrush(Color.FromArgb(a, 255, 222, 115)))
                    g.FillEllipse(b, 35 + (i * 79) % 930, 65 + (i * 113) % 390, 4 + i % 4, 4 + i % 4);
            }
            if (_hero != null)
            {
                var scale = System.Math.Min(300f / _hero.Width, 270f / _hero.Height);
                var w = _hero.Width * scale; var h = _hero.Height * scale;
                g.DrawImage(_hero, 690 - w / 2, 280 - h / 2, w, h);
            }
            using (var title = DryTheme.Font(62))
            using (var sub = DryTheme.Font(25))
            using (var small = DryTheme.Font(15))
            using (var gold = new SolidBrush(DryTheme.Gold))
            using (var soft = new SolidBrush(Color.FromArgb(210, 232, 235, 228)))
            {
                g.DrawString("CLASH OF", title, Brushes.White, 74, 130);
                g.DrawString("DRAYVEN", title, gold, 74, 205);
                g.DrawString("Build. Raid. Rise.", sub, Brushes.White, 80, 292);
                g.DrawString("IrAutoX Online Realm", small, soft, 82, 338);
            }
            var bar = new RectangleF(80, 455, 840, 15);
            using (var dark = new SolidBrush(Color.FromArgb(90, 255, 255, 255))) DryTheme.FillRound(g, dark, bar, 8);
            using (var gold = new SolidBrush(DryTheme.Gold)) DryTheme.FillRound(g, gold, new RectangleF(bar.X, bar.Y, bar.Width * _p, bar.Height), 8);
            using (var f = DryTheme.Font(12)) g.DrawString("Loading protected asset packs...", f, Brushes.White, 80, 482);
        }
    }

    internal sealed class AuthForm : Form
    {
        private readonly DrayvenApiClient _api;
        private readonly TextBox _username = new TextBox();
        private readonly TextBox _email = new TextBox();
        private readonly TextBox _password = new TextBox();
        private readonly Label _status = new Label();
        private readonly Button _primary = new Button();
        private readonly Button _loginTab = new Button();
        private readonly Button _registerTab = new Button();
        private bool _register;
        public AuthResult Result { get; private set; }

        public AuthForm(DrayvenApiClient api)
        {
            _api = api;
            Text = "Clash Of Drayven Account";
            ClientSize = new Size(940, 610);
            MinimumSize = MaximumSize = Size;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = DryTheme.Night;
            Font = DryTheme.Font(14);
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            var title = new Label { Text = "CLASH OF DRAYVEN", ForeColor = DryTheme.Text, Font = DryTheme.Font(32), AutoSize = true, Location = new Point(70, 60), BackColor = Color.Transparent };
            var subtitle = new Label { Text = "Your village lives on the Drayven realm.", ForeColor = Color.Gainsboro, Font = DryTheme.Font(15), AutoSize = true, Location = new Point(72, 110), BackColor = Color.Transparent };
            Controls.Add(title); Controls.Add(subtitle);

            _loginTab.Text = "LOGIN"; _registerTab.Text = "REGISTER";
            SetupButton(_loginTab, new Rectangle(560, 62, 135, 42), true);
            SetupButton(_registerTab, new Rectangle(705, 62, 160, 42), false);
            _loginTab.Click += delegate { SetMode(false); };
            _registerTab.Click += delegate { SetMode(true); };
            Controls.Add(_loginTab); Controls.Add(_registerTab);

            AddCaption("CHIEF NAME / EMAIL", 530, 160);
            SetupText(_username, 530, 186, false); Controls.Add(_username);
            AddCaption("EMAIL", 530, 258);
            SetupText(_email, 530, 284, false); Controls.Add(_email);
            AddCaption("PASSWORD", 530, 356);
            SetupText(_password, 530, 382, true); Controls.Add(_password);

            _primary.Text = "ENTER THE VILLAGE";
            SetupButton(_primary, new Rectangle(530, 462, 335, 54), true);
            _primary.Click += async delegate { await SubmitAsync(); };
            Controls.Add(_primary);

            _status.ForeColor = Color.FromArgb(255, 210, 110);
            _status.Font = DryTheme.Font(12);
            _status.Location = new Point(530, 530);
            _status.Size = new Size(345, 50);
            Controls.Add(_status);
            SetMode(false);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var b = new LinearGradientBrush(new Rectangle(0, 0, 470, Height), Color.FromArgb(47, 92, 77), Color.FromArgb(22, 52, 59), 90))
                g.FillRectangle(b, 0, 0, 470, Height);
            var image = AssetLibrary.Find("barbarian") ?? AssetLibrary.Find("troop") ?? AssetLibrary.Find("hero");
            if (image != null)
            {
                var scale = System.Math.Min(320f / image.Width, 350f / image.Height);
                g.DrawImage(image, 235 - image.Width * scale / 2, 345 - image.Height * scale / 2, image.Width * scale, image.Height * scale);
            }
            using (var f = DryTheme.Font(14)) using (var b = new SolidBrush(Color.FromArgb(210, 255, 255, 255)))
                g.DrawString("ONLINE  •  irautox.ir:8456", f, b, 72, 552);
        }

        private void AddCaption(string text, int x, int y)
        {
            var l = new Label { Text = text, ForeColor = Color.Gainsboro, Font = DryTheme.Font(11), AutoSize = true, Location = new Point(x, y) };
            Controls.Add(l);
        }

        private void SetupText(TextBox box, int x, int y, bool password)
        {
            box.Location = new Point(x, y); box.Size = new Size(335, 42); box.Font = DryTheme.Font(17); box.BackColor = Color.FromArgb(50, 62, 68); box.ForeColor = Color.White;
            box.BorderStyle = BorderStyle.FixedSingle; box.UseSystemPasswordChar = password;
        }

        private static void SetupButton(Button b, Rectangle bounds, bool primary)
        {
            b.Bounds = bounds; b.FlatStyle = FlatStyle.Flat; b.FlatAppearance.BorderSize = 0; b.Cursor = Cursors.Hand;
            b.BackColor = primary ? DryTheme.Gold : DryTheme.PanelLight; b.ForeColor = primary ? Color.FromArgb(65, 44, 15) : Color.White;
            b.Font = DryTheme.Font(13);
        }

        private void SetMode(bool register)
        {
            _register = register;
            _email.Visible = register;
            foreach (Control c in Controls)
                if (c is Label && c.Text == "EMAIL") c.Visible = register;
            _primary.Text = register ? "CREATE DRAYVEN ACCOUNT" : "ENTER THE VILLAGE";
            _loginTab.BackColor = register ? DryTheme.PanelLight : DryTheme.Gold;
            _loginTab.ForeColor = register ? Color.White : Color.FromArgb(65, 44, 15);
            _registerTab.BackColor = register ? DryTheme.Gold : DryTheme.PanelLight;
            _registerTab.ForeColor = register ? Color.FromArgb(65, 44, 15) : Color.White;
            _status.Text = register ? "New accounts start with 7,000 Gold, 7,000 Elixir and 250 Gems." : "Sign in to restore your village from the server.";
        }

        private async Task SubmitAsync()
        {
            _primary.Enabled = false; _status.Text = "Contacting Drayven realm...";
            try
            {
                var user = _username.Text.Trim(); var email = _email.Text.Trim(); var pass = _password.Text;
                Result = await Task.Run(delegate { return _register ? _api.Register(user, email, pass) : _api.Login(user, pass); });
                if (Result != null && Result.Ok)
                {
                    SessionStore.SaveToken(Result.Token);
                    DialogResult = DialogResult.OK;
                    Close();
                }
            }
            catch (Exception ex) { _status.Text = ex.Message; }
            finally { if (!IsDisposed) _primary.Enabled = true; }
        }
    }
}
