// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using JetBrains.UI.Interop;

namespace JetBrains.Omea.Base
{
    public class GraphicalUtils
    {
        public static Image GenerateImageThumbnail( Image oImage, int nMaxWidth, int nMaxHeight )
        {
            int nWidth = oImage.Width;
            int nHeight = oImage.Height;

            //calculate the thumb image size if needed
            if (oImage.Width > nMaxWidth || oImage.Height > nMaxHeight)
            {
                float fRatio;
                if (oImage.Width >= oImage.Height)
                {
                    fRatio = ((float)oImage.Height) / ((float)oImage.Width);
                    nWidth = nMaxWidth;
                    nHeight = Convert.ToInt32(nMaxHeight * fRatio);
                }
                else
                {
                    fRatio = ((float)oImage.Width) / ((float)oImage.Height);
                    nWidth = Convert.ToInt32(nMaxWidth * fRatio);
                    nHeight = nMaxHeight;
                }
            }

            //create the thumbnail ans set it’s settings
            Image thumbnail = new Bitmap(nWidth, nHeight);
            Graphics oGraphic = Graphics.FromImage(thumbnail);

            oGraphic.InterpolationMode = InterpolationMode.HighQualityBicubic;
            oGraphic.SmoothingMode = SmoothingMode.HighQuality;
            oGraphic.PixelOffsetMode = PixelOffsetMode.HighQuality;
            oGraphic.CompositingQuality = CompositingQuality.HighQuality;

            oGraphic.DrawImage(oImage, 0, 0, nWidth, nHeight);

            return thumbnail;
        }

        public static Bitmap ConvertIco2Bmp( Icon icon )
        {
            return ConvertIco2Bmp( icon, SystemBrushes.Window );
        }

        public static Bitmap ConvertIco2Bmp( Icon icon, Brush backBrush )
        {
            Bitmap bmp;

            // Declare the raw handles
            IntPtr hdcDisplay = IntPtr.Zero, hdcMem = IntPtr.Zero, hBitmap = IntPtr.Zero;
            try
            {
                // Get the display's Device Context so that the generated bitmaps were compatible
                hdcDisplay = Win32Declarations.CreateDC("DISPLAY", null, null, IntPtr.Zero);
                if (hdcDisplay == IntPtr.Zero)
                    throw new Exception("Failed to get the display device context.");

                // Create a pixel format compatible device context, and a bitmap to draw into using this context
                hdcMem = Win32Declarations.CreateCompatibleDC(hdcDisplay);
                if (hdcMem == IntPtr.Zero)
                    throw new Exception("Could not cerate a compatible device context.");
                hBitmap = Win32Declarations.CreateCompatibleBitmap(hdcDisplay, icon.Width, icon.Height);
                if (hBitmap == IntPtr.Zero)
                    throw new Exception("Could not cerate a compatible offscreen bitmap.");
                Win32Declarations.SelectObject(hdcMem, hBitmap);

                // Attach a GDI+ Device Context to the offscreen facility, and render the scene
                using (Graphics gm = Graphics.FromHdc(hdcMem))
                {
                    gm.FillRectangle(backBrush, 0, 0, icon.Width, icon.Height);
                    gm.DrawIcon(icon, 0, 0);
                }

                // Wrap the offscreen bitmap into a .NET bitmap in order to save it
                bmp = Image.FromHbitmap( hBitmap);
            }
            finally
            {
                // Cleanup the native resources in use
                if (hBitmap != IntPtr.Zero)
                    Win32Declarations.DeleteObject(hBitmap);
                if (hdcMem != IntPtr.Zero)
                    Win32Declarations.DeleteDC(hdcMem);
                if (hdcDisplay != IntPtr.Zero)
                    Win32Declarations.DeleteDC(hdcDisplay);
            }
            return bmp;
        }

        public static Bitmap ConvertIco2Bmp( Image image, Brush backBrush )
        {
            Bitmap bmp;

            // Declare the raw handles
            IntPtr hdcDisplay = IntPtr.Zero, hdcMem = IntPtr.Zero, hBitmap = IntPtr.Zero;
            try
            {
                // Get the display's Device Context so that the generated bitmaps were compatible
                hdcDisplay = Win32Declarations.CreateDC( "DISPLAY", null, null, IntPtr.Zero );
                if (hdcDisplay == IntPtr.Zero)
                    throw new Exception( "Failed to get the display device context." );

                // Create a pixel format compatible device context, and a bitmap to draw into using this context
                hdcMem = Win32Declarations.CreateCompatibleDC( hdcDisplay );
                if (hdcMem == IntPtr.Zero)
                    throw new Exception( "Could not cerate a compatible device context." );
                hBitmap = Win32Declarations.CreateCompatibleBitmap( hdcDisplay, image.Width, image.Height );
                if (hBitmap == IntPtr.Zero)
                    throw new Exception( "Could not cerate a compatible offscreen bitmap." );
                Win32Declarations.SelectObject( hdcMem, hBitmap );

                // Attach a GDI+ Device Context to the offscreen facility, and render the scene
                using( Graphics gm = Graphics.FromHdc( hdcMem ) )
                {
                    gm.FillRectangle( backBrush, 0, 0, image.Width, image.Height );
                    gm.DrawImage( image, 0, 0 );
                }

                // Wrap the offscreen bitmap into a .NET bitmap in order to save it
                bmp = Image.FromHbitmap( hBitmap );
            }
            finally
            {
                // Cleanup the native resources in use
                if( hBitmap != IntPtr.Zero )
                    Win32Declarations.DeleteObject( hBitmap );
                if( hdcMem != IntPtr.Zero )
                    Win32Declarations.DeleteDC( hdcMem );
                if( hdcDisplay != IntPtr.Zero )
                    Win32Declarations.DeleteDC( hdcDisplay );
            }
            return bmp;
        }
    }
}
