using System;
using System.Collections.Generic;
using System.Text;
using System.Data;

namespace com.magicsoftware.unipaas.management.data
{
   /// <summary>
   /// This class is implemented to support DataColumn of DataTable in mgXP.
   /// </summary>
   public class MGDataColumn
   {
      internal String Name { get; set; }
      internal Type DataType { get; set; }
      internal int FldIdx { get; set; }
   }

   /// <summary>
   /// This class is implemented to handle functionality of DataTable in mgXP.
   /// </summary>
   public abstract class MGDataTable
   {
      public List<MGDataColumn> ColumnList { get; set; } //Column's list in dataTable

      /// <summary>
      /// Constructor.
      /// </summary>
      internal MGDataTable()
      {
         //create DataTable object.
         DataTblObj = new DataTable();
      }

      internal DataTable DataTblObj { private set; get; }

      /// <summary>
      ///   Add columns to DataTable object.
      /// </summary>
      internal void AddColumns()
      {
         foreach (MGDataColumn column in ColumnList)
         {
            DataTblObj.Columns.Add(column.Name, column.DataType);
         }
      }
   }
}
