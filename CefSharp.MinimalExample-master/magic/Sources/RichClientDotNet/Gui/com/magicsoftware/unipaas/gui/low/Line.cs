using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using com.magicsoftware.unipaas.gui;
using com.magicsoftware.util;
using com.magicsoftware.controls;
using Controls.com.magicsoftware;
#if !PocketPC
using Controls.com.magicsoftware.controls.MgLine;
#endif
#if PocketPC
using com.magicsoftware.richclient.mobile.gui;
#endif

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary>
   /// implement line control
   /// </summary>
   internal class Line : LogicalControl, ILine
   {

      /// <summary>
      /// translate line coordinate according to Axe to table coordinates
      /// </summary>
      /// <param name="coordinate"></param>
      /// <param name="num"></param>
      /// <returns></returns>
      private int translateCoordinate(COORDINATE coordinate, int num)
      {
         if (_coordinator is TableCoordinator)
         {
            switch (coordinate)
            {
               case COORDINATE.LEFT:
               case COORDINATE.RIGHT:
                  num = ((TableCoordinator)_coordinator).transformXToTable(num);
                  break;
               case COORDINATE.TOP:
               case COORDINATE.BOTTOM:
                  num = ((TableCoordinator)_coordinator).transformYToTable(num);
                  break;
               default:
                  break;
            }
         }

         return LineHelper.translateCoordinate(coordinate, num);

      }

      public override Point GetLeftTop()
      {
         return new Point(X1, Y1);
      }

      public override Point GetRightBottom()
      {
         return new Point(X2, Y2);
      }

      // TODO - see if X1, X2, Y1,Y2 are needed here when the TableCoordinator is handled 

      private int _x1;
      internal int X1
      {
         get
         {
            if (_coordinator is TableCoordinator)
            {
               COORDINATE coordinate = (_x1 <= _x2 ? COORDINATE.LEFT : COORDINATE.RIGHT);
               return translateCoordinate(coordinate, _x1);
            }
            return LineHelper.X1;
         }
         set
         {
            if (_coordinator is TableCoordinator)
            {
               value = ((TableCoordinator)_coordinator).transformXToCell(value);
               _x1 = value;
            }
          
            LineHelper.X1 = value;

         }
      }

      private int _x2;
      internal int X2
      {
         get
         {
            if (_coordinator is TableCoordinator)
            {
               COORDINATE coordinate = (_x1 > _x2 ? COORDINATE.LEFT : COORDINATE.RIGHT);
               return translateCoordinate(coordinate, _x2);
            }
            return LineHelper.X2;
         }
         set
         {
            if (_coordinator is TableCoordinator)
            {
               value = ((TableCoordinator)_coordinator).transformXToCell(value);
               _x2 = value;
            }
            LineHelper.X2 = value;
         }
      }


      private int _y1;
      internal int Y1
      {
         get
         {
            if (_coordinator is TableCoordinator)
            {
               COORDINATE coordinate = (_y1 <= _y2 ? COORDINATE.TOP : COORDINATE.BOTTOM);
               return translateCoordinate(coordinate, _y1);
            }

            return LineHelper.Y1;
         }
         set
         {
            if (_coordinator is TableCoordinator)
            {
               value = ((TableCoordinator)_coordinator).transformYToCell(value);
               _y1 = value;
            }
           
            LineHelper.Y1 = value;

         }
      }

      private int _y2;
      internal int Y2
      {
         get
         {
            if (_coordinator is TableCoordinator)
            {
               COORDINATE coordinate = (_y1 > _y2 ? COORDINATE.TOP : COORDINATE.BOTTOM);
               return translateCoordinate(coordinate, _y2);
            }
            return LineHelper.Y2;
         }
         set
         {
            if (_coordinator is TableCoordinator)
            {
               value = ((TableCoordinator)_coordinator).transformYToCell(value);
               _y2 = value;
            }
            LineHelper.Y2 = value;
         }
      }

      internal override ControlStyle Style
      {
         get
         {
            return base.Style;
         }
         set
         {
            base.Style = value;
            LineHelper.Style = value;
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="guiMgControl"></param>
      /// <param name="containerControl"></param>
      internal Line(GuiMgControl guiMgControl, Control containerControl)
         : base(guiMgControl, containerControl)
      {

      }

      LineHelper lineHelper;
      public LineHelper LineHelper
      {
         get
         {
            if (lineHelper == null)
            {
#if !PocketPC
               bool containerRightToLeft = (_containerControl.RightToLeft == System.Windows.Forms.RightToLeft.Yes);
#else
               bool containerRightToLeft = false;
#endif
               lineHelper = new LineHelper(this._coordinator, containerRightToLeft);
               lineHelper.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(lineHelper_PropertyChanged);
            }
            return lineHelper;
         }
      }

      /// <summary>
      /// invalidate the control when property changes 
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void lineHelper_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
      {
         Invalidate(true);
      }


      /// <summary>
      /// 
      /// </summary>
      /// <param name="control"></param>
      internal override void setSpecificControlPropertiesForFormDesigner(Control control)
      {
         SetControlProperties(control);
      }

      /// <summary>
      /// sets prorties on MgLine
      /// </summary>
      /// <param name="control"></param>
      void SetControlProperties(Control control)
      {
#if !PocketPC

         MgLine lineControl = control as MgLine;
         lineControl.LineColor = FgColor;
         lineControl.ControlStyle = LineHelper.Style;
         lineControl.LineWidth = LineHelper.LineWidth;
         lineControl.CtrlLineType = LineHelper.LineStyle;
         lineControl.BackColor = Color.Transparent;
         lineControl.ForeColor = Color.Transparent;
#endif
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="g"></param>
      /// <param name="rect"></param>
      /// <param name="bgColor"></param>
      /// <param name="fgColor"></param>
      /// <param name="keepColor"></param>
      internal override void paintBackground(Graphics g, Rectangle rect, Color bgColor, Color fgColor, bool keepColor)
      {
         //no bg paint for line
      }

      /// <summary>
      /// paint line control
      /// </summary>
      /// <param name="g"></param>
      internal override void paintForeground(Graphics g, Rectangle rect, Color fgColor)
      {
         if (!Visible)
            return;

         Point pt1 = new Point(X1, Y1);
         Point pt2 = new Point(X2, Y2);
#if PocketPC
         BasicControlsManager staticControlsManager = ContainerManager as BasicControlsManager;
         if (staticControlsManager != null)
         {
            Point offset = staticControlsManager.ContainerOffset();        
            pt1.Offset(offset.X, offset.Y);
            pt2.Offset(offset.X, offset.Y);
         }
#endif
         ControlRenderer.PaintLineForeGround(g, fgColor, LineHelper.Style, LineHelper.LineWidth, LineHelper.LineStyle, pt1, pt2);
      }

      /// <summary>
      /// returns true is line contains point
      /// if line width is small uses 2 pixel offset for calculations
      /// </summary>
      /// <param name="pt"></param> point coordinates that include scrollbar offset
      /// <returns></returns>
      internal bool Contains(Point pt)
      {
         double dis = GuiUtils.ShortestDistance(pt, new Point(X1, Y1), new Point(X2, Y2));
         if (dis <= Math.Max(2, LineHelper.LineWidth / 2))
            return true;
         return false;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="wholeParent"></param>
      internal override void Invalidate(bool wholeParent)
      {
         //for Line control need to invalidate previous location 
         if (wholeParent && ContainerManager is TableManager)
            _containerControl.Invalidate();
         else
            base.Invalidate(wholeParent);
      }

      internal override void setSpecificControlProperties(Control control)
      {
#if !PocketPC
         MgLine line = (MgLine)control;
         if (X1 == X2)
            line.DirectionOfLine = LineDirection.Vertical;
         else if (Y1 == Y2)
            line.DirectionOfLine = LineDirection.Horizontal;
         else if ((X1 < X2) == (Y1 > Y2)) // points 1 and 2 sizes can be in any order. What realy matters is if X is big when Y is big
            line.DirectionOfLine = LineDirection.NESW;
         else
            line.DirectionOfLine = LineDirection.NWSE;

         if (!GuiMgControl.IsTableHeaderChild)
         {
            line.StartPoint = new Point(X1, Y1);
            line.EndPoint = new Point(X2, Y2);
         }

         // base label control values
         line.Height = Math.Abs(Y2 - Y1);
         line.Width = Math.Abs(X2 - X1);
         line.Top = Math.Min(Y1, Y2);
         line.Left = Math.Min(X1, X2);

         if (GuiMgControl.IsTableHeaderChild)
         {
            SetControlProperties(control);
         }
#endif
      }

      /// <summary>
      /// Calculate Display rectangle
      /// </summary>
      /// <returns></returns>
      public Rectangle calcDisplayRect()
      {
         return LineHelper.calcDisplayRect(calcLineRectangle());
      }

      /// <summary>
      /// returns Line control rectangle
      /// </summary>
      /// <returns></returns>
      private Rectangle calcLineRectangle()
      {
         Rectangle rect = new Rectangle();
         rect.X = Math.Min(X1, X2);
         rect.Y = Math.Min(Y1, Y2);
         rect.Height = Math.Abs(Y1 - Y2);
         rect.Width = Math.Abs(X1 - X2);
         return rect;
      }

   }
}
