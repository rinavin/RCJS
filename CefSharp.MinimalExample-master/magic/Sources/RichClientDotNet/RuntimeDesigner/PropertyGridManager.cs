using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Windows.Forms;
using com.magicsoftware.controls;
using Controls.com.magicsoftware.controls.MgLine;

namespace RuntimeDesigner
{
   /// <summary>
   /// manage the run-time designer property grid
   /// </summary>
   class PropertyGridManager
   {
      PropertyGrid propertyGrid;
      ISelectionService selectionService;
      RuntimeHostSurface surface;

      // property grid context menu
      ContextMenuStrip contextMenu;
      ToolStripButton resetMenuItem;
      //ToolStripButton breakMenuItem;

      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="propertyGrid"></param>
      /// <param name="selectionService"></param>
      internal PropertyGridManager(PropertyGrid propertyGrid, ISelectionService selectionService, IDesignerHost host, RuntimeHostSurface surface)
      {
         this.propertyGrid = propertyGrid;
         this.selectionService = selectionService;
         this.surface = surface;

         selectionService.SelectionChanged += selectionService_SelectionChanged;

         host.TransactionClosed += host_TransactionClosed;
         CreateContextMenu();
         SetPropertyGridStyle();
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void host_TransactionClosed(object sender, DesignerTransactionCloseEventArgs e)
      {
         propertyGrid.Refresh();
      }


      /// <summary>
      /// 
      /// </summary>
      /// <param name="control"></param>
      /// <returns></returns>
      internal static Form FindForm(Control control)
      {
         //if the send obj is control climb up the parents till it find the form. 
         while (control != null && !(control is Form))
         {
            if (control.Parent != null)
               control = control.Parent;
         }
         return (Form)control;
      }

      /// <summary>
      /// return true if it is spliter container of spliter 
      /// </summary>
      /// <param name="control"></param>
      /// <returns></returns>
      bool IsMgSplitContainer(Control control)
      {
         bool isMgSplitContainer = false;

         if (control != null)
         {
            String str = control.GetType().ToString();
            if (str.EndsWith("MgSplitContainer") || ((str.EndsWith("Splitter") && control.Parent != null && IsMgSplitContainer(control.Parent))))
               isMgSplitContainer = true;
         }

         return isMgSplitContainer;
      }

      /// <summary>
      /// return true for panel that is sub of SplitContainer
      /// </summary>
      /// <param name="control"></param>
      /// <returns></returns>
      bool IsPanelOfMgSpliter(Control control)
      {
         bool isPanelOfMgSpliter = false;
         // direct panel of the MgSplitContainer is allowed it is the subformto be selected 
         if (control is Panel)
         {
            Control parentItem = control.Parent;
            if (IsMgSplitContainer(parentItem) ||
               (IsMgSplitContainer(parentItem.Parent)))
               isPanelOfMgSpliter = true;
         }
         return isPanelOfMgSpliter;
      }
      /// <summary>
      /// selection changed - create the components wrappers and set the PropertyGrid's selected component 
      /// </summary>
      private void selectionService_SelectionChanged(object sender, EventArgs e)
      {
         if (selectionService != null)
         {
            ICollection selectedComponents = selectionService.GetSelectedComponents();

            bool replaceSelection = false;
            List<object> newSelection = new List<object>();
            // go over selected components, check they are selectable and select the correct ancestor if not.
            foreach (var item in selectedComponents)
            {
               if (IsMgSplitContainer(item as Control))
               {
                  newSelection.Add(FindForm(item as Control));
                  replaceSelection = true;
               }
               else if (item is MgPanel)
               {
                  if (IsPanelOfMgSpliter(item as Control))
                  {
                     newSelection.Add(item);
                     replaceSelection = false;
                  }
                  else
                  {
                     if (((Control)item).Parent is MgTabPage)     // panel on tabpage - add the tab control
                        newSelection.Add(((Control)item).Parent.Parent);
                     else if (((Control)item).Parent is MgPanel)  // panel on other panel - subform
                        newSelection.Add(item);
                     else if (!(((Control)item).Parent is GuiForm) && selectedComponents.Count > 1)
                        newSelection.Add(((Control)item).Parent);
                     else if(selectedComponents.Count == 1)
                        newSelection.Add(((Control)item).Parent);
                     replaceSelection = true;
                  }
               }
               else if (item is MgTabPage)
               {
                  // select the tab control
                  newSelection.Add(((Control)item).Parent);
                  replaceSelection = true;
               }
               else if (item is GuiForm && selectedComponents.Count > 1)
                  replaceSelection = true;
               else
                  newSelection.Add(item);
            }

            if (replaceSelection)
            {
               selectionService.SelectionChanged -= selectionService_SelectionChanged;
               selectionService.SetSelectedComponents(newSelection, SelectionTypes.Replace);
               selectionService.SelectionChanged += selectionService_SelectionChanged;
            }

            // create the selected objects list for the propertygrid
            object[] comps = new object[newSelection.Count];
            int i = 0;

            foreach (Object o in newSelection)
            {
               comps[i] = surface.GetComponentWrapper(o as Control);
               i++;
            }

            propertyGrid.SelectedObjects = comps;
         }

      }

      /// <summary>
      /// create the context menu
      /// </summary>
      /// <returns></returns>
      void CreateContextMenu()
      {
         contextMenu = new ContextMenuStrip();
         contextMenu.Opening += contextMenu_Opening;

         resetMenuItem = new ToolStripButton("Reset", null, ResetMenuItem_Click);
         contextMenu.Items.Add(resetMenuItem);

         //breakMenuItem = new ToolStripButton("Break", null, BreakMenuItem_Click);
         //contextMenu.Items.Add(breakMenuItem);

         propertyGrid.ContextMenuStrip = contextMenu;
      }

      /// <summary>
      /// handle opening of context menu
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void contextMenu_Opening(object sender, CancelEventArgs e)
      {
         PropertyDescriptor designPropertyDescriptor;
         // if there is no selected item, or the PropertyDescriptor is a MergePropertyDescriptor
         if (propertyGrid.SelectedGridItem == null || propertyGrid.SelectedObjects.Length > 1)
         {
            e.Cancel = true;
            return;
         }

         designPropertyDescriptor = propertyGrid.SelectedGridItem.PropertyDescriptor;

         resetMenuItem.Enabled = designPropertyDescriptor != null && designPropertyDescriptor.CanResetValue(propertyGrid.SelectedObject);

         //breakMenuItem.Enabled = !resetMenuItem.Enabled;
      }


      /// <summary>
      /// 
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void ResetMenuItem_Click(object sender, EventArgs e)
      {
         IDesignerHost host = (IDesignerHost)surface.GetService(typeof(IDesignerHost));
         DesignerTransaction transaction = host.CreateTransaction("Reset Menu Item Clicked");
         
         propertyGrid.ResetSelectedProperty();
         
         transaction.Commit();
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      //void BreakMenuItem_Click(object sender, EventArgs e)
      //{
      //   ((RTDesignerPropertyDescriptor)propertyGrid.SelectedGridItem.PropertyDescriptor).BreakProperty();
      //   propertyGrid.Refresh();
      //}

      /// <summary>
      /// set the colors and font of the property grid
      /// </summary>
      void SetPropertyGridStyle()
      {
         propertyGrid.LineColor = Color.FromArgb(unchecked((int)0xfff0f0f0));
         propertyGrid.CategoryForeColor = Color.FromArgb(unchecked((int)0xffa8b3c2));
         propertyGrid.CommandsLinkColor = Color.FromArgb(unchecked((int)0xff0066cc));
         propertyGrid.CommandsActiveLinkColor = Color.FromArgb(unchecked((int)0xff3399ff));
         propertyGrid.HelpBackColor = Color.FromArgb(unchecked((int)0xffdee1e7));
         propertyGrid.Font = new Font("Segoe UI", 9);
         propertyGrid.Controls[3].BackColor = Color.FromArgb(0xBC, 0xC7, 0xD8);
         propertyGrid.HelpVisible = false;
      }

      /// <summary>
      /// 
      /// </summary>
      internal void Refresh()
      {
         propertyGrid.Refresh();
      }
   }
}
