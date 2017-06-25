using System.Collections.Generic;
using System.Diagnostics;

namespace com.magicsoftware.controls
{
   /// <summary>
   /// Manages Columns in TableControl
   /// </summary>
   public class ColumnsManager
   {
      #region fields/properties

      //array of all columns, the order of columns is according to the table RTL style
      //i.e. in RTL first column is right most column
      readonly List<ILogicalColumn> _columns;

      public int ColumnsCount { get { return Columns.Count; } }
      public List<ILogicalColumn> Columns { get { return _columns; } }

      #endregion

      #region Ctors

      public ColumnsManager(int columnsCount)
      {
         // create columns
         _columns = new List<ILogicalColumn>();
         for (int i = 0; i < columnsCount; i++)
            _columns.Add(null);
      }

      #endregion

      #region Helper methods

      /// <summary> translates magic column index into SWT column index
      /// </summary>
      /// <param name="mgColumn"></param>
      /// <returns></returns>
      public int getGuiColumnIdx(int mgColumn)
      {
         Debug.Assert(mgColumn < _columns.Count);
         return getLgColumnByMagicIdx(mgColumn).GuiColumnIdx;
      }

      public TableColumn getColumnByMagicIdx(int mgColumn)
      {
         return getLgColumnByMagicIdx(mgColumn).TableColumn;
      }

      /// <summary> translates Gui column index into magic column index
      /// </summary>
      /// <param name="guiColumn"></param>
      /// <returns></returns>
      public int getMagicColumnIndex(int guiColumn)
      {
         for (int i = 0; i < ColumnsCount; i++)
            if (_columns[i].GuiColumnIdx == guiColumn)
               return _columns[i].MgColumnIdx;
         return -1;
      }

      /// <summary>
      /// return column by gui index
      /// </summary>
      /// <param name="guiColumn"></param><returns></returns>
      public ILogicalColumn getLgColumnByGuiIdx(int guiColumn)
      {
         for (int i = 0; i < ColumnsCount; i++)
            if (_columns[i].GuiColumnIdx == guiColumn)
               return _columns[i];
         return null;
      }

      /// <summary>
      /// return column by gui index
      /// </summary>
      /// <param name="column"></param>
      /// <returns></returns>
      public ILogicalColumn getLgColumnByColumn(TableColumn column)
      {
         for (int i = 0; i < ColumnsCount; i++)
            if (_columns[i].TableColumn == column)
               return _columns[i];
         return null;
      }

      /// <summary>
      /// return columnManager by magic index
      /// </summary>
      /// <param name="mgColumn"></param>
      /// <returns></returns>
      public ILogicalColumn getLgColumnByMagicIdx(int mgColumn)
      {
         for (int i = 0; i < ColumnsCount; i++)
            if (_columns[i] != null && _columns[i].MgColumnIdx == mgColumn)
               return _columns[i];
         return null;
      }

      /// <summary>
      /// Return Column
      /// </summary>
      /// <param name="idx"></param>
      /// <returns></returns>
      public ILogicalColumn getColumn(int idx)
      {
         Debug.Assert(idx < _columns.Count);
         return (ILogicalColumn)_columns[idx];
      }

      /// <summary>
      /// Inserts column to Column list
      /// </summary>
      /// <param name="column"></param>
      /// <param name="mgColumn"></param>
      /// <param name="rightToLeft"></param>
      public void Insert(ILogicalColumn column, int mgColumn, bool rightToLeft)
      {
         int guiColumn = rightToLeft ? ColumnsCount - 1 - mgColumn : mgColumn;
         _columns[guiColumn] = column;
      }

      #endregion
   }
}
