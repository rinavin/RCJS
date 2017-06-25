using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.Design;
using System.Windows.Forms;
using System.Collections;
using com.magicsoftware.controls;
using System.ComponentModel;
using com.magicsoftware.util;
using RuntimeDesigner.Serialization;
using com.magicsoftware.support;

namespace RuntimeDesigner
{  
   /// <summary>
   /// MenuCommandService for the runtime designer. Used to activate the designer context menu
   /// </summary>
   class RuntimeMenuService : MenuCommandService
   {
      internal const string STR_MENU_RESET = "Reset to default";
      internal const string STR_MENU_DELETE = "&Delete";
      internal const string STR_MENU_SEPARATOR = "separator";

      /// <summary>
      /// The context menu to show
      /// </summary>
      ContextMenuStrip menu;

      RuntimeHostSurface surface;

      public override DesignerVerbCollection Verbs
      {
         get
         {
            ArrayList selectedItems = GetSelectedItems();
            IDesignerHost host = (IDesignerHost)surface.GetService(typeof(IDesignerHost));
            DesignerVerbCollection verbs =  ControlUtils.GetVerbsForControl(host, selectedItems, AllowDesignerActions);
            while (verbs.Count > 10)
            {
               verbs.RemoveAt(verbs.Count - 1);
            }

            return verbs;
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      private ArrayList GetSelectedItems()
      {
         ISelectionService selectionService = (ISelectionService)(GetService(typeof(ISelectionService)));
         ICollection selectedCollection = selectionService.GetSelectedComponents();
         ArrayList selectedItems = new ArrayList();
         foreach (var item in selectedCollection)
            selectedItems.Add(item);
         return selectedItems;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="obj"></param>
      /// <returns></returns>
      bool AllowDesignerActions(object obj)
      {
         return true;
      }
      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="serviceProvider"></param>
      internal RuntimeMenuService(RuntimeHostSurface serviceProvider)
         : base(serviceProvider)
      {
         surface = serviceProvider;
         InitMenu();
      }

      /// <summary>
      /// Initialize the context menu
      /// </summary>
      void InitMenu()
      {
         menu = new ContextMenuStrip();

         String str = RuntimeHostSurface.GetTranslatedString(surface, STR_MENU_RESET);
         ToolStripMenuItem t = new ToolStripMenuItem(str, null, OnResetClicked);
         t.Name = t.Text;
         menu.Items.Add(t);

         ToolStripSeparator separator = new ToolStripSeparator();
         separator.Name = STR_MENU_SEPARATOR;
         menu.Items.Add(separator);

         str = RuntimeHostSurface.GetTranslatedString(surface, STR_MENU_DELETE);
         t = new ToolStripMenuItem(str, null, OnDeleteClicked);
         t.Name = t.Text;
         menu.Items.Add(t);
      }

      /// <summary>
      /// open the context menu
      /// </summary>
      /// <param name="menuID"></param>
      /// <param name="x"></param>
      /// <param name="y"></param>
      public override void ShowContextMenu(CommandID menuID, int x, int y)
      {
         menu.Items[RuntimeHostSurface.GetTranslatedString(surface, STR_MENU_DELETE)].Visible = surface.AdminMode;
         menu.Items[RuntimeHostSurface.GetTranslatedString(surface, STR_MENU_DELETE)].Enabled = surface.CanDeleteSelectedItems();
         menu.Items[STR_MENU_SEPARATOR].Visible = surface.AdminMode;

         menu.Items[RuntimeHostSurface.GetTranslatedString(surface, STR_MENU_RESET)].Enabled = CanResetSelectedItems();

         menu.Show(x, y);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="control"></param>
      /// <returns></returns>
      bool IsFrame(Control control)
      {
         bool isFrame = false;
         if (control != null)
         {
            ControlDesignerInfo controlDesignerInfo = (control.Tag as ControlDesignerInfo);
            isFrame = (controlDesignerInfo != null && controlDesignerInfo.IsFrame) || controlDesignerInfo.ControlType == MgControlType.CTRL_TYPE_CONTAINER;
         }
         return isFrame;
      }

  

      /// <summary>
      /// Are there selected controls to reset
      /// </summary>
      /// <returns></returns>
      private bool CanResetSelectedItems()
      {
         ISelectionService selectionService = (ISelectionService)(GetService(typeof(ISelectionService)));
         ICollection collection = selectionService.GetSelectedComponents();
         foreach (var item in collection)
         {
            // fixed defect #:128232, frame set can't be reset to default 
            if (IsFrame(item as Control))
               return false;
            else if(!(item is Form))
            {
               return true;
            }
         }

         return false;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      internal void OnResetClicked(object sender, EventArgs e)
      {
         surface.ResetSelectedControls();
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      internal void OnDeleteClicked(object sender, EventArgs e)
      {
         surface.DeleteSelectedControls();
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="disposing"></param>
      protected override void Dispose(bool disposing)
      {
         surface = null;
         base.Dispose(disposing);
      }
   }
}
