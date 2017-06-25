using System.ComponentModel;
using System;
#if !PocketPC
using com.magicsoftware.controls.designers;
using System.Windows.Forms;
using System.Drawing;
#endif

namespace com.magicsoftware.controls
{
   public class TableControlUnlimitedItemsRuntimeDesigner : TableControlUnlimitedItems, ISetSpecificControlPropertiesForFormDesigner
   {
      public TableControlUnlimitedItemsRuntimeDesigner()
         : base()
      {
         
      }

      public void setSpecificControlPropertiesForFormDesigner(Control fromControl)
      {
         // fixed defect #:132104, the context menu will not be allowed for runtime designer 
         ShowContextMenu = false;
      }
   }      
}
