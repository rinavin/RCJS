using System;
using System.Windows.Forms;
using System.Drawing;
using com.magicsoftware.controls;
using System.Collections;
using System.Collections.Generic; 
using System.Diagnostics;
using Controls.com.magicsoftware;
#if PocketPC
using Panel = com.magicsoftware.controls.MgPanel;
#endif

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary>
   ///  implement magic placement
   /// </summary>
   /// <author>  rinav</author>
   internal class EditorSupportingPlacementLayout : PlacementLayoutBase
   {
      private int? _runtimeDesignerDiffX = null;
      private int? _runtimeDesignerDiffY = null;

      /// <summary> </summary>
      /// <param name="mainComposite">main composite for placement </param>
      /// <param name="rect">original rectangle </param>
      /// <param name="initLogSize">used for subforms </param>
      internal EditorSupportingPlacementLayout(Control mainComposite, Rectangle rect, bool initLogSize, int? runtimeDesignerDiffX, int? runtimeDesignerDiffY)
         : base(mainComposite, rect, initLogSize)
      {
         _runtimeDesignerDiffX = runtimeDesignerDiffX;
         _runtimeDesignerDiffY = runtimeDesignerDiffY;
      }

      #region PlacementLayoutBase overridden methods

      /// <summary>
      /// Returns inner control for a container control
      /// </summary>
      /// <param name="container"></param>
      /// <returns></returns>
      protected override Control GetInnerControl(Control container)
      {
         return GuiUtils.getInnerControl(container);
      }

      /// <summary>
      /// Return LogicalControlsContainer for a container control
      /// </summary>
      /// <param name="container"></param>
      /// <returns></returns>
      protected override LogicalControlsContainer GetLogicalControlsContainer(Control container)
      {
         LogicalControlsContainer logicalControlsContainer = null;

         BasicControlsManager staticControlsManager = findStaticControlManager(container);
         if (staticControlsManager != null)
            logicalControlsContainer = staticControlsManager.LogicalControlsContainer;

         return logicalControlsContainer;
      }

      /// <summary>
      /// Return maximum value for width and height from all controls in container
      /// </summary>
      /// <param name="container"></param>
      /// <param name="width"></param>
      /// <param name="height"></param>
      protected override void GetMaxOfActualControlDimensions(Control container, ref int width, ref int height)
      {
         // compute logical size of composite
         foreach (Control child in container.Controls)
         {
#if PocketPC
            // Don't add dummy control
            if (container is Panel && ((Panel)container).dummy == child)
               continue;
#endif

            if (isControlVisible(container, child) && isMagicControl(child))
            {
               Rectangle rect = GuiUtils.getBounds(child);
               width = Math.Max(width, rect.X + rect.Width);
               height = Math.Max(height, rect.Y + rect.Height);
            }
         }
      }

      /// <summary>
      /// Returns whether layout can be performed for a form
      /// </summary>
      /// <param name="containerControl"></param>
      /// <returns></returns>
      protected override bool CanPerformLayout(Control containerControl)
      {
         Form form = GuiUtils.FindForm(containerControl);
         
         if(form != null)
            return !((TagData)form.Tag).Minimized;

         return true;
      }

      /// <summary>
      /// Returns whether can LimitPlacement to container control
      /// </summary>
      /// <param name="containerControl"></param>
      /// <returns></returns>
      protected override bool CanLimitPlacement(Control containerControl)
      {
         return ((TagData)containerControl.Tag).IsInnerPanel;
      }

      /// <summary>
      /// Returns whether LimitPlacement should be applied to control
      /// </summary>
      /// <param name="control"></param>
      /// <returns></returns>
      protected override bool ShouldLimitPlacement(Control control)
      {
         return !(control != null && control.Tag is TagData && ((TagData)control.Tag).IsEditor); //it will be handled by its logicalcontrol
      }

      protected override bool IsTableInColumnCreation(TableControl control)
      {
         TableManager tableManager = GuiUtils.getTableManager((TableControl)control);
         return tableManager.InColumnsCreation;
      }

      protected override ITableManager GetTableManager(TableControl control)
      {
         return GuiUtils.getTableManager((TableControl)control);
      }

      protected override void ExecuteTablePlacement(TableControl control, int prevWidth, int dx, Rectangle rect)
      {
         // update columns on table placement
         GuiUtils.getTableManager((TableControl)control).ExecuteTablePlacement(prevWidth, dx, rect);
      }

      /// <summary>
      /// Returns whether placement can be applied to object
      /// </summary>
      /// <param name="control"></param>
      /// <returns></returns>
      protected override bool ShouldApplyPlacement(object control)
      {
         return isMagicControl(control);
      }

      /// <summary>
      /// Return bounds of control saved earlier
      /// </summary>
      /// <param name="control"></param>
      /// <returns></returns>
      protected override Rectangle? GetSavedBounds(Control control)
      {
         return GuiUtils.getSavedBounds(control);
      }

      /// <summary>
      /// Set bounds to control
      /// </summary>
      /// <param name="control"></param>
      /// <param name="rect"></param>
      protected override void SetBounds(Control control, Rectangle rect)
      {
         GuiUtils.setBounds(control, rect);
      }

      /// <summary>
      /// recalculate control's position and refresh it 
      /// </summary>
      /// <param name="obj"></param>
      protected override void ReCalculateAndRefresh(Object obj)
      {
         base.ReCalculateAndRefresh(obj);
         EditorSupportingCoordinator staticControl = (EditorSupportingCoordinator)((LogicalControl)obj).Coordinator;
         if (staticControl.getEditorControl() != null)
         {
            StaticControlEditor staticControlEditor = staticControl.getEditor();

            if (staticControlEditor != null)
               staticControlEditor.Layout();
         }
      }

      /// <summary>
      /// Placement Data of object
      /// </summary>
      /// <param name="obj"></param>
      /// <returns></returns>
      protected override PlacementData PlacementData(Object obj)
      {
         PlacementData placementData = null;
         if (obj is Control)
         {
            Control control = (Control)obj;
            if (control.Tag != null)
               placementData = ((TagData)control.Tag).PlacementData;
         }
         else
         {
            if (obj is LogicalControl)
            {
               LogicalControl staticControl = (LogicalControl)obj;
               placementData = staticControl.PlacementData;
            }
         }
         return placementData;
      }

      /// <summary>
      /// bounds of object
      /// </summary>
      /// <param name="obj"></param>
      /// <returns></returns>
      protected override Rectangle Bounds(Object obj)
      {
         Rectangle rect = new Rectangle();
         if (obj is TableControl)
         {
            TagData td = (TagData)((Control)obj).Tag;
            if (td.Bounds != null)
               rect = (Rectangle)td.Bounds;
         }
         else if (obj is Control)
            rect = GuiUtils.getBounds((Control)obj);
         else if (obj is LogicalControl)
            rect = base.Bounds(obj);
         else
            Debug.Assert(false);
         return rect;
      }

   #endregion

      /// <summary> 
      /// checks if control is visible, does not checks recursive if parents r visible we need this method, 
      /// since control.isvisible() always returns false
      /// until shell is opened.
      /// </summary>
      /// <param name="parent"></param>
      /// <param name="control"></param>
      /// <returns></returns>
      private bool isControlVisible(Control parent, Control control)
      {
         bool res = true;
         if (parent.Visible)
            res = control.Visible;
         else if (control.Tag != null)
         {
            Boolean visible = ((TagData)control.Tag).Visible;
            if (!((Boolean)visible))
               res = false;
         }
         return res;
      }

      /// <summary>
      /// return static control manager of container, null if staticControlsManager is not found
      /// this is relevant for client panel of form, client panel of tab or groupbox
      /// </summary>
      /// <param name="mainControl"></param>
      /// <returns></returns>
      BasicControlsManager findStaticControlManager(Control mainControl)
      {
         BasicControlsManager staticControlsManager = null;
         if (mainControl.Tag != null && ((TagData)mainControl.Tag).ContainerManager is BasicControlsManager)
            staticControlsManager = (BasicControlsManager)(((TagData)mainControl.Tag).ContainerManager);

         return staticControlsManager;

      }

      /// <summary>
      /// check that obj is logical control or real control
      /// </summary>
      /// <param name="obj"></param>
      /// <returns></returns>
      static bool isMagicControl(Object obj)
      {
         Control control = obj as Control;
         if (control != null)
         {
            if (control.Tag is TagData && ((TagData)control.Tag).IsEditor) //it will be handled by its logicalcontrol
               return false;
            if (control.Dock != DockStyle.None) //status bar 
               return false;
         }

         return true;
      }


      protected override void FixRuntimeDesignerPlacement(ref Rectangle newRect)
      {
         //this is needed to prevent placement caused by change of Size of container in runtime designer
         if (_runtimeDesignerDiffX != null)
         {
            Rectangle prevPlacementrect = GetPlacementDifRect(_mainComposite);
            //we create the width that whould have happen if there was no change in runtime designer
            newRect.Width = _prevRect.Width + (int)_runtimeDesignerDiffX + prevPlacementrect.Width;
            //do it only once
            _runtimeDesignerDiffX = null;
         }

         if (_runtimeDesignerDiffY != null)
         {
            Rectangle prevPlacementrect = GetPlacementDifRect(_mainComposite);
            newRect.Height = _prevRect.Height + (int)_runtimeDesignerDiffY + prevPlacementrect.Height;
            _runtimeDesignerDiffY = null;
         }
      }
      /// <summary>
      /// return previous placement rectangle 
      /// </summary>
      /// <param name="component"></param>
      /// <returns></returns>
      public static Rectangle GetPlacementDifRect(object component)
      {
         Rectangle rect = new Rectangle();
         Control control = (Control)component;
         Rectangle? currRect = GuiUtils.getSavedBounds(control);
         if (currRect == null)
            currRect = GuiUtils.getBounds(control);
         if (((TagData)control.Tag).LastBounds != null)
         {
            Rectangle lastSetRect = (Rectangle)((TagData)control.Tag).LastBounds;

            if (lastSetRect.X != GuiConstants.DEFAULT_VALUE_INT)
               rect.X = ((Rectangle)currRect).X - lastSetRect.X;

            if (lastSetRect.Y != GuiConstants.DEFAULT_VALUE_INT)
               rect.Y = ((Rectangle)currRect).Y - lastSetRect.Y;

            if (lastSetRect.Width != GuiConstants.DEFAULT_VALUE_INT)
               rect.Width = ((Rectangle)currRect).Width - lastSetRect.Width;

            if (lastSetRect.Height != GuiConstants.DEFAULT_VALUE_INT)
               rect.Height = ((Rectangle)currRect).Height - lastSetRect.Height;
         }
         else
            rect = new Rectangle();
         return rect;


      }
   }
}
