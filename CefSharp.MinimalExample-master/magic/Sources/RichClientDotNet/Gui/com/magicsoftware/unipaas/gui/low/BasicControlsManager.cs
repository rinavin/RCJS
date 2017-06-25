using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using com.magicsoftware.controls;
using com.magicsoftware.editors;
using Controls.com.magicsoftware;

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary>the class is responsible for managing of all static controls
   /// it will be defined on controls that support this -  can be panel of form or subform, groupbox, inner panel of tab
   /// </summary>
   internal class BasicControlsManager : ContainerManager
   {
      LogicalControlsContainer logicalControlsContainer;
      public LogicalControlsContainer LogicalControlsContainer
      {
         get { return logicalControlsContainer; }
      }

      internal StaticControlEditor tmpEditor { get; set; }

      /// <summary>
      /// list of logical controls on the container
      /// </summary>
      internal List<PlacementDrivenLogicalControl> LogicalControls
      {
         get { return logicalControlsContainer.LogicalControls; }
      }

      internal BasicControlsManager(Control control)
         : base(control)
      {
         logicalControlsContainer = new LogicalControlsContainer(control);
         tmpEditor = new StaticControlEditor(mainControl);
      }

      /// <summary>finds static control on point</summary>
      /// <param name="pt"></param> point
      /// <param name="findExact"></param> - not relevant for this implementation
      /// <param name="checkEnabled"></param> check if statuc control is enabled
      /// <returns></returns>
      internal override MapData HitTest(Point pt, bool findExact, bool checkEnabled)
      {
         Point offset = ContainerOffset();
         pt.Offset(-offset.X, -offset.Y);
         if (LogicalControls != null)
         {
            for (int i = LogicalControls.Count - 1; i >= 0; i--)
            {
               LogicalControl lg = (LogicalControl)LogicalControls[i];
               if (lg.canHit(checkEnabled) && ((EditorSupportingCoordinator)lg.Coordinator).Contains(pt))
                  return new MapData(lg.GuiMgControl);
            }
         }
         return null;
      }

      /// <summary>dispose static controls</summary>
      internal override void Dispose()
      {
         if (LogicalControls != null)
         {
            foreach (LogicalControl lg in LogicalControls)
               lg.Dispose();
         }
      }

      internal override Editor getEditor()
      {
         return tmpEditor;
      }


      /// <summary>
      /// create List for all controls
      /// </summary>
      /// <returns></returns>
      public Dictionary<Control, bool> CreateAllControlsForFormDesigner()
      {
         Dictionary<Control, bool> list = new Dictionary<Control, bool>();

         if (LogicalControls != null)
         {
            foreach (LogicalControl lg in LogicalControls)
            {
               Control control;
               // if the logical control does not have an editor, or if its editor is hidden, create a control for it
               // and add it to the list
               if (lg.getEditorControl() == null || IsHiddenEditor(lg.getEditorControl()))
               {
                  control = toControl(lg.GuiMgControl);
                  control.Parent = null;
                  ControlsMap.getInstance().setMapData(lg.GuiMgControl, lg._mgRow, control);
                  lg.setProperties(control);
                  lg.setPasswordToControl(control);

                  list[control] = true;
                  lg.setSpecificControlPropertiesForFormDesigner(control);
               }
               else
               {
                  control = lg.getEditorControl();
               }
               ((TagData)control.Tag).Bounds = lg.getRectangle(); // to handle negative values
               control.Bounds = lg.getRectangle();
               Rectangle rect = new Rectangle(lg.X, lg.Y, lg.Width, lg.Height);
               ((TagData)control.Tag).LastBounds = rect;
               //TODO: RTL
               //if (containerRightToLeft)
               //   rect.X -= dx;

            }
         }

         // add the real child controls, unless the child control is a hidden tmp editor or a form
         foreach (Control item in this.mainControl.Controls)
         {
            if (!IsHiddenEditor(item) && !(item is Form))
               list[item] = false;
         }
         return list;
      }

      /// <summary>
      /// is the control a hidden editor
      /// </summary>
      /// <param name="control"></param>
      /// <returns></returns>
      private bool IsHiddenEditor(Control control)
      {
         return tmpEditor.Control == control && tmpEditor.isHidden();
      }

      /// <summary>calculate offset of the container</summary>
      /// <param name="containerControl"></param>
      /// <returns></returns>
      internal Point ContainerOffset()
      {

         TagData tagData = mainControl.Tag as TagData;
         Point offset = new Point();

         if (mainControl is Panel)
         {
            //corner for the forms
            if (tagData.IsInnerPanel)
               offset = ((Panel)mainControl).AutoScrollPosition;
         }
         return offset;
      }

      /// <summary>paints static controls on the container</summary>
      /// <param name="e"></param>
      /// <param name="sender"></param>
      internal void Paint(Graphics gr)
      {
#if !PocketPC
         ImageAnimator.UpdateFrames();
#endif
         if (LogicalControls != null)
         {
#if !PocketPC
            Point offset = ContainerOffset();
            gr.TranslateTransform(offset.X, offset.Y);
#endif
            foreach (LogicalControl lg in LogicalControls)
               lg.paint(gr);

#if !PocketPC
            gr.ResetTransform();
#endif
         }
      }

      /// <summary> open temporary editor for tree child create a text control for the editor</summary>
      /// <param name="child"></param>
      /// <returns></returns>
      internal Control showTmpEditor(LogicalControl lg)
      {
         GuiUtils.SetTmpEditorOnTagData(GuiUtils.FindForm(mainControl), tmpEditor);

         //When the focus passed from control with hint to control without hint (and vise versa) or between two controls with hint, the control should be created.
         bool shouldCreate = GuiUtils.ShouldCreateControl(tmpEditor.Control, lg);

         linkControlToEditor(lg, tmpEditor, (tmpEditor.Control == null || shouldCreate) && !lg.isLastFocussedControl());
#if !PocketPC
         // The RightToLeft causes a select all. but sometimes the outcome has a problem.
         // so, deselect all. after that we have our own set selection anyway.
         if (tmpEditor.Control is TextBox && tmpEditor.Control.RightToLeft == RightToLeft.Yes)
            ((TextBox)tmpEditor.Control).DeselectAll();
#endif

         return tmpEditor.Control;
      }

      /// <summary>link control to the editor</summary>
      /// <param name="staticControl"></param>
      /// <param name="editor"></param>
      /// <param name="create"></param>
      internal void linkControlToEditor(LogicalControl lg, StaticControlEditor editor, bool create)
      {
         Control control;
         if (create)
         {
            if (editor.Control != null)
               GuiUtilsBase.ControlToDispose = editor.Control;

            control = toControl(lg.GuiMgControl);
            control.Size = new Size();
         }
         else
            control = editor.Control;

         ControlsMap.getInstance().setMapData(lg.GuiMgControl, lg._mgRow, control);
         editor.Control = control;

         lg.setProperties(control);

         editor.BoundsComputer = (BoundsComputer)lg.Coordinator;
         editor.Layout();

         lg.setPasswordToControl(control);
      }

   }
}
