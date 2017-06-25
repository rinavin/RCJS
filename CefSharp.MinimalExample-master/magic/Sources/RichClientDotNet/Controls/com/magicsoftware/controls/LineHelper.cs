using System;
using com.magicsoftware.util;
using System.Drawing;
using Controls.com.magicsoftware;
using System.ComponentModel;

namespace com.magicsoftware.controls
{
   /// <summary>
   /// enum of coordinate type
   /// </summary>
   public enum COORDINATE
   {
      LEFT = 0,
      RIGHT,
      TOP,
      BOTTOM
   }

   /// <summary>
   /// Helper class for line control
   /// </summary>
   public class LineHelper : INotifyPropertyChanged
   {
      bool containerRightToLeft;
      ICoordinator coordinator;

      /// <summary>
      /// ctor
      /// </summary>
      /// <param name="coordinator"></param>
      /// <param name="containerRightToLeft"></param>
      public LineHelper(ICoordinator coordinator, bool containerRightToLeft)
      {
         this.coordinator = coordinator;
         this.containerRightToLeft = containerRightToLeft;
      }

      private int x1;
      public int X1
      {
         get
         {
            COORDINATE coordinate = (x1 <= x2 ? COORDINATE.LEFT : COORDINATE.RIGHT);
            return translateCoordinate(coordinate, x1);
         }
         set
         {
            if (x1 != value)
            {
               // TODO : handle for TableCoordinator
               x1 = value;
               OnPropertyChanged("X1");
            }
         }
      }

      private int x2;
      public int X2
      {
         get
         {
            COORDINATE coordinate = (x1 > x2 ? COORDINATE.LEFT : COORDINATE.RIGHT);
            return translateCoordinate(coordinate, x2);
         }
         set
         {
            if (x2 != value)
            {
               // TODO : handle for TableCoordinator
               x2 = value;
               OnPropertyChanged("X2");
            }
         }
      }

      private int y1;
      public int Y1
      {
         get
         {
            COORDINATE coordinate = (y1 <= y2 ? COORDINATE.TOP : COORDINATE.BOTTOM);
            return translateCoordinate(coordinate, y1);
         }
         set
         {

            if (y1 != value)
            {
               // TODO : handle for TableCoordinator
               y1 = value;
               OnPropertyChanged("Y1");
            }
         }
      }

      private int y2;
      public int Y2
      {
         get
         {
            COORDINATE coordinate = (y1 > y2 ? COORDINATE.TOP : COORDINATE.BOTTOM);
            return translateCoordinate(coordinate, y2);
         }
         set
         {
            if (y2 != value)
            {
               // TODO : handle for TableCoordinator
               y2 = value;
               OnPropertyChanged("Y2");
            }
         }
      }

      private CtrlLineDirection lineDir;
      public CtrlLineDirection LineDir
      {
         get { return lineDir; }
         set
         {
            lineDir = value;
            OnPropertyChanged("LineDir");
         }
      }

      ///   style
      private ControlStyle style;
      public ControlStyle Style
      {
         get { return style; }
         set
         {
            style = value;
         }
      }

      private int lineWidth;
      /// <summary>
      /// line width
      /// </summary>
      public int LineWidth
      {
         get { return lineWidth; }
         set
         {
            lineWidth = value;
            
            if (coordinator is BasicCoordinator)
               calcDisplayRect(calcLineRectangle());
            OnPropertyChanged("LineWidth");
         }
      }

      private CtrlLineType lineStyle;
      public CtrlLineType LineStyle
      {
         get { return lineStyle; }
         set
         {
            lineStyle = value;
            OnPropertyChanged("LineStyle");
         }
      }

      #region methods

      /// <summary>
      /// translate line coordinate according to Axe to table coordinates
      /// </summary>
      /// <param name="coordinate"></param>
      /// <param name="num"></param>
      /// <returns></returns>
      public int translateCoordinate(COORDINATE coordinate, int num)
      {
         // TODO : handle for TableCoordinator
         return num + PlacementDif(coordinate);
      }

      /// <summary>
      /// calcultate diplay rectangle of control
      /// based on calc_line_rect form ctrl_utl.cpp in online
      /// </summary>
      public Rectangle calcDisplayRect(Rectangle rect)
      {
         int tmp;

         if (Style == ControlStyle.ThreeD || Style == ControlStyle.ThreeDSunken)
         {
            tmp = LineWidth;

            if (tmp > 1)
            {
               if (rect.Y == rect.Bottom)
               {
                  rect.Y -= (tmp / 2);
                  rect.Height = tmp;
               }
               if (rect.X == rect.Right)
               {
                  rect.X -= (tmp / 2);
                  rect.Width = tmp;
               }
            }
            else
            {
               if (rect.Y == rect.Bottom)
                  rect.Height = 2;

               if (rect.X == rect.Right)
                  rect.Width = 2;
            }
         }
         else
         {
            tmp = LineWidth;

            if (rect.Y == rect.Bottom)
            {
               rect.Y -= (tmp / 2);
               rect.Height = tmp;
            }
            if (rect.X == rect.Right)
            {
               rect.X -= (tmp / 2);
               rect.Width = tmp;
            }
         }
         if (rect.Width < 0)
         {
            rect.X += rect.Width;
            rect.Width = -rect.Width;
         }

         if (rect.Height < 0)
         {
            rect.Y += rect.Height;
            rect.Height = -rect.Height;
         }

         rect.Inflate(lineWidth / 2, lineWidth / 2);

         if (coordinator is BasicCoordinator)
            ((BasicCoordinator)coordinator).DisplayRect = rect;

         return rect;
      }

      /// <summary>
      /// calculate rectangle line
      /// </summary>
      public Rectangle calcLineRectangle()
      {
         Rectangle rect = new Rectangle();
         rect.X = Math.Min(X1, X2);
         rect.Y = Math.Min(Y1, Y2);
         rect.Height = Math.Abs(Y1 - Y2);
         rect.Width = Math.Abs(X1 - X2);
         return rect;
      }

      /// <summary>
      /// get diff caused by placement according to coordinate
      /// </summary>
      /// <param name="coord"></param>
      /// <returns></returns>
      private int PlacementDif(COORDINATE coord)
      {
         int placementDiff = 0;
         int dx;
         switch (coord)
         {
            case COORDINATE.LEFT:
               placementDiff = coordinator.getPlacementDif(PlacementDim.PLACE_X);
               if (containerRightToLeft)
               {
                  dx = coordinator.getPlacementDif(PlacementDim.PLACE_DX);
                  placementDiff -= dx;
               }
               break;

            case COORDINATE.RIGHT:
               placementDiff = coordinator.getPlacementDif(PlacementDim.PLACE_X);
               if (!containerRightToLeft)
               {
                  dx = coordinator.getPlacementDif(PlacementDim.PLACE_DX);
                  placementDiff += dx;
               }
               break;

            case COORDINATE.TOP:
               placementDiff = coordinator.getPlacementDif(PlacementDim.PLACE_Y);
               break;

            case COORDINATE.BOTTOM:
               placementDiff = coordinator.getPlacementDif(PlacementDim.PLACE_Y);
               placementDiff += coordinator.getPlacementDif(PlacementDim.PLACE_DY);
               break;

            default:
               break;
         }

         return placementDiff;
      }

      #endregion

      #region INotifyPropertyChanged

      public event PropertyChangedEventHandler PropertyChanged;

      public void OnPropertyChanged(string propertyName)
      {
         if (PropertyChanged != null)
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
      }

      #endregion
   }
}