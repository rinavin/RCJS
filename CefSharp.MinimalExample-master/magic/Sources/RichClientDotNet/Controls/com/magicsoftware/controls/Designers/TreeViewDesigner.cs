using System.ComponentModel.Design;
using System.Windows.Forms.Design;

namespace com.magicsoftware.controls.designers
{
   /// <summary>
   /// TreeViewDesigner - designer for MgTreeView
   /// </summary>
   internal class TreeViewDesigner : ControlDesigner
   {
      private DesignerActionListCollection actionLists;

      public TreeViewDesigner()
      {
         base.AutoResizeHandles = true;
      }

      public override DesignerActionListCollection ActionLists
      {
         get
         {
            if (this.actionLists == null)
               this.actionLists = new DesignerActionListCollection();

            return this.actionLists;
         }
      }
   }
}
