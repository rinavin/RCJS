using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using com.magicsoftware.controls;
using com.magicsoftware.util;
using com.magicsoftware.controls.utils;
using com.magicsoftware.controls.designers;
using Controls.com.magicsoftware.support;
using Controls.com.magicsoftware.controls.PropertyInterfaces;

namespace Controls.com.magicsoftware.controls.MgShape
{
   [Designer(typeof(ShapeDesigner))]
   public class MgShape : UserControl, ITextProperty, IMultilineProperty, IContentAlignmentProperty, INotifyPropertyChanged, IFontProperty, IFontDescriptionProperty, IBorderTypeProperty
   {
      public MgShape()
      {
         this.BackColor = Color.Transparent;
         TextAlign = ContentAlignment.MiddleCenter;
         FontDescription = new FontDescription(Font);
      }

      /// <summary>
      ///  Holds the shapes which should be drawn
      /// </summary>
      private ShapeTypes shapes;
      [Category("Appearance"),
      EditorAttribute(typeof(FlagEnumUIEditor), typeof(UITypeEditor))]
      public ShapeTypes Shapes
      {
         get { return shapes; }
         set
         {
            shapes = value;

            if ((value & ShapeTypes.Group) != 0) // When shape is set to Group, remove other shapes 
               shapes = ShapeTypes.Group;

            OnPropertyChanged("Shapes");
         }
      }

      /// <summary>
      /// Denotes the style of shape (2D, 3D-Raised..)
      /// </summary>
      private ControlStyle controlStyle = ControlStyle.TwoD;
      [Category("Appearance")]
      public ControlStyle ControlStyle
      {
         get
         {
            return controlStyle;
         }
         set
         {
            if (controlStyle != value)
            {
               controlStyle = value;
               Invalidate();
               OnPropertyChanged("ControlStyle");
            }
         }
      }

      /// <summary>
      /// Denotes the type of Line (Dash, dash-dot..)
      /// </summary>
      private CtrlLineType lineType = CtrlLineType.Normal;
      [Category("Appearance")]
      public CtrlLineType LineType
      {
         get
         {
            return lineType;
         }
         set
         {
            if (lineType != value)
            {
               lineType = value;
               OnPropertyChanged("LineType");
            }
         }
      }

      /// <summary>
      /// denotes the width of the line
      /// </summary>
      private int lineWidth = 1;
      [Category("Appearance")]
      public int LineWidth
      {
         get
         {
            return lineWidth;
         }
         set
         {
            if (lineWidth != value)
            {
               lineWidth = value;
               OnPropertyChanged("LineWidth");
            }
         }
      }

      #region IBorderTypeProperty Members

      private BorderType borderType = BorderType.Thin;

      /// <summary>
      /// Type of border (Thick / Thin / No Border)
      /// </summary>
      public BorderType BorderType
      {
         get { return borderType; }
         set
         {
            if (borderType != value)
            {
               borderType = value;
               Invalidate();
            }
         }
      }

      #endregion

      /// <summary>
      /// Denotes if text is multiline
      /// </summary>
      private bool multiline = false;
      [Category("Appearance")]
      public bool Multiline
      {
         get
         {
            return multiline;
         }
         set
         {
            if (multiline != value)
            {
               multiline = value;
               OnPropertyChanged("Multiline");
            }
         }
      }

      /// <summary>
      /// Denotes the alignment of Text
      /// </summary>
      ///
      private ContentAlignment textAlignment = ContentAlignment.MiddleCenter;
      [Category("Appearance")]
      public ContentAlignment TextAlign
      {
         get
         {
            return textAlignment;
         }
         set
         {
            if (textAlignment != value)
            {
               textAlignment = value;
               OnPropertyChanged("TextAlign");
            }
         }
      }

      /// <summary>
      /// denotes the orientation of font
      /// </summary>
      [Category("Appearance")]
      public int FontOrientation { get; set; }

