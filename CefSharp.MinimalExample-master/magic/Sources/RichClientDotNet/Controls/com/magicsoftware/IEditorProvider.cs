using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace Controls.com.magicsoftware
{
   /// <summary>
   /// Provides Editor for logical control
   /// </summary>
   public interface IEditorProvider
   {
      /// <summary>
      /// returns editor of the logical control
      /// </summary>
      /// <returns></returns>
      Control getEditorControl();
   }
}
