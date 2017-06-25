using System;
using System.Diagnostics;

namespace com.magicsoftware.util
{
   /// <summary>
   /// 
   /// </summary>
   public class MgColor
   {
      public int Alpha { get; private set; }
      public int Blue { get; private set; }
      public int Green { get; private set; }
      public int Red { get; private set; }
      public bool IsTransparent { get; private set; }
      public bool IsSystemColor { get; set; }
      public MagicSystemColor SystemColor { get; set; }

      /// <summary>
      /// 
      /// </summary>
      public MgColor()
      {
         Alpha = 255;
         Blue = 0;
         Green = 0;
         Red = 0;
         IsTransparent = false;
         IsSystemColor = false;
         SystemColor = MagicSystemColor.Undefined;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="alpha"></param>
      /// <param name="red"></param>
      /// <param name="green"></param>
      /// <param name="blue"></param>
      /// <param name="magicSystemColor"></param>
      /// <param name="isTransparent"></param>
      public MgColor(int alpha, int red, int green, int blue, MagicSystemColor magicSystemColor, bool isTransparent)
      {
         Alpha = alpha;
         Red = red;
         Green = green;
         Blue = blue;
         SystemColor = magicSystemColor;
         IsTransparent = isTransparent;

         IsSystemColor = (SystemColor != MagicSystemColor.Undefined);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="colorStr"></param>
      public MgColor(String colorStr)
      {
         SystemColor = MagicSystemColor.Undefined;

         // check if it is a system color
         if (colorStr.StartsWith("FF"))
         {
            String subStr = colorStr.Substring(0, 8);
            int res = -hexToInt(subStr);

            // sometimes values of colors in magic color table are corrupted
            if (Enum.IsDefined(SystemColor.GetType(), res))
               SystemColor = (MagicSystemColor)res;

            IsSystemColor = (SystemColor != MagicSystemColor.Undefined);

            // initialize the rest of the final members
            Red = 0;
            Green = 0;
            Blue = 0;
            Alpha = 255;
         }
         else
         {
            Alpha = 255 - Convert.ToInt32(colorStr.Substring(0, (2) - (0)), 16); 
            Blue = Convert.ToInt32(colorStr.Substring(2, (4) - (2)), 16);
            Green = Convert.ToInt32(colorStr.Substring(4, (6) - (4)), 16);
            Red = Convert.ToInt32(colorStr.Substring(6, (8) - (6)), 16);

            // initialize the rest of the final members
            IsSystemColor = false;
         }
         IsTransparent = colorStr[8] == 'Y';
      }

      /// <summary> As opposed to Integer.parseInt() this method may recieve negative numbers in their two's complement
      /// representation. For example Integer.parseInt() will convert FF to 255 whereas this method will convert
      /// it to -1.
      /// </summary>
      /// <param name="hex">hex number in two's complment representation</param>
      /// <returns> int</returns>
      private int hexToInt(String hex)
      {
         Debug.Assert(hex.Length <= 8);

         int result = Int32.Parse(hex, System.Globalization.NumberStyles.HexNumber);

         return result;
      }
   }
}