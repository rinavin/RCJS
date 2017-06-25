using Controls.com.magicsoftware;
using System.Drawing;

namespace com.magicsoftware.controls
{
   /// <summary>
   /// Coordinator for controls under TableControl
   /// </summary>
   public class TableCoordinatorBase : ICoordinator, IRefreshable
   {
      #region fields/properties

      private int _xorigin; // the distance from column X position according to RTL settings
      private int _width;
      private int _x;
      private int _y;
      
      protected readonly ITableManager _tableManager;
      protected PlacementDrivenLogicalControl _logicalControl;
      
      public int MgRow { set; get; }
      public int MgColumn { private set; get; }

      #endregion

      #region Ctors

      public TableCoordinatorBase(ITableManager tableManager, PlacementDrivenLogicalControl logicalControl, int mgColumn)
      {
         _tableManager = tableManager;
         _logicalControl = logicalControl;
         MgColumn = mgColumn;
         MgRow = logicalControl._mgRow;
      }

      #endregion

      #region IRefreshable members

      /// <summary>
      /// 
      /// </summary>
      /// <param name="changed"></param>
      public virtual void Refresh(bool changed)
      {
         if (changed && (_tableManager != null))
            _tableManager.addRowToRefresh(MgRow);
      }

      public bool RefreshNeeded
      {
         get { return false; }
         set
         {
            if (value)
               Refresh(true);
         }
      }

      #endregion

      #region ICoordinator Members

      public int Width
      {
         get { return _width; }
         set
         {
            _width = value;
            if (_tableManager.RightToLeftLayout)
               updateXorigin();
            Refresh(true);
         }
      }

      public int X
      {
         get { return _x; }
         set
         {
            _x = value;
            updateXorigin();
            Refresh(true);
         }
      }

      public int Y
      {
         get { return _y; }
         set { _y = value - GetTableTop(); }
      }

      public int Height { get; set; }

      /// <summary>
      ///   return the rectangle of table child the x,y,w,h related to table
      /// </summary>
      public Rectangle getRectangle()
      {
         Rectangle cellRect = GetCellRect(MgColumn, MgRow);
         Rectangle dispRect = getDisplayRect(cellRect, false);

         return (dispRect);
      }

      public virtual Rectangle GetCellRect(int mgColumn, int mgRow)
      {
         return Rectangle.Empty;
      }

      protected virtual int GetTableTop()
      {
         return _tableManager.getMgTableTop();
      }

      /// <summary>
      /// </summary>
      /// <param name="placementDim"></param>
      /// <returns></returns>
      public int getPlacementDif(PlacementDim placementDim)
      {
         int result = 0;

#if !PocketPC
         //tmp
         bool containerRightToLeft = _tableManager.RightToLeftLayout;
#else
         bool containerRightToLeft = false;
#endif
         switch (placementDim)
         {
            case PlacementDim.PLACE_X:
            case PlacementDim.PLACE_DX:
               ILogicalColumn columnManager = _tableManager.ColumnsManager.getLgColumnByMagicIdx(MgColumn);
               int dx = columnManager.getDx();
               result = dx * _logicalControl.PlacementData.getPlacement(placementDim, containerRightToLeft) / 100;
               break;
            case PlacementDim.PLACE_Y:
            case PlacementDim.PLACE_DY:
               result = 0; // no Y placement in table's children
               break;
            default:
               break;
         }


         return result;
      }

      #endregion

      #region helper methods

      public Point GetLeftTop()
      {
         return _logicalControl.GetLeftTop();
      }

      public Point GetRightBottom()
      {
         return _logicalControl.GetRightBottom();
      }

      /// <summary>
      ///   returns control rectangle
      /// </summary>
      /// <param name = "cellRect">rectangle of table's cell </param>
      /// <returns> </returns>
      public virtual Rectangle getDisplayRect(Rectangle cellRect, bool isHeaderEditor)
      {
         var rect = new Rectangle(0, 0, 0, 0);
         //if (_logicalControl is Line)
         //   rect = ((Line)_logicalControl).calcDisplayRect();
         //else
         {
            // compute rectangle relativly to table
            rect.Height = Height;
            rect.Width = Width;
            PlacementData placementData = _logicalControl.PlacementData;

            // apply X placement (depends on column width)
            Rectangle placementRect = placementData.Placement;
            ILogicalColumn columnManager = _tableManager.ColumnsManager.getLgColumnByMagicIdx(MgColumn);
            int dx = columnManager.getDx();
            if (_tableManager.RightToLeftLayout)
            {
               int right = cellRect.Right - _xorigin;
               right -= dx * placementRect.X / 100;
               rect.Width += ((dx * placementRect.Width) / 100);
               rect.X = right - rect.Width;
            }
            else
            {
               rect.X = GetDisplayRectangleLeft(cellRect);
               rect.X += dx * placementRect.X / 100;
               rect.Width += ((dx * placementRect.Width) / 100);
            }

            // apply Y placement (depends on row height)
            // Defect 138536: For Header control, do not consider change in row height and hence do not consider placement for Y & Height
            int dy = isHeaderEditor ? 0 : _tableManager.GetChangeInRowHeight();
            rect.Y = GetDisplayRectangleTop(cellRect);
            rect.Y += dy * placementRect.Y / 100;

            //Height of the 3D ComboBox is fixed and depends on the Font.
            //We cannot change it by setting the Height.
            //So, the Height placement is irrelevant.
            //Sets height only for 2D combobox
            if (!IsComboControl() || Is2DComboBoxControl())
               rect.Height += ((dy * placementRect.Height) / 100);

            rect = _tableManager.GetTableMultiColumnStrategy(isHeaderEditor).GetRectToDraw(rect, cellRect);
         }

         return rect;
      }

      /// <summary>
      /// Return Left for control
      /// </summary>
      /// <param name="cellRect"></param>
      /// <returns></returns>
      protected virtual int GetDisplayRectangleLeft(Rectangle cellRect)
      {
         return cellRect.X + _xorigin;
      }

      /// <summary>
      /// Return Top for control
      /// </summary>
      /// <param name="cellRect"></param>
      /// <returns></returns>
      protected virtual int GetDisplayRectangleTop(Rectangle cellRect)
      {
         return cellRect.Y + Y;
      }

      protected virtual bool IsComboControl()
      {
         return false;
      }

      protected virtual bool Is2DComboBoxControl()
      {
         return false;
      }

      /// <summary>
      ///   update X origine of the child
      /// </summary>
      private void updateXorigin()
      {
         _xorigin = transformXToCell(X);
      }

      /// <summary>
      /// </summary>
      /// <param name="x"></param>
      /// <returns></returns>
      public int transformXToCell(int x)
      {
         ILogicalColumn columnManager = _tableManager.ColumnsManager.getLgColumnByMagicIdx(MgColumn);

         if (_tableManager.RightToLeftLayout)
         {
            int right = x + Width;
            int columnRight = columnManager.getStartXPos();
            return columnRight - right;
         }
         else
         {
            return x - columnManager.getStartXPos();
         }
      }

      #endregion
   }
}
