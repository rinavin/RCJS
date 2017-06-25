using System.Drawing;
using com.magicsoftware.util;

namespace com.magicsoftware.controls
{
   //TODO: Kaushal. The scope of this class and GetInstance() should be internal.
   public class PensCache : ResourcesCache<Color, Pen>
   {
      private static PensCache _instance;

      /// <summary>
      /// 
      /// </summary>
      private PensCache()
      {
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      public static PensCache GetInstance()
      {
         if (_instance == null)
         {
            // synchronize on the class object
            lock (typeof (PensCache))
            {
               if (_instance == null)
                  _instance = new PensCache();
            }
         }
         return _instance;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="color"></param>
      /// <returns></returns>
      protected override Pen CreateInstance(Color color)
      {
         return new Pen(color);
      }
   }
}