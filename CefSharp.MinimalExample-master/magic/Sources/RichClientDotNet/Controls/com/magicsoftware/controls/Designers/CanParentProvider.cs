using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using System.ComponentModel.Design;
using System.ComponentModel;
using System.Collections;
using System.Drawing.Design;

namespace com.magicsoftware.controls.designers
{
  

   /// <summary>
   /// the class is responsible for providing answers to "CanParent" questions 
   /// </summary>
   public class CanParentProvider
   {
      ParentControlDesigner parentControlDesigner; //container's designer
      ICanParent container;   //container


      /// <summary>
      /// currently selected toolbox item
      /// </summary>
      ToolboxItem SelectedToolboxItem
      {
         get
         {
            ToolboxItem t = null; ;
            IToolboxService toolboxService = (IToolboxService)GetService(typeof(IToolboxService));
            if (toolboxService != null)
               t = toolboxService.GetSelectedToolboxItem((IDesignerHost)this.GetService(typeof(IDesignerHost)));
            return t;

         }
      }

      /// <summary>
      /// ctor
      /// </summary>
      /// <param name="p"></param>
      public CanParentProvider(ParentControlDesigner p)
      {
         this.parentControlDesigner = p;
         container = p.Component as ICanParent;
         if (container == null && p.Control != null)
            container = p.Control.Parent as ICanParent;

      }



      public bool CanEnterDrag(DragEventArgs de, bool isOnTable)
      {
         bool canEnterDrag = true;
         List<IComponent> comps = ControlUtils.GetDraggedComponents(de);
         if (comps.Count == 0) //this is drag operation
            canEnterDrag = CanDropFromSelectedToolboxItem(de, isOnTable);
         else //this is move operation
            foreach (var item in comps)
            {
               if (!CanParent(item.GetType(), (Control)item, DragOperationType.Move))
               {
                  canEnterDrag = false;
                  break;
               }
            }       

         if (!canEnterDrag) //remove the effect
            de.Effect = DragDropEffects.None;
         return canEnterDrag;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="de"></param>
      /// <returns></returns>
      public bool CanEnterDrag(DragEventArgs de)
      {
         return CanEnterDrag(de, false);
      }

      /// <summary>
      /// returns component site
      /// </summary>
      /// <param name="type"></param>
      /// <returns></returns>
      private object GetService(Type type)
      {
         return parentControlDesigner.Component.Site.GetService(type);
      }

      /// <summary>
      /// true if can drop item from selected toolbox item
      /// </summary>
      /// <returns></returns>
      public bool CanDropFromSelectedToolboxItem(DragEventArgs de, bool isOnTable)
      {
         bool canDrop = true;
         ToolboxItem toolboxItem = GetToolboxItem(de) ?? SelectedToolboxItem;
         if (toolboxItem != null)
         {
            IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));
            Type type;
            CanParentArgs canParentArgs;
            if (toolboxItem is ICanParentArgsProvider)
               canParentArgs = ((ICanParentArgsProvider)toolboxItem).GetCanParentArgs(host, isOnTable, DragOperationType.Drop);
            else
            {
               type = toolboxItem.GetType(host);
               canParentArgs = new CanParentArgs(type, null, DragOperationType.Drop);
            }

            if (canParentArgs.ChildControlType != null)
               canDrop = CanParent(canParentArgs);
            else
               canDrop = false;
         }
         return canDrop;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      public bool CanDropFromSelectedToolboxItem()
      {
         return CanDropFromSelectedToolboxItem(null, false);
      }

      /// <summary>
      /// get the current toolbox item from the drag event
      /// </summary>
      /// <param name="de"></param>
      /// <returns></returns>
      ToolboxItem GetToolboxItem(DragEventArgs de)
      {
         ToolboxItem t = null; ;
         IToolboxService toolboxService = (IToolboxService)GetService(typeof(IToolboxService));
         if (toolboxService != null && de != null)
            t = toolboxService.DeserializeToolboxItem(de.Data);
         return t;

      }

      /// <summary>
      /// can parent control
      /// </summary>
      /// <param name="type"></param>
      /// <param name="childControl"></param>
      /// <param name="dragOperationType">drop or move</param>
      /// <returns></returns>
      public bool CanParent(Type type, Control childControl, DragOperationType dragOperationType)
      {
         return CanParent(new CanParentArgs(type, childControl, dragOperationType));
      }

      public bool CanParent(CanParentArgs canParentArgs)
      {
         if (container != null)
            return container.CanParent(canParentArgs);
         else
            return true;
      }
   }
}
