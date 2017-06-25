using System;
using System.Windows.Forms;

namespace com.magicsoftware.controls
{
   public class MgToolStripMenuItem : ToolStripMenuItem
   {
       public MgToolStripMenuItem()
       {
           //Story 126642 : If the number of menus are larger than the window can occupy, we use Overflow property and let system handle the displaying of menus
           this.Overflow = ToolStripItemOverflow.AsNeeded;
       }

       public override bool CanSelect
       {
           get
           {
               return this.Enabled;
           }
       }
   }
}