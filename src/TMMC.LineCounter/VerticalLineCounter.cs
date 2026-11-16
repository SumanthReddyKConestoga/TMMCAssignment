using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace TMMC.LineCounter
{
    internal static class VerticalLineCounter
    {
        private const int BlackThreshold = 128;
        private const int BytesPerPixel = 3;

        internal static int CountFile(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                throw new ArgumentException("The image path must not be empty.");
            }

            // On Windows, IsPathRooted alone also accepts C:image.jpg and \image.jpg.
            // Their roots have fewer than three characters and depend on the current
            // directory or drive. A full drive root or UNC share is independent of both.
            if (!Path.IsPathRooted(imagePath) || Path.GetPathRoot(imagePath).Length < 3)
            {
                throw new ArgumentException("The image path must be an absolute Windows path.");
            }

            // Open explicitly so missing files, denied access, and sharing violations
            // retain their useful I/O exception messages. GDI+ requires this stream to
            // stay open until the decoded image has been disposed.
            using (FileStream stream = File.OpenRead(imagePath))
            using (Image image = Image.FromStream(stream, false, true))
            {
                // Inspect the encoded format, not the extension: a renamed non-JPEG
                // file is not a valid assignment input.
                if (image.RawFormat.Guid != ImageFormat.Jpeg.Guid)
                {
                    throw new InvalidDataException("The input file must contain a JPEG image.");
                }

                return Count(image);
            }
        }

        internal static int Count(Image image)
        {
            if (image == null)
            {
                throw new ArgumentNullException("image");
            }

            // Every permitted line is straight, continuous, and present in both halves.
            // Therefore every line intersects row Height / 2, even when the height is
            // odd or a line does not touch the top/bottom edge. Each distinct line is
            // exactly one contiguous black run in this row.
            // Counting is O(width) time and O(width) extra space. JPEG decoding still
            // costs O(width * height) time and can allocate a full decoded image.
            using (Bitmap row = new Bitmap(image.Width, 1, PixelFormat.Format24bppRgb))
            {
                // Normalize only the selected row to a known pixel format. This also
                // handles grayscale JPEG decoders without assuming their pixel layout.
                // Equal pixel dimensions and nearest-neighbor sampling avoid blending
                // adjacent columns, which could erase a one-pixel line or white gap.
                using (Graphics graphics = Graphics.FromImage(row))
                {
                    graphics.CompositingMode = CompositingMode.SourceCopy;
                    graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                    graphics.PixelOffsetMode = PixelOffsetMode.Half;
                    graphics.DrawImage(image, new Rectangle(0, 0, image.Width, 1),
                        0, image.Height / 2, image.Width, 1, GraphicsUnit.Pixel);
                }

                byte[] pixels = new byte[checked(row.Width * BytesPerPixel)];
                BitmapData data = row.LockBits(new Rectangle(0, 0, row.Width, 1),
                    ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                try
                {
                    // Copy just the useful bytes of this one row; scanline alignment
                    // padding is not image content. No unsafe code or per-pixel native
                    // calls are needed, and a multi-row stride is never assumed.
                    Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
                }
                finally
                {
                    row.UnlockBits(data);
                }

                int count = 0;
                bool previousWasBlack = false;
                for (int offset = 0; offset < pixels.Length; offset += BytesPerPixel)
                {
                    // JPEG is lossy: nominal black/white pixels may decode as gray.
                    // Compare average BGR intensity with the midpoint instead of
                    // requiring RGB(0,0,0). Never merge gaps or discard thin runs.
                    int intensitySum = pixels[offset] + pixels[offset + 1] + pixels[offset + 2];
                    bool isBlack = intensitySum < BlackThreshold * BytesPerPixel;
                    if (isBlack && !previousWasBlack)
                    {
                        count++;
                    }

                    // Starting in white counts a run at the left edge. Counting on
                    // entry also counts the final run and an entirely black image.
                    previousWasBlack = isBlack;
                }

                return count;
            }
        }
    }
}
