using System;
using System.Drawing;
using System.Windows.Forms;
using com.magicsoftware.controls;
using com.magicsoftware.editors;
using Controls.com.magicsoftware;

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary>
   ///   base class for static controls : line, rect, etc
   /// </summary>
   internal class EditorSupportingCoordinator : BasicCoordinator, BoundsComputer, IEditorProvider
   {
      private readonly BasicControlsManager _staticControlsManager;

      private bool _supportEditor
      {
         get
         {
            if (((LogicalControl)_lg).GuiMgControl.isTextControl())
               return true;
            return false;
         }
      }

      /// <summary>
      ///   coordinater for controls on the form
      /// </summary>
      /// <param name = "lg"></param>
      internal EditorSupportingCoordinator(LogicalControl lg)
         : base(lg, ((BasicControlsManager)lg.ContainerManager).LogicalControlsContainer)
      {
         _staticControlsManager = (BasicControlsManager)lg.ContainerManager;
         lg.StyleChanged += lg_OnStyleChanged;
         lg.BorderTypeChanged += lg_OnBorderTypeChanged;
      }

      #region BoundsComputer Members

      /// <summary>
      ///   implement interface
      /// </summary>
      /// <param name = "cellRectangle"></param>
      /// <returns></returns>
      Rectangle BoundsComputer.computeEditorBounds(Rectangle cellRectangle, bool isHeaderEditor)
      {
         Point offset = _staticControlsManager.ContainerOffset();
         Rectangle rect = DisplayRect;
         rect.Offset(offset.X, offset.Y);
         return rect;
      }

      #endregion

      #region ICoordinator Members

      public override void Refresh(bool changed)
      {
         if (changed)
            ((LogicalControl)_lg).Invalidate(false);
      }

      #endregion

      #region IEditor Members

      /// <summary>
      ///   default implementation of get editor control
      /// </summary>
      /// <returns></returns>
      public Control getEditorControl()
      {
         if (_supportEditor)
         {
            StaticControlEditor staticControlEditor = getEditor();
            Control control = staticControlEditor.Control;
            if (control != null)
            {
               MapData mapData = ControlsMap.getInstance().getMapData(control);
               if (mapData != null && ((LogicalControl)_lg).GuiMgControl == mapData.getControl())
                  // this child has temporary editor
                  return control;
            }
         }
         return null;
      }

      #endregion

      /// <summary> Refreshes the coordinator and its parent when
      /// the bounds of the coordinator changes. </summary>
      /// <param name="changed"></param>
      protected override void OnBounds(bool changed)
      {
         base.OnBounds(changed);
#if !PocketPC


         //If the coordinator's bounds changes, it might lead to 
         //showing or hiding of the scrollbar on its parent. 
         //So, perform the layout to its parent to refresh it.
         if (changed && _containerControl is ScrollableControl)
         {


            //we call Panel.PerformLayout() so that Layout event will be fired and from there, the scrollbar will be re-evaluated.
            //While handling the Layout event, we call PlacementLayout.layout(), which in-turn calls updateAutoScrollMinSize() 
            //(via computeAndUpdateLogicalSize()).
            //Now, updateAutoScrollMinSize() actually sets the Panel.AutoScrollMinSize
            //Perform layout must have active control attribute to effect the scrollbar immideatly  
            Control c = getEditorControl();
            if (c != null)
               getEditor().Layout();
         }
#endif
      }

      /// <summary>
      ///   when style is chenged - recompute display rect
      /// </summary>
      /// <param name = "sender"></param>
      /// <param name = "e"></param>
      private void lg_OnStyleChanged(object sender, EventArgs e)
      {
         calcDisplayRect();
      }

      /// <summary>
      ///   when style is chenged - recompute display rect
      /// </summary>
      /// <param name = "sender"></param>
      /// <param name = "e"></param>
      private void lg_OnBorderTypeChanged(object sender, EventArgs e)
      {
         calcDisplayRect();
      }

      protected override void OnVisibleChanged(object sender, EventArgs e)
      {
         base.OnVisibleChanged(sender, e);

         if (_containerControl is ScrollableControl)
         {
            //update parent's scrollbar 

#if PocketPC
            if (_containerControl is TableControl)
            {
               ((TableControl)_containerControl).PerformLayout();
               ((TableControl)_containerControl).PerformLayout();
            }
#endif
         }
         ((LogicalControl)_lg).Invalidate(false);
      }

      public override void Refresh(bool changed, bool wholeParent)
      {

         if (changed)
            ((LogicalControl)_lg).Invalidate(wholeParent);
      }


      /// <summary>
      ///   returns true is control contains point
      /// </summary>
      /// <param name = "pt"></param>
      /// point coordinates that include scrollbar offset
      /// <returns></returns>
      internal bool Contains(Point pt)
      {
         if (_lg is Line)
            return ((Line)_lg).Contains(pt);
         return DisplayRect.Contains(pt);
      }

      /// <summary>
      ///   default implementation of get editor control
      /// </summary>
      /// <returns></returns>
      /// <summary>
      ///   get editor
      /// </summary>
      /// <returns></returns>
      internal StaticControlEditor getEditor()
      {
         if (_supportEditor)
            return _staticControlsManager.tmpEditor;
         return null;
      }
   }
}