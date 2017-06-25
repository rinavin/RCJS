using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.commands.ClientToServer;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.unipaas.management.tasks;
using com.magicsoftware.richclient.data;
using com.magicsoftware.gatewaytypes.data;
using com.magicsoftware.richclient.local.data.gateways.commands;
using com.magicsoftware.util;
using com.magicsoftware.richclient.local.data.gateways;
using com.magicsoftware.richclient.local.data.cursor;
using com.magicsoftware.gatewaytypes;
using System.Data;
using MgDataView = com.magicsoftware.richclient.data.DataView;
using com.magicsoftware.unipaas.dotnet;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.unipaas.management.exp;
using util.com.magicsoftware.util;
using com.magicsoftware.unipaas.management.data;

namespace com.magicsoftware.richclient.remote
{
   /// <summary>
   /// Execute DataViewToDataSource command
   /// </summary>
   class RemoteDataViewToDataSourceCommand : RemoteDataViewCommandBase
   {
      private DataViewOutputCommand dataViewOutputCommand;
      DataSourceDefinition destinationDataSourceDefinition;
      TaskBase currTask;
      TaskBase ancestorTask;
      

      /// <summary>
      /// contructor
      /// </summary>
      /// <param name="dataViewOutputCommand"></param>
      public RemoteDataViewToDataSourceCommand(DataViewOutputCommand dataViewOutputCommand)
         : base(dataViewOutputCommand)
      {
         this.dataViewOutputCommand = dataViewOutputCommand;
         currTask = (TaskBase)MGDataCollection.Instance.GetTaskByID(dataViewOutputCommand.TaskTag);
         ancestorTask = GuiExpressionEvaluator.GetContextTask((TaskBase)currTask, dataViewOutputCommand.Generation);
         destinationDataSourceDefinition = ClientManager.Instance.LocalManager.ApplicationDefinitions.DataSourceDefinitionManager.GetDataSourceDefinition(ancestorTask.getCtlIdx(), dataViewOutputCommand.DestinationDataSourceNumber);
      }

      /// <summary>
      /// Execute command
      /// </summary>
      /// <returns></returns>
      internal override ReturnResultBase Execute()
      {
         ReturnResult result= new ReturnResult();
         string error = string.Empty;
         
         if (destinationDataSourceDefinition == null)
         {
            // We maintain table for DatasourceIndex and its corresponding Isn for all the tables present in the data repository.
            // if entry for given datasource number is present in that table, but  it is present in the list of Local data sources then it indicate that given 
            // datasource is of server type database. So give error. 
            DataSourceId dataSourceId = ClientManager.Instance.GetDataSourceId(ancestorTask.getCtlIdx(), dataViewOutputCommand.DestinationDataSourceNumber);
            if (dataSourceId == null)
            {
               error = "DataViewToDataSource - Invalid data source.";
               Logger.Instance.WriteErrorToLog(error);
               ClientManager.Instance.ErrorToBeWrittenInServerLog = error;
            }
            else
            {
               error = "DataViewToDataSource- The main data source and destination data source cannot both be on the server database";
               Logger.Instance.WriteErrorToLog(error);
               ClientManager.Instance.ErrorToBeWrittenInServerLog = error;
            }

            result = new ReturnResult(MsgInterface.STR_DATAVIEW_TO_DATASOURCE_OPERATION_FAILED);
         }
         else
         {
            if (!AreSourceColumnsValid() || !AreDestinationColumnsValid())
            {
               result = new ReturnResult(MsgInterface.STR_DATAVIEW_TO_DATASOURCE_OPERATION_FAILED);
            }
            else
            {
               List<FieldDef> sourceVarList = PrepareSourceFieldList();
               List<DBField> destinationDataSourceFieldList = PrepareDestinationFieldList(sourceVarList.Count);
               

               if (!AreTypesCompatible(sourceVarList, destinationDataSourceFieldList))
               {
                  error = "DataViewToDataSource - Source and destination types are incompatible.";
                  Logger.Instance.WriteErrorToLog(error);
                  ClientManager.Instance.ErrorToBeWrittenInServerLog = error;
                  result = new ReturnResult(MsgInterface.STR_DATAVIEW_TO_DATASOURCE_OPERATION_FAILED);
               }
               else
               {
                  string listOfIndexesOfSelectedDestinationFields = string.Empty;
                  bool isFirstField = true;

                  foreach (DBField dbField in destinationDataSourceFieldList)
                  {
                     if (isFirstField)
                     {
                        isFirstField = false;
                     }
                     else
                     {
                        listOfIndexesOfSelectedDestinationFields += ',';
                     }
                     
                     listOfIndexesOfSelectedDestinationFields += dbField.IndexInRecord;
                  }

                  string dataViewContent = ClientManager.Instance.EventsManager.GetDataViewContent(currTask, dataViewOutputCommand.Generation, dataViewOutputCommand.TaskVarNames, DataViewOutputType.ClientFile, dataViewOutputCommand.DestinationDataSourceNumber, listOfIndexesOfSelectedDestinationFields);

                  if (!string.IsNullOrEmpty(dataViewContent))
                  {
                     //Create Command to update dataview to datasource.
                     IClientCommand command = CommandFactory.CreateUpdateDataViewToDataSourceCommand(ancestorTask.getTaskTag(),
                                                dataViewOutputCommand.TaskVarNames, dataViewOutputCommand.DestinationDataSourceNumber, dataViewOutputCommand.DestinationDataSourceName, dataViewOutputCommand.DestinationColumns, dataViewContent, sourceVarList, destinationDataSourceFieldList);

                     //execute UpdateDataViewToDataSource command.
                     result = ((Task)currTask).DataviewManager.LocalDataviewManager.Execute(command);
                  }

               }
            }
         }

         //Get the dataview content from the server.
         
         return result;
      }
      /// <summary>
      /// Check if selected columns of source and destination are compatible or not.
      /// </summary>
      /// <returns></returns>
      private bool AreTypesCompatible(List<FieldDef> sourceVarList, List<DBField> destinationColumnList)
      {
         bool isTypeCompatible = false;

         for (int fieldIndex = 0; fieldIndex < destinationColumnList.Count; fieldIndex++)
         {
            isTypeCompatible = StorageAttributeCheck.IsTypeCompatibile(sourceVarList[fieldIndex].getType(), (StorageAttribute)destinationColumnList[fieldIndex].Attr);
            if (!isTypeCompatible)
            {
               break;
            }
         }

         return isTypeCompatible;
      }

