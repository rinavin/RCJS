using System;
using System.Windows.Forms.Design;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel.Design;
using com.magicsoftware.controls.utils;

namespace com.magicsoftware.controls.designers
{
   /// <summary>
   /// GroupBoxDesigner - original designer for groupbox + can parent functionality
   /// </summary>
   public class GroupBoxDesigner : ParentControlDesigner
   {
      #region original GroupBoxDesigner
      //private /*InheritanceUI*/ object inheritanceUI; //TODO

      protected override void OnPaintAdornments(PaintEventArgs pe)
      {
         if (this.DrawGrid)
         {
            Control control = this.Control;
            Rectangle displayRectangle = this.Control.DisplayRectangle;
            displayRectangle.Width++;
            displayRectangle.Height++;
            ControlPaint.DrawGrid(pe.Graphics, displayRectangle, base.GridSize, control.BackColor);
         }
         //if (base.Inherited)
         //{
         //   if (this.inheritanceUI == null)
         //   {
         //      this.inheritanceUI = (InheritanceUI)this.GetService(typeof(InheritanceUI));
         //   }
         //   if (this.inheritanceUI != null)
         //   {
         //      pe.Graphics.DrawImage(this.inheritanceUI.InheritanceGlyph, 0, 0);
         //   }
         //}
      }

      protected override void WndProc(ref Message m)
      {
         if (m.Msg == 0x84)
         {
            base.WndProc(ref m);
            if (((int)((long)m.Result)) == -1)
            {
               m.Result = (IntPtr)1;
            }
         }
         else
         {
            base.WndProc(ref m);
         }
      }

      protected override Point DefaultControlLocation
      {
         get
         {
            GroupBox control = (GroupBox)this.Control;
            return new Point(control.DisplayRectangle.X, control.DisplayRectangle.Y);
         }
      }

      protected override bool AllowControlLasso
      {
         get
         {
            return false;
         }
      }

      #endregion
      #region CanParent

      CanParentProvider canParentProvider;
      DragDropHandler dragDropHandler;

      public override void Initialize(System.ComponentModel.IComponent component)
      {
         base.Initialize(component);
         canParentProvider = new CanParentProvider(this);
         dragDropHandler = new DragDropHandler((Control)component, this.BehaviorService, (IDesignerHost)this.GetService(typeof(IDesignerHost)));
      }

      protected override void OnMouseDragBegin(int x, int y)
      {
         if (canParentProvider.CanDropFromSelectedToolboxItem())
            base.OnMouseDragBegin(x, y);
      }

      protected override void OnDragEnter(DragEventArgs de)
      {
         if (canParentProvider.CanEnterDrag(de))
            base.OnDragEnter(de);
      }


      #endregion

      /// <summary>
      /// handle the drag drop of other controls on this Group control
      /// </summary>
      /// <param name="de"></param>
      protected override void OnDragDrop(DragEventArgs de)
      {
         dragDropHandler.BeforeDragDrop();
         bool isDroppedFromToolbox = Utils.GetIsDroppedFromToolbox(this);
         base.OnDragDrop(de);
         dragDropHandler.AfterDragDrop(de, isDroppedFromToolbox);
      }
   }
}
