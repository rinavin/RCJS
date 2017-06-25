using System;
using System.Windows.Forms;
using com.magicsoftware.win32;
using System.Drawing;

namespace com.magicsoftware.controls
{
   /// <summary>
   /// this class is fill gradient rect on modile and support only 2 style
   /// Horizontal & Vertical
   /// this class was copy from: http://msdn.microsoft.com/en-us/library/ms229655.aspx
   /// </summary>
   internal sealed class GradientFill
   {
      // This method wraps the PInvoke to GradientFill.
      // Parmeters:
      //  gr - The Graphics object we are filling
      //  rc - The rectangle to fill
      //  startColor - The starting color for the fill
      //  endColor - The ending color for the fill
      //  fillDir - The direction to fill
      //
      // Returns true if the call to GradientFill succeeded; false
      // otherwise.
      internal static bool Fill(
          Graphics gr,
          Rectangle rc,
          Color startColor, Color endColor,
          FillDirection fillDir)
      {

         // Initialize the data to be used in the call to GradientFill.
         user32.TRIVERTEX[] tva = new user32.TRIVERTEX[2];
         tva[0] = new user32.TRIVERTEX(rc.X, rc.Y, startColor);
         tva[1] = new user32.TRIVERTEX(rc.Right, rc.Bottom, endColor);
         user32.GRADIENT_RECT[] gra = new user32.GRADIENT_RECT[] {new user32.GRADIENT_RECT(0, 1)};

         // Get the hDC from the Graphics object.
         IntPtr hdc = gr.GetHdc();

         // PInvoke to GradientFill.
         bool b;

         b = user32.GradientFill(
                 hdc,
                 tva,
                 (uint)tva.Length,
                 gra,
                 (uint)gra.Length,
                 (uint)fillDir);
         System.Diagnostics.Debug.Assert(b, string.Format(
             "GradientFill failed: {0}",
             System.Runtime.InteropServices.Marshal.GetLastWin32Error()));

         // Release the hDC from the Graphics object.
         gr.ReleaseHdc(hdc);

         return b;
      }

      // The direction to the GradientFill will follow
      internal enum FillDirection
      {
         //
         // The fill goes horizontally
         //
         Horizontal = user32.GRADIENT_FILL_RECT_H, //  LeftToRight 
         //
         // The fill goes vertically
         //
         Vertical = user32.GRADIENT_FILL_RECT_V //TopToBottom 
      }
   }
}