      /// <summary>
      /// Color which is used to fill the shape.
      /// We cannot use BackColor because when we set BackColor, it is applies  for entire control .
      /// i.e while drawing ellipse , only ellipse must be colored. But if we use BackColor it is applied to whole control
      /// </summary>
      private Color fillColor;
      [Category("Appearance")]
      public Color FillColor
      {
         get
         {
            if (ControlStyle == ControlStyle.TwoD)
               return fillColor;
            else
               return SystemColors.ButtonFace;
         }
         set
         {
            if (fillColor != value)
            {
               fillColor = value;
               OnPropertyChanged("FillColor");
            }
         }
      }

      /// <summary>
      /// Represents the text
      /// </summary>
      [Browsable(true)]
      public override string Text
      {
         get
         {
            return base.Text;
         }
         set
         {
            base.Text = value;
            OnPropertyChanged("Text");
         }
      }

      #region IFontProperty
      public FontDescription FontDescription { get; set; }
      public override Font Font
      {
         get
         {
            return base.Font;
         }

         set
         {
            base.Font = value;
            FontDescription = new FontDescription(value);
         }
      }
      #endregion IFontProperty


      /// <summary>
      /// Paint the shape
      /// </summary>
      /// <param name="e"></param>
      protected override void OnPaint(PaintEventArgs e)
      {
         Rectangle textRect = ClientRectangle; // set the text Rect as client rect

         using (Pen pen = PaintHelper.InitPenForPaint(ForeColor, ControlStyle, LineType, LineWidth))
         {
            if ((Shapes & ShapeTypes.Group) != 0)
            {
               // Get the text rect for Group
               textRect = GetTextRectForGroup();
               GroupRenderer.Draw(e.Graphics, ClientRectangle, pen, FillColor, controlStyle, textRect, Font, !string.IsNullOrEmpty(Text));
            }

            // draw shapes
            if ((Shapes & ShapeTypes.Rect) != 0)
            {
               RectangleRenderer.Draw(e.Graphics, pen, ClientRectangle, FillColor, ControlStyle);
            }
            if ((Shapes & ShapeTypes.RoundRect) != 0)
            {
               RectangleRenderer.DrawRoundedRectangle(e.Graphics, pen, ClientRectangle, FillColor, ControlStyle);
            }
            if ((Shapes & ShapeTypes.Ellipse) != 0)
            {
               EllipseRenderer.Draw(e.Graphics, pen, ClientRectangle, FillColor, ControlStyle);
            }
            if ((Shapes & ShapeTypes.NWSELine) != 0)
            {
               LineRenderer.Draw(e.Graphics, pen, ClientRectangle, FillColor, ControlStyle, LineDirection.NWSE);
            }
            if ((Shapes & ShapeTypes.NESWLine) != 0)
            {
               LineRenderer.Draw(e.Graphics, pen, ClientRectangle, FillColor, ControlStyle, LineDirection.NESW);
            }
            if ((Shapes & ShapeTypes.HorizontalLine) != 0)
            {
               LineRenderer.Draw(e.Graphics, pen, ClientRectangle, FillColor, ControlStyle, LineDirection.Horizontal);
            }
            if ((Shapes & ShapeTypes.VerticalLine) != 0)
            {
               LineRenderer.Draw(e.Graphics, pen, ClientRectangle, FillColor, ControlStyle, LineDirection.Vertical);
            }
         }
         // Draw text
         DrawForeground(e.Graphics, textRect);
      }

      /// <summary>
      /// Draw foreground on the Shape control.
      /// </summary>
      /// <param name="graphics"></param>
      /// <param name="rect"></param>
      protected virtual void DrawForeground(Graphics graphics, Rectangle rect)
      {
         ControlRenderer.PrintText(graphics, rect, ForeColor, FontDescription, Text, Multiline, TextAlign,
                                   Enabled, true, true, false, (RightToLeft == RightToLeft.Yes), FontOrientation);
      }

