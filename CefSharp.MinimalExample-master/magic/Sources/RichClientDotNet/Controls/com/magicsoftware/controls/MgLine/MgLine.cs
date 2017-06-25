using System;
using com.magicsoftware.controls;
using System.ComponentModel;
using System.Windows.Forms;
using System.Drawing;
using com.magicsoftware.util;
using System.Diagnostics;
using com.magicsoftware.controls.designers;
using Controls.com.magicsoftware.controls.MgShape;

namespace Controls.com.magicsoftware.controls.MgLine
{
   /// <summary>
   /// Defines Line control for the form designer.
   /// </summary>
   [Designer(typeof(LineDesigner))]
   public class MgLine : Label, INotifyPropertyChanged, IOwnerDrawnControl
   {
      private bool refreshControl = true;
      public bool CreateAsTableHeaderChild { get; set; }

      // The size should me minimum 2 pixels for painting purpose
      public int MinSize { get { return Math.Max(LineWidth, 3); } }


      /// <summary>
      /// indicates if only horizontal and vertical line must be supported
      /// </summary>
      private bool supportDiagonalLines = true;
      public bool SupportDiagonalLines
      {
         get
         {
            return supportDiagonalLines;
         }
         set
         {
            if (supportDiagonalLines != value)
            {
               supportDiagonalLines = value;
               OnPropertyChanged("SupportDiagonalLines");
            }
         }
      }

      #region CTOR
    
      /// <summary>
      /// Constructor
      /// </summary>
      public MgLine()
      {
         this.BackColor = Color.Transparent;
         this.ForeColor = Color.Transparent;
         DirectionOfLine = LineDirection.Horizontal;
      }

      #endregion

      /// <summary>
      /// Override this method to set the minimum size as linewidth
      /// </summary>
      /// <param name="x"></param>
      /// <param name="y"></param>
      /// <param name="width"></param>
      /// <param name="height"></param>
      /// <param name="specified"></param>
      protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
      {
         if (!CreateAsTableHeaderChild && DirectionOfLine == LineDirection.Horizontal)
         {
            if (((specified & (BoundsSpecified.Height | BoundsSpecified.Y)) == (BoundsSpecified.Height | BoundsSpecified.Y)))
            {
               base.SetBoundsCore(x, Top, width, MinSize, specified); // when top glyph is dragged , don't change the top , keep it as Control's  current Top
            }
            else
               base.SetBoundsCore(x, y, width, MinSize, specified);
         }
         else if (!CreateAsTableHeaderChild && DirectionOfLine == LineDirection.Vertical)
         {
            if (((specified & (BoundsSpecified.Width | BoundsSpecified.X)) == (BoundsSpecified.Width | BoundsSpecified.X)))
            {
               base.SetBoundsCore(Left, y, MinSize, height, specified);// when left glyph is dragged , don't change the left, keep it as Control's current Left
            }
            else
               base.SetBoundsCore(x, y, MinSize, height, specified);
         }
         else if(CreateAsTableHeaderChild)
            base.SetBoundsCore(x, y, Math.Max(width, LineWidth * 2), height, specified);
         else
            base.SetBoundsCore(x, y, width, height, specified);
      }

      #region Overridden methods of control
      protected override void OnSizeChanged(EventArgs e)
      {
         base.OnSizeChanged(e);
         SetLinePoints();
      }

      protected override void OnLocationChanged(EventArgs e)
      {
         base.OnLocationChanged(e);
         SetLinePoints();
      }
      #endregion

      #region events
      public delegate void LineWidthChangeDelegate(object sender, LineWidthChangedEventArgs args);

      public event LineWidthChangeDelegate LineWidthChanged;

      public void OnLineWidthChanged(int oldWidth)
      {
         if (LineWidthChanged != null)
            LineWidthChanged(this, new LineWidthChangedEventArgs(oldWidth));
      }
      
      public delegate void LineDirectionChangingDelegate(object sender, EventArgs args);

      public event LineDirectionChangingDelegate LineDirectionChanging;

      public void OnLineDirectionChanging()
      {
         if (LineDirectionChanging != null)
            LineDirectionChanging(this, EventArgs.Empty);
      }

      public delegate void LineDirectionChangedDelegate(object sender, EventArgs args);

      public event LineDirectionChangedDelegate LineDirectionChanged;
      
      public void OnLineDirectionChanged()
      {
         if (LineDirectionChanged != null)
            LineDirectionChanged(this, EventArgs.Empty);
      }

      public event PropertyChangedEventHandler PropertyChanged;

      public  void OnPropertyChanged(string name)
      {
         if(PropertyChanged != null)
         {
            PropertyChanged(this, new PropertyChangedEventArgs(name));
         }

      }
      #endregion


      #region Line Properties

      /// <summary>
      /// Start point of the line
      /// </summary>
      public Point StartPoint { get; set; }

      /// <summary>
      /// End point of the line
      /// </summary>
      public Point EndPoint { get; set; }

