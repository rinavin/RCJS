namespace com.magicsoftware.unipaas.util
{
   public class MgPoint
   {
      public int x;
      public int y;

      public MgPoint(int x, int y)
      {
         this.x = x;
         this.y = y;
      }
   }

   /// <summary>
   /// float mgpoint
   /// </summary>
   public class MgPointF
   {
      public float x;
      public float y;

      public MgPointF(float x, float y)
      {
         this.x = x;
         this.y = y;
      }

      public MgPointF(MgPoint point)
      {
         this.x = point.x;
         this.y = point.y;
      }

   }
}
