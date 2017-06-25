using System.ComponentModel;
#if !PocketPC
using com.magicsoftware.controls.designers;
using System.Windows.Forms;
using System.Security.Permissions;
using System.Drawing;
using System.Runtime.InteropServices;
#endif

namespace com.magicsoftware.controls
{

#if !PocketPC
   [Designer(typeof(MgWebBrowserRuntimeDesignerDesigner)), Docking(DockingBehavior.Never)]
   [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
   [ToolboxBitmap(typeof(WebBrowser))]
#endif
   [ComVisible(true)]
   public class MgWebBrowserRuntimeDesigner : MgWebBrowser
   {
      public MgWebBrowserRuntimeDesigner()
         : base()
      {
      }
   }      
}