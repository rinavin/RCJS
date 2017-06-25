using System.ComponentModel;
#if !PocketPC
using com.magicsoftware.controls.designers;
using System.Windows.Forms;
#endif

namespace com.magicsoftware.controls
{
#if !PocketPC
   [Designer(typeof(MgPanelRuntimeDesignerDesigner)), Docking(DockingBehavior.Never)]
#endif
   public class MgPanelRuntimeDesigner : MgPanel
   {
      public MgPanelRuntimeDesigner()
         : base()
      {
      }
   }      
}