      Rectangle GetTextRectForGroup()
      {
         Rectangle textRect = new Rectangle();
         Rectangle boundsRect = ClientRectangle;

         if (!string.IsNullOrEmpty(Text))
         {
            Size textSize = PaintHelper.GetTextExt(FontDescription.FontHandle, Text, this);
            int textWidth = textSize.Width;
            int textHeight = textSize.Height;

            PointF fontMetrics;
            using (Graphics g = this.CreateGraphics())
               Utils.GetTextMetrics(g, FontDescription.FontHandle, FontDescription.LogFont.lfFaceName, PaintHelper.LogFontHeightToFontSize(FontDescription.LogFont, this), out fontMetrics);

            int fontDX = (int)Math.Round(fontMetrics.X);
            int fontDY = (int)Math.Round(fontMetrics.Y);

            int top = boundsRect.Top;
            int bottom = Math.Min(boundsRect.Bottom, top + textHeight);
            int left = boundsRect.Left;
            int right = boundsRect.Right;

            if (TextAlign == ContentAlignment.TopLeft)
               left += fontDX;
            else if (TextAlign == ContentAlignment.TopCenter)
               left = Math.Max(left, left + (right - left - textWidth) / 2);
            else
               left = Math.Max(left, right - textWidth - fontDX);

            right = Math.Min(left + textWidth, boundsRect.Right);

            textRect = new Rectangle(new Point(left, top), new Size(Math.Abs(right - left), Math.Abs(top - bottom)));
         }

         return textRect;
      }

      #region INotifyPropertyChanged Members

      public event PropertyChangedEventHandler PropertyChanged;

      public void OnPropertyChanged(string propertyName)
      {
         if (PropertyChanged != null)
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
      }

      #endregion INotifyPropertyChanged Members

   }

   public class FlagCheckedListBox : CheckedListBox
   {
      private System.ComponentModel.Container components = null;

      public FlagCheckedListBox()
      {
         // This call is required by the Windows.Forms Form Designer.
         InitializeComponent();

         // TODO: Add any initialization after the InitForm call
      }

      protected override void Dispose(bool disposing)
      {
         if (disposing)
         {
            if (components != null)
               components.Dispose();
         }
         base.Dispose(disposing);
      }

      #region Component Designer generated code

      private void InitializeComponent()
      {
         //
         // FlaggedCheckedListBox
         //
         this.CheckOnClick = true;
      }

      #endregion Component Designer generated code

      // Adds an integer value and its associated description
      public FlagCheckedListBoxItem Add(int v, string c)
      {
         FlagCheckedListBoxItem item = new FlagCheckedListBoxItem(v, c);
         Items.Add(item);
         return item;
      }

      public FlagCheckedListBoxItem Add(FlagCheckedListBoxItem item)
      {
         Items.Add(item);
         return item;
      }

      protected override void OnItemCheck(ItemCheckEventArgs e)
      {
         base.OnItemCheck(e);

         if (isUpdatingCheckStates)
            return;

         // Get the checked/unchecked item
         FlagCheckedListBoxItem item = Items[e.Index] as FlagCheckedListBoxItem;
         // Update other items
         UpdateCheckedItems(item, e.NewValue);
      }

      // Checks/Unchecks items depending on the give bitvalue
      protected void UpdateCheckedItems(int value)
      {
         isUpdatingCheckStates = true;

         // Iterate over all items
         for (int i = 0; i < Items.Count; i++)
         {
            FlagCheckedListBoxItem item = Items[i] as FlagCheckedListBoxItem;

            if (item.value == 0)
            {
               SetItemChecked(i, value == 0);
            }
            else
            {
               // If the bit for the current item is on in the bitvalue, check it
               if ((item.value & value) == item.value && item.value != 0)
                  SetItemChecked(i, true);
               // Otherwise uncheck it
               else
                  SetItemChecked(i, false);
            }
         }

         isUpdatingCheckStates = false;
      }

