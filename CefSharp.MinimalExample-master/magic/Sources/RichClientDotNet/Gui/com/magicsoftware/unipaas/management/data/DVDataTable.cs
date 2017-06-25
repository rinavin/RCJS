using System;
using System.Collections;
using System.Data;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.management.tasks;
using GuiCommandType = com.magicsoftware.unipaas.gui.CommandType;

namespace com.magicsoftware.unipaas.management.data
{
   /// <summary>
   /// This class is implemented to handle DataTable functionality for DataView Control.
   /// </summary>
   public class DVDataTable : MGDataTable
   {
      public int CurrRow { get; set; } // It will hold last parked row. 

      /// <summary>
      /// Constructor.
      /// </summary>
      internal DVDataTable()
      {
         //Create a column to store PosIsn and it will be hidden
         DataTblObj.Columns.Add("Isn", typeof(int));
         DataTblObj.Columns[0].ColumnMapping = MappingType.Hidden;

         //Define Column "Isn" as primary key of DataTabel
         DataColumn[] PrimaryKeyColumns = new DataColumn[1];
         PrimaryKeyColumns[0] = DataTblObj.Columns["Isn"];
         DataTblObj.PrimaryKey = PrimaryKeyColumns;
      }

      /// <summary>
      /// This will return fldIdx for columnIdx passed.
      /// </summary>
      /// <param name="columnId"></param>
      /// <returns>fldIdx</returns>
      public int GetFldIdx(int columnId)
      {
         return ColumnList[columnId-1].FldIdx;
      }

      /// <summary>
      /// This will return columnIdx for fldIdx passed.
      /// </summary>
      /// <param name="fldIdx"></param>
      /// <returns>columnId</returns>
      internal int GetColumnId (int fldIdx)
      {
	       int columnId = 0;	
	      foreach (MGDataColumn column in ColumnList)
	      {
            columnId++;
            if (column.FldIdx == fldIdx)
               return columnId;
	      }
	      return columnId;
      }

      /// <summary>
      /// This will be called, while fetching records into DataTable at the start to create row.
      /// </summary>
      /// <param name="idx"></param>
      internal void checkAndCreateRow (int idx)
      {
	      createRow (idx);
         CurrRow = idx;
      }

      /// <summary>
      /// LoadNewRow() 
      /// </summary>
      /// <param name="row"></param>
      public void LoadNewRow(object[] row)
      {
         DataRow newRow = DataTblObj.NewRow();
         newRow["Isn"] = 0;
         for (int i = 0; i < DataTblObj.Columns.Count; i++)
            newRow[i] = row[i];
         DataTblObj.Rows.InsertAt(newRow, CurrRow);
      }

      /// <summary>
      /// When user creates new row in DataTable by F4/ any event, this method will be directly called to 
      /// insert row at idx in DataTable.
      /// </summary>
      /// <param name="idx"></param>
      internal void createRow (int idx)
      {
         Commands.addAsync(GuiCommandType.CREATE_ROW_IN_DVCONTROL, this, idx, true);
      }

      /// <summary>
      /// This method prepares column List selected for DataView Control. Add it to DataTable.
      /// </summary>
      /// <param name="task"></param>
      /// <param name="fieldList"></param>
      internal void PrepareColumns (TaskBase task, String fieldList)
      {
         ColumnList = new List<MGDataColumn>();

         var fieldsTable = task.DataView.GetFieldsTab();

         IEnumerator tokens = StrUtil.tokenize(fieldList, ",").GetEnumerator();
         while (tokens.MoveNext())
         {
            String strToken = (String)tokens.Current;
            int i = int.Parse(strToken);

            var field = (Field)fieldsTable.getField(i - 1);
            if (field.getType() == StorageAttribute.DOTNET || field.getType() == StorageAttribute.BLOB ||
               field.getType() == StorageAttribute.BLOB_VECTOR)
               continue;
            var newColumn = new MGDataColumn
            {
               Name = field.getVarName(),
               DataType = field.GetDefaultDotNetTypeForMagicType(),
               FldIdx = i - 1
            };
            ColumnList.Add(newColumn);
         }

         // If  main table and linked table are having same column names, then append prefix such as _1, _2 for duplicate names (QCR # 291279)
         for (int i = 0; i < ColumnList.Count; i++)
         {
            int cnt = 1;
            String columnName = ColumnList[i].Name;
            for (int j = i+1; j < ColumnList.Count; j++)
            {
               if (columnName.Equals(ColumnList[j].Name))
               {
                  ColumnList[j].Name = ColumnList[j].Name+"_"+cnt.ToString();
                  cnt++;
               }
            }
         }

         AddColumns();
      }
   }
}

