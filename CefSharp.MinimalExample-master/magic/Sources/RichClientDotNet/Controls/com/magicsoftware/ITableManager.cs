using System.Drawing;
using Controls.com.magicsoftware;

namespace com.magicsoftware.controls
{
   /// <summary>
   /// Interfaces for managing Table control
   /// </summary>
   public interface ITableManager
   {
      /// <summary>
      /// Is table RightToLeft
      /// </summary>
      bool RightToLeftLayout { get; }

      /// <summary>
      /// Columns Manager
      /// </summary>
      ColumnsManager ColumnsManager { get; }

      /// <summary>
      /// Table Placement Manager
      /// </summary>
      TablePlacementManagerBase TablePlacementManager { get; }
      
      /// <summary>
      /// Get Top for table control
      /// </summary>
      /// <returns></returns>
      int getMgTableTop();

      /// <summary>
      /// Adds rows to refresh row list
      /// </summary>
      /// <param name="mgRow"></param>
      void addRowToRefresh(int mgRow);

      /// <summary>
      /// Gets change in height of row
      /// </summary>
      /// <returns></returns>
      int GetChangeInRowHeight();

      /// <summary>
      /// Gets table MultiColumnStrategy
      /// </summary>
      /// <returns></returns>
      TableMultiColumnStrategyBase GetTableMultiColumnStrategy(bool isHeaderEditor);
   }
}
