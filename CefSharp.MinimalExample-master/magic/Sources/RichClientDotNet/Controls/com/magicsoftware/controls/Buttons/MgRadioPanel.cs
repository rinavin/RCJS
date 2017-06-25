using System;
using System.Drawing;
using System.Windows.Forms;
using com.magicsoftware.controls.utils;
using com.magicsoftware.util;

#if !PocketPC
using System.Windows.Forms.Layout;
using System.ComponentModel;
using com.magicsoftware.controls.designers;
using System.ComponentModel.Design;
#else
using LayoutEventArgs = com.magicsoftware.mobilestubs.LayoutEventArgs;
using Appearance = com.magicsoftware.mobilestubs.Appearance;
using LayoutEngine = com.magicsoftware.mobilestubs.LayoutEngine;
using Panel = com.magicsoftware.controls.MgPanel;
using ContentAlignment = OpenNETCF.Drawing.ContentAlignment2;
using FlatStyle = com.magicsoftware.mobilestubs.FlatStyle;
using PointF = com.magicsoftware.mobilestubs.PointF;
using RightToLeft = com.magicsoftware.mobilestubs.RightToLeft;
#endif

namespace com.magicsoftware.controls
{
   ///<summary>
   ///  This class demonstrates a simple custom layout panel.
   ///  It overrides the LayoutEngine property of the Panel
   ///  control to provide a custom layout engine.
   ///</summary>
#if !PocketPC
   [Designer(typeof(RadioControlDesigner)), Docking(DockingBehavior.Never)]
   // ToolboxBitMap is used to define Radio Button as the toolbox image icon of RadioPanel.
   [ToolboxBitmap(typeof(RadioButton))]
   public class MgRadioPanel : Panel, IMultilineProperty, IContentAlignmentProperty, IRightToLeftProperty, IGradientColorProperty, IChoiceControl, ISetSpecificControlPropertiesForFormDesigner
#else
   public class MgRadioPanel : Panel, IMultilineProperty, IContentAlignmentProperty, IRightToLeftProperty, IGradientColorProperty
#endif
   {
      public RadioLayout _layoutEngine { get; set; }

      #region IMultilineRenderer Members

        private bool multiline = false;
        public bool Multiline
        {
           get { return multiline; }
           set
           {
              if (multiline != value)
              {
                 multiline = value;
                 Refresh();
              }
           }
        }

      #endregion

      #region IContentAlignmentRenderer Members

      ContentAlignment textAlign = ContentAlignment.MiddleCenter;
      public ContentAlignment TextAlign
      {
         get { return textAlign; }
         set
         {
            if (textAlign != value)
            {
               textAlign = value;
               foreach (MgRadioButton radioButton in Controls)
                  radioButton.TextAlign = value;
            }
         }
      }

      #endregion

      #region IGradientColorProperty Members

#if !PocketPC
      public GradientColor GradientColor { get; set; }
      public GradientStyle GradientStyle { get; set; }
#endif
      #endregion

#if !PocketPC
      #region Fields/Properties

      private FlatStyle flatStyle = FlatStyle.Standard;
      public FlatStyle FlatStyle
      {
          get { return flatStyle; }
          set
          {
              if (flatStyle != value)
              {
                  flatStyle = value;
                  foreach (MgRadioButton radioButton in Controls)
                      radioButton.FlatStyle = value;
              }
          }
      }

      private Appearance appearance = Appearance.Normal;
      public Appearance Appearance
      {
         get { return appearance; }
         set
         {
            if (appearance != value)
            {
               appearance = value;
               foreach (MgRadioButton radioButton in Controls)
                  radioButton.Appearance = value;
            }
         }
      }

      private int selectedIndex = -1;
      public int SelectedIndex
      {
         get
         {
            return selectedIndex;
         }
         set
         {
            UpdateSelectedIndex(value);
         }
      }

      #endregion Fields/Properties
#else
      public FlatStyle FlatStyle { get; set; }
      public Appearance Appearance { get; set; }
#endif

      /// <summary></summary>
      public MgRadioPanel()
      {
#if PocketPC
         // We have only real controls on this panel, so we don't want the dunny control
         Controls.Remove(dummy);
         dummy = null;
         _layoutEngine = new RadioLayout();
#else
         this.ControlAdded += new ControlEventHandler(MgRadioPanel_ControlAdded);
         this.ControlRemoved += new ControlEventHandler(MgRadioPanel_ControlRemoved);
#endif
         GradientStyle = GradientStyle.None;
      }

#if !PocketPC
      public event EventHandler SelectedIndexChanged;

      // Invoke the SelectedIndexChanged event
      protected virtual void OnSelectedIndexChanged(EventArgs e)
      {
         if (SelectedIndexChanged != null)
            SelectedIndexChanged(this, e);
      }

      /// <summary>
      /// Event handler used to set properties of MgRadioButton added.
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void MgRadioPanel_ControlAdded(object sender, ControlEventArgs e)
      {
          (e.Control as MgRadioButton).FlatStyle = FlatStyle;
          (e.Control as MgRadioButton).Appearance = Appearance;
          (e.Control as MgRadioButton).TextAlign = TextAlign;
          (e.Control as MgRadioButton).CheckedChanged += MgRadioButton_CheckedChanged;
      }

      /// <summary>
      /// Event handler used to set properties of MgRadioButton removed.
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void MgRadioPanel_ControlRemoved(object sender, ControlEventArgs e)
      {
         //Fixed defect # 117066 : When radio Panel is displayed through subForm it was not displayed it's selected value.
         //This is because when controls are removed & added again, we set selected index of radioButton. In this case, when it was trying
         //to set the selected index of another radio button the old selected index & new selected index was same. So it was not entering in the if condition. 
         //Solution: When controls are removed form Panel, its selected index should be reset to -1. This will ensure that selected index of deleted control is reset.
         if ((e.Control as MgRadioButton).Checked)
            selectedIndex = -1;
         (e.Control as MgRadioButton).CheckedChanged -= MgRadioButton_CheckedChanged;
      }

      void MgRadioButton_CheckedChanged(object sender, EventArgs e)
      {
         if (((MgRadioButton)sender).Checked == true)
            SelectedIndex = this.Controls.IndexOf((MgRadioButton)sender);
         else
            //If all controls inside the container are unchecked then set radioPanel's SelectedIndex = -1.
            SelectedIndex = -1;
      }

      private void UpdateSelectedIndex(int newValue)
      {
         MgRadioButton radioButton = null;

         if (SelectedIndex != newValue)
         {
            Control.ControlCollection c = (Control.ControlCollection)(Controls);
            if (c.Count > 0)
            {
               //unregister & register the CheckedChanged event of radioButton & unCheck the old selected radioButton.
               if (SelectedIndex > -1)
               {
                  radioButton = (MgRadioButton)Controls[SelectedIndex];
                  radioButton.CheckedChanged -= MgRadioButton_CheckedChanged;
                  radioButton.Checked = false;
                  radioButton.CheckedChanged += MgRadioButton_CheckedChanged;
               }

               //unregister & register the CheckedChanged event of radioButton & Check the new selected radioButton.
               if (newValue > -1)
               {
                  radioButton = (MgRadioButton)Controls[newValue];
                  radioButton.CheckedChanged -= MgRadioButton_CheckedChanged;
                  radioButton.Checked = true;
                  radioButton.CheckedChanged += MgRadioButton_CheckedChanged;
               }
            }

            selectedIndex = newValue;
            OnSelectedIndexChanged(EventArgs.Empty);
         }
      }
#endif

      /// <summary>
      /// 
      /// </summary>
#if !PocketPC
      public override LayoutEngine LayoutEngine
#else
      internal LayoutEngine LayoutEngine
#endif
      {
         get
         {
            if (_layoutEngine == null)
            {
               _layoutEngine = new RadioLayout();
            }

            return _layoutEngine;
         }
      }

      /// <summary>
      ///   set number of columns in the mg radio panel
      /// </summary>
      public int NumColumns
      {
         set
         {
            _layoutEngine.setNumColumns(value);
            OnLayout(new LayoutEventArgs(null, null));
         }
      }

      /// <summary>
      ///   return the number of columns in the MgRadio Panel
      /// </summary>
      /// <param name = "choiceCnt"></param>
      /// <returns></returns>
      public int getCountOfButtonsInColumn(int choiceCnt)
      {
         return _layoutEngine.getCountOfButtonsInColumn(choiceCnt, Appearance == Appearance.Button);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="e"></param>
      protected override void OnPaintBackground(PaintEventArgs e)
      {
         //Simulate Transparency
         if (BackColor == Color.Transparent)
         {
            if (Parent is ISupportsTransparentChildRendering)
               ((ISupportsTransparentChildRendering)Parent).TransparentChild = this;

            System.Drawing.Drawing2D.GraphicsContainer g = e.Graphics.BeginContainer();
            Rectangle translateRect = this.Bounds;
            int borderWidth = (this.Width - this.ClientSize.Width) / 2;
            e.Graphics.TranslateTransform(-(Left + borderWidth), -(Top + borderWidth));
            PaintEventArgs pe = new PaintEventArgs(e.Graphics, translateRect);
            this.InvokePaintBackground(Parent, pe);
            this.InvokePaint(Parent, pe);
            e.Graphics.ResetTransform();
            e.Graphics.EndContainer(g);
            pe.Dispose();

            if (Parent is ISupportsTransparentChildRendering)
               ((ISupportsTransparentChildRendering)Parent).TransparentChild = null;
         }
         else
            base.OnPaintBackground(e);
      }

      /// <summary>
      ///   create radio layout
      /// </summary>
      [Serializable]
      public class RadioLayout : LayoutEngine
      {       
         private int _numColumns = 1;

#if !PocketPC
         private const int RADIO_RECT_DX = 13;
         private const int RADIO_RECT_DY = 13;
         private const int RADIO_TEXT_SPACE = 7;
#else
         private const int RADIO_RECT_DX = 26;
         private const int RADIO_RECT_DY = 26;
         private const int RADIO_TEXT_SPACE = 13;
#endif

         /// <summary>
         ///   save the num of column on this radio panel
         /// </summary>
         /// <param name = "numColumns"></param>
         internal void setNumColumns(int numColumns)
         {
            _numColumns = numColumns;
         }

         /**
          * calc how many radio buttons should be in a single column Same as calc_sum_rb_in_col()
          */

         internal int getCountOfButtonsInColumn(int choiceCnt, bool isAppearanceButton)
         {
            int radioInColumn = 0;
            int numColumns = _numColumns;

            if (isAppearanceButton)
               numColumns = Math.Min(numColumns, choiceCnt);

            /* check if number of cols is logical */
            /* ---------------------------------- */
            radioInColumn = choiceCnt / numColumns;
            if (radioInColumn * numColumns < choiceCnt)
               radioInColumn++;
            return (radioInColumn);
         }

         /// <summary>
         /// </summary>
         /// <param name = "container"></param>
         /// <param name = "layoutEventArgs"></param>
         /// <returns></returns>
         public override bool Layout(object container, LayoutEventArgs layoutEventArgs)
         {
            Control parent = container as Control;

            if (parent.Controls.Count == 0)
               return false;
            Size clientRect = ((Panel) container).Size;
            int radioLineSeperator = 0;
            int radioColSeperator = 0;
            MgRadioButton radioButton = (MgRadioButton)parent.Controls[0];
            Boolean isAppearanceButton = (radioButton.Appearance == Appearance.Button);
            int radionInColumn = getCountOfButtonsInColumn(parent.Controls.Count, isAppearanceButton);

            Rectangle radioRect = new Rectangle(0, 0, 0, 0);

            if (parent.Controls.Count == 1)
            {
               radioButton.SetBounds(0, 0, clientRect.Width, clientRect.Height);
            }
            else
            {
               int i = 0;
               int width = 0;
               int height = 0;

               int numColumns = _numColumns;

               if (isAppearanceButton)
               {
                  numColumns = Math.Min(numColumns, parent.Controls.Count);
                  width = radioRect.Width = clientRect.Width / numColumns;
                  height = radioRect.Height = clientRect.Height/radionInColumn;
                  // Fixed defect #:137271: radioColSeperator & radioLineSeperator will be 0 for button
                  radioColSeperator = 0;
                  radioLineSeperator = 0;
                  radioRect.X = radioLineSeperator;
                  radioRect.Y = radioColSeperator;
               }
               else
               {
                  MgRadioPanel mgRadioPanel = container as MgRadioPanel;
                  if (radioButton.Image != null)
                  {
                     // Measure the radio button image size.
                     width = radioButton.Image.Width;
                     height = radioButton.Image.Height;
                  }
                  else
                  {
                     //find the maximun string size in the composite
                     foreach (Control c in parent.Controls)
                     {
                        MgRadioButton mgRadioButton = (MgRadioButton) c;
                        Size textExt = Utils.GetTextExt(mgRadioPanel.Font, mgRadioButton.TextToDisplay, c);
                        width = Math.Max(width, textExt.Width);
                        height = Math.Max(height, textExt.Height);
                     }
                  }
                  // add extras
                  width = (int)((float)(RADIO_RECT_DX + RADIO_TEXT_SPACE + width + 1) * Utils.GetDpiScaleRatioX(parent));
                  height = (int)((float)(Math.Max(RADIO_RECT_DY, height + 2) * Utils.GetDpiScaleRatioY(parent)));

                  PointF fm = Utils.GetFontMetricsByFont(mgRadioPanel, (mgRadioPanel).Font);

                  radioLineSeperator = (int)fm.Y/2;
                  height = Math.Max(height, (clientRect.Height - (radionInColumn*radioLineSeperator))/radionInColumn);
                  radioColSeperator = (int)Math.Max((clientRect.Width - (numColumns * width)) / (numColumns + 1), fm.X / 2);

                  // Use DisplayRectangle so that parent.Padding is honored.
#if !PocketPC
                  Rectangle parentDisplayRectangle = parent.DisplayRectangle;
#else
                  Rectangle parentDisplayRectangle = parent.Bounds;
                  parentDisplayRectangle.X = parentDisplayRectangle.Y = 0;
#endif
                  Point nextControlLocation = parentDisplayRectangle.Location;

                  // calculate the bounds of the first button in the first column
                  radioRect.Y = radioLineSeperator;
                  radioRect.Height = height;
                  // TODO: Hebrew

                  if (mgRadioPanel.RightToLeft == RightToLeft.Yes)
                    radioRect.X = mgRadioPanel.Width - radioColSeperator - width;
                  else
                     radioRect.X = radioColSeperator;
                  radioRect.Width = width;
               }

               foreach (Control c in parent.Controls)
               {
                  radioButton = (MgRadioButton) c;
                  {
                     // checks if skip to another column is needed
                     if (numColumns > 1 && i > 0 && (i % radionInColumn) == 0)
                     {
                        // TODO: Hebrew
                        MgRadioPanel mgRadioPanel = container as MgRadioPanel;
                        if (mgRadioPanel.RightToLeft == RightToLeft.Yes)
                           radioRect.X -= width + radioColSeperator;
                        else
                           radioRect.X += width + radioColSeperator;
                        radioRect.Y = radioLineSeperator;
                     }
                  }

                  if (radioRect.X + radioRect.Width > clientRect.Width)
                     radioRect.Width = clientRect.Width - radioRect.X;
                  if (radioRect.Y + radioRect.Height > clientRect.Height)
                     radioRect.Height = clientRect.Height - radioRect.Y;
                  // set the radio bounds
                  radioButton.SetBounds(radioRect.X, radioRect.Y, radioRect.Width, radioRect.Height);

                  // sets the next radio location
                  radioRect.Y += radioLineSeperator + height;
                  i++;
               }
            }
            return false;
         }
      }

#if PocketPC
      internal void OnLayout(LayoutEventArgs levent)
      {
         LayoutEngine.Layout(this, levent);
      }
#endif

      public void setSpecificControlPropertiesForFormDesigner(Control fromControl)
      {

#if !PocketPC
         // get the designer of the control and reset the ResetVerbs, so the designer action will calc again 
         IDesignerHost designerHost = (IDesignerHost)this.GetService(typeof(IDesignerHost));
         IDesigner designer = designerHost.GetDesigner(this);

         RadioControlDesigner rcd = designer as RadioControlDesigner;
         rcd.ResetVerbs();
#endif
      }
   }
}
