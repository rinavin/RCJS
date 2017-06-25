using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.controls.designers;
using System.Drawing;
using System.Windows.Forms;

namespace com.magicsoftware.controls
{
   class TablePanelDragDropHandler : DragDropHandler
   {
      public TablePanelDragDropHandler(Control control, System.Windows.Forms.Design.Behavior.BehaviorService behaviorService, System.ComponentModel.Design.IDesignerHost iDesignerHost)
         : base(control, behaviorService, iDesignerHost)
      {

      }

      protected override void SetParent()
      {
         foreach (var c in components)
         {
            // When we drop control on panel  (i.e area below table control when Row Height is ste ), they should actually be children of Tables' Parnet .
            // So set the parent as TableControls parent .
            ((Control)c).Parent = this.control.Parent;
         }
      }
   }
}
