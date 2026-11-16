using System;
using System.Globalization;
using System.IO;

namespace TMMC.LineCounter
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            return Run(args, Console.Out, Console.Error);
        }

        internal static int Run(string[] args, TextWriter output, TextWriter error)
        {
            try
            {
                // Check the argument count before accessing args[0]. Do not prompt or
                // pause: the application must also work in scripts and redirected consoles.
                if (args.Length != 1)
                {
                    error.WriteLine("Error: Exactly one argument is required: the absolute path of a JPEG image.");
                    error.WriteLine("Usage: TMMC.LineCounter.exe \"C:\\images\\img_1.jpg\"");
                    return 2;
                }

                int count = VerticalLineCounter.CountFile(args[0]);
                output.WriteLine(count.ToString(CultureInfo.InvariantCulture));
                return 0;
            }
            catch (Exception exception)
            {
                // This is the application boundary: report recoverable .NET exceptions
                // instead of exposing an unhandled-exception crash to the caller.
                // Keep errors separate so successful standard output is only a number.
                error.WriteLine("Error ({0}): {1}", exception.GetType().Name, exception.Message);
                return 1;
            }
        }
    }
}
