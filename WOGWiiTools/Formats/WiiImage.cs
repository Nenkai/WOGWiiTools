using AuroraLib.Compression.Algorithms;
using Syroot.BinaryData;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace WOGWiiTools.Formats
{
    // Boy::WiiImage
    public class WiiImage
    {
        /// <summary>
        /// Texture width, padded.
        /// </summary>
        public uint padWidth { get; set; }

        /// <summary>
        /// Texture height, padded.
        /// </summary>
        public uint padHeight { get; set; }

        /// <summary>
        /// Actual used width bytes in the texture.
        /// </summary>
        public uint totalWidth { get; set; }

        /// <summary>
        /// Actual used height bytes in the texture.
        /// </summary>
        public uint totalHeight { get; set; }

        /// <summary>
        /// Original image width before conversion. NOT the actual texture width.
        /// </summary>
        public uint originalWidth { get; set; }

        /// <summary>
        /// Original image height before conversion. NOT the actual texture height.
        /// </summary>
        public uint originalHeight { get; set; }
        public byte[] channelMap { get; set; } = new byte[4];

        public byte[] Data { get; set; }

        public void Read(Stream stream)
        {
            // Decompress image first - it's compressed separately from pak
            using var output = new MemoryStream();
            var lz11 = new LZ11();
            lz11.Decompress(stream, output);
            output.Flush();

            // Begin to read header
            output.Position = 0;
            using var bs = new BinaryStream(output, ByteConverter.Big);
            ReadHeader(bs);

            // Store texel data
            ReadTexelData(bs);
        }

        private void ReadHeader(BinaryStream bs)
        {
            // 0x20 header
            padWidth = bs.ReadUInt32();
            padHeight = bs.ReadUInt32();
            totalWidth = bs.ReadUInt32();
            totalHeight = bs.ReadUInt32();
            originalWidth = bs.ReadUInt32();
            originalHeight = bs.ReadUInt32();
            channelMap = bs.ReadBytes(4);
            bs.Position += 4;
        }

        private void ReadTexelData(BinaryStream bs)
        {
            int numChannels = channelMap.Distinct().Count();
            Data = bs.ReadBytes((int)(padWidth * padHeight * numChannels));
        }

        public void ConvertToPng(string path, bool arrange = false)
        {
            var img = new Image<Rgba32>((int)padWidth, (int)padHeight);
            byte[,] channelBuffer = new byte[padWidth, padHeight];

            // Massive credits to Allan Blomquist for explaining the process
            for (int channelIndex = 0; channelIndex < 4; channelIndex++)
            {
                int channel = channelMap[channelIndex];

                byte oldLeft = 127;
                byte oldUp = 127;
                byte oldDiag = 127;

                for (int y = 0; y < totalHeight; y++)
                {
                    for (int x = 0; x < totalWidth; x++)
                    {
                        byte color;
                        if (arrange)
                        {
                            // In JP which was released later on (at least), data is arranged to be more friendly to compression algos, de-arrange it
                            // Likely was used to shave some bytes by making the data more friendly for LZ11 to compress - JP inherently uses more assets
                            // Smart stuff
                            
                            if (x > 0)
                                oldLeft = channelBuffer[x - 1, y];
                            else if (y > 0)
                                oldLeft = channelBuffer[x, y - 1];

                            if (y > 0)
                                oldUp = channelBuffer[x, y - 1];
                            else if (x > 0)
                                oldUp = channelBuffer[x - 1, y];

                            if (x > 0 && y > 0)
                                oldDiag = channelBuffer[x - 1, y - 1];
                            else if (y > 0)
                                oldDiag = channelBuffer[x, y - 1];
                            else if (x > 0)
                                oldDiag = channelBuffer[x - 1, y];

                            int vertDiff = oldLeft - oldDiag;
                            int horzDiff = oldUp - oldDiag;

                            if (Math.Abs(vertDiff) < Math.Abs(horzDiff))
                                color = (byte)(oldUp + GetChannelTexelColor(channel, x, y));
                            else
                                color = (byte)(oldLeft + GetChannelTexelColor(channel, x, y));
                        }
                        else
                        {
                            color = GetChannelTexelColor(channel, x, y);
                        }

                        channelBuffer[x, y] = color;

                        Rgba32 p = img[x, y];
                        if (channelIndex == 0)
                            p.B = color;
                        else if (channelIndex == 1)
                            p.G = color;
                        else if (channelIndex == 2)
                            p.R = color;
                        else if (channelIndex == 3)
                            p.A = color;

                        img[x, y] = p;
                    }
                }
            }

            img.Save(path);
        }

        /// <summary>
        /// Gets a texel color for the specified channel.
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private byte GetChannelTexelColor(int channel, int x, int y)
        {
            int channelDataSize = (int)(padWidth * padHeight);
            int channelOffset = channelDataSize * channel;

            return Data[channelOffset + y * padWidth + x];
        }
    }
}
