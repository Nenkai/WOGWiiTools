using CommandLine;
using CommandLine.Text;

using WOGWiiTools.Formats.Pak;
using WOGWiiTools.Formats;

namespace WOGWiiTools
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("-----------------------------------------");
            Console.WriteLine("- WOGWiiTools by Nenkai");
            Console.WriteLine("-----------------------------------------");
            Console.WriteLine("- https://github.com/Nenkai");
            Console.WriteLine("-----------------------------------------");
            Console.WriteLine("");

            var p = Parser.Default.ParseArguments<PakVerbs, ImageToPngVerbs>(args)
                .WithParsed<PakVerbs>(Pak)
                .WithParsed<ImageToPngVerbs>(ImageToPng);
        }

        public static void Pak(PakVerbs verbs)
        {
            if (!File.Exists(verbs.InputPath))
            {
                Console.WriteLine("ERROR: Input pak file does not exist");
                return;
            }

            if (File.Exists(verbs.OutputPath))
            {
                Console.WriteLine("ERROR: Invalid output directory - is a file");
                return;
            }

            using var fs = new FileStream(verbs.InputPath, FileMode.Open);
            using var pak = new Pak(fs);
            pak.Load();
            pak.ExtractAll(verbs.OutputPath);
        }

        public static void ImageToPng(ImageToPngVerbs verbs)
        {
            if (verbs.InputPaths.Count() == 1 && Directory.Exists(verbs.InputPaths.First()))
            {
                foreach (var file in Directory.GetFiles(verbs.InputPaths.First()))
                {
                    try
                    {
                        ProcessImage(file);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Skipped: {file} - {e.Message}");
                    }
                }
            }
            else
            {
                foreach (var file in verbs.InputPaths)
                {
                    try
                    {
                        ProcessImage(file);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Skipped: {file} - {e.Message}");
                    }
                }
            }
        }

        private static void ProcessImage(string file)
        {
            using var fss = new FileStream(file, FileMode.Open);
            Console.WriteLine($"Processing: {file}");
            try
            {
                var image = new WiiImage();
                image.Read(fss);

                Console.WriteLine($"- Original Dimensions: {image.originalWidth}x{image.originalHeight}");
                Console.WriteLine($"- Pad Dimensions: {image.padWidth}x{image.padHeight}");
                Console.WriteLine($"- Total Bytes Dimensions: {image.totalWidth}x{image.totalHeight}");

                string output;
                if (Path.GetExtension(file) == ".png.binbig")
                    output = Path.GetFileNameWithoutExtension(file);
                else
                    output = Path.ChangeExtension(file, ".png");

                image.ConvertToPng(output);
                Console.WriteLine($"Converted to {output}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Skipped: {file} - {e.Message}");
            }
        }
    }

    [Verb("pak", HelpText = "Extracts a World of Goo Wii Pak (master.pak, etc)")]
    public class PakVerbs
    {
        [Option('i', "input", Required = true, HelpText = "Input pak file.")]
        public string InputPath { get; set; }

        [Option('o', "output", Required = true, HelpText = "Output directory.")]
        public string OutputPath { get; set; }
    }

    [Verb("image-to-png", HelpText = "Converts an image/texture (.png.binbig) to png")]
    public class ImageToPngVerbs
    {
        [Option('i', "input", Required = true, HelpText = "Input file.")]
        public IEnumerable<string> InputPaths { get; set; }
    }
}