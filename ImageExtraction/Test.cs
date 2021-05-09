using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Tesseract;

namespace ImageExtraction
{
    public class Test
    {
        private const int HORIZONTAL_DISTANCE = 32;
        private const int VERTICAL_DISTANCE = 140;

        private const int X7= 6;
        private const int X8= 13;
        private const int X9= 19;
        private const int X10= 26;
        private const int BACK_ROW_Y_BORDER = 326;
        private const int BACK_ROW_Y_CENTER = 328;

        private const int X4 = 9;
        private const int X5 = 16;
        private const int X6 = 22;
        private const int THIRD_ROW_Y_BORDER = 332;
        private const int THIRD_ROW_Y_CENTER = 333;

        private const int X2 = 13;
        private const int X3 = 19;
        private const int SECOND_ROW_Y_BORDER = 337;
        private const int SECOND_ROW_Y_CENTER = 338;

        private const int X1 = 16;
        private const int FIRST_ROW_Y_BORDER = 343;
        private const int FIRST_ROW_Y_CENTER = 344;

        public static string ExtractText(Bitmap bmp)
        {
            var textLine = PageIteratorLevel.TextLine;
            using (var engine = new TesseractEngine(@".\tessdata", "eng"))
            {
                var games = new List<GameImg>();
                var gameImgHeight = 0;

                var pix = PixConverter.ToPix(bmp);
                using (var page = engine.Process(pix))
                using (var iter = page.GetIterator())
                {
                    var gameNumbers = 0;
                    iter.Begin();
                    do
                    {
                        if (iter.TryGetBoundingBox(textLine, out var rect))
                        {
                            // extract games out of overall image
                            var curText = iter.GetText(textLine);
                            if (curText.Contains("Game", StringComparison.OrdinalIgnoreCase) && Regex.IsMatch(curText, "Game [0-9]+", RegexOptions.IgnoreCase))
                            {
                                var num = ++gameNumbers;
                                games.Add(new GameImg { GameStartPos = rect.Y2, LabelStartPos = rect.Y1, GameName = $"Game {num}", GameNumber = num });
                            }
                            else if (curText.Contains("Total", StringComparison.OrdinalIgnoreCase) && games.Any())
                            {
                                var lastGame = games.Last();
                                lastGame.GameEndPos = rect.Y1;
                                gameImgHeight = lastGame.GameEndPos - lastGame.GameStartPos;
                            }
                            else if (curText.Contains("JP") || curText.Contains("Puglisi") || (curText.Contains("NG") && curText.Contains("Nicholas")) || curText.Contains("BG") || curText.Contains("TD") || curText.Contains("BH"))
                            {
                                Console.WriteLine($"Game belongs to {curText}");
                            }
                            else if (curText.Contains("When"))
                            {
                                Console.WriteLine($"Date: {curText}");
                            }
                        }
                    }
                    while (iter.Next(textLine));
                }

                for (var g = 0; g < games.Count; g++)
                {
                    var frames = new List<Frame>();
                    for (var f = 0; f < 12; f++)
                    {
                        var roll1 = new Roll();
                        var roll2 = new Roll();
                        var pins = GetPins(bmp, f, g);
                        foreach (var pin in pins)
                        {
                            var blackCenter = pin.CenterColor.IsCloseToBlack();
                            var whiteBorder = pin.BorderColor.IsCloseToWhite();

                            if (f < 9)
                            {
                                if (blackCenter && !whiteBorder)
                                    roll1.PinsKnockedDown.Add(pin.PinNumber);
                                else if (blackCenter && whiteBorder)
                                    roll2.PinsKnockedDown.Add(pin.PinNumber);
                            }
                            else
                            {
                                if (blackCenter)
                                    roll1.PinsKnockedDown.Add(pin.PinNumber);
                            }
                        }
                        frames.Add(new Frame { FrameNumber = f + 1, Rolls = new List<Roll> { roll1, roll2 } });
                    }
                }

                //foreach (var game in games)
                //{
                //    if (game.GameEndPos == 0)
                //        game.GameEndPos = game.GameStartPos + gameImgHeight;

                //    Console.WriteLine($"{game.GameName}: starts at {game.GameStartPos} and ends at {game.GameEndPos}");


                //    var (x, y, width, height) = (2, game.GameStartPos, pix.Width - 2, game.GameEndPos - game.GameStartPos);
                //    var gameImg = bmp.Clone(new Rectangle(x, y, width, height), bmp.PixelFormat);
                //    gameImg.Save($@"C:\Users\nicho\Desktop\{game.GameName}.png", System.Drawing.Imaging.ImageFormat.Png);

                //    // extract frames out of each game
                //    // find every X coordinate where the entire vertical line of pixels is black
                //    var frameImages = new List<Bitmap>();
                //    var blackLineColumns = GetVerticalBlackLineCoordinates(gameImg);
                //    var currentFrameNumber = 0;
                //    for (var i = 0; i < blackLineColumns.Count; i++)
                //    {
                //        // ignore consecutive columns
                //        if (i == 0 || blackLineColumns[i - 1] != blackLineColumns[i] - 1)
                //        {
                //            currentFrameNumber++;

                //            var rightEdge = i == blackLineColumns.Count - 1 ? gameImg.Width : blackLineColumns[i];
                //            var leftEdge = i == 0 ? 0 : blackLineColumns[i - 1];

                //            (x, y, width, height) = (leftEdge, 0, rightEdge - leftEdge, gameImg.Height);
                //            if (x + width > gameImg.Width)
                //                width = gameImg.Width - x;
                //            var frameImg = gameImg.Clone(new Rectangle(x, y, width, height), gameImg.PixelFormat);
                //            frameImages.Add(frameImg);
                //            frameImg.Save($@"C:\Users\nicho\Desktop\{game.GameName}-frame{currentFrameNumber}.png", System.Drawing.Imaging.ImageFormat.Png);
                //        }
                //    }
                    
                //    if (frameImages.Count != 12)
                //    {
                //        Console.WriteLine($"Unable to extract 12 frames from game {game.GameName}. Got {frameImages.Count} instead.");
                //        continue;
                //    }

                //    var frames = new List<Frame>();
                //    for (var i = 0; i < frameImages.Count; i++)
                //    {
                //        var frameNumber = i + 1;
                //        var frame = ExtractFrameFromImage(frameImages[i], frameNumber);
                //        frames.Add(frame);
                //    }

                //    Console.WriteLine($"------------ {game.GameName} ---------------");
                //    foreach (var frame in frames)
                //    {
                //        Console.WriteLine($"Frame {frame.FrameNumber}");
                //        if (frame.Rolls != null)
                //        {
                //            for (var r = 0; r < frame.Rolls.Count; r++)
                //            {
                //                var roll = frame.Rolls[r];
                //                Console.WriteLine($"Roll {r + 1}: {roll.NumberOfPinsKnockedDown} pins ({string.Join(", ", roll.PinsKnockedDown)})");
                //            }
                //        }
                //        else
                //        {
                //            Console.WriteLine($"Unknown frame score");
                //        }
                //    }
                //}
            }

            Console.WriteLine("waiting");
            Console.ReadLine();
            return "";
        }

