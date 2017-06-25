using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace com.magicsoftware.controls.designers
{
   public delegate bool CanResizeDelegate(object sender, CanResizeArgs canResizeArgs);

   public interface ICanResize
   {
      event CanResizeDelegate CanResizeEvent;
      bool CanResize(CanResizeArgs canResizeArgs);
   }
}
