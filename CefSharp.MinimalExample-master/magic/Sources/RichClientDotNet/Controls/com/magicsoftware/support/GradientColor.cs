using System.Drawing;
#if !PocketPC
using System.Runtime.Serialization;
#endif
using System;

namespace com.magicsoftware.controls
{
#if !PocketPC
   [Serializable]
#endif
   public struct GradientColor 
   {
      private const int PRIME_NUMBER = 37;
      private const int SEED = 23;

      public Color From { get; private set; }
      public Color To { get; private set; }

      public GradientColor(Color fromColor, Color toColor)
         : this()
      {
         this.From = fromColor;
         this.To = toColor;
      }

      public static bool operator ==(GradientColor gradientColor1, GradientColor gradientColor2)
      {
         return ((gradientColor1.From == gradientColor2.From) && (gradientColor1.To == gradientColor2.To));
      }

      public static bool operator !=(GradientColor gradientColor1, GradientColor gradientColor2)
      {
         return !(gradientColor1 == gradientColor2);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      public override int GetHashCode()
      {
         int hash = SEED;

         hash = PRIME_NUMBER * hash + From.GetHashCode();
         hash = PRIME_NUMBER * hash + To.GetHashCode();

         return hash;
      }

      public override bool Equals(object obj)
      {
         if (obj != null && obj is GradientColor)
            return (this == (GradientColor)obj);
         else
            return false;
      }
   }
}