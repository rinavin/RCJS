using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.unipaas.management.tasks;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.dotnet;
using com.magicsoftware.unipaas.gui.low;
using GuiCommandType = com.magicsoftware.unipaas.gui.CommandType;

namespace com.magicsoftware.unipaas.management.gui
{
   /// <summary>
   /// This class handles the functionality of DataTable attached as DataSource of DataView Control.
   /// It contains DVDataTable and DVControl. So, all the operations like creating/deleting row,
   /// updating column, rejecting changes of DataTable will be handled thr' commands. These commands will be 
   /// sent by DVControlManager.
   /// </summary>
   public class DVControlManager
   {
      public DVDataTable DVDataTableObj { set; get; }
      public MgControlBase DVControl { set; get; }

      private bool initialDataLoading { get; set; }
      private bool isDataBound { get; set; }
      /// <summary>
      /// Constructor.
      /// </summary>
      /// <param name="dvControl"></param>
      /// <param name="dvDataTable"></param>
      internal DVControlManager(MgControlBase dvControl, DVDataTable dvDataTable)
      {
         isDataBound = false;
         DVDataTableObj = dvDataTable;
         DVControl = dvControl;
         DVDataTableCollection.Add(dvDataTable.DataTblObj, this);
      }

      /// <summary>Refresh row of DVControl.
      /// </summary>
      internal void RefreshDisplay()
      {
         if (DVDataTableObj.ColumnList != null && !DVControl.getForm().getTask().DataView.isEmptyDataview())
         {
            object[] row = new object[DVDataTableObj.DataTblObj.Columns.Count];

            for (int i = 0; i <= DVDataTableObj.ColumnList.Count; i++)
               ComputeAndRefreshColumn(i, false, ref row[i]);

            if (initialDataLoading)
               DVDataTableObj.LoadNewRow(row);
            else
               Commands.addAsync(GuiCommandType.UPDATE_DVCONTROL_ROW, DVDataTableObj, DVDataTableObj.CurrRow, row);
         }
      }

      /// <summary>
      /// ComputeAndRefresh DataColumn
      /// </summary>
      /// <param name="colID"></param>
      /// <param name="removeHandler"></param>
      /// <param name="colVal"></param>
      private void ComputeAndRefreshColumn(int colID, bool removeHandler, ref object colVal)
      {
         Field field = null;
         if (colID > 0)
         {
            int fldIdx = DVDataTableObj.GetFldIdx(colID);
            field = (Field)DVControl.getForm().getTask().DataView.getField(fldIdx);
         }

         //First column is 'Isn'. Give a call to core to fetch PosCache's current Isn. And store it
         if (colID == 0)
            colVal = (int)((TaskBase)DVControl.getForm().getTask()).GetDVControlPosition();
         else if (field != null)
         {
            String value = String.Empty;
            Object dnValue = null;
            bool isNull = false;

            //get field's value by 
            ((TaskBase)field.getTask()).getFieldDisplayValue(field, ref value, ref isNull);

            dnValue = DNConvert.convertMagicToDotNet(value, field.getType(), typeof(Object));
            colVal = dnValue;
         }
      }

      /// <summary>
      /// This will be called, while fetching records into DataTable at the start to create row.
      /// </summary>
      /// <param name="idx"></param>
      internal void checkAndCreateRow(int idx)
      {
         if (!DVControl.getForm().getTask().DataView.isEmptyDataview())
         {
            if (!initialDataLoading)
            {
               // When new row is created in dataTable,"Isn" column which is primary key of dataTable
               // need to be initialized. this invokes of DataTable's ColumnChanged Handler. So, in order to avoid
               // this, DVControl Handler should be unregisterd before creating new row.
               Commands.addAsync(GuiCommandType.REMOVE_DVCONTROL_HANDLER, DVControl, DVDataTableObj.DataTblObj);
               DVDataTableObj.checkAndCreateRow(idx);
               Commands.addAsync(GuiCommandType.ADD_DVCONTROL_HANDLER, DVControl, DVDataTableObj.DataTblObj);
            }
            else
               DVDataTableObj.CurrRow = idx;
           }
         }

      /// <summary>Insert row in dataTable.
      /// </summary>
      /// <returns>returns CurrRow</returns>
      public int InsertRow()
      {
         Commands.addAsync(GuiCommandType.REMOVE_DVCONTROL_HANDLER, DVControl, DVDataTableObj.DataTblObj);

         DVDataTableObj.CurrRow++;
         
         Commands.addAsync(GuiCommandType.CREATE_ROW_IN_DVCONTROL, DVDataTableObj, DVDataTableObj.CurrRow, false);
         Commands.addAsync(GuiCommandType.SET_DVCONTROL_ROW_POSITION, DVControl, DVDataTableObj.CurrRow, DVDataTableObj.DataTblObj);
         Commands.addAsync(GuiCommandType.ADD_DVCONTROL_HANDLER, DVControl, DVDataTableObj.DataTblObj);

         return DVDataTableObj.CurrRow;
      }

