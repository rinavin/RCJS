using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.unipaas;
using System.Diagnostics;

namespace com.magicsoftware.unipaas.dotnet
{
#if !PocketPC
   /// <summary>
   /// This class is used for .Net choice controls
   /// <summary>
   internal class DNChoiceControlDataTable : MGDataTable
   {
      /// <summary>
      /// For .Net choice control the columns added will always be DisplayMember & ValueMember
      /// </summary>
      /// <param name="dataType">.Net choice control's Attribute</param>
      /// <param name="itmList">Value Member List to get the .Net type from ctrls Atrribute(Magic type)</param>
      internal void AddColumns(StorageAttribute dataType, string[] itmList)
      {
         Type dnDataType = null;

         if (dataType == StorageAttribute.NUMERIC)
         {
            for (int i = 0; i < itmList.Length; i++)
            {
               dnDataType = DNConvert.getDefaultDotNetTypeForMagicType(itmList[i], dataType);
               if (dnDataType == typeof(Double))
                  break;
            }
         }
         else
            dnDataType = DNConvert.getDefaultDotNetTypeForMagicType(null, dataType);

         // ValueMember column of the DataTable must have the type of the LinkField if set, else
         // Data if set else unicode.
         MGDataColumn valMemCol = new MGDataColumn()
         {
            Name = GuiConstants.STR_VALUE_MEMBER,
            DataType = dnDataType
         };

         MGDataColumn dispMemCol = new MGDataColumn()
         {
            Name = GuiConstants.STR_DISPLAY_MEMBER,
            DataType = typeof(String)
         };

         // Add to the columns to the ColumList collection of the MGDataTable
         ColumnList = new List<MGDataColumn>();
         ColumnList.Add(valMemCol);
         ColumnList.Add(dispMemCol);

         // Add the columns from the column list to the DataTable
         AddColumns();
      }

      /// <summary>
      /// This function converts the itmVals values to the datatype of ValueMember column
      /// and add the row to DataTable.
      /// </summary>
      /// <param name="ctrl"></param>
      /// <param name="itmVals"></param>
      /// <param name="dispVals"></param>
      internal void AddRows(StorageAttribute dataType, string[] itmVals, string[] dispVals)
      {
         Debug.Assert(itmVals.Length == dispVals.Length);

         Object obj = null;
         Type dnDataType = (DataTblObj.Columns[GuiConstants.STR_VALUE_MEMBER]).DataType;

         for (int i = 0; i < dispVals.Length; i++)
         {
            // convert the value to type of ValueMember column of the DataTable
            obj = DNConvert.convertMagicToDotNet(itmVals[i], dataType, dnDataType);
            DataTblObj.Rows.Add(obj, dispVals[i]);
         }
      }
   }
#endif
}
