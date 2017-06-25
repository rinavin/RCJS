using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using com.magicsoftware.controls.designers;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using System.Drawing;
using System.Linq;

namespace com.magicsoftware.controls
{
   /// <summary>
   /// control providing split functionality
   /// </summary>
   [Designer(typeof(EnhancedSplitContainerDesigner))]
   public class EnhancedSplitContainer : Panel, ICanResize, ICanParent
   {
      #region fields

      List<Panel> panels = new List<Panel>();
      List<Splitter> splitters = new List<Splitter>();
      int splitterThinkness = 6;
      private Orientation orientation = Orientation.Horizontal;

     

      /// <summary>
      /// indicates whether we should create spliiter for each panel
      /// </summary>
      private bool createSplitterForEachPanel = false;
   
      #endregion

      #region properties

      /// <summary>
      /// Orientation of the splitter
      /// </summary>
      public virtual Orientation Orientation
      {
         get { return orientation; }
         set
         {

            if (orientation != value)
            {
               orientation = value;
               foreach (var item in panels)
                  UpdateDocking(item);
               foreach (var item in splitters)
               {
                  item.Size = new System.Drawing.Size(SplitterThinkness, SplitterThinkness);
                  UpdateDocking(item);
               }

               PerformLayout();

            }
         }
      }

      /// <summary>
      /// child panels
      /// </summary>
      public List<Panel> Panels
      {
         get { return panels; }
      }

      /// <summary>
      /// list of splitters
      /// </summary>
      public List<Splitter> Splitters
      {
         get { return splitters; }
      }

      /// <summary>
      /// thinkness of the splitter
      /// </summary>
      public int SplitterThinkness
      {
         get { return splitterThinkness; }
         set { splitterThinkness = value; }
      }

      /// <summary>
      /// TBD : delegate for the devider control
      /// </summary>
      public CreateSplitterControlDelegate CreateSplitterControl { get; set; }


      /// <summary>
      /// count of the splitter panels
      /// </summary>
      public int SplitPanelsCount
      {
         get { return panels.Count; }
         set
         {
            foreach (var item in panels)
               item.Dispose();
            foreach (var item in splitters)
               item.Dispose();
            panels.Clear();
            splitters.Clear();
            for (int i = 0; i < value; i++)
               AddChild(string.Empty, 0);
         }
      }


      #endregion


      #region ctor
      public EnhancedSplitContainer()
      {
      }

      public EnhancedSplitContainer(bool createSplitterForEachPanel)
      {
         this.createSplitterForEachPanel = createSplitterForEachPanel;
      }

      #endregion
      #region methods

      /// <summary>
      /// add splitter child
      /// </summary>
      /// <returns></returns>
      public virtual Panel AddChild(string name, int panelsIndex)
      {
         int childIndex = panelsIndex > 0 ? GetChildIndexByPanelIndex(Panels.Count, panelsIndex) : 0;

         if (createSplitterForEachPanel || panels.Count > 0)
         {
            Splitter splitter = CreateDefaultSplitterControl(name);// CreateSplitterControl();
            splitter.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            splitter.Size = new System.Drawing.Size(SplitterThinkness, SplitterThinkness);
           
            UpdateDocking(splitter);

            Controls.Add(splitter);
            Controls.SetChildIndex(splitter, childIndex);
            // add splitter at correct position
            splitters.Insert(panelsIndex > 0 ? panelsIndex : splitters.Count, splitter);

         }
         Panel panel =  new Panel();
         panel.Name = name;
         UpdateDocking(panel);
         Controls.Add(panel);
         Controls.SetChildIndex(panel, childIndex);
         // add panel at correct position
         panels.Insert(panelsIndex > 0 ? panelsIndex : panels.Count, panel);

         return panel;
      }

      /// <summary>
      /// Remove panel and hence its child form and its corresponding splitter
      /// </summary>
      /// <param name="panel"></param>
      public virtual void RemoveChild(Panel panel)
      {
         int index = panels.IndexOf(panel);

         if (!createSplitterForEachPanel)
            index--;

         panel.Dispose();
         panels.Remove(panel);
                  
         if (index >= 0)
         {
            splitters[index].Dispose();
            splitters.RemoveAt(index);
         }
      }

      protected int noOfControlsCreatedForEachPanel = 2; // no of controls created per group (i.e 1 splitter and 1 panel )

      /// <summary>
      /// reorder forms
      /// </summary>
      /// <param name="panels"></param>
      /// <param name="indexes"></param>
      public virtual void Reorder(List<Panel> panels, List<int> indexes)
      {
        
      }

      /// <summary>
      /// get index of child from panel list' index
      /// </summary>
      /// <param name="totalChildCount"></param>
      /// <param name="index"></param>
      /// <returns></returns>
      protected virtual int GetChildIndexByPanelIndex(int totalChildCount, int newIndex)
      {
         return 0;
      }

      /// <summary>
      /// return child by index
      /// </summary>
      /// <param name="idx"></param>
      /// <returns></returns>
      public Control GetChildByIndex(int idx)
      {
         return Panels[idx];
      }


      /// <summary>
      ///       update docking
      /// </summary>
      /// <param name="control"></param>
      private void UpdateDocking(Control control)
      {
         control.Dock = orientation == Orientation.Horizontal ? DockStyle.Top : DockStyle.Left;
      }


      /// <summary>
      /// create default splitter control
      /// </summary>
      /// <returns></returns>
      protected virtual Splitter CreateDefaultSplitterControl(string name)
      {
         Splitter splitter = new Splitter();
         return splitter;
      }

      /// <summary>
      /// Get the offset of splitter (from control location)
      /// </summary>
      /// <returns></returns>
      public virtual Point GetSplitterOffset()
      {
         return Point.Empty;
      }


      /// <summary>
      /// indicates if split container is readonly
      /// </summary>
      public bool IsReadOnly { get; private set; }

      /// <summary>
      /// Set readonly property
      /// </summary>
      public void UpdateIsReadOnly()
      {
         // if all panels are locked readonly is true 
         IsReadOnly = Panels.All(panel => (bool)TypeDescriptor.GetProperties(panel.Controls[0])["Locked"].GetValue(panel.Controls[0]));
      }

      #endregion methods

      #region ICanResize
      public event CanResizeDelegate CanResizeEvent;

      public bool CanResize(CanResizeArgs canResizeArgs)
      {
         if (CanResizeEvent != null)
            return CanResizeEvent(this, canResizeArgs);

         if (canResizeArgs.Size > 5)
            return true;

         return false;
      }
      #endregion

      #region ICanParent members
      public event CanParentDelegate CanParentEvent;

      public bool CanParent(CanParentArgs allowDragDropArgs)
      {
         if (CanParentEvent != null)
            return CanParentEvent(this, allowDragDropArgs);
         return true;
      }
      #endregion

   }


   public delegate Control CreateSplitterControlDelegate();

   
}