using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ClashOfDrayven
{
    internal static class AssetPackRuntime
    {
        private static readonly byte[] Magic = { 0x43, 0x4C, 0x44, 0x52, 0x59, 0x50, 0x4B, 0x1A };
        private const int Version = 1;
        public static string CacheDirectory { get; private set; }

        public static void Prepare()
        {
            var packsDir = Path.Combine(AppContext.BaseDirectory, "Assets");
            var packs = Enumerable.Range(1, 20).Select(i => Path.Combine(packsDir, "CLDRYPK" + i)).Where(File.Exists).ToArray();
            var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IrAutoX", "ClashOfDrayven", "asset-cache-v3");
            Directory.CreateDirectory(root);
            CacheDirectory = root;
            if (packs.Length == 0)
            {
                var fallback = Path.Combine(AppContext.BaseDirectory, "Assets", "External");
                if (Directory.Exists(fallback)) CacheDirectory = fallback;
                AssetLibrary.Refresh();
                return;
            }

            var stamp = Fingerprint(packs);
            var stampFile = Path.Combine(root, ".stamp");
            if (File.Exists(stampFile) && string.Equals(File.ReadAllText(stampFile), stamp, StringComparison.Ordinal))
            {
                AssetLibrary.Refresh();
                return;
            }

            try
            {
                foreach (var dir in Directory.GetDirectories(root)) Directory.Delete(dir, true);
                foreach (var file in Directory.GetFiles(root)) File.Delete(file);
                for (var i = 0; i < packs.Length; i++) Unpack(packs[i], root);
                File.WriteAllText(stampFile, stamp);
            }
            catch { }
            AssetLibrary.Refresh();
        }

        private static string Fingerprint(string[] packs)
        {
            using (var sha = SHA256.Create())
            {
                var text = string.Join("|", packs.Select(p => Path.GetFileName(p) + ":" + new FileInfo(p).Length));
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "").ToLowerInvariant();
            }
        }

        private static void Unpack(string inputFile, string outputDirectory)
        {
            var temp = Path.GetTempFileName();
            try
            {
                long expectedLength;
                byte[] expectedHash;
                using (var input = File.OpenRead(inputFile))
                using (var reader = new BinaryReader(input, Encoding.UTF8, true))
                {
                    if (!reader.ReadBytes(Magic.Length).SequenceEqual(Magic)) throw new InvalidDataException("Invalid CLDRYPK magic.");
                    if (reader.ReadInt32() != Version) throw new InvalidDataException("Unsupported CLDRYPK version.");
                    expectedLength = reader.ReadInt64();
                    expectedHash = reader.ReadBytes(reader.ReadInt32());
                    using (var deflate = new DeflateStream(input, CompressionMode.Decompress, true))
                    using (var output = File.Create(temp)) deflate.CopyTo(output);
                }
                if (new FileInfo(temp).Length != expectedLength) throw new InvalidDataException("CLDRYPK length mismatch.");
                using (var sha = SHA256.Create())
                using (var raw = File.OpenRead(temp))
                    if (!sha.ComputeHash(raw).SequenceEqual(expectedHash)) throw new InvalidDataException("CLDRYPK archive hash mismatch.");

                var root = Path.GetFullPath(outputDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                using (var raw = File.OpenRead(temp))
                using (var reader = new BinaryReader(raw, Encoding.UTF8, true))
                {
                    var count = reader.ReadInt32();
                    for (var i = 0; i < count; i++)
                    {
                        var relative = reader.ReadString().Replace('/', Path.DirectorySeparatorChar);
                        var length = reader.ReadInt64();
                        var hash = reader.ReadBytes(reader.ReadInt32());
                        var destination = Path.GetFullPath(Path.Combine(root, relative));
                        if (!destination.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Unsafe path in pack.");
                        Directory.CreateDirectory(Path.GetDirectoryName(destination));
                        using (var output = File.Create(destination))
                        {
                            var buffer = new byte[128 * 1024];
                            long remaining = length;
                            while (remaining > 0)
                            {
                                var take = (int)System.Math.Min(buffer.Length, remaining);
                                var read = raw.Read(buffer, 0, take);
                                if (read <= 0) throw new EndOfStreamException();
                                output.Write(buffer, 0, read);
                                remaining -= read;
                            }
                        }
                        using (var sha = SHA256.Create())
                        using (var verify = File.OpenRead(destination))
                            if (!sha.ComputeHash(verify).SequenceEqual(hash)) throw new InvalidDataException("CLDRYPK file hash mismatch: " + relative);
                    }
                }
            }
            finally { try { File.Delete(temp); } catch { } }
        }
    }

    internal static class AssetLibrary
    {
        private static readonly object Gate = new object();
        private static string[] _files = new string[0];
        private static readonly Dictionary<string, Image> Images = new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);

        public static void Refresh()
        {
            lock (Gate)
            {
                foreach (var image in Images.Values) image.Dispose();
                Images.Clear();
                var root = AssetPackRuntime.CacheDirectory;
                _files = !string.IsNullOrWhiteSpace(root) && Directory.Exists(root)
                    ? Directory.GetFiles(root, "*", SearchOption.AllDirectories).Where(IsImage).ToArray()
                    : new string[0];
            }
        }

        public static Image Find(params string[] keywords)
        {
            var key = string.Join("|", keywords ?? new string[0]).ToLowerInvariant();
            lock (Gate)
            {
                Image existing;
                if (Images.TryGetValue(key, out existing)) return existing;
                var terms = (keywords ?? new string[0]).Where(s => !string.IsNullOrWhiteSpace(s)).Select(Norm).ToArray();
                string best = null;
                int bestScore = int.MinValue;
                foreach (var file in _files)
                {
                    var n = Norm(file);
                    var score = 0;
                    foreach (var term in terms)
                    {
                        if (n.Contains(term)) score += 25;
                        else score -= 5;
                    }
                    if (n.Contains("cc0")) score += 14;
                    if (n.Contains("button") || n.Contains("resource") || n.Contains("troop") || n.Contains("building")) score += 2;
                    if (score > bestScore) { bestScore = score; best = file; }
                }
                if (best == null || (terms.Length > 0 && bestScore <= 0)) return null;
                try
                {
                    using (var stream = File.OpenRead(best))
                    using (var source = Image.FromStream(stream))
                    {
                        var clone = new Bitmap(source);
                        Images[key] = clone;
                        return clone;
                    }
                }
                catch { return null; }
            }
        }

        public static string FindFile(params string[] keywords)
        {
            var root = AssetPackRuntime.CacheDirectory;
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) return null;
            var all = Directory.GetFiles(root, "*", SearchOption.AllDirectories);
            var terms = (keywords ?? new string[0]).Where(s => !string.IsNullOrWhiteSpace(s)).Select(Norm).ToArray();
            return all.Select(f => new { File = f, N = Norm(f), Score = terms.Sum(t => Norm(f).Contains(t) ? 1 : 0) })
                .OrderByDescending(x => x.Score).ThenBy(x => x.File.Length).Where(x => x.Score > 0).Select(x => x.File).FirstOrDefault();
        }

        private static bool IsImage(string p)
        {
            var e = Path.GetExtension(p).ToLowerInvariant();
            return e == ".png" || e == ".jpg" || e == ".jpeg" || e == ".bmp";
        }
        private static string Norm(string s)
        {
            var b = new StringBuilder();
            foreach (var c in (s ?? "").ToLowerInvariant()) if (char.IsLetterOrDigit(c)) b.Append(c);
            return b.ToString();
        }
    }

    internal static class DryTheme
    {
        private static PrivateFontCollection _fonts;
        public static FontFamily Family { get; private set; } = FontFamily.GenericSansSerif;
        public static readonly Color Night = Color.FromArgb(18, 29, 36);
        public static readonly Color Panel = Color.FromArgb(38, 51, 59);
        public static readonly Color PanelLight = Color.FromArgb(60, 75, 83);
        public static readonly Color Gold = Color.FromArgb(250, 194, 48);
        public static readonly Color Elixir = Color.FromArgb(225, 69, 211);
        public static readonly Color Gem = Color.FromArgb(31, 218, 141);
        public static readonly Color Danger = Color.FromArgb(225, 72, 62);
        public static readonly Color Text = Color.FromArgb(248, 244, 225);

        public static void Initialize()
        {
            var candidates = new[]
            {
                Path.Combine(AssetPackRuntime.CacheDirectory ?? "", "Fonts", "DRY.ttf"),
                AssetLibrary.FindFile("dry", "ttf"),
                Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts", "DRY.ttf")
            };
            foreach (var file in candidates.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                if (!File.Exists(file)) continue;
                try
                {
                    _fonts = new PrivateFontCollection();
                    _fonts.AddFontFile(file);
                    if (_fonts.Families.Length > 0) { Family = _fonts.Families[0]; return; }
                }
                catch { }
            }
            try { Family = new FontFamily("Segoe UI Black"); } catch { }
        }

        public static Font Font(float size, FontStyle style = FontStyle.Regular) { return new Font(Family, size, style, GraphicsUnit.Pixel); }

        public static GraphicsPath Round(RectangleF r, float radius)
        {
            var d = radius * 2;
            var p = new GraphicsPath();
            p.AddArc(r.Left, r.Top, d, d, 180, 90); p.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.Left, r.Bottom - d, d, d, 90, 90); p.CloseFigure();
            return p;
        }

        public static void FillRound(Graphics g, Brush brush, RectangleF rect, float radius) { using (var p = Round(rect, radius)) g.FillPath(brush, p); }
        public static void OutlineRound(Graphics g, Pen pen, RectangleF rect, float radius) { using (var p = Round(rect, radius)) g.DrawPath(pen, p); }
    }
}
