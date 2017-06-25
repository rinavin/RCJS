using System.ComponentModel;
#if !PocketPC
using com.magicsoftware.controls.designers;
#endif

namespace com.magicsoftware.controls
{
#if !PocketPC
   [Designer(typeof(MgTextBoxRuntimeDesignerDesigner))]
#endif
   public class MgTextBoxRuntimeDesigner : MgTextBox
   {

      public MgTextBoxRuntimeDesigner()
         : base()
      {
      }
   }
}
