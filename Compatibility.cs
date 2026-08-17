using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace System.Runtime.CompilerServices
{
    public sealed class IsExternalInit { }
}

namespace System.Text.Json
{
    public sealed class JsonSerializerOptions
    {
        public bool PropertyNameCaseInsensitive { get; set; }
        public bool WriteIndented { get; set; }
    }

    public static class JsonSerializer
    {
        private static JavaScriptSerializer NewSerializer()
        {
            return new JavaScriptSerializer { MaxJsonLength = int.MaxValue, RecursionLimit = 128 };
        }

        public static T Deserialize<T>(string json, JsonSerializerOptions options = null)
        {
            return NewSerializer().Deserialize<T>(json);
        }

        public static string Serialize<T>(T value, JsonSerializerOptions options = null)
        {
            return NewSerializer().Serialize(value);
        }
    }
}

namespace ClashOfDrayven
{
    internal static class ApplicationConfiguration
    {
        public static void Initialize()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
        }
    }

    internal static class Math
    {
        public const double PI = System.Math.PI;
        public static int Max(int a, int b) => System.Math.Max(a, b);
        public static float Max(float a, float b) => System.Math.Max(a, b);
        public static double Max(double a, double b) => System.Math.Max(a, b);
        public static int Min(int a, int b) => System.Math.Min(a, b);
        public static float Min(float a, float b) => System.Math.Min(a, b);
        public static double Min(double a, double b) => System.Math.Min(a, b);
        public static double Round(double value) => System.Math.Round(value);
        public static double Pow(double x, double y) => System.Math.Pow(x, y);
        public static double Sin(double x) => System.Math.Sin(x);
        public static double Sqrt(double x) => System.Math.Sqrt(x);
        public static double Abs(double x) => System.Math.Abs(x);
        public static float Abs(float x) => System.Math.Abs(x);
        public static int Abs(int x) => System.Math.Abs(x);
        public static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }

    internal static class DictionaryCompat
    {
        public static bool TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (dictionary.ContainsKey(key)) return false;
            dictionary.Add(key, value);
            return true;
        }

        public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key)
        {
            TValue value;
            return dictionary.TryGetValue(key, out value) ? value : default(TValue);
        }
    }
}
