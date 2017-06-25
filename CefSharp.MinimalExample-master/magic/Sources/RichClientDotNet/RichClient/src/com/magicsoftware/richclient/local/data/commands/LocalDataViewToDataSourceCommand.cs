using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.commands.ClientToServer;
using com.magicsoftware.richclient.util;
using com.magicsoftware.gatewaytypes.data;
using com.magicsoftware.richclient.data;
using com.magicsoftware.richclient.events;
using util.com.magicsoftware.util;
using com.magicsoftware.util;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.richclient.remote;

namespace com.magicsoftware.richclient.local.data.commands
{
   /// <summary>
   /// local dataview command to provide the functionality of DataView output to DataSource.
   /// </summary>
   class LocalDataViewToDataSourceCommand : LocalDataViewOutputCommandBase
   {
      private DataViewOutputCommand command;
      private List<bool> selectedFldIdxList;
      private StringBuilder dataViewContent;
      private bool recordFound;

      /// <summary>
      /// ctor
      /// </summary>
      /// <param name="command"></param>
      public LocalDataViewToDataSourceCommand(DataViewOutputCommand dataViewOutputCommand)
         : base(dataViewOutputCommand)
      {
         dataViewContent = null;
         selectedFldIdxList = null;
         this.command = dataViewOutputCommand;
         recordFound = false;
      }

      /// <summary>
      /// 
      /// </summary>
      internal override ReturnResultBase Execute()
      {
         ReturnResultBase result = new ReturnResult();
         int dataSourceNumber = command.DestinationDataSourceNumber;
         DataSourceDefinition dataSourceDefintion = null;
         DataSourceId dataSourceId = ClientManager.Instance.GetDataSourceId(Task.getCtlIdx(), dataSourceNumber);
         string error = string.Empty;
         
         if (dataSourceId != null)
            dataSourceDefintion = ClientManager.Instance.LocalManager.ApplicationDefinitions.DataSourceDefinitionManager.GetDataSourceDefinition(Task.getCtlIdx(),dataSourceNumber);
         else 
         {
            error = "DataViewToDataSource - Invalid data source.";
            Logger.Instance.WriteExceptionToLog(error);
            ClientManager.Instance.ErrorToBeWrittenInServerLog = error;
            return new ReturnResult(MsgInterface.STR_DATAVIEW_TO_DATASOURCE_OPERATION_FAILED);
         }
         
         if (dataSourceDefintion != null)
         {
            error = "DataViewToDataSource - The main data source and destination data source cannot be both be on the local database.";
            Logger.Instance.WriteExceptionToLog(error);
            ClientManager.Instance.ErrorToBeWrittenInServerLog = error;
            return new ReturnResult(MsgInterface.STR_DATAVIEW_TO_DATASOURCE_OPERATION_FAILED);
         }
         else
         {
            result = base.Execute();

            if (result.Success)
            {
               //create and execute server command to execute event  MG_ACT_UPDATE_DATAVIEW_TO_DATASOURCE
               bool success = UpdateDataViewToRemoteDataSource();
               if (!success)
               {
                  error = "DataViewToDataSource - Failed to update destination data source.";
                  Logger.Instance.WriteExceptionToLog(error);
                  ClientManager.Instance.ErrorToBeWrittenInServerLog = error;
                  return new ReturnResult(MsgInterface.STR_DATAVIEW_TO_DATASOURCE_OPERATION_FAILED);

               }
            }


         }
         return result;
      }

      /// <summary>
      /// 
      /// </summary>
      internal override void Init()
      {
         var fieldsTable = Task.DataView.GetFieldsTab();

         dataViewContent = new StringBuilder();
         selectedFldIdxList = new List<bool>();

         for (var i = 0; i < fieldsTable.getSize(); i++)
         {
            selectedFldIdxList.Add(SelectedFields.Contains((Field)fieldsTable.getField(i)));
         }

         dataViewContent.Append("<" + ConstInterface.MG_TAG_DATAVIEW + XMLConstants.TAG_CLOSE);
      }

      /// <summary>
      /// 
      /// </summary>
      internal override void Terminate ()
      {
         if (recordFound)
         {
            dataViewContent.Append("</" + ConstInterface.MG_TAG_DATAVIEW + XMLConstants.TAG_CLOSE);
         }
         else
         {
            dataViewContent = null;
         }
      }

      /// <summary>
      /// Serialize record 
      /// </summary>
      internal override void Serialize(Record record)
      {
         var fieldsTable = Task.DataView.GetFieldsTab();

         // set the shrink flags for non selected fields in record i.e. set FLAG_VALUE_NOT_PASSED
         for (var i = 0; i < fieldsTable.getSize(); i++)
         {
            if (!selectedFldIdxList[i])
               record.setShrinkFlag(i);
         }

         // Serialize the record.
         record.buildXMLForDataViewToDataSource(dataViewContent);
         recordFound = true;
      }

      /// <summary>
      ///   send a command to server to update dataview to datasource.
      /// </summary>
      internal bool UpdateDataViewToRemoteDataSource()
      {
         if (dataViewContent != null)
         {
            IClientCommand cmd = CommandFactory.CreateUpdateDataViewToDataSourceCommand(Task.getTaskTag(), command.TaskVarNames,
                                                           command.DestinationDataSourceNumber, command.DestinationDataSourceName, command.DestinationColumns, dataViewContent.ToString(), null, null);


            Task.getMGData().CmdsToServer.Add(cmd);

            ResultValue res = new ResultValue();

            // Do not serialize flow monitor messages along with MG_ACT_UPDATE_DATAVIEW_TO_DATASOURCE command 
            bool origVal = FlowMonitorQueue.Instance.ShouldSerialize;
            FlowMonitorQueue.Instance.ShouldSerialize = false;

            //Execute MG_ACT_UPDATE_DATAVIEW_TO_DATASOURCE command.
            RemoteCommandsProcessor.GetInstance().Execute(CommandsProcessorBase.SendingInstruction.ONLY_COMMANDS, CommandsProcessorBase.SessionStage.NORMAL, res);

            FlowMonitorQueue.Instance.ShouldSerialize = origVal;
            return (res.Value.Equals("1") ? true : false);
         }

         return true;
      }

   }
}
