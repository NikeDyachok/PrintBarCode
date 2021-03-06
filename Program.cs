﻿using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.IO;
using ZXing;
using ZXing.Rendering;

namespace PrintPDF {
    class Program {

        struct BarCodeInfo {
            int Width;
            int Height;
            int Margin;
            ZXing.BarcodeFormat Format;
            string Code;

        };
        static void Main (string[] args) {
            PrintDocument pd = new PrintDocument ();
            pd.DocumentName = "test1";
            pd.PrinterSettings = new PrinterSettings { PrinterName = "Microsoft XPS Document Writer", PrintToFile = true, PrintFileName = ".\test.oxps" };
            pd.PrintPage += new PrintPageEventHandler (pd_PrintPage);
            pd.Print ();
        }

        static System.Drawing.Bitmap getBarCodeBitmap () {
            var info = new { Width = 100, Height = 50, Margin = 10, Format = ZXing.BarcodeFormat.CODE_128, Code = "12345EAN12345678", ShowCode = false };
            var width = info.Width; // width of the Qr Code   
            var height = info.Height; // height of the Qr Code   
            var margin = info.Margin;
            var CodeWriter = new ZXing.BarcodeWriterPixelData {
                Format = info.Format,
                Options = new ZXing.Common.EncodingOptions { Height = height, Width = width, Margin = margin },

            };

            var pixelData = CodeWriter.Write (info.Code);
            // creating a bitmap from the raw pixel data; if only black and white colors are used it makes no difference
            // that the pixel data ist BGRA oriented and the bitmap is initialized with RGB
            using (var bitmap = new
                System.Drawing.Bitmap (pixelData.Width, pixelData.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb))
            using (var ms = new MemoryStream ()) {
                var bitmapData = bitmap.LockBits (new System.Drawing.Rectangle (0, 0, pixelData.Width, pixelData.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                try {
                    // we assume that the row stride of the bitmap is aligned to 4 byte multiplied by the width of the image
                    System.Runtime.InteropServices.Marshal.Copy (pixelData.Pixels, 0, bitmapData.Scan0, pixelData.Pixels.Length);
                } finally {
                    bitmap.UnlockBits (bitmapData);
                }
                return new Bitmap (bitmap);
            }
        }

        static void AddDownString (System.Drawing.Bitmap source) {
            var info = new { Width = 100, Height = 50, Margin = 10, Format = ZXing.BarcodeFormat.CODE_128, Code = "12345EAN12345678", ShowCode = true };

            using (Graphics g = Graphics.FromImage (source)) {

                using (Font font = new Font ("Tahoma", 12)) {
                    StringFormat stringFormat = new StringFormat ();
                    stringFormat.Alignment = StringAlignment.Center;
                    stringFormat.LineAlignment = StringAlignment.Near;

                    var size = g.MeasureString (info.Code, font);

                    RectangleF rectf = new RectangleF (0, source.Height - size.Height, source.Width, source.Height);
                    g.DrawRectangle (Pens.White, Rectangle.Round (rectf));
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.FillRectangle (Brushes.White, rectf);

                    g.DrawString (info.Code, font, Brushes.Black, rectf, stringFormat);

                    g.Flush ();
                    //bitmap.Save (ms, System.Drawing.Imaging.ImageFormat.Png);
                }
                //return new Bitmap(source);
                //return source;
            }
        }

        private static void GetPointBounds (PointF[] points,
            out float xmin, out float xmax,
            out float ymin, out float ymax) {
            xmin = points[0].X;
            xmax = xmin;
            ymin = points[0].Y;
            ymax = ymin;
            foreach (PointF point in points) {
                if (xmin > point.X) xmin = point.X;
                if (xmax < point.X) xmax = point.X;
                if (ymin > point.Y) ymin = point.Y;
                if (ymax < point.Y) ymax = point.Y;
            }
        }

        // Return a bitmap rotated around its center.
        private static Bitmap RotateBitmap (Bitmap bm, float angle) {
            // Make a Matrix to represent rotation
            // by this angle.
            Matrix rotate_at_origin = new Matrix ();
            rotate_at_origin.Rotate (angle);

            // Rotate the image's corners to see how big
            // it will be after rotation.
            PointF[] points = {
                new PointF (0, 0),
                new PointF (bm.Width, 0),
                new PointF (bm.Width, bm.Height),
                new PointF (0, bm.Height),
            };
            rotate_at_origin.TransformPoints (points);
            float xmin, xmax, ymin, ymax;
            GetPointBounds (points, out xmin, out xmax,
                out ymin, out ymax);

            // Make a bitmap to hold the rotated result.
            int wid = (int) Math.Round (xmax - xmin);
            int hgt = (int) Math.Round (ymax - ymin);
            Bitmap result = new Bitmap (wid, hgt);

            // Create the real rotation transformation.
            Matrix rotate_at_center = new Matrix ();
            rotate_at_center.RotateAt (angle,
                new PointF (wid / 2f, hgt / 2f));

            // Draw the image onto the new bitmap rotated.
            using (Graphics gr = Graphics.FromImage (result)) {
                // Use smooth image interpolation.
                gr.InterpolationMode = InterpolationMode.High;

                // Clear with the color in the image's upper left corner.
                gr.Clear (bm.GetPixel (0, 0));

                //// For debugging. (It's easier to see the background.)
                //gr.Clear(Color.LightBlue);

                // Set up the transformation to rotate.
                gr.Transform = rotate_at_center;

                // Draw the image centered on the bitmap.
                int x = (wid - bm.Width) / 2;
                int y = (hgt - bm.Height) / 2;
                gr.DrawImage (bm, x, y);
            }

            // Return the result bitmap.
            return result;
        }

        static void pd_PrintPage (object sender, PrintPageEventArgs e) {
            var info = new { Width = 100, Height = 50, Margin = 10, Format = ZXing.BarcodeFormat.CODE_128, Code = "12345EAN12345678", ShowCode = false };
            var bitmap = getBarCodeBitmap ();
            AddDownString (bitmap);
            bitmap = RotateBitmap (bitmap, 90);
            bitmap.Save ("temp1.bmp");
            e.Graphics.DrawImage (bitmap, 0, 0);
        }
    }
}