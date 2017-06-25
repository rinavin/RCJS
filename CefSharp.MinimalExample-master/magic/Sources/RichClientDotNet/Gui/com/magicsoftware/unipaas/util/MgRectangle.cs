using System;
namespace com.magicsoftware.unipaas.util
{
	public class MgRectangle
	{
		public int x;
		public int y;
		public int width;
		public int height;

      public bool IsEmpty
      {
         get
         {
            return (x == 0 && y == 0 && width == 0 && height == 0);
         }
      }

		public MgRectangle(int x, int y, int width, int height)
		{
			this.x = x;
			this.y = y;
			this.width = width;
			this.height = height;
		}
		
		public MgRectangle()
		{
		}

      /// <summary> Returns the difference in size from the the specified rectangle. </summary>
      /// <param name="rectangle"></param>
      /// <returns></returns>
      internal MgPoint GetSizeDifferenceFrom(MgRectangle rectangle)
      {
         MgPoint sizeDifference = new MgPoint(0, 0);

         sizeDifference.x = this.width - rectangle.width;
         sizeDifference.y = this.height - rectangle.height;

         return sizeDifference;
      }
	}
}
