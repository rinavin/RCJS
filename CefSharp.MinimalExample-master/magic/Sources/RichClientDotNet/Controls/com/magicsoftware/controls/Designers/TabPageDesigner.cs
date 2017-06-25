using System;
using System.Windows.Forms.Design.Behavior;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel.Design;
using com.magicsoftware.controls.utils;

namespace com.magicsoftware.controls.designers
{
   /// <summary>
   /// TabPageDesigner - original designer for Tabpage + can parent functionality
   /// </summary>
   public class TabPageDesigner : PanelControlDesigner
   {
      #region original TabPageDesigner
      public override bool CanBeParentedTo(IDesigner parentDesigner)
      {
         return ((parentDesigner != null) && (parentDesigner.Component is TabControl));
      }

      protected override ControlBodyGlyph GetControlGlyph(GlyphSelectionType selectionType)
      {
         this.OnSetCursor();
         return new ControlBodyGlyph(Rectangle.Empty, Cursor.Current, this.Control, this);
      }

      public override GlyphCollection GetGlyphs(GlyphSelectionType selectionType)
      {
         return new GlyphCollection();
      }

      internal void OnDragDropInternal(DragEventArgs de)
      {
         this.OnDragDrop(de);
      }

      internal void OnDragEnterInternal(DragEventArgs de)
      {
         this.OnDragEnter(de);
      }

      internal void OnDragLeaveInternal(EventArgs e)
      {
         this.OnDragLeave(e);
      }

      internal void OnDragOverInternal(DragEventArgs e)
      {
         this.OnDragOver(e);
      }

      internal void OnGiveFeedbackInternal(GiveFeedbackEventArgs e)
      {
         this.OnGiveFeedback(e);
      }

      public override bool CanParent(Control control)
      {
         bool result;
         result = base.CanParent(control);
         if (result)
         {
            result = canParentProvider.CanParent(control.GetType(), control, DragOperationType.Move);
         }
         return result;
      }

      public override System.Windows.Forms.Design.SelectionRules SelectionRules
      {
         get
         {
            System.Windows.Forms.Design.SelectionRules selectionRules = base.SelectionRules;
            if (this.Control.Parent is TabControl)
            {
               selectionRules &= ~System.Windows.Forms.Design.SelectionRules.AllSizeable;
            }
            return selectionRules;
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
      /// handle the drag drop of other controls on Tab control
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
