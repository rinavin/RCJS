using System;
using System.Text;

namespace com.magicsoftware.util
{
   public class MgArrayList : System.Collections.ArrayList
   {
      /// <summary>
      /// add null cells up to 'size' cells.
      /// </summary>
      /// <param name="size"></param>
      public void SetSize(int size)
      {
         while (Count < size)
            Add(null);

         if (Count > size)
            RemoveRange(size, Count - size);
      }
   }
}
