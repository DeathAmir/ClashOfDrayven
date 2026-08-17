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
        private static readonly byte[] Magic = { 0x43, 0x4C, 0x44, 0x52, 0x59, 0x50, 0x4B, 0x1A }; // CLDRYPK + SUB
        private const int Version = 1;

        private static int Main(string[] args)
        {
            try
            {
                if (args.Length < 1) return Usage();
                var command = args[0].ToLowerInvariant();
                if (command == "pack" && args.Length == 3) { Pack(args[1], args[2]); return 0; }
                if (command == "unpack" && args.Length == 3) { Unpack(args[1], args[2]); return 0; }
                if (command == "info" && args.Length == 2) { Info(args[1]); return 0; }
                return Usage();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("cldrypk: " + ex.Message);
                return 2;
            }
        }

        private static int Usage()
        {
            Console.WriteLine("cldrypk - Clash Of Drayven lossless asset packer");
            Console.WriteLine("  cldrypk pack <input-directory> <output.cldrypk>");
            Console.WriteLine("  cldrypk unpack <input.cldrypk> <output-directory>");
            Console.WriteLine("  cldrypk info <input.cldrypk>");
            return 1;
        }

        private static void Pack(string inputDirectory, string outputFile)
        {
            inputDirectory = Path.GetFullPath(inputDirectory);
            if (!Directory.Exists(inputDirectory)) throw new DirectoryNotFoundException(inputDirectory);

            var files = Directory.GetFiles(inputDirectory, "*", SearchOption.AllDirectories)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
            var tempArchive = Path.GetTempFileName();
            try
            {
                using (var fs = new FileStream(tempArchive, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var writer = new BinaryWriter(fs, Encoding.UTF8, false))
                using (var sha = SHA256.Create())
                {
                    writer.Write(files.Length);
                    foreach (var file in files)
                    {
                        var relative = file.Substring(inputDirectory.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                            .Replace(Path.DirectorySeparatorChar, '/');
                        var info = new FileInfo(file);
                        writer.Write(relative);
                        writer.Write(info.Length);
                        byte[] hash;
                        using (var input = File.OpenRead(file)) hash = sha.ComputeHash(input);
                        writer.Write(hash.Length);
                        writer.Write(hash);
                        using (var input = File.OpenRead(file)) input.CopyTo(fs);
                    }
                }

                byte[] archiveHash;
                long rawLength = new FileInfo(tempArchive).Length;
                using (var sha = SHA256.Create())
                using (var input = File.OpenRead(tempArchive)) archiveHash = sha.ComputeHash(input);

                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputFile)) ?? ".");
                using (var output = new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var header = new BinaryWriter(output, Encoding.UTF8, true))
                {
                    header.Write(Magic);
                    header.Write(Version);
                    header.Write(rawLength);
                    header.Write(archiveHash.Length);
                    header.Write(archiveHash);
                    using (var compressed = new DeflateStream(output, CompressionLevel.Optimal, true))
                    using (var input = File.OpenRead(tempArchive)) input.CopyTo(compressed);
                }

                var packedLength = new FileInfo(outputFile).Length;
                var ratio = rawLength == 0 ? 0 : (100.0 * packedLength / rawLength);
                Console.WriteLine("Packed {0:N0} files", files.Length);
                Console.WriteLine("Raw: {0:N0} bytes", rawLength);
                Console.WriteLine("Pack: {0:N0} bytes ({1:0.0}%)", packedLength, ratio);
                Console.WriteLine("Header: 43 4C 44 52 59 50 4B 1A / version {0}", Version);
            }
            finally
            {
                try { File.Delete(tempArchive); } catch { }
            }
        }

        private static void Unpack(string inputFile, string outputDirectory)
        {
            var tempArchive = Path.GetTempFileName();
            try
            {
                long expectedLength;
                byte[] expectedHash;
                using (var input = new FileStream(inputFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var reader = new BinaryReader(input, Encoding.UTF8, true))
                {
                    var magic = reader.ReadBytes(Magic.Length);
                    if (!magic.SequenceEqual(Magic)) throw new InvalidDataException("Not a CLDRYPK file.");
                    var version = reader.ReadInt32();
                    if (version != Version) throw new InvalidDataException("Unsupported CLDRYPK version: " + version);
                    expectedLength = reader.ReadInt64();
                    var hashLength = reader.ReadInt32();
                    expectedHash = reader.ReadBytes(hashLength);
                    using (var deflate = new DeflateStream(input, CompressionMode.Decompress, true))
                    using (var temp = new FileStream(tempArchive, FileMode.Create, FileAccess.Write, FileShare.None)) deflate.CopyTo(temp);
                }

                if (new FileInfo(tempArchive).Length != expectedLength) throw new InvalidDataException("Archive length check failed.");
                using (var sha = SHA256.Create())
                using (var input = File.OpenRead(tempArchive))
                    if (!sha.ComputeHash(input).SequenceEqual(expectedHash)) throw new InvalidDataException("Archive SHA-256 check failed.");

                var root = Path.GetFullPath(outputDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                Directory.CreateDirectory(root);
                using (var fs = File.OpenRead(tempArchive))
                using (var reader = new BinaryReader(fs, Encoding.UTF8, false))
                using (var sha = SHA256.Create())
                {
                    var count = reader.ReadInt32();
                    for (var i = 0; i < count; i++)
                    {
                        var relative = reader.ReadString().Replace('/', Path.DirectorySeparatorChar);
                        var length = reader.ReadInt64();
                        var hashLength = reader.ReadInt32();
                        var expectedFileHash = reader.ReadBytes(hashLength);
                        var destination = Path.GetFullPath(Path.Combine(root, relative));
                        if (!destination.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Unsafe path in pack: " + relative);
                        Directory.CreateDirectory(Path.GetDirectoryName(destination));
                        using (var output = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None))
                        using (var hashing = new CryptoStream(output, sha, CryptoStreamMode.Write))
                        {
                            CopyExactly(fs, hashing, length);
                            hashing.FlushFinalBlock();
                            if (!sha.Hash.SequenceEqual(expectedFileHash)) throw new InvalidDataException("File SHA-256 failed: " + relative);
                        }
                        sha.Initialize();
                    }
                    Console.WriteLine("Unpacked {0:N0} files to {1}", count, root);
                }
            }
            finally
            {
                try { File.Delete(tempArchive); } catch { }
            }
        }

        private static void Info(string inputFile)
        {
            using (var input = new FileStream(inputFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new BinaryReader(input, Encoding.UTF8, false))
            {
                var magic = reader.ReadBytes(Magic.Length);
                if (!magic.SequenceEqual(Magic)) throw new InvalidDataException("Not a CLDRYPK file.");
                var version = reader.ReadInt32();
                var rawLength = reader.ReadInt64();
                var hashLength = reader.ReadInt32();
                var hash = reader.ReadBytes(hashLength);
                Console.WriteLine("CLDRYPK version: " + version);
                Console.WriteLine("Packed bytes: " + new FileInfo(inputFile).Length.ToString("N0"));
                Console.WriteLine("Raw archive bytes: " + rawLength.ToString("N0"));
                Console.WriteLine("SHA-256: " + BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant());
            }
        }

        private static void CopyExactly(Stream input, Stream output, long bytes)
        {
            var buffer = new byte[1024 * 128];
            while (bytes > 0)
            {
                var requested = (int)Math.Min(buffer.Length, bytes);
                var read = input.Read(buffer, 0, requested);
                if (read <= 0) throw new EndOfStreamException();
                output.Write(buffer, 0, read);
                bytes -= read;
            }
        }
    }
}