      /// <summary>
      /// Check for if all the column names specified in the column list is valid Or not.
      /// </summary>
      /// <returns></returns>
      private bool AreDestinationColumnsValid()
      {
         bool isValidColumn = false;
         string[] fieldNamesList = dataViewOutputCommand.DestinationColumns.Split(',');
         foreach (string fieldName in fieldNamesList)
         {
            isValidColumn = false;
            foreach (DBField dbField in destinationDataSourceDefinition.Fields)
            {
               if (fieldName == dbField.Name)
               {
                  isValidColumn = true;
                  break;
               }
            }

            if (!isValidColumn)
            {
               string error = string.Format("DataViewToDataSource - Illegal destination columns specified : {0}", fieldName);
               Logger.Instance.WriteErrorToLog(error);
               ClientManager.Instance.ErrorToBeWrittenInServerLog = error;
               break;
            }
         }

         return isValidColumn;
      }

      /// <summary>
      /// Check for if all the column names specified in the column list is valid Or not.
      /// </summary>
      /// <returns></returns>
      private bool AreSourceColumnsValid()
      {
         bool isValidColumn = false;
         string[] fieldNamesList = dataViewOutputCommand.TaskVarNames.Split(',');
         com.magicsoftware.unipaas.management.data.FieldsTable sourceFieldsTable = currTask.DataView.GetFieldsTab();
         foreach (string fieldName in fieldNamesList)
         {
            isValidColumn = false;
            for (int fldIndex = 0; fldIndex < sourceFieldsTable.getSize(); fldIndex++)
            {
               if (fieldName == sourceFieldsTable.getField(fldIndex).getVarName())
               {
                  isValidColumn = true;
                  break;
               }
            }

            if (!isValidColumn)
            {
               string error = string.Format("DataViewToDataSource - Illegal source columns specified : {0}", fieldName);
               Logger.Instance.WriteErrorToLog(error);
               ClientManager.Instance.ErrorToBeWrittenInServerLog = error;
               break;
            }
         }

         return isValidColumn;
      }

      /// <summary>
      /// Prepare the list of selected columns of destination datasource.
      /// </summary>
      /// <returns></returns>
      private List<DBField> PrepareDestinationFieldList(int srcVarListCount)
      {
         string[] fieldNamesList = dataViewOutputCommand.DestinationColumns.Split(',');
         int destinationColumnCount = srcVarListCount >= fieldNamesList.GetLength(0) ? fieldNamesList.GetLength(0) : srcVarListCount;
         List<DBField> destinationFieldList = new List<DBField>();

         for (int fieldNameIndex = 0; fieldNameIndex < destinationColumnCount; fieldNameIndex++)
         {
            string fieldName = fieldNamesList[fieldNameIndex];

            foreach (DBField dbField in destinationDataSourceDefinition.Fields)
            {
               if (dbField.Name == fieldName)
               {
                  destinationFieldList.Add(dbField);
                  break;
               }
            }
         }

         return destinationFieldList;
      }

      /// <summary>
      /// Prepare the list of selected columns of source dataview.
      /// </summary>
      /// <returns></returns>
      private List<FieldDef> PrepareSourceFieldList()
      {
         string[] fieldNamesList = dataViewOutputCommand.TaskVarNames.Split(',');
         List<FieldDef> sourceFieldList = new List<FieldDef>();

         foreach (string fieldName in fieldNamesList)
         {
            for (int fieldIndex = 0; fieldIndex < ancestorTask.DataView.GetFieldsTab().getSize(); fieldIndex++)
            {
               FieldDef field = ancestorTask.DataView.GetFieldsTab().getField(fieldIndex);

               if (field.getVarName() == fieldName)
               {
                  sourceFieldList.Add(field);
                  break;
               }
            }
         }
         return sourceFieldList;
      }
   }
}
