using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using com.magicsoftware.editors;
using com.magicsoftware.unipaas.gui;
using com.magicsoftware.util;

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary>base class for all containers</summary>
   internal abstract class ContainerManager
   {
      readonly internal Control mainControl; // container control, can be panel of form or subform, groupbox, inner panel of tab, table or tree
      protected internal ControlsMap controlsMap;
      internal MapData lastClicked; // mapData of last clicked table child
      protected internal long lastClickTime; // time of last click
      internal Form Form {get; set;}

      internal ContainerManager(Control mainControl)
      {
         controlsMap = ControlsMap.getInstance();
         this.mainControl = mainControl;
         if (mainControl.Tag == null)
             GuiUtils.CreateTagData(mainControl);
         TagData td = (TagData)mainControl.Tag;
         td.ContainerManager = this;
      }

      /// <summary> hides temporary editor</summary>
      /// <param name="form"></param>
      internal static void hideTmpEditor(Form form)
      {
         Editor editor = GuiUtils.GetTmpEditorFromTagData(form);
         if (editor != null)
         {
            if (editor.Control != null)
               ControlsMap.removeMapData(editor.Control);
            editor.Hide();
         }
      }

      /// <summary> refresh temporary editor of static controls</summary>
      /// <param name="form"></param>
      internal static void refreshTmpEditor(Form form)
      {
         Editor editor = GuiUtils.GetTmpEditorFromTagData(form);
         if (editor != null && (!editor.isHidden()) )
         {
            if (editor.Control != null)
            {
               MapData mapData = ControlsMap.getInstance().getMapData(editor.Control);
               Object obj = ControlsMap.getInstance().object2Widget(mapData.getControl(), mapData.getIdx());
               //for now only refesh static text
               LgText staticControl = obj as LgText;
               if (staticControl != null && staticControl.RefreshNeeded)
                  ((LgText)staticControl).setProperties(editor.Control);
            }  
         }
      }

      /// <summary> Create Control from logical control</summary>
      /// <returns></returns>
      internal Control toControl(GuiMgControl guiMgControl)
      {
         return toControl(guiMgControl, guiMgControl.getCreateCommandType());
      }

      /// <summary>Create Control from logical control</summary>
      /// <returns></returns>
      internal Control toControl(GuiMgControl guiMgControl, CommandType commandType)
      {
         Control control = (Control)GuiUtils.createSimpleControlForEditor(commandType, mainControl, guiMgControl);
         ((TagData)control.Tag).IsEditor = true;
         return control;
      }

      /// <summary> the method checks if double click event should be raised We can not use DoubleClick event, since it is possible that first click is
      /// performed on the container, and the second on loogical's control editor.</summary>
      /// <param name="mapData"></param>
      /// <returns></returns>
      internal bool isDoubleClick(MapData mapData)
      {
         if (!GuiUtils.isOwnerDrawControl(mapData.getControl()))
            return false;
         bool ret = false;
         long time = Misc.getSystemMilliseconds();
         long diff = time - lastClickTime;

         if (lastClicked != null && lastClicked.Equals(mapData) && diff < SystemInformation.DoubleClickTime)
         {
            // this is double click
            ret = true;
            time = -1;
            lastClicked = null;
         }
         else
         {
            lastClicked = mapData;
            lastClickTime = time;
         }
         return ret;
      }

      /// <summary>finds control on point</summary>
      /// <param name="pt"></param>
      /// <param name="findExact"></param> if true, find control that includes the point
      /// <param name="checkEnabled"></param> if true, consider only enabled control
      /// <returns></returns>
      internal abstract MapData HitTest(Point pt, bool findExact, bool checkEnabled);

      /// <summary>any action needed for disposing contained controls</summary>
      internal virtual void Dispose()
      {
         //remove memory leak
         Form form = this.Form;
         if (form != null)
         {
            TagData td = (TagData)form.Tag;
            if (getEditor() != null && td.TmpEditor == getEditor())
            {
               disposeEditor(getEditor());
               td.TmpEditor = null;
            }
         }
      }

      /// <summary>dispose Editor</summary>
      /// <param name="editor"></param>
      internal void disposeEditor(Editor editor)
      {
         Control control = editor.Control;
         editor.Dispose();
         editor = null;
      }

      internal abstract Editor getEditor();
   
   }
}