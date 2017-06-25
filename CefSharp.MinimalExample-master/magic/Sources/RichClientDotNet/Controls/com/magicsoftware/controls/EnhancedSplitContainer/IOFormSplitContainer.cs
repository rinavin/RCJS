using com.magicsoftware.controls;
using com.magicsoftware.controls.designers;
using com.magicsoftware.win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace com.magicsoftware.controls
{
   /// <summary>
   /// Split container for IOForm .
   /// This allows resizing the form width in addition to resizing by splitters
   /// </summary>

   [Designer(typeof(IOFormSplitContainerDesigner))]
   public class IOFormSplitContainer : EnhancedSplitContainer 
   {
      private int originalWidth = 0;

      // Event which indicates that IsExpanded property of panel is changed
      public event EventHandler IsExpandedChanged;

      private void OnIsExpandedChangedChanged(object sender)
      {
         if (IsExpandedChanged != null)
            IsExpandedChanged(sender, EventArgs.Empty);
      }

      public IOFormSplitContainer()
         : base(true)
      {
         Orientation = Orientation.Horizontal;
         originalWidth = this.Width;
         BorderStyle = BorderStyle.FixedSingle;
      }

      public override Orientation Orientation
      {
         get
         {
            return Orientation.Horizontal;
         }

         set
         {
            if (value == Orientation.Vertical)
               Debug.Assert(false, "vertical orientation is not supported for IoForms");
         }
      }

      public override Panel AddChild(string name, int index)
      {
         Panel panel = base.AddChild(name, index);
         panel.SizeChanged += Panel_SizeChanged;
         panel.VisibleChanged += Panel_VisibleChanged;
         return panel;
      }

      /// <summary>
      /// create splitter with expander
      /// </summary>
      /// <returns></returns>
      protected override Splitter CreateDefaultSplitterControl(string name)
      {
         IDesignerHost host = this.GetService(typeof(IDesignerHost)) as IDesignerHost;
         SplitterWithExpander splitter=  host.CreateComponent(typeof(SplitterWithExpander)) as SplitterWithExpander;
         splitter.Initialize(SplitterThinkness, name);
         splitter.PropertyChanged += Splitter_PropertyChanged;
         return splitter;
      }

      /// <summary>
      /// set the visibility of panel when expander is expanded/ collapsed
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void Splitter_PropertyChanged(object sender, PropertyChangedEventArgs e)
      {
         if (e.PropertyName == "IsExpanded")
         {
            SplitterWithExpander splitterWithExpander = ((SplitterWithExpander)(sender));
            Panel panel = GetPanelBySplitter(splitterWithExpander);
            panel.Visible = splitterWithExpander.IsExpanded; // set the visibility of panel
            OnIsExpandedChangedChanged(panel); // raise isexpanded changed event
         }
      }

      /// <summary>
      /// get panel below splitter
      /// </summary>
      /// <param name="splitter"></param>
      /// <returns></returns>
      public Panel GetPanelBySplitter (SplitterWithExpander splitter)
      {
         int index = this.Controls.GetChildIndex(splitter);
         Panel panel = this.Controls[index - 1] as Panel; // get the panel below splitter
         return panel;
      }

      /// <summary>
      /// register the size changed events
      /// </summary>
      private void RegisterSizeChangedEvents()
      {
         foreach (Panel panel in Panels)
            panel.SizeChanged += Panel_SizeChanged;
      }

      /// <summary>
      /// unregister the size changed events
      /// </summary>
      private void UnRegisterSizeChangedEvents()
      {
         foreach (Panel panel in Panels)
            panel.SizeChanged -= Panel_SizeChanged;
      }

      private void Panel_SizeChanged(object sender, EventArgs e)
      {
         UnRegisterSizeChangedEvents();


         if (originalWidth != ((Panel)sender).Width)
            UpdateWidth(((Panel)sender).Width);   // sets the width of all panels
         else
            UpdateHeight();

         RegisterSizeChangedEvents();
      }

      /// <summary>
      /// update height of splitter when panel is expanded/collapsed
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void Panel_VisibleChanged(object sender, EventArgs e)
      {
         UpdateHeight();
      }

      /// <summary>
      /// When width of one panels changes , set width of all panels
      /// </summary>
      /// <param name="width"></param>
      public void UpdateWidth(int width)
      {
         foreach (Control c in this.Panels)
            c.Width = width;

         originalWidth = width;
         this.ClientSize = new Size(width, ClientSize.Height);
      }

      /// <summary>
      /// Update the height of split container
      /// Height of split container is sum if height of all panels and height req. for splitters
      /// </summary>
      public void UpdateHeight()
      {
         int totalHeight = Panels.Where(x => x.Visible).Sum(x => x.Height);
         totalHeight += SplitPanelsCount * SplitterThinkness;

         // set the splitter's dimensions (total ht of all visible panels and height of all splitters)
         this.ClientSize = new Size(ClientSize.Width, totalHeight);
      }

      /// <summary>
      /// Show the panel if it is not visible
      /// </summary>
      /// <param name="panel"></param>
      public void EnsureExpanded(Panel panel)
      {
         if (!panel.Visible)
         {
            int index = Controls.GetChildIndex(panel);
            SplitterWithExpander splitterWithExpander = Controls[index + 1] as SplitterWithExpander;

            splitterWithExpander.IsExpanded = true;
         }
      }

      /// <summary>
      /// expand all panels 
      /// </summary>
      public void ExpandAll()
      {
         // Suspend painting until all panels are visible
         NativeWindowCommon.SendMessage(Handle, NativeWindowCommon.WM_SETREDRAW, false, 0);

         foreach (Panel panel in Panels)
            EnsureExpanded(panel);

         // Resume painting
         NativeWindowCommon.SendMessage(Handle, NativeWindowCommon.WM_SETREDRAW, true, 0);

         Refresh(); // refresh 
      }

      /// <summary>
      /// Activate the splitters which belong to current panel
      /// </summary>
      /// <param name="currentPanels"></param>
      public void ActivateSplitters(List<Panel> currentPanels)
      {
         foreach (SplitterWithExpander splitter in Splitters.OfType<SplitterWithExpander>())
         {
            splitter.IsActive = currentPanels.Contains(GetPanelBySplitter(splitter));
         }
      }


      /// <summary>
      /// update the text of splitter
      /// </summary>
      /// <param name="index"></param>
      /// <param name="text"></param>
      public void UpdateSplitterText(int index, string text)
      {
         SplitterWithExpander splitter = Splitters[index] as SplitterWithExpander;
         splitter.UpdateText(text);
      }

      /// <summary>
      /// get offset of splitter form control location
      /// </summary>
      /// <returns></returns>
      public override Point GetSplitterOffset()
      {
         // exclude the area of button (which is same as SplitterThinkness) and margin in X
         return new Point(SplitterThinkness, 0);
      }

      /// <summary>
      /// remove the panel
      /// </summary>
      /// <param name="panel"></param>
      public override void RemoveChild(Panel panel)
      {
         base.RemoveChild(panel);

         UpdateHeight();
      }

   
      /// <summary>
      /// get the actual index in gui from the index of panel in list
      /// </summary>
      /// <param name="panelsCount"></param>
      /// <param name="newIndex"></param>
      /// <param name="currentPanelIndex"></param>
      /// <returns></returns>
      protected override int GetChildIndexByPanelIndex(int panelsCount, int newIndex)
      {
         int childIndex =  (panelsCount - newIndex) * noOfControlsCreatedForEachPanel;
         return childIndex;
      }

      public override void Reorder(List<Panel> panels, List<int> indexes)
      {
         SuspendLayout();

         for (int i = 0; i < panels.Count; i++)
         {
            int panelIndex = this.Controls.GetChildIndex(panels[i]);
            Splitter splitter = this.Controls[panelIndex + noOfControlsCreatedForEachPanel - 1] as Splitter; // get the panel below splitter

            int newIndex = GetChildIndexByPanelIndex(Panels.Count - 1, indexes[i]);

            // when new position is below the current position, need to insert at new positon + 1
            // i.e when splitter is moved from 2 to 8 it should be inserted at 9 
            // but when  it is moved from 8 to 2 it should be inserted at 2 .

            // Some explanation ...(Strange .net behavior !!) 
         
            //Say that we have 5 splitter(s1..s5)  and panels (p1…p5) , with z order as shown

            // Move from 2 to 8 :
            // p1, s1, p2 , s2 ..... p5, s5
            // Now when we move p2 (at 2) to index 8 , all the controls after index 3(s2) to index 8(p5) will be moved upward by 1.
            // p1, s1, s2, p3, s3, p4, s4, p5, p2 ,s5  (not expected behavior)
            // Now as you can see when we move from 2 to 8 ,  P2 is inserted between P5 and S5 which is not desired behavior for us.
            //So, we need to insert P2 at index 9 and then S2 at 9  again  so that splitter and panels remain together.


            //Now consider the case when we move from 8 to 2.
            //When we move from 8 to 2, all controls from 2 to 7 are moved downwards.
            // So controls are :
            // p1, s1, p5, p2, s2, p3, s3, p4, s4, s5 (expected behavior )
            // (If we will move at 3 using +1 logic it will result in incorrect behavior )
            

            if (newIndex > panelIndex)
               newIndex += noOfControlsCreatedForEachPanel - 1;

            if (newIndex != panelIndex)
            {
               Controls.SetChildIndex(splitter, newIndex); // add splitter at 

               //correct position
               Splitters.Remove(splitter);
               Splitters.Insert(indexes[i], splitter);

               if (newIndex > panelIndex)
                  newIndex -= noOfControlsCreatedForEachPanel - 1;
               Controls.SetChildIndex(panels[i], newIndex);  // add panel at correct 

               //position
               Panels.Remove(panels[i]);
               Panels.Insert(indexes[i], panels[i]);

            }
         }

         ResumeLayout();
      }

      protected override void Dispose(bool disposing)
      {
         if (disposing)
         {
            UnRegisterSizeChangedEvents();
            foreach (Panel panel in Panels)
               panel.VisibleChanged -= Panel_VisibleChanged;

            foreach (SplitterWithExpander splitter in Splitters.OfType<SplitterWithExpander>())
               splitter.PropertyChanged -= Splitter_PropertyChanged;
         }
         base.Dispose(disposing);
      }

   }
}