      // Updates items in the checklistbox
      // composite = The item that was checked/unchecked
      // cs = The check state of that item
      protected void UpdateCheckedItems(FlagCheckedListBoxItem composite, CheckState cs)
      {
         // If the value of the item is 0, call directly.
         if (composite.value == 0)
            UpdateCheckedItems(0);

         // Get the total value of all checked items
         int sum = 0;
         for (int i = 0; i < Items.Count; i++)
         {
            FlagCheckedListBoxItem item = Items[i] as FlagCheckedListBoxItem;

            // If item is checked, add its value to the sum.
            if (GetItemChecked(i))
               sum |= item.value;
         }

         // If the item has been unchecked, remove its bits from the sum
         if (cs == CheckState.Unchecked)
            sum = sum & (~composite.value);
         // If the item has been checked, combine its bits with the sum
         else
            sum |= composite.value;

         // Update all items in the checklistbox based on the final bit value
         UpdateCheckedItems(sum);
      }

      private bool isUpdatingCheckStates = false;

      // Gets the current bit value corresponding to all checked items
      public int GetCurrentValue()
      {
         int sum = 0;

         for (int i = 0; i < Items.Count; i++)
         {
            FlagCheckedListBoxItem item = Items[i] as FlagCheckedListBoxItem;

            if (GetItemChecked(i))
               sum |= item.value;
         }

         return sum;
      }

      private Type enumType;
      private Enum enumValue;

      // Adds items to the checklistbox based on the members of the enum
      private void FillEnumMembers()
      {
         foreach (string name in Enum.GetNames(enumType))
         {
            object val = Enum.Parse(enumType, name);
            int intVal = (int)Convert.ChangeType(val, typeof(int));

            Add(intVal, name);
         }
      }

      // Checks/unchecks items based on the current value of the enum variable
      private void ApplyEnumValue()
      {
         int intVal = (int)Convert.ChangeType(enumValue, typeof(int));
         UpdateCheckedItems(intVal);
      }

      [DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
      public Enum EnumValue
      {
         get
         {
            object e = Enum.ToObject(enumType, GetCurrentValue());
            return (Enum)e;
         }
         set
         {
            Items.Clear();
            enumValue = value; // Store the current enum value
            enumType = value.GetType(); // Store enum type
            FillEnumMembers(); // Add items for enum members
            ApplyEnumValue(); // Check/uncheck items depending on enum value
         }
      }
   }

   // Represents an item in the checklistbox
   public class FlagCheckedListBoxItem
   {
      public FlagCheckedListBoxItem(int v, string c)
      {
         value = v;
         caption = c;
      }

      public override string ToString()
      {
         return caption;
      }

      // Returns true if the value corresponds to a single bit being set
      public bool IsFlag
      {
         get
         {
            return ((value & (value - 1)) == 0);
         }
      }

      // Returns true if this value is a member of the composite bit value
      public bool IsMemberFlag(FlagCheckedListBoxItem composite)
      {
         return (IsFlag && ((value & composite.value) == value));
      }

      public int value;
      public string caption;
   }

   // UITypeEditor for flag enums
   public class FlagEnumUIEditor : UITypeEditor
   {
      // The checklistbox
      private FlagCheckedListBox flagEnumCB;

      public FlagEnumUIEditor()
      {
         flagEnumCB = new FlagCheckedListBox();
         flagEnumCB.BorderStyle = BorderStyle.None;
      }

      public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
      {
         if (context != null
            && context.Instance != null
            && provider != null)
         {
            IWindowsFormsEditorService edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));

            if (edSvc != null)
            {
               Enum e = (Enum)Convert.ChangeType(value, context.PropertyDescriptor.PropertyType);
               flagEnumCB.EnumValue = e;
               edSvc.DropDownControl(flagEnumCB);
               return flagEnumCB.EnumValue;
            }
         }
         return null;
      }

      public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
      {
         return UITypeEditorEditStyle.DropDown;
      }
   }
}
