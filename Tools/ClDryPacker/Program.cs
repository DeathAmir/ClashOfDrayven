using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ClDryPacker
{
    internal static class Program
    {
        private static readonly byte[] Magic = { 0x43, 0x4C, 0x44, 0x52, 0x59, 0x50, 0x4B, 0x1A };
        private const int Version = 1;
        private const int PartCount = 20;

        private static int Main(string[] args)
        {
            try
            {
                if (args.Length < 1) return Usage();
                var c = args[0].ToLowerInvariant();
                if (c == "pack" && args.Length == 3) { Pack(args[1], args[2]); return 0; }
                if (c == "pack20" && args.Length == 3) { Pack20(args[1], args[2]); return 0; }
                if (c == "unpack" && args.Length == 3) { Unpack(args[1], args[2]); return 0; }
                if (c == "info" && args.Length == 2) { Info(args[1]); return 0; }
                if (c == "verify20" && args.Length == 2) { Verify20(args[1]); return 0; }
                return Usage();
            }
            catch (Exception ex) { Console.Error.WriteLine("cldrypk: " + ex); return 2; }
        }

        private static int Usage()
        {
            Console.WriteLine("cldrypk - Clash Of Drayven deterministic lossless packer");
            Console.WriteLine("  cldrypk pack <input-directory> <output>");
            Console.WriteLine("  cldrypk pack20 <input-directory> <output-directory>");
            Console.WriteLine("  cldrypk unpack <input> <output-directory>");
            Console.WriteLine("  cldrypk info <input>");
            Console.WriteLine("  cldrypk verify20 <directory>");
            return 1;
        }

        private static void Pack(string inputDirectory, string outputFile)
        {
            var root = Path.GetFullPath(inputDirectory);
            if (!Directory.Exists(root)) throw new DirectoryNotFoundException(root);
            var files = Directory.GetFiles(root, "*", SearchOption.AllDirectories).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
            PackFiles(root, files, outputFile);
        }

        private static void Pack20(string inputDirectory, string outputDirectory)
        {
            var root = Path.GetFullPath(inputDirectory);
            if (!Directory.Exists(root)) throw new DirectoryNotFoundException(root);
            Directory.CreateDirectory(outputDirectory);
            var buckets = Enumerable.Range(0, PartCount).Select(_ => new List<string>()).ToArray();
            var totals = new long[PartCount];
            var files = Directory.GetFiles(root, "*", SearchOption.AllDirectories).OrderByDescending(f => new FileInfo(f).Length).ThenBy(f => f, StringComparer.OrdinalIgnoreCase).ToArray();
            foreach (var file in files)
            {
                var target = 0;
                for (var i = 1; i < PartCount; i++) if (totals[i] < totals[target]) target = i;
                buckets[target].Add(file); totals[target] += new FileInfo(file).Length;
            }
            for (var i = 0; i < PartCount; i++)
            {
                var output = Path.Combine(outputDirectory, "CLDRYPK" + (i + 1));
                PackFiles(root, buckets[i].OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray(), output);
                Console.WriteLine("part {0:00}/20: {1:N0} source bytes -> {2}", i + 1, totals[i], output);
            }
            var manifest = new StringBuilder();
            manifest.AppendLine("CLASH OF DRAYVEN / CLDRYPK20");
            manifest.AppendLine("format=" + Version); manifest.AppendLine("parts=20"); manifest.AppendLine("files=" + files.Length);
            using (var sha = SHA256.Create())
            {
                for (var i = 1; i <= PartCount; i++)
                {
                    var p = Path.Combine(outputDirectory, "CLDRYPK" + i);
                    using var fs = File.OpenRead(p);
                    manifest.AppendLine("CLDRYPK" + i + "=" + Hex(sha.ComputeHash(fs)) + ";" + fs.Length);
                }
            }
            File.WriteAllText(Path.Combine(outputDirectory, "CLDRYPK.manifest"), manifest.ToString(), new UTF8Encoding(false));
        }

        private static void PackFiles(string root, string[] files, string outputFile)
        {
            var temp = Path.GetTempFileName();
            try
            {
                using (var fs = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var writer = new BinaryWriter(fs, Encoding.UTF8, true))
                using (var sha = SHA256.Create())
                {
                    writer.Write(files.Length);
                    foreach (var file in files)
                    {
                        var rel = Path.GetRelativePath(root, file).Replace(Path.DirectorySeparatorChar, '/');
                        writer.Write(rel);
                        var length = new FileInfo(file).Length; writer.Write(length);
                        byte[] hash; using (var input = File.OpenRead(file)) hash = sha.ComputeHash(input);
                        writer.Write(hash.Length); writer.Write(hash);
                        using (var input = File.OpenRead(file)) input.CopyTo(fs);
                    }
                }
                long rawLength = new FileInfo(temp).Length; byte[] archiveHash;
                using (var sha = SHA256.Create()) using (var input = File.OpenRead(temp)) archiveHash = sha.ComputeHash(input);
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputFile)) ?? ".");
                using (var output = new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var writer = new BinaryWriter(output, Encoding.UTF8, true))
                {
                    writer.Write(Magic); writer.Write(Version); writer.Write(rawLength); writer.Write(archiveHash.Length); writer.Write(archiveHash);
                    using (var compressed = new DeflateStream(output, CompressionLevel.SmallestSize, true)) using (var input = File.OpenRead(temp)) input.CopyTo(compressed);
                }
            }
            finally { try { File.Delete(temp); } catch { } }
        }

        private static void Unpack(string inputFile, string outputDirectory)
        {
            var temp = Path.GetTempFileName();
            try
            {
                long expectedLength; byte[] expectedHash;
                using (var input = File.OpenRead(inputFile)) using (var reader = new BinaryReader(input, Encoding.UTF8, true))
                {
                    if (!reader.ReadBytes(Magic.Length).SequenceEqual(Magic)) throw new InvalidDataException("Not a CLDRYPK file.");
                    if (reader.ReadInt32() != Version) throw new InvalidDataException("Unsupported CLDRYPK version.");
                    expectedLength = reader.ReadInt64(); expectedHash = reader.ReadBytes(reader.ReadInt32());
                    using var deflate = new DeflateStream(input, CompressionMode.Decompress, true); using var raw = File.Create(temp); deflate.CopyTo(raw);
                }
                if (new FileInfo(temp).Length != expectedLength) throw new InvalidDataException("Archive length failed.");
                using (var sha = SHA256.Create()) using (var raw = File.OpenRead(temp)) if (!sha.ComputeHash(raw).SequenceEqual(expectedHash)) throw new InvalidDataException("Archive SHA-256 failed.");
                var root = Path.GetFullPath(outputDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar; Directory.CreateDirectory(root);
                using (var raw = File.OpenRead(temp)) using (var reader = new BinaryReader(raw, Encoding.UTF8, true))
                {
                    var count = reader.ReadInt32();
                    for (var i = 0; i < count; i++)
                    {
                        var rel = reader.ReadString().Replace('/', Path.DirectorySeparatorChar); var length = reader.ReadInt64(); var expected = reader.ReadBytes(reader.ReadInt32());
                        var dest = Path.GetFullPath(Path.Combine(root, rel)); if (!dest.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Unsafe path: " + rel);
                        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                        using (var output = File.Create(dest)) { var buffer = new byte[131072]; long left = length; while (left > 0) { var want = (int)Math.Min(buffer.Length, left); var read = raw.Read(buffer, 0, want); if (read <= 0) throw new EndOfStreamException(); output.Write(buffer, 0, read); left -= read; } }
                        using var sha = SHA256.Create(); using var verify = File.OpenRead(dest); if (!sha.ComputeHash(verify).SequenceEqual(expected)) throw new InvalidDataException("File SHA-256 failed: " + rel);
                    }
                }
            }
            finally { try { File.Delete(temp); } catch { } }
        }

        private static void Info(string inputFile)
        {
            using var input = File.OpenRead(inputFile); using var reader = new BinaryReader(input, Encoding.UTF8, true);
            if (!reader.ReadBytes(Magic.Length).SequenceEqual(Magic)) throw new InvalidDataException("Not CLDRYPK.");
            var version = reader.ReadInt32(); var raw = reader.ReadInt64(); var hash = reader.ReadBytes(reader.ReadInt32());
            Console.WriteLine("version=" + version); Console.WriteLine("packed=" + input.Length); Console.WriteLine("raw=" + raw); Console.WriteLine("sha256=" + Hex(hash));
        }

        private static void Verify20(string dir)
        {
            for (var i = 1; i <= PartCount; i++) { var p = Path.Combine(dir, "CLDRYPK" + i); if (!File.Exists(p)) throw new FileNotFoundException(p); Info(p); }
            Console.WriteLine("All 20 CLDRYPK parts are present and readable.");
        }

        private static string Hex(byte[] value) => BitConverter.ToString(value).Replace("-", "").ToLowerInvariant();
    }
}