        private static List<Pin> GetPins(Bitmap image, int frameIndex, int gameIndex)
        {
            var deltaX = frameIndex * HORIZONTAL_DISTANCE;
            var deltaY = gameIndex * VERTICAL_DISTANCE;

            var backYCenter = BACK_ROW_Y_CENTER + deltaY;
            var backYBorder = BACK_ROW_Y_BORDER + deltaY;
            var pin7X = X7 + deltaX;
            var pin8X = X8 + deltaX;
            var pin9X = X9 + deltaX;
            var pin10X = X10 + deltaX;

            var thirdYCenter = THIRD_ROW_Y_CENTER + deltaY;
            var thirdYBorder = THIRD_ROW_Y_BORDER + deltaY;
            var pin4X = X4 + deltaX;
            var pin5X = X5 + deltaX;
            var pin6X = X6 + deltaX;

            var secondYCenter = SECOND_ROW_Y_CENTER + deltaY;
            var secondYBorder = SECOND_ROW_Y_BORDER + deltaY;
            var pin3X = X3 + deltaX;
            var pin2X = X2 + deltaX;

            var firstYCenter = FIRST_ROW_Y_CENTER + deltaY;
            var firstYBorder = FIRST_ROW_Y_BORDER + deltaY;
            var pin1X = X1 + deltaX;


            return new List<Pin>
            {
                new Pin(1, image.GetPixel(pin1X, firstYCenter), image.GetPixel(pin1X, firstYBorder)),
                new Pin(2, image.GetPixel(pin2X, secondYCenter), image.GetPixel(pin2X, secondYBorder)),
                new Pin(3, image.GetPixel(pin3X, secondYCenter), image.GetPixel(pin3X, secondYBorder)),
                new Pin(4, image.GetPixel(pin4X, thirdYCenter), image.GetPixel(pin4X, thirdYBorder)),
                new Pin(5, image.GetPixel(pin5X, thirdYCenter), image.GetPixel(pin5X, thirdYBorder)),
                new Pin(6, image.GetPixel(pin6X, thirdYCenter), image.GetPixel(pin6X, thirdYBorder)),
                new Pin(7, image.GetPixel(pin7X, backYCenter), image.GetPixel(pin7X, backYBorder)),
                new Pin(8, image.GetPixel(pin8X, backYCenter), image.GetPixel(pin8X, backYBorder)),
                new Pin(9, image.GetPixel(pin9X, backYCenter), image.GetPixel(pin9X, backYBorder)),
                new Pin(10, image.GetPixel(pin10X, backYCenter), image.GetPixel(pin10X, backYBorder))
            };
        }

