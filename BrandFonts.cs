using System;
using System.Drawing;
using System.Drawing.Text;
using System.IO;

namespace ClashOfDrayven
{
    internal static class BrandFonts
    {
        private static PrivateFontCollection _magic;
        private static FontFamily _magicFamily;

        private static FontFamily MagicFamily
        {
            get
            {
                if (_magicFamily != null) return _magicFamily;
                try
                {
                    var path = Path.Combine(AppContext.BaseDirectory, "Assets", "IrAutoX-Magic_5.ttf");
                    if (File.Exists(path))
                    {
                        _magic = new PrivateFontCollection();
                        _magic.AddFontFile(path);
                        if (_magic.Families.Length > 0) _magicFamily = _magic.Families[0];
                    }
                }
                catch { }
                return _magicFamily ?? DryTheme.Family;
            }
        }

        public static Font Magic(float size, FontStyle style = FontStyle.Regular)
        {
            return new Font(MagicFamily, size, style, GraphicsUnit.Pixel);
        }
    }
}
