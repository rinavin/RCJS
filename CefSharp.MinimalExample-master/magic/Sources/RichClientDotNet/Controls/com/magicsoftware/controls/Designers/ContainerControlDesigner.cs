using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using com.magicsoftware.controls.designers;
using System.Windows.Forms;
#if !PocketPC
using System.Windows.Forms.Design;
#endif

namespace com.magicsoftware.controls.designers
{
#if !PocketPC
   /// <summary>
   /// Designer for Container Control
   /// </summary>
   public class ContainerControlDesigner : DocumentDesigner
   {
      public override void Initialize(IComponent component)
      {
         base.Initialize(component);
         canParentProvider = new CanParentProvider(this);
      }

      protected override void OnPaintAdornments(PaintEventArgs pe)
      {
         // do not do anything here as we do not want to paint grid
      }

      #region CanParent
      CanParentProvider canParentProvider;


      /// <summary>
      /// handle  drop using selected toolbox item
      /// </summary>
      /// <param name="x"></param>
      /// <param name="y"></param>
      protected override void OnMouseDragBegin(int x, int y)
      {
         if (canParentProvider.CanDropFromSelectedToolboxItem())
            base.OnMouseDragBegin(x, y);
      }

      /// <summary>
      /// handle drag/drop or move
      /// </summary>
      /// <param name="de"></param>
      protected override void OnDragEnter(DragEventArgs de)
      {
         if (canParentProvider.CanEnterDrag(de))
            base.OnDragEnter(de);
      }
      #endregion


      public override SelectionRules SelectionRules
      {
         get
         {
            return SelectionRules.None;
         }
      }
   }
#endif
}
