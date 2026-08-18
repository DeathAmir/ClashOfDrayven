using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClashOfDrayven
{
    internal sealed class DrySplashForm : Form
    {
        private readonly Timer _timer = new Timer { Interval = 24 };
        private readonly Image _brand;
        private DateTime _started;

        public DrySplashForm()
        {
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1000, 560);
            BackColor = Color.White;
            var path = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "IrAutoX-splash.png");
            try { if (System.IO.File.Exists(path)) _brand = Image.FromFile(path); } catch { }
            _timer.Tick += delegate
            {
                if ((DateTime.UtcNow - _started).TotalMilliseconds >= 4000) { _timer.Stop(); Close(); return; }
                Invalidate();
            };
            Shown += delegate { _started = DateTime.UtcNow; _timer.Start(); };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            var t = (DateTime.UtcNow - _started).TotalMilliseconds / 1000.0;
            var alpha = (float)(0.22 + 0.78 * (0.5 + 0.5 * System.Math.Cos(t * System.Math.PI * 2.0 / 1.30)));
            if (_brand != null)
            {
                var maxW = Width * .62f; var maxH = Height * .62f;
                var scale = System.Math.Min(maxW / _brand.Width, maxH / _brand.Height);
                var w = _brand.Width * scale; var h = _brand.Height * scale;
                var rect = new RectangleF((Width - w) / 2f, (Height - h) / 2f, w, h);
                using (var attrs = new ImageAttributes())
                {
                    var matrix = new ColorMatrix { Matrix33 = alpha };
                    attrs.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                    g.DrawImage(_brand, Rectangle.Round(rect), 0, 0, _brand.Width, _brand.Height, GraphicsUnit.Pixel, attrs);
                }
            }
            else
            {
                using (var f = DryTheme.Font(54))
                using (var b = new SolidBrush(Color.FromArgb((int)(alpha * 255), 20, 20, 20)))
                {
                    var s = "IrAutoX"; var z = g.MeasureString(s, f);
                    g.DrawString(s, f, b, (Width-z.Width)/2, (Height-z.Height)/2);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _timer.Dispose(); if (_brand != null) _brand.Dispose(); }
            base.Dispose(disposing);
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
            Text = "Clash Of Drayven";
            ClientSize = new Size(940, 610);
            MinimumSize = MaximumSize = Size;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = DryTheme.Night;
            Font = DryTheme.Font(14);
            RightToLeft = RightToLeft.Yes;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            var title = new Label { Text = "CLASH OF DRAYVEN", ForeColor = DryTheme.Text, Font = DryTheme.Font(32), AutoSize = true, Location = new Point(70, 60), BackColor = Color.Transparent, RightToLeft = RightToLeft.No };
            var subtitle = new Label { Text = "دهکده شما روی قلمرو Drayven ذخیره می‌شود.", ForeColor = Color.Gainsboro, Font = DryTheme.Font(15), AutoSize = true, Location = new Point(72, 110), BackColor = Color.Transparent };
            Controls.Add(title); Controls.Add(subtitle);

            _loginTab.Text = "ورود"; _registerTab.Text = "ثبت نام";
            SetupButton(_loginTab, new Rectangle(560, 62, 135, 42), true);
            SetupButton(_registerTab, new Rectangle(705, 62, 160, 42), false);
            _loginTab.Click += delegate { SetMode(false); };
            _registerTab.Click += delegate { SetMode(true); };
            Controls.Add(_loginTab); Controls.Add(_registerTab);

            AddCaption("نام فرمانده / ایمیل", 530, 160);
            SetupText(_username, 530, 186, false); Controls.Add(_username);
            AddCaption("ایمیل", 530, 258);
            SetupText(_email, 530, 284, false); Controls.Add(_email);
            AddCaption("رمز عبور", 530, 356);
            SetupText(_password, 530, 382, true); Controls.Add(_password);

            _primary.Text = "ورود به دهکده";
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
            using (var b = new LinearGradientBrush(new Rectangle(0, 0, 470, Height), Color.FromArgb(47, 92, 77), Color.FromArgb(22, 52, 59), 90)) g.FillRectangle(b, 0, 0, 470, Height);
            var image = AssetLibrary.Find("barbarian") ?? AssetLibrary.Find("troop") ?? AssetLibrary.Find("hero");
            if (image != null) { var scale = System.Math.Min(320f / image.Width, 350f / image.Height); g.DrawImage(image, 235 - image.Width * scale / 2, 345 - image.Height * scale / 2, image.Width * scale, image.Height * scale); }
            using (var f = DryTheme.Font(14)) using (var b = new SolidBrush(Color.FromArgb(210, 255, 255, 255))) g.DrawString("IrAutoX • Online", f, b, 72, 552);
        }

        private void AddCaption(string text, int x, int y) { var l = new Label { Text = text, ForeColor = Color.Gainsboro, Font = DryTheme.Font(11), AutoSize = true, Location = new Point(x, y), RightToLeft = RightToLeft.Yes }; Controls.Add(l); }
        private void SetupText(TextBox box, int x, int y, bool password) { box.Location = new Point(x, y); box.Size = new Size(335, 42); box.Font = DryTheme.Font(17); box.BackColor = Color.FromArgb(50, 62, 68); box.ForeColor = Color.White; box.BorderStyle = BorderStyle.FixedSingle; box.UseSystemPasswordChar = password; box.RightToLeft = RightToLeft.Yes; }
        private static void SetupButton(Button b, Rectangle bounds, bool primary) { b.Bounds = bounds; b.FlatStyle = FlatStyle.Flat; b.FlatAppearance.BorderSize = 0; b.Cursor = Cursors.Hand; b.BackColor = primary ? DryTheme.Gold : DryTheme.PanelLight; b.ForeColor = primary ? Color.FromArgb(65, 44, 15) : Color.White; b.Font = DryTheme.Font(13); }
        private void SetMode(bool register)
        {
            _register = register; _email.Visible = register;
            foreach (Control c in Controls) if (c is Label && c.Text == "ایمیل") c.Visible = register;
            _primary.Text = register ? "ساخت حساب Drayven" : "ورود به دهکده";
            _loginTab.BackColor = register ? DryTheme.PanelLight : DryTheme.Gold; _loginTab.ForeColor = register ? Color.White : Color.FromArgb(65, 44, 15);
            _registerTab.BackColor = register ? DryTheme.Gold : DryTheme.PanelLight; _registerTab.ForeColor = register ? Color.FromArgb(65, 44, 15) : Color.White;
            _status.Text = register ? "حساب جدید با ۷۰۰۰ طلا، ۷۰۰۰ اکسیر و ۲۵۰ جم شروع می‌شود." : "برای بازیابی دهکده وارد حساب خود شوید.";
        }
        private async Task SubmitAsync()
        {
            _primary.Enabled = false; _status.Text = "در حال اتصال به قلمرو Drayven...";
            try
            {
                var user = _username.Text.Trim(); var email = _email.Text.Trim(); var pass = _password.Text;
                Result = await Task.Run(delegate { return _register ? _api.Register(user, email, pass) : _api.Login(user, pass); });
                if (Result != null && Result.Ok) { SessionStore.SaveToken(Result.Token); DialogResult = DialogResult.OK; Close(); }
            }
            catch (Exception ex) { _status.Text = ex.Message; }
            finally { if (!IsDisposed) _primary.Enabled = true; }
        }
    }
}
