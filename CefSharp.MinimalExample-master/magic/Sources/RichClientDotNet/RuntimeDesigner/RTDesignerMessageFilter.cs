using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel.Design;
using com.magicsoftware.win32;
using System.Collections;
using System.ComponentModel;

namespace RuntimeDesigner
{
   /// <summary>
   /// message filter for the runtime designer
   /// </summary>
   class RTDesignerMessageFilter : IMessageFilter
   {
      DesignerActionUIService actionService;
      ISelectionService selectionService;

      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="actionService"></param>
      /// <param name="selectionService"></param>
      public RTDesignerMessageFilter(DesignerActionUIService actionService, ISelectionService selectionService)
      {
         this.actionService = actionService;
         this.selectionService = selectionService;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="m"></param>
      /// <returns></returns>
      public bool PreFilterMessage(ref Message m)
      {
         switch (m.Msg)
         {
            // close the smart tags when the user clicks somewhere
            case NativeWindowCommon.WM_LBUTTONDOWN:
            case NativeWindowCommon.WM_RBUTTONDOWN:
               HideSmartTag(actionService, selectionService);
               break;
         }

         return false;
      }

      /// <summary>
      /// hide smart tag for all selected controls
      /// </summary>
      void HideSmartTag(DesignerActionUIService actionUIService, ISelectionService selectionService)
      {
         if (actionUIService != null)
         {
            ICollection collection = selectionService.GetSelectedComponents();

            foreach (var item in collection)
               actionUIService.HideUI(item as Component);
         }
      }
   }
}