        private static List<int> GetVerticalBlackLineCoordinates(Bitmap image)
        {
            var blackLines = new Dictionary<int, int>();
            for (var j = 0; j < image.Width; j++)
            {
                blackLines[j] = 0;
                for (var k = 0; k < image.Height; k++)
                {
                    var pixel = image.GetPixel(j, k);
                    //Console.WriteLine($"Pixel {j}x{k} : {pixel}");
                    if (pixel.IsCloseToBlack())
                    {
                        blackLines[j]++;
                    }
                }
            }
            var tolerance = 80;
            //foreach (var kvp in blackLines)
            //{
            //    var percentBlackPixels = ((double)kvp.Value / image.Height) * 100;
            //    if (percentBlackPixels > tolerance)
            //    {
            //        Console.WriteLine($"Column {kvp.Key} contains {percentBlackPixels}% black pixels");
            //    }
            //}

            return blackLines.Where(kvp => (((double)kvp.Value / image.Height) * 100) > tolerance).Select(kvp => kvp.Key).ToList();
        }

        /*private static Frame ExtractFrameFromImage(Bitmap image, int frameNumber)
        {
            // determine vertical pin area based on gray color
            var startingY = 0;
            var endingY = 0;
            for (var y = 0; y < image.Height; y++)
            {
                var closeGray = Color.FromArgb(106, 106, 106);
                if (image.GetPixel(2, y).IsSimilar(closeGray))
                {
                    if (startingY == 0)
                    {
                        startingY = y;
                        //Console.WriteLine($"First gray-ish pixel is at y={startingY}");
                    }
                    else if (!image.GetPixel(2, y + 1).IsSimilar(closeGray))
                    {
                        endingY = y;
                        //Console.WriteLine($"Last gray-ish pixel is at y={endingY}");
                        break;
                    }
                }
            }

            // might be a yellow modified frame
            if (startingY == 0 || endingY == 0 || startingY > image.Height / 2)
            {
                //Console.WriteLine($"Modified frame - unable to extract score");
                return new Frame { FrameNumber = frameNumber };
            }


            var pins = new List<Pin>();

            var borderDistance = 5;
            var horizontalDistance = 9;
            var halfHorizontalDistance = (int)Math.Ceiling((double)horizontalDistance / 2d);
            var verticalDistance = 8;

            List<Color> GetBorderPixels(int x, int y)
            {
                var borderPixels = new List<Color>();
                // get up to 5 pixels in each direction from center
                for (var i = 0; i < borderDistance; i++)
                {
                    borderPixels.Add(image.GetPixel(x, y - i));
                    borderPixels.Add(image.GetPixel(x, y + i));
                    borderPixels.Add(image.GetPixel(x - i, y));
                    borderPixels.Add(image.GetPixel(x + i, y));

                    borderPixels.Add(image.GetPixel(x + i, y + i/2));
                    borderPixels.Add(image.GetPixel(x + i/2, y + i));
                    borderPixels.Add(image.GetPixel(x + i/2, y + i/2));
                    borderPixels.Add(image.GetPixel(x, y + i/2));
                    borderPixels.Add(image.GetPixel(x + i/2, y));
                }
                return borderPixels;
            }

            // Back row 7-10
            var backRowCenterY = startingY + 7;
            var backRowStartingX = 4;
            var backRowEndingX = 37;

            // determine where pins start by finding first black or white pixel
            for (var x = 3; x < image.Width; x++)
            {
                var pixel = image.GetPixel(x, backRowCenterY);
                if (pixel.IsCloseToBlack() || pixel.IsCloseToWhite())
                {
                    backRowStartingX = Math.Max(x + 2, 4);
                    break;
                }
            }

            // determine vertically where the pins start
            for (var y = startingY; y < startingY + 10; y++)
            {
                var pixel = image.GetPixel(backRowStartingX, y);
                if (pixel.IsCloseToBlack() || pixel.IsCloseToWhite())
                {
                    backRowCenterY = y + 3;
                    break;
                }
            }

            // determine where pins end
            for (var x = image.Width - 3; x > backRowStartingX; x--)
            {
                var pixel = image.GetPixel(x, backRowCenterY);
                if (pixel.IsCloseToBlack() || pixel.IsCloseToWhite())
                {
                    backRowEndingX = x - 2;
                    break;
                }
            }
            backRowCenterY -= 1;
            pins.Add(new Pin(7, image.GetPixel(backRowStartingX, backRowCenterY), GetBorderPixels(backRowStartingX, backRowCenterY)));
            pins.Add(new Pin(8, image.GetPixel(backRowStartingX + horizontalDistance, backRowCenterY), GetBorderPixels(backRowStartingX + horizontalDistance, backRowCenterY)));
            pins.Add(new Pin(9, image.GetPixel(backRowEndingX - horizontalDistance, backRowCenterY), GetBorderPixels(backRowEndingX - horizontalDistance, backRowCenterY)));
            pins.Add(new Pin(10, image.GetPixel(backRowEndingX, backRowCenterY), GetBorderPixels(backRowEndingX, backRowCenterY)));

            // Third row 4-6
            var thirdRowCenterY = backRowCenterY + verticalDistance;
            var thirdRowStartingX = backRowStartingX + halfHorizontalDistance;
            var thirdRowEndingX = backRowEndingX - halfHorizontalDistance;

            pins.Add(new Pin(4, image.GetPixel(thirdRowStartingX, thirdRowCenterY), GetBorderPixels(thirdRowStartingX, thirdRowCenterY)));
            pins.Add(new Pin(5, image.GetPixel(thirdRowStartingX + horizontalDistance, thirdRowCenterY), GetBorderPixels(thirdRowStartingX + horizontalDistance, thirdRowCenterY)));
            pins.Add(new Pin(6, image.GetPixel(thirdRowEndingX, thirdRowCenterY), GetBorderPixels(thirdRowEndingX, thirdRowCenterY)));

            // Second row 2-3
            var secondRowCenterY = thirdRowCenterY + verticalDistance - 1;
            var secondRowStartingX = thirdRowStartingX + halfHorizontalDistance;
            var secondRowEndingX = thirdRowEndingX - halfHorizontalDistance;

            pins.Add(new Pin(2, image.GetPixel(secondRowStartingX, secondRowCenterY), GetBorderPixels(secondRowStartingX, secondRowCenterY)));
            pins.Add(new Pin(3, image.GetPixel(secondRowEndingX, secondRowCenterY), GetBorderPixels(secondRowEndingX, secondRowCenterY)));

            // First row 1
            var firstRowCenterY = secondRowCenterY + verticalDistance - 1;
            var firstRowX = secondRowStartingX + halfHorizontalDistance;

            pins.Add(new Pin(1, image.GetPixel(firstRowX, firstRowCenterY), GetBorderPixels(firstRowX, firstRowCenterY)));


            var frame = new Frame { FrameNumber = frameNumber };
            var roll1 = new Roll { PinsKnockedDown = new List<int>() };
            var roll2 = new Roll { PinsKnockedDown = new List<int>() };

            foreach (var pin in pins)
            {
                var blackCenter = pin.CenterColor.IsCloseToBlack();
                var whiteBorder = pin.BorderColors.Any(p => p.IsCloseToWhite());
                //Console.WriteLine($"Pin {pin.PinNumber} center black - {pin.CenterColor.IsCloseToBlack()} ({pin.CenterColor})");
                //Console.WriteLine($"Pin {pin.PinNumber} border white - {pin.BorderColors.Any(p => p.IsCloseToWhite())}");

                if (frameNumber < 10)
                {
                    if (blackCenter && !whiteBorder)
                        roll1.PinsKnockedDown.Add(pin.PinNumber);
                    else if (blackCenter && whiteBorder)
                        roll2.PinsKnockedDown.Add(pin.PinNumber);
                }
                else // different rules for frame 10 
                {
                    // each roll is essentially considered a separate frame, so we'll just have one roll per "frame" in the 10th
                    // it will be up to the consumer to interpret 10th frame rolls
                    // if there's a 9 -, for example, that should look like frame 10 and frame 11 both having the same roll1 pins knocked down
                    // if there's a 9 /, that would be frame 10 roll1 having 9 pins knocked down, and frame 11 roll1 having 10 pins knocked down
                    // an 8 1 would be frame 10 roll1 having 8 pins knocked down, and frame 11 roll1 having 9 pins where the extra pin is one of the two that was left standing
                    if (blackCenter)
                        roll1.PinsKnockedDown.Add(pin.PinNumber);
                }
            }

            frame.Rolls.Add(roll1);
            if (frameNumber < 10)
                frame.Rolls.Add(roll2);
            return frame; 
        }*/
    }

    public class Pin
    {
        public Color CenterColor { get; set; }

        public Color BorderColor { get; set; }

        public int PinNumber { get; set; }

        public Pin(int number, Color center, Color border)
        {
            PinNumber = number;
            CenterColor = center;
            BorderColor = border;
        }
    }

    public class GameImg
    {
        public string GameName { get; set; }

        public int GameNumber { get; set; }

        public int LabelStartPos { get; set; }

        public int GameStartPos { get; set; }

        public int GameEndPos { get; set; }
    }

    public class Frame
    {
        public int FrameNumber { get; set;
        }
        public List<Roll> Rolls { get; set; }
    }

    public class Roll
    {
        public List<int> PinsKnockedDown { get; set; }

        public int NumberOfPinsKnockedDown
        {
            get
            {
                return PinsKnockedDown?.Count ?? _numberOfPinsKnockedDown;
            }
            set
            {
                _numberOfPinsKnockedDown = value;
            }
        }

        private int _numberOfPinsKnockedDown;
    }
}