using System;
using System.Drawing;
using System.IO;

namespace ImageExtraction
{
    public static class Utilities
    {
        public static bool Contains(this string source, string toCheck, StringComparison comp) => source?.IndexOf(toCheck, comp) >= 0;

        public static bool IsCloseToBlack(this Color color) => color.GetBrightness() < 0.3f;

        public static bool IsCloseToWhite(this Color color) => color.R > 160 && color.G > 160 && color.B > 160;

        public static bool IsBlack(this Color color) => color.R == 0 && color.G == 0 && color.B == 0;

        public static bool IsWhite(this Color color) => color.R == 255 && color.G == 255 && color.B == 255;

        public static bool IsSimilar(this Color a, Color z, int threshold = 25)
        {
            int r = (int)a.R - z.R,
                g = (int)a.G - z.G,
                b = (int)a.B - z.B;
            return (r * r + g * g + b * b) <= threshold * threshold;
        }
    }
}
