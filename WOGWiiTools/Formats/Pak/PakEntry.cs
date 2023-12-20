using Syroot.BinaryData;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WOGWiiTools.Formats.Pak
{
    public class PakEntry
    {
        public string Path { get; set; }
        public uint Hash { get; set; }
        public uint FileOffset { get; set; }
        public uint FileSize { get; set; }
        public uint Meta { get; set; }

        public void Read(BinaryStream bs)
        {
            Hash = bs.ReadUInt32();
            FileOffset = bs.ReadUInt32();
            FileSize = bs.ReadUInt32();
            Meta = bs.ReadUInt32();
        }

        public override string ToString()
        {
            return $"'{(string.IsNullOrEmpty(Path) ? "Unk" : Path)}' (0x{Hash:X8}) Offset:0x{FileOffset} Size: 0x{FileSize:X8}";
        }
    }
}