      private LineDirection directionOfLine;
      public LineDirection DirectionOfLine
      {
         get
         {
            return directionOfLine;
         }
         set
         {
            if (directionOfLine != value)
            {
               OnLineDirectionChanging();

               LineDirection oldValue = directionOfLine;
               directionOfLine = value;
               if (refreshControl)
                  UpdateControlSize(oldValue);
               OnLineDirectionChanged();
            }
         }
      }

      /// <summary>
      /// Update the size of control according to new direction
      /// </summary>
      /// <param name="oldValue"></param>
      private void UpdateControlSize(LineDirection oldValue)
      {
         if (DirectionOfLine == LineDirection.Vertical)
         {
            if (oldValue == LineDirection.Horizontal)
               this.Size = new Size(MinSize, Width);
            else
               this.Size = new Size(MinSize, Height);
         }
         else if (DirectionOfLine == LineDirection.Horizontal)
         {
            if (oldValue == LineDirection.Vertical)
               this.Size = new Size(Height, MinSize);
            else
               this.Size = new Size(Width, MinSize);
         }
         else
         {
            if (oldValue == LineDirection.Horizontal)
               this.Size = new Size(Width, 20);
            if (oldValue == LineDirection.Vertical)
               this.Size = new Size(20, Height);
         }

         Invalidate();
         SetLinePoints();
      }

      /// <summary>
      /// denotes the width of the line
      /// </summary>
      int lineWidth = 1;
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

               int oldLineWidth = lineWidth;
               lineWidth = value;
               OnLineWidthChanged(oldLineWidth);
               SetLinePoints();
               Invalidate();
            }
         }
      }

      /// <summary>
      /// property for control style i.e 2-d , 3-d etc
      /// </summary>
      CtrlLineType ctrlLineType = CtrlLineType.Normal;
      public CtrlLineType CtrlLineType
      {
         get
         {
            return ctrlLineType;
         }
         set
         {
            if (ctrlLineType != value)
            {
               ctrlLineType = value;
               Invalidate();
            }
         }
      }

      /// <summary>
      /// property for line type - dashed .. dotted etc
      /// </summary>
      ControlStyle controlStyle = ControlStyle.ThreeD;
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
            }
         }
      }

      /// <summary>
      /// denotes the color of the line
      /// </summary>
      Color lineColor = Color.Black;
      public Color LineColor
      {
         get
         {
            return lineColor;
         }
         set
         {
            if (lineColor != value)
            {
               lineColor = value;
               Invalidate();
            }
         }
      }

      #endregion

      /// <summary>
      /// 
      /// </summary>
      /// <param name="pevent"></param>
      protected override void OnPaintBackground(PaintEventArgs pevent)
      {
         if (Parent is ISupportsTransparentChildRendering)
            ((ISupportsTransparentChildRendering)Parent).TransparentChild = this;

         base.OnPaintBackground(pevent);

         if (Parent is ISupportsTransparentChildRendering)
            ((ISupportsTransparentChildRendering)Parent).TransparentChild = null;
      }

      /// <summary>
      /// Paint the line
      /// </summary>
      /// <param name="e"></param>
      protected override void OnPaint(PaintEventArgs e)
      {
         ControlRenderer.PaintLineForeGround(e.Graphics, LineColor, ControlStyle, LineWidth, CtrlLineType, StartPoint, EndPoint);
      }

      /// <summary>
      /// Set the Line's Start and End points
      /// </summary>
      public void SetLinePoints()
      {
         // TODO : See if we can remove StartPoint and EndPoint and if we can directly use LineRenderer.Draw() which calculates points from rectangle 
         Point[] linePoints = LineRenderer.GetLinePoints(this.ClientRectangle, lineWidth, directionOfLine);
         StartPoint = linePoints[0];
         EndPoint = linePoints[1];
      }

      /// <summary>
      /// Set the direction of the line
      /// </summary>
      /// <param name="newLineDirection"></param>
      /// <param name="refreshControl"></param>
      public void SetLineDirection(LineDirection newLineDirection, bool refreshControl)
      {
         this.refreshControl = refreshControl;
         DirectionOfLine = newLineDirection;
      }

      #region IOwnerDrawnControl Members

      /// <summary>
      /// Draw the control on the given Graphics at the specified rectangle.
      /// </summary>
      /// <param name="graphics"></param>
      /// <param name="rect"></param>
      public void Draw(Graphics graphics, Rectangle rect)
      {
         Point[] linePoints = LineRenderer.GetLinePoints(rect, lineWidth, directionOfLine);
         ControlRenderer.PaintLineForeGround(graphics, LineColor, ControlStyle, LineWidth, CtrlLineType, linePoints[0], linePoints[1]);
      }

      #endregion
   }

   /// <summary>
   /// Event args for line width changed event
   /// </summary>
   public class LineWidthChangedEventArgs : EventArgs
   {
      public int OldLineWidth { get; private set; }

      public LineWidthChangedEventArgs(int oldWidth)
      {
         this.OldLineWidth = oldWidth;
      }
   }
}
