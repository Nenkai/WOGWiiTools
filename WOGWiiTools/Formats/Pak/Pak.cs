using AuroraLib.Compression.Algorithms;
using Syroot.BinaryData;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WOGWiiTools.Formats.Pak
{
    public class Pak : IDisposable
    {
        private Stream _stream;

        public uint InitialHash { get; set; }
        public bool IsCompressed { get; set; }

        public List<PakEntry> Entries { get; set; } = new List<PakEntry>();
        public Dictionary<uint, string> Paths { get; set; } = new();

        public Pak(Stream stream)
        {
            _stream = stream;
        }

        public void Load()
        {
            var bs = new BinaryStream(_stream, ByteConverter.Big);

            uint numResources = bs.ReadUInt32();
            InitialHash = bs.ReadUInt32();
            IsCompressed = bs.ReadBoolean(BooleanCoding.Dword);

            using var tx = new StreamReader("file_list.txt");
            while (!tx.EndOfStream)
            {
                var path = tx.ReadLine();
                uint hash = this.Hash(path);

                if (!Paths.TryGetValue(hash, out _))
                    Paths.Add(hash, path);
            }
            tx.Dispose();

            Console.WriteLine("Creating output text file");
            using var outs = new StreamWriter("out.txt");

            int j = 0;
            for (int i = 0; i < numResources; i++)
            {
                PakEntry entry = new PakEntry();
                entry.Read(bs);

                if (Paths.TryGetValue(entry.Hash, out string foundPath))
                {
                    entry.Path = foundPath;
                    j++;
                }

                outs.WriteLine(entry);
                Entries.Add(entry);
            }

            outs.Flush();
            outs.Dispose();

            Console.WriteLine($"{j}/{numResources} hashes found");
        }

        public void ExtractAll(string outputDir)
        {
            Directory.CreateDirectory(outputDir);

            foreach (PakEntry entry in Entries)
            {
                string outputFile = Path.Combine(outputDir, string.IsNullOrEmpty(entry.Path) ? $"0x{entry.Hash:X8}" : entry.Path);
                Directory.CreateDirectory(Path.GetDirectoryName(outputFile));

                using var outFile = new FileStream(outputFile, FileMode.Create);

                _stream.Position = entry.FileOffset;

                if (IsCompressed)
                {
                    Console.WriteLine($"Extracting compressed: {outputFile}");
                    var lz11 = new LZ11();
                    lz11.Decompress(_stream, outFile);
                }
                else
                {
                    Console.WriteLine($"Extracting raw: {outputFile}");

                    byte[] data = _stream.ReadBytes((int)entry.FileSize);
                    outFile.Write(data);
                }
            }
        }

        public void Dispose()
        {
            _stream?.Dispose();
        }

        public uint Hash(string path)
        {
            int i = 0;
            uint value = InitialHash; // Unk bool in header might be used as start value?

            // If starts by \, skip x chars
            // If is below 48 (below number range), skip x chars
            // Then start looping 

            string str = path.ToLower();
            for (i = 0; i < str.Length; i++)
            {
                if (str[i] == '\\')
                    continue;

                if (str[i] != '\\' && str[i] != '/')
                    value = (uint)(str[i] ^ (uint)(value << 5 | value >> 27));
            }

            return value;
        }
    }
}
