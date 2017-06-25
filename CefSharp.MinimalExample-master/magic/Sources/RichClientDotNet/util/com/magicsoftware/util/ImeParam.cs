using System;
using System.Collections.Generic;
using System.Text;

namespace com.magicsoftware.util
{
   public class ImeParam
   {
      public IntPtr HWnd;
      public int Msg;
      public IntPtr WParam;
      public IntPtr LParam;
      //public int selStart;
      //public int selEnd;

      public ImeParam()
      {
         Reset();
      }

      public ImeParam(IntPtr h, int m, IntPtr w, IntPtr l)
      {
         HWnd = h;
         Msg = m;
         WParam = w;
         LParam = l;
      }

      public void Reset()
      {
         HWnd = (IntPtr)0;
         Msg = 0;
         LParam = (IntPtr)0;
         WParam = (IntPtr)0;
         //selStart = 0;
         //selEnd = 0;
      }
   }
}
