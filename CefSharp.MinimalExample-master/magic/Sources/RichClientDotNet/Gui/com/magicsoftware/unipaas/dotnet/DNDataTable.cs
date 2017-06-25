using System;
using System.Collections;
using System.Collections.Generic;

using System.Data;
using System.Text;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.unipaas.management.tasks;
using util.com.magicsoftware.util;
using com.magicsoftware.util.Xml;

namespace com.magicsoftware.unipaas.dotnet
{
#if !PocketPC
   /// <summary>
   /// This class is implemented to handle functionality of DataTable for MgXP function DataViewToDNDataTable ().
   /// </summary>
   internal class DNDataTable : MGDataTable
   {

      /// <summary>
      ///   Add rows to DataTable object.
      /// </summary>
      /// <param name="dataViewContent">string holding the dataViewContent</param>
      internal void AddRows(String dataViewContent)
      {
         var parser = new XmlParser(dataViewContent);
         string currTagName = parser.getNextTag();
         while (currTagName != null)
         {
            switch (currTagName)
            {
               case XMLConstants.MG_TAG_RECORD: //<Record>
                  parser.setCurrIndex2EndOfTag();
                  AddRow(parser);
                  break;

                  //case XMLConstants.MG_TAG_PRINT_DATA: //<Print_data> tag
                  //case XMLConstants.MG_TAG_PRINT_DATA_END://</Print_data>
                  //case XMLConstants.MG_TAG_RECORD_END://</Record>
               default:
                  parser.setCurrIndex2EndOfTag();
                  break;
            }
            currTagName = parser.getNextTag();
         }
      }

      /// <summary>
      ///   Add row to DataTable object.
      /// </summary>
      /// <param name="parser">current XML parser.</param>
      private void AddRow(XmlParser parser)
      {
         DataRow row = DataTblObj.NewRow();

         foreach (DataColumn column in DataTblObj.Columns)
         {
            //read column tag
            parser.setCurrIndex2EndOfTag();

            //read index till end if column tag
            int endContext = parser.getXMLdata().IndexOf(XMLConstants.END_TAG, parser.getCurrIndex());
            if (endContext != -1)
            {
               // read column value
               String valueStr = parser.getXMLsubstring(endContext);
               if (column.DataType == typeof(Byte[]))
               {
                  //convert the base64 string to byte[]
                  Byte[] bstr = Base64.decodeToByte(valueStr);
                  row[column.ColumnName] = bstr;
               }
               else
               {
                  if (valueStr.Equals("_DBNull_"))
                     row[column.ColumnName] = Convert.DBNull;
                  else
                     row[column.ColumnName] = XmlParser.unescape(valueStr);
               }

               //TODO : need to check and handle : how to insert binary data in a column
               parser.setCurrIndex2EndOfTag();
            }
         }
         DataTblObj.Rows.Add(row);
      }

      /// <summary>
      /// Prepares a columns list.
      /// </summary>
      /// <param name="taskID"></param>
      /// <param name="taskVariableNamesString">task's variable (fields) names, comma delimted.</param>
      /// <param name="displayNamesString">display names (to be used as column names), comma delimted.</param>
      internal List<MGDataColumn> PrepareColumnsList(string taskID, String taskVariableNamesString, String displayNamesString)
      {
         ColumnList = new List<MGDataColumn>();
         bool useTaskVariableNamesAsDisplayNames = false;
         StringBuilder invalidVariableNames = new StringBuilder();

         List<string> taskVariableNames = CreateList(taskVariableNamesString); // task variables names.
         List<string> displayNames = null;

         if (displayNamesString.Length == 0 || displayNamesString.Equals("@"))
            useTaskVariableNamesAsDisplayNames = true;
         else //prepare display names
         {
            displayNames = CreateList(displayNamesString);
            if (displayNames.Count < taskVariableNames.Count)
            {
               int cnt = taskVariableNames.Count - displayNames.Count;
               // if display variable list is less than task vars list , add blank names for the remaining columns.
               // If we add empty string here, datagrid adds it's default names (like Column1, Column2 ...), to avoid this add single blank char.
               for (var j = 0; j < cnt; j++)
                  displayNames.Add(" ");
            }
         }

         var task = (TaskBase)Manager.MGDataTable.GetTaskByID(taskID);

         // Prepare column list of task variables of required generation & all it's parents
         // as we support taskVariableNames from required task & it's parents as well.
         var allTasksColumnsList = new List<MGDataColumn>();
         while (task != null)
         {
            var fieldsTable = task.DataView.GetFieldsTab();
            for (var j = 0; j < fieldsTable.getSize(); j++)
            {
               var field = (Field)fieldsTable.getField(j);
               var newColumn = new MGDataColumn
               {
                  Name = field.getVarName(),
                  DataType = field.GetDefaultDotNetTypeForMagicType(),
                  FldIdx = 0
               };
               allTasksColumnsList.Add(newColumn);
            }
            task = (TaskBase)task.GetTaskAncestor(1);
         }

         // prepare 'columnsList' with one column for each task's variable contained within 'taskVariableNamesString'.
         int i = 0;
         foreach (var taskVariableName in taskVariableNames)
         {
            bool variableFound = false;
            for (var j = 0; j < allTasksColumnsList.Count; j++)
            {
               if (taskVariableName == allTasksColumnsList[j].Name)
               {
                  var newColumn = new MGDataColumn
                  {
                     Name = (useTaskVariableNamesAsDisplayNames
                                ? allTasksColumnsList[j].Name
                                : displayNames[i]),
                     DataType = allTasksColumnsList[j].DataType
                  };
                  ColumnList.Add(newColumn);
                  variableFound = true;
                  break;
               }
            }
            if (!variableFound)
            {
               if (invalidVariableNames.Length > 0)
                  invalidVariableNames.Append(',');
               invalidVariableNames.Append(taskVariableName);
            }

            i++;
         }

         //error handling
         if (ColumnList.Count == 0)
            Events.WriteErrorToLog(taskVariableNames.Count == 0
                                      ? "DataViewToDNDataTable - No variables are specified."
                                      : String.Format("DataViewToDNDataTable - Illegal variables specified : {0}", taskVariableNamesString));
         else if (ColumnList.Count < taskVariableNames.Count)
            Events.WriteErrorToLog(String.Format("DataViewToDNDataTable - Illegal variables specified : {0}", invalidVariableNames.ToString()));

         return ColumnList;
      }

      /// <summary>
      /// Parse a comma delimited string into a list.
      /// </summary>
      /// <param name="varStr">comma delimited string.</param>
      private List<String> CreateList(String varStr)
      {
         int i = 0;
         var retList = new List<String>();

         IEnumerator tokens = StrUtil.tokenize(varStr, ",").GetEnumerator();
         while (tokens.MoveNext())
         {
            var strToken = (String)tokens.Current;

            // if previous string ends with '\\', it means ',' is part of string & not a separator.
            if (retList.Count > 0 && retList[i - 1].EndsWith("\\"))
            {
               StringBuilder str = new StringBuilder();
               str.Append(retList[i - 1]);
               str.Remove(str.Length - 1, 1); //remove last char '\\'
               str.Append(","); //',' is part of string, so append it.
               str.Append(strToken);
               retList[i - 1] = str.ToString();
            }
            else
            {
               retList.Add(strToken);
               i++;
            }
         }
         return retList;
      }

   }
#endif
}