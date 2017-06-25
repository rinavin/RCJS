using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.Design;
using System.Windows.Forms.Design;
using Controls.com.magicsoftware.controls.MgLine;
using com.magicsoftware.util;

namespace com.magicsoftware.controls.designers
{
   /// <summary>
   /// Designer class overridden for the purpose of showing proper glyphs
   /// </summary>
   internal class LineDesigner : System.Windows.Forms.Design.ControlDesigner
   {
      DesignerVerbCollection designerVerbCollection = new DesignerVerbCollection();
      DesignerVerb toVertical;
      DesignerVerb toHorizontal;
      DesignerVerb toNWSE;
      DesignerVerb toNESW;

      MgLine Line { get { return (MgLine)Component; } }

      public override void Initialize(System.ComponentModel.IComponent component)
      {
         base.Initialize(component);
         toVertical = new DesignerVerb(Controls.Properties.Resources.Vertical_s, new EventHandler(OnChangeToVertical));
         toHorizontal = new DesignerVerb(Controls.Properties.Resources.Horizontal_s, new EventHandler(OnChangeToHorizontal));
         toNWSE = new DesignerVerb(Controls.Properties.Resources.NWSE_s, new EventHandler(OnChangeToNWSE));
         toNESW = new DesignerVerb(Controls.Properties.Resources.NESW_s, new EventHandler(OnChangeToNESW));

         designerVerbCollection.Add(toVertical);
         designerVerbCollection.Add(toHorizontal);
         designerVerbCollection.Add(toNESW);
         designerVerbCollection.Add(toNWSE);

         SetVerbStatus();
         ((MgLine)Component).PropertyChanged += LineDesigner_PropertyChanged;
         ((MgLine)Component).LineDirectionChanged += new MgLine.LineDirectionChangedDelegate(LineDesigner_LineDirectionChanged);
      }
      #region Event handlers

      private void LineDesigner_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
      {
         if (e.PropertyName == "SupportDiagonalLines")
         {
            SetVerbStatus();
         }
      }

      void LineDesigner_LineDirectionChanged(object sender, EventArgs args)
      {
         SetVerbStatus();
      }

      private void OnChangeToVertical(object sender, System.EventArgs e)
      {
         SetLineDirection(LineDirection.Vertical);
      }

      private void OnChangeToHorizontal(object sender, System.EventArgs e)
      {
         SetLineDirection(LineDirection.Horizontal);
      }

      private void OnChangeToNWSE(object sender, System.EventArgs e)
      {
         SetLineDirection(LineDirection.NWSE);
      }

      private void OnChangeToNESW(object sender, System.EventArgs e)
      {
         SetLineDirection(LineDirection.NESW);
      }

      private void SetLineDirection(LineDirection direction)
      {
         IDesignerHost host = (IDesignerHost)this.GetService(typeof(IDesignerHost));
         using (DesignerTransaction designerTransaction = host.CreateTransaction("Changing Line Direction"))
         {
            Line.SetLineDirection(direction, true);
            designerTransaction.Commit();
         }
      }
      #endregion

      /// <summary>
      /// Set the status of the verb
      /// </summary>
      private void SetVerbStatus()
      {
         // Set the visibility of verbs depending on whether diagonal lines are supported 
         toNESW.Visible = Line.SupportDiagonalLines; 
         toNWSE.Visible = Line.SupportDiagonalLines;
         
         toHorizontal.Enabled = Line.DirectionOfLine != LineDirection.Horizontal;
         toVertical.Enabled = Line.DirectionOfLine != LineDirection.Vertical;
         toNESW.Enabled = Line.DirectionOfLine != LineDirection.NESW;
         toNWSE.Enabled = Line.DirectionOfLine != LineDirection.NWSE;
      }

      public override System.ComponentModel.Design.DesignerVerbCollection Verbs
      {
         get { return designerVerbCollection; }
      }

      public override SelectionRules SelectionRules
      {
         get
         {
            MgLine line = Control as MgLine;

            // Horizontal Line - show right and left glyphs
            if (line.DirectionOfLine == LineDirection.Horizontal)
               return System.Windows.Forms.Design.SelectionRules.Moveable |
               System.Windows.Forms.Design.SelectionRules.Visible |
               System.Windows.Forms.Design.SelectionRules.LeftSizeable |
               System.Windows.Forms.Design.SelectionRules.RightSizeable |
               System.Windows.Forms.Design.SelectionRules.TopSizeable;

            // Vertical Line - show top  and bottom glyphs
            else if (line.DirectionOfLine == LineDirection.Vertical)
            {
               return System.Windows.Forms.Design.SelectionRules.Moveable |
               System.Windows.Forms.Design.SelectionRules.Visible |
               System.Windows.Forms.Design.SelectionRules.LeftSizeable |
               System.Windows.Forms.Design.SelectionRules.TopSizeable |
               System.Windows.Forms.Design.SelectionRules.BottomSizeable;
            }

            else  // Diagonal line - show all glyphs
               return System.Windows.Forms.Design.SelectionRules.AllSizeable |
                   System.Windows.Forms.Design.SelectionRules.Visible |
                   System.Windows.Forms.Design.SelectionRules.Moveable;
         }
      }
   }
}
