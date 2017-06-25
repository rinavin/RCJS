using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;
using System.Drawing.Design;
using System.Drawing;
using System.ComponentModel.Design;
using System.Collections;
using System.Diagnostics;

namespace com.magicsoftware.controls.designers
{
   /// <summary>
   /// derived from studio tab control designer, to implement specific behavior for the runtime designer
   /// </summary>
   class MgTabControlRuntimeDesigner : MgTabControlDesigner
   {
      /// <summary>
      /// 
      /// </summary>
      /// <param name="disposing"></param>
      protected override void Dispose(bool disposing)
      {
         if (disposing)
            dragOverTimer.Dispose();

         base.Dispose(disposing);
      }


      public override DesignerVerbCollection Verbs
      {
         get
         {
            return new DesignerVerbCollection();
         }
      }
      /// <summary>
      /// 
      /// </summary>
      /// <param name="component"></param>
      public override void Initialize(IComponent component)
      {
         base.Initialize(component);

         dragOverTimer = new System.Windows.Forms.Timer();
         dragOverTimer.Interval = 300;
         dragOverTimer.Tick += dragOverTimer_Tick;
      }

      /// <summary>
      /// handle dropping components on tab pages
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void MgTabControlDesigner_ComponentDropped(object sender, ComponentDroppedArgs e)
      {
         // If components are dropped on tab labels, they'll be located outside of the tab page. They would be put in  (0, 0)
         TabControl tabControl = (TabControl)sender;
         if (tabControl.Alignment == TabAlignment.Top)
         {
            foreach (var item in e.Components)
            {
               if (((Control)item).Top < 0)
                  ((Control)item).Location = new Point();
            }
         }
         else
         {
            foreach (var item in e.Components)
            {
               if (((Control)item).Bottom > tabControl.DisplayRectangle.Bottom)
                  ((Control)item).Location = new Point();
            }
         }

      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="de"></param>
      protected override void OnDragDrop(DragEventArgs de)
      {
         if (this.ForwardOnDrag)
         {
            MgPanelDesigner selectedTabPageDesigner = GetDraggedOverPanelDesigner(de);
            if (selectedTabPageDesigner != null)
            {
               selectedTabPageDesigner.OnDragDropInternal(de);
            }
         }
         else
         {
            base.OnDragDrop(de);
         }
         this.ForwardOnDrag = false;
         ((MgTabControl)this.Control).IsDragging = false;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="de"></param>
      protected override void OnDragOver(DragEventArgs de)
      {
         if (this.ForwardOnDrag)
         {
            ProcessDragOver(de);
            MgPanelDesigner designer = GetDraggedOverPanelDesigner(de);
            designer.OnDragOverInternal(de);
         }
         else
         {
            base.OnDragOver(de);
         }
      }

      /// <summary>
      /// check if the selected tab should be changed, and set the timer
      /// </summary>
      /// <param name="de"></param>
      private void ProcessDragOver(DragEventArgs de)
      {
         TabPage selectedTabPage = GetDraggedOverTabPage(de);

         if (selectedTabPage != draggedOverTabPage)
            SetDragOverTimer();

         draggedOverTabPage = selectedTabPage;

      }

      /// <summary>
      /// Get the designer of panel
      /// </summary>
      /// <param name="de"></param>
      /// <returns></returns>
      private MgPanelDesigner GetDraggedOverPanelDesigner(DragEventArgs de)
      {
         MgTabControl control = (MgTabControl)this.Control;
         MgPanel panel = control.SelectedTab.Controls[0] as MgPanel;
         Debug.Assert(panel != null);
         MgPanelDesigner designer = null;
         
         IDesignerHost service = (IDesignerHost)this.GetService(typeof(IDesignerHost));
         if (service != null)
         {
            designer = service.GetDesigner(panel) as MgPanelDesigner;
         }
         return designer;
      }

      /// <summary>
      /// get the designer of the tab page corresponding to the supplied point.
      /// </summary>
      /// <param name="pt">point in the label area of the tab control</param>
      /// <returns></returns>
      private MgTabPage GetTabPageFromLocation(Point pt)
      {
         MgTabPage selectedTab = null;
         TabControl tab = (TabControl)Control;
         // loop over tab page labels rectangles, check if they contain our point
         for (int i = 0; i < tab.TabCount; i++)
         {
            Rectangle rect = tab.GetTabRect(i);
            // add the edges around the rectangles
            if (tab.Alignment == TabAlignment.Top || tab.Alignment == TabAlignment.Bottom)
            {
               rect.Y -= 2;
               rect.Height += 4;
            }
            else
            {
               rect.X -= 2;
               rect.Width += 4;
            }

            if (rect.Contains(pt))
            {
               selectedTab = tab.TabPages[i] as MgTabPage;
               break;
            }
         }

         return selectedTab;
      }

      #region drag over tab labels
      // hold the tabpage currently dragged over
      TabPage draggedOverTabPage = null;
      // timer for tab page changing when dragging over the tabpage labels
      System.Windows.Forms.Timer dragOverTimer;

      /// <summary>
      /// 
      /// </summary>
      private void SetDragOverTimer()
      {
         dragOverTimer.Stop();
         dragOverTimer.Start();
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void dragOverTimer_Tick(object sender, EventArgs e)
      {
         if (draggedOverTabPage != null)
            ((TabControl)Control).SelectedTab = (TabPage)draggedOverTabPage;
         dragOverTimer.Stop();
      }

      #endregion

      /// <summary>
      /// prevent the automatic selection of the tab when tabpage changes due to control dragging
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      protected override void OnTabSelectedIndexChanged(object sender, EventArgs e)
      {
      }

      /// <summary>
      /// Get the TabPage over which a control is dragged, considering also the tab headers
      /// </summary>
      /// <param name="de"></param>
      /// <returns></returns>
      private TabPage GetDraggedOverTabPage(DragEventArgs de)
      {
         MgTabPage selectedTabPage = null;
         MgTabControl control = (MgTabControl)this.Control;

         // If the current point is not on the tab page
         Point pt = this.Control.PointToClient(new Point(de.X, de.Y));
         if (!control.DisplayRectangle.Contains(pt))
         {
            if (control.ClientRectangle.Contains(pt))
            {
               //  current point is in the labels area
               selectedTabPage = this.GetTabPageFromLocation(pt);
            }
         }
         else
            selectedTabPage = (MgTabPage)control.SelectedTab;

         return selectedTabPage;
      }
   }
}