      /// <summary>
      /// Delete Current row in dataTable.
      /// </summary>
      /// <param name="rowIdx"></param>
      /// <returns>returns posIsn of next row to be focused.</returns>
      public int DeleteRow(int rowIdx)
      {
         int rows = DVDataTableObj.DataTblObj.Rows.Count;
         int posIsn = -1;

         if (rows > 1)
         {
            if (rowIdx == rows - 1)
               posIsn = Commands.GetDVControlPositionIsn(DVDataTableObj.DataTblObj, rowIdx - 1);
            else
               posIsn = Commands.GetDVControlPositionIsn(DVDataTableObj.DataTblObj, rowIdx + 1);

            Commands.addAsync(GuiCommandType.REMOVE_DVCONTROL_HANDLER, DVControl, DVDataTableObj.DataTblObj);

            Commands.addAsync(GuiCommandType.DELETE_DVCONTROL_ROW, DVDataTableObj, rowIdx, null, false);

            Commands.addAsync(GuiCommandType.ADD_DVCONTROL_HANDLER, DVControl, DVDataTableObj.DataTblObj);

            if (rowIdx == rows - 1)
               DVDataTableObj.CurrRow--;

            Commands.beginInvoke();
         }
         return posIsn;
      }

      /// <summary>Reject column changes in DataTable.
      /// </summary>
      /// <param name="pos"></param>
      /// <param name="fldIdx"></param>
      public void RejectColumnChanges(int pos, int fldIdx)
      {
         DVDataTableObj.CurrRow = pos;
         int colID = DVDataTableObj.GetColumnId(fldIdx - 1);
         Commands.addAsync(GuiCommandType.REJECT_DVCONTROL_COLUMN_CHANGES, DVControl, DVDataTableObj.CurrRow, colID, DVDataTableObj.DataTblObj);
         Commands.beginInvoke();
      }

      /// <summary>InitiateLoadingDataTable()
      /// </summary>
      public void InitiateLoadingDataTable()
      {
         initialDataLoading = true;

         // isDataBound will be true, if we are here due to vew_refresh(). remove the data binding of dvcontrol
         // before loading records.
         if (isDataBound) 
         {
            // Set data table to datasource.
            string propName = DVControl.getProp(PropInterface.PROP_TYPE_DN_CONTROL_DATA_SOURCE_PROPERTY).getValue();
            Commands.SetDataSourceToDataViewControl(DVControl, null, propName);
         }
      }

      /// <summary>TerminateLoadingDataTable()
      /// </summary>
      public void TerminateLoadingDataTable()
      {
         initialDataLoading = false;

         DVDataTableObj.DataTblObj.AcceptChanges(); // commit all rows.

         // bind data table to datasource.
         string propName = DVControl.getProp(PropInterface.PROP_TYPE_DN_CONTROL_DATA_SOURCE_PROPERTY).getValue();
         Commands.SetDataSourceToDataViewControl(DVControl, DVDataTableObj.DataTblObj, propName);
         isDataBound = true;

         // add handlers
         Commands.addAsync(GuiCommandType.ADD_DVCONTROL_HANDLER, DVControl, DVDataTableObj.DataTblObj);
      }

      /// <summary>Clear contents of DataTable.
      /// </summary>
      public void ClearDataTable()
      {
         if (DVDataTableObj.DataTblObj.Rows.Count > 0)
            Commands.ClearDatatable(DVControl, DVDataTableObj.DataTblObj);
      }

      /// <summary>compute DataTable's column value
      /// </summary>
      /// <param name="fldIdx">Identifies DataColumn.</param>
      public void ComputeColumnValue(int fldIdx)
      {
         int colID = -1;
         
         //if fldIdx is 0, means it's first column i.e. "Isn"
         if (fldIdx == 0)
            colID = 0;
         else
            colID = DVDataTableObj.GetColumnId(fldIdx - 1);

         object colVal = null;
         ComputeAndRefreshColumn(colID, true, ref colVal);
         Commands.addAsync(GuiCommandType.UPDATE_DVCONTROL_COLUMN, DVDataTableObj, DVDataTableObj.CurrRow, colID, colVal);
         Commands.beginInvoke();
      }
   }
}
