using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace com.magicsoftware.controls.designers
{
   public class CanResizeArgs : EventArgs
   {
      public Panel Panel { get; private set; }
      public int Size { get; private set; }
      public Orientation Orientation { get; private set; } // denote the property which is changed

      public CanResizeArgs(Panel panel, int size, Orientation orientation)
      {
         Panel = panel;
         Size = size;
         Orientation = orientation;
      }
   }
}
