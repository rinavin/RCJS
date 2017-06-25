using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms.Design;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.ComponentModel.Design;

namespace com.magicsoftware.controls.designers
{
   class MgPanelRuntimeDesignerDesigner : MgPanelDesigner
   {
      public MgPanelRuntimeDesignerDesigner()
      {

      }

      public override System.ComponentModel.Design.DesignerVerbCollection Verbs
      {
         get
         {
            return new DesignerVerbCollection();
         }
      }
   }
}
