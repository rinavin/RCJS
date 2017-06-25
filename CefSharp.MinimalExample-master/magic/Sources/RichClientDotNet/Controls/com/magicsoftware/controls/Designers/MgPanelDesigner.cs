using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace com.magicsoftware.controls.designers
{
   /// <summary>
   /// designer for MgPanel. based on PanelDesigner, with an override for OnDragEnter, to enable blocking
   /// of re-parenting in the run-time designer
   /// </summary>
   internal class MgPanelDesigner : PanelControlDesigner
   {
      public MgPanelDesigner()
      {
         base.AutoResizeHandles = true;
      }

      public override void Initialize(IComponent component)
      {
         base.Initialize(component);
         canParentProvider = new CanParentProvider(this);
      }

      CanParentProvider canParentProvider;

      protected override void OnDragEnter(DragEventArgs de)
      {
         if (canParentProvider.CanEnterDrag(de))
            base.OnDragEnter(de);
      }

      protected override void DrawBorder(Graphics graphics)
      {

      }


      /// <summary>
      /// 
      /// </summary>
      /// <param name="e"></param>
      protected override void OnDragDrop(DragEventArgs e)
      {
         List<IComponent> components = ControlUtils.GetDraggedComponents(e);

         ((IMgContainer)Component).OnComponentDropped(new ComponentDroppedArgs() { Components = components });

         base.OnDragDrop(e);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="e"></param>
      public void OnDragOverInternal(DragEventArgs e)
      {
         OnDragOver(e);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="e"></param>
      public void OnDragDropInternal(DragEventArgs e)
      {
         OnDragDrop(e);
      }
   }
}


