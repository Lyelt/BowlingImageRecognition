using System;
using System.Linq;
using System.IO;
using System.Drawing;

namespace BowlingImageExtractor
{
    class Program
    {
        static void Main(string[] args)
        {
            var filePath = args.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
            {
                using var bmp = new Bitmap(filePath);
                var text = ImageExtraction.Test.ExtractText(bmp);
                Console.WriteLine($"Extracted: {text}");
            }

            Console.ReadLine();
        }
    }
}
