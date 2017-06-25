using System.Drawing;
using com.magicsoftware.util;

namespace com.magicsoftware.controls
{
   //TODO: Kaushal. The scope of this class and GetInstance() should be internal.
   public class SolidBrushCache : ResourcesCache<Color, SolidBrush>
   {
      private static SolidBrushCache _instance;

      private SolidBrushCache()
      {
      }

      public static SolidBrushCache GetInstance()
      {
         if (_instance == null)
         {
            // synchronize on the class object
            lock (typeof (SolidBrushCache))
            {
               if (_instance == null)
                  _instance = new SolidBrushCache();
            }
         }
         return _instance;
      }

      /// <summary>
      /// </summary>
      /// <param name="color"></param>
      /// <returns></returns>
      protected override SolidBrush CreateInstance(Color color)
      {
         return new SolidBrush(color);
      }
   }
}