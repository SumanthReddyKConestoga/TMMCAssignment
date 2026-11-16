using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using CounterProgram = TMMC.LineCounter.Program;

namespace TMMC.LineCounter.Tests
{
    internal static class Program
    {
        private static int passed;
        private static int failed;
        private static string temporaryDirectory;

        private static int Main()
        {
            temporaryDirectory = Path.Combine(Path.GetTempPath(), "TMMC-LineCounter-Tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temporaryDirectory);
            try
            {
                RunTests();
            }
            catch (Exception exception)
            {
                failed++;
                Console.Error.WriteLine("Test infrastructure failure: " + exception);
            }
            finally
            {
                // This directory is unique to this test run; no supplied files are modified.
                Directory.Delete(temporaryDirectory, true);
            }

            Console.WriteLine("{0} passed; {1} failed.", passed, failed);
            return failed == 0 ? 0 : 1;
        }

        private static void RunTests()
        {
            int[] expectedCounts = { 1, 3, 1, 7 };
            for (int index = 0; index < expectedCounts.Length; index++)
            {
                int expected = expectedCounts[index];
                string name = "img_" + (index + 1) + ".jpg";
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test-images", name);
                Test(name + " = " + expected + " (actual executable)", delegate
                {
                    AssertProcess("\"" + path + "\"", 0, expected.ToString(CultureInfo.InvariantCulture) + Environment.NewLine, null);
                });
            }

            Test("zero arguments (actual executable)", delegate { AssertProcess("", 2, "", "Exactly one argument"); });
            Test("two arguments (actual executable)", delegate { AssertProcess("one two", 2, "", "Exactly one argument"); });
            Test("empty argument", delegate { AssertError(new[] { "" }, "must not be empty"); });
            Test("whitespace argument", delegate { AssertError(new[] { "   " }, "must not be empty"); });
            Test("relative path", delegate { AssertError(new[] { "img_1.jpg" }, "absolute Windows path"); });
            Test("drive-relative path", delegate { AssertError(new[] { "C:img_1.jpg" }, "absolute Windows path"); });
            Test("root-relative path", delegate { AssertError(new[] { "\\img_1.jpg" }, "absolute Windows path"); });
            Test("forward-slash root-relative path", delegate { AssertError(new[] { "/img_1.jpg" }, "absolute Windows path"); });
            Test("missing file", delegate { AssertError(new[] { Path.Combine(temporaryDirectory, "missing.jpg") }, "FileNotFoundException"); });
            Test("directory as input", delegate { AssertError(new[] { temporaryDirectory }, "Error ("); });
            Test("invalid path characters", delegate { AssertError(new[] { temporaryDirectory + "\\bad\0.jpg" }, "ArgumentException"); });
            Test("corrupt JPEG", delegate
            {
                string path = Path.Combine(temporaryDirectory, "corrupt.jpg");
                File.WriteAllText(path, "This is not a JPEG.");
                AssertError(new[] { path }, "Error (");
            });
            Test("empty JPEG", delegate
            {
                string path = Path.Combine(temporaryDirectory, "empty.jpg");
                File.WriteAllBytes(path, new byte[0]);
                AssertError(new[] { path }, "Error (");
            });
            Test("PNG renamed to JPEG", delegate
            {
                string path = Path.Combine(temporaryDirectory, "renamed.jpg");
                using (Bitmap image = MakeImage(30, 30, new Rectangle(10, 0, 3, 30)))
                {
                    image.Save(path, ImageFormat.Png);
                }
                AssertError(new[] { path }, "must contain a JPEG");
            });
            Test("locked file", delegate
            {
                string path = Path.Combine(temporaryDirectory, "locked.jpg");
                using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                {
                    AssertError(new[] { path }, "IOException");
                }
            });

            TestImage("white image", 43, 37, 0);
            TestImage("black image", 43, 37, 1, new Rectangle(0, 0, 43, 37));
            TestImage("left and right borders", 101, 40, 2, new Rectangle(0, 0, 7, 40), new Rectangle(94, 0, 7, 40));
            TestImage("one-pixel-wide image", 1, 40, 1, new Rectangle(0, 0, 1, 40));
            TestImage("single white pixel", 1, 1, 0);
            TestImage("single black pixel", 1, 1, 1, new Rectangle(0, 0, 1, 1));
            TestImage("one-pixel lines and gaps", 7, 19, 4, new Rectangle(0, 0, 1, 19),
                new Rectangle(2, 0, 1, 19), new Rectangle(4, 0, 1, 19), new Rectangle(6, 0, 1, 19));
            TestImage("minimum crossing at even center", 61, 40, 2,
                new Rectangle(3, 19, 5, 2), new Rectangle(30, 19, 10, 2));
            TestImage("minimum crossing at odd center", 61, 41, 2,
                new Rectangle(3, 19, 5, 3), new Rectangle(30, 19, 10, 3));
            TestImage("different lengths and widths", 127, 101, 3,
                new Rectangle(3, 3, 2, 90), new Rectangle(25, 20, 30, 70), new Rectangle(91, 45, 11, 12));

            Test("gray threshold and row padding", delegate
            {
                using (Bitmap image = new Bitmap(5, 5))
                {
                    Color[] colors = { Color.FromArgb(127, 127, 127), Color.FromArgb(128, 128, 128),
                        Color.FromArgb(20, 20, 20), Color.FromArgb(230, 230, 230), Color.Black };
                    for (int column = 0; column < colors.Length; column++)
                    {
                        for (int row = 0; row < image.Height; row++)
                        {
                            image.SetPixel(column, row, colors[column]);
                        }
                    }
                    Equal(3, VerticalLineCounter.Count(image));
                }
            });
            Test("JPEG compression qualities", delegate
            {
                using (Bitmap image = MakeImage(151, 81, new Rectangle(0, 3, 4, 70),
                    new Rectangle(20, 20, 12, 50), new Rectangle(62, 0, 2, 81), new Rectangle(143, 0, 8, 81)))
                {
                    foreach (long quality in new long[] { 10, 30, 60, 90, 100 })
                    {
                        string path = Path.Combine(temporaryDirectory, "quality-" + quality + ".jpg");
                        SaveJpeg(image, path, quality);
                        AssertSuccess(path, 4);
                    }
                }
            });
            Test("absolute path with spaces and Unicode; releases file", delegate
            {
                string path = Path.Combine(temporaryDirectory, "image with spaces \u6d4b\u8bd5.jpg");
                using (Bitmap image = MakeImage(31, 31, new Rectangle(4, 0, 3, 31)))
                {
                    SaveJpeg(image, path, 95);
                }
                AssertProcess("\"" + path + "\"", 0, "1" + Environment.NewLine, null);
                AssertSuccess(path.Replace('\\', '/'), 1);
                for (int repeat = 0; repeat < 10; repeat++)
                {
                    AssertSuccess(path, 1);
                }
                // An exclusive open detects image/stream handles leaked by the counter.
                using (FileStream exclusive = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    if (exclusive.Length == 0) { throw new Exception("JPEG unexpectedly empty."); }
                }
            });
            Test("100 deterministic generated JPEGs", delegate
            {
                Random random = new Random(4621);
                for (int sample = 0; sample < 100; sample++)
                {
                    int expected = random.Next(0, 21);
                    int height = random.Next(10, 100);
                    Rectangle[] lines = new Rectangle[expected];
                    int column = random.Next(0, 5);
                    for (int line = 0; line < expected; line++)
                    {
                        int width = random.Next(1, 13);
                        int top = random.Next(0, height / 2);
                        int bottom = random.Next(height / 2 + 1, height + 1);
                        lines[line] = new Rectangle(column, top, width, bottom - top);
                        column += width + random.Next(1, 10);
                    }
                    using (Bitmap image = MakeImage(column + 5, height, lines))
                    {
                        // Expected counts come from separated rectangles constructed
                        // here, independently of the counter's pixel classification.
                        Equal(expected, VerticalLineCounter.Count(image));
                        string path = Path.Combine(temporaryDirectory, "generated.jpg");
                        SaveJpeg(image, path, 95);
                        AssertSuccess(path, expected);
                    }
                }
            });
        }

        private static void TestImage(string name, int width, int height, int expected, params Rectangle[] lines)
        {
            Test(name, delegate
            {
                using (Bitmap image = MakeImage(width, height, lines))
                {
                    Equal(expected, VerticalLineCounter.Count(image));
                    string path = Path.Combine(temporaryDirectory, "boundary.jpg");
                    SaveJpeg(image, path, 95);
                    AssertSuccess(path, expected);
                }
            });
        }

        private static Bitmap MakeImage(int width, int height, params Rectangle[] lines)
        {
            Bitmap image = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            using (Graphics graphics = Graphics.FromImage(image))
            {
                graphics.Clear(Color.White);
                foreach (Rectangle line in lines)
                {
                    graphics.FillRectangle(Brushes.Black, line);
                }
            }
            return image;
        }

        private static void SaveJpeg(Image image, string path, long quality)
        {
            foreach (ImageCodecInfo codec in ImageCodecInfo.GetImageEncoders())
            {
                if (codec.FormatID == ImageFormat.Jpeg.Guid)
                {
                    using (EncoderParameters parameters = new EncoderParameters(1))
                    {
                        parameters.Param[0] = new EncoderParameter(Encoder.Quality, quality);
                        image.Save(path, codec, parameters);
                    }
                    return;
                }
            }
            throw new Exception("The Windows JPEG encoder is unavailable.");
        }

        private static void AssertSuccess(string path, int expected)
        {
            using (StringWriter output = new StringWriter())
            using (StringWriter error = new StringWriter())
            {
                Equal(0, CounterProgram.Run(new[] { path }, output, error));
                Equal(expected.ToString(CultureInfo.InvariantCulture) + Environment.NewLine, output.ToString());
                Equal("", error.ToString());
            }
        }

        private static void AssertError(string[] arguments, string messageFragment)
        {
            using (StringWriter output = new StringWriter())
            using (StringWriter error = new StringWriter())
            {
                Equal(1, CounterProgram.Run(arguments, output, error));
                Equal("", output.ToString());
                Contains(error.ToString(), messageFragment);
            }
        }

        private static void AssertProcess(string arguments, int exitCode, string expectedOutput, string errorFragment)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(typeof(VerticalLineCounter).Assembly.Location, arguments);
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            using (Process process = Process.Start(startInfo))
            {
                if (!process.WaitForExit(10000))
                {
                    process.Kill();
                    throw new Exception("The application did not exit within ten seconds.");
                }
                Equal(exitCode, process.ExitCode);
                Equal(expectedOutput, process.StandardOutput.ReadToEnd());
                string error = process.StandardError.ReadToEnd();
                if (errorFragment == null) { Equal("", error); }
                else { Contains(error, errorFragment); }
            }
        }

        private static void Test(string name, Action test)
        {
            try
            {
                test();
                passed++;
                Console.WriteLine("PASS " + name);
            }
            catch (Exception exception)
            {
                failed++;
                Console.Error.WriteLine("FAIL {0}: {1}", name, exception);
            }
        }

        private static void Equal<TValue>(TValue expected, TValue actual)
        {
            if (!object.Equals(expected, actual))
            {
                throw new Exception(string.Format("Expected <{0}>, got <{1}>.", expected, actual));
            }
        }

        private static void Contains(string actual, string expectedFragment)
        {
            if (!actual.Contains(expectedFragment))
            {
                throw new Exception("Expected error containing <" + expectedFragment + ">, got <" + actual + ">.");
            }
        }
    }
}
