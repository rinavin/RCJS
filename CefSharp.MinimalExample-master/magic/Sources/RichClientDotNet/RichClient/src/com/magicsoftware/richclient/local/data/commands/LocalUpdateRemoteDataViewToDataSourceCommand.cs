using System;
using com.magicsoftware.richclient.commands.ClientToServer;
using com.magicsoftware.richclient.util;
using com.magicsoftware.gatewaytypes.data;
using com.magicsoftware.unipaas.management.tasks;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.util;
using System.Collections.Generic;
using com.magicsoftware.richclient.local.data.gateways.commands;
using com.magicsoftware.richclient.local.data.gateways;
using com.magicsoftware.richclient.local.data.cursor;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.richclient.data;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.richclient.local.application.datasources.converter;
using util.com.magicsoftware.util;
using FieldsTable = com.magicsoftware.unipaas.management.data.FieldsTable;  

namespace com.magicsoftware.richclient.local.data.commands
{
   /// <summary>
   /// Class responsible for updating remote dataview to local datasource.
   /// </summary>
   class LocalUpdateRemoteDataViewToDataSourceCommand : LocalDataViewCommandBase
   {
      private UpdateDataViewToDataSourceEventCommand updateDataViewToDataSourceCommand;

      private List<DBField> destinationColumnList;
      private List<FieldDef> sourceVarList;
      private Dictionary<string, int> destinationToSourceFieldIndexMapping;
      private DataSourceDefinition destinationDataSourceDefinition;
      private TaskBase task;
      private DBKey uniqueKey;

      /// <summary>
      /// Contructor
      /// </summary>
      /// <param name="updateDataViewToDataSourceCommand"></param>
      public LocalUpdateRemoteDataViewToDataSourceCommand(UpdateDataViewToDataSourceEventCommand updateDataViewToDataSourceCommand)
         : base(updateDataViewToDataSourceCommand)
      {
         this.updateDataViewToDataSourceCommand = updateDataViewToDataSourceCommand;
         this.destinationColumnList = updateDataViewToDataSourceCommand.DestinationDataSourceFieldsList;
         this.sourceVarList = updateDataViewToDataSourceCommand.SourceVarList;
         destinationToSourceFieldIndexMapping = new Dictionary<string, int>();
         task = (TaskBase)MGDataCollection.Instance.GetTaskByID(updateDataViewToDataSourceCommand.TaskTag);
         destinationDataSourceDefinition = ClientManager.Instance.LocalManager.ApplicationDefinitions.DataSourceDefinitionManager.GetDataSourceDefinition(task.getCtlIdx(), updateDataViewToDataSourceCommand.DestDataSource);
      }

      /// <summary>
      /// Execute command
      /// </summary>
      /// <returns></returns>
      internal override ReturnResultBase Execute()
      {
         ReturnResultBase result = new ReturnResult();
         PrepareDestinationToSourceFieldIndexMapping(sourceVarList);
         result = UpdateDataViewToDataSource();
         if (!result.Success)
         {
            string error = "DataViewToDataSource - Failed to update destination data source.";
            Logger.Instance.WriteExceptionToLog(error);

            if (!string.IsNullOrEmpty(ClientManager.Instance.ErrorToBeWrittenInServerLog))
            {
               ClientManager.Instance.ErrorToBeWrittenInServerLog += "\r\n";
            }

            ClientManager.Instance.ErrorToBeWrittenInServerLog += error;
         }

         return result;
      }

      /// <summary>
      /// Update dataview to DataSource.
      /// </summary>
      private ReturnResultBase UpdateDataViewToDataSource()
      {
         bool transactionOpened = false;
         string error = string.Empty;

         string dataSourceName = ClientManager.Instance.getEnvParamsTable().translate(updateDataViewToDataSourceCommand.DestDataSourceName);
         
         if (string.IsNullOrEmpty(dataSourceName))
         {
            dataSourceName = destinationDataSourceDefinition.Name;
         }

         GatewayResult result = GatewayCommandsFactory.CreateFileExistCommand(dataSourceName, destinationDataSourceDefinition, ClientManager.Instance.LocalManager).Execute();

         bool insertMode = !result.Success;

         if (!insertMode)
         {
            uniqueKey = GetUniqueKey();

            if (uniqueKey == null)
            {
               error = "DataViewToDataSource - When using the DataViewtoDataSource function, a unique index must be defined in the destination data source .";
               Logger.Instance.WriteExceptionToLog(error);
               ClientManager.Instance.ErrorToBeWrittenInServerLog = error;
               return new ReturnResult(MsgInterface.STR_DATAVIEW_TO_DATASOURCE_OPERATION_FAILED);
            }
            else if (!CheckDestinationColumnListContainUniqueKeyColumns())
            {
               error = "DataViewToDataSource - When using the DataViewtoDataSource function, all the segments of the unique index must be selected.";
               Logger.Instance.WriteExceptionToLog(error);
               ClientManager.Instance.ErrorToBeWrittenInServerLog = error;
               return new ReturnResult(MsgInterface.STR_DATAVIEW_TO_DATASOURCE_OPERATION_FAILED);
            }
         }

         result = GatewayCommandsFactory.CreateFileOpenCommand(dataSourceName, destinationDataSourceDefinition, Access.Write, ClientManager.Instance.LocalManager).Execute();
         if (result.Success)
         {
            //Build the runtime cursor.
            MainCursorBuilder cursorBuilder = new MainCursorBuilder(null);
            RuntimeCursor destinationRuntimeCursor = cursorBuilder.Build(destinationDataSourceDefinition, Access.Write);

            destinationRuntimeCursor.CursorDefinition.StartPosition = new DbPos(true);
            destinationRuntimeCursor.CursorDefinition.CurrentPosition = new DbPos(true);

            // Prepare the cursor.
            result = GatewayCommandsFactory.CreateCursorPrepareCommand(destinationRuntimeCursor, ClientManager.Instance.LocalManager).Execute();

            if (result.Success)
            {
               //If tansaction is not open then open the transaction.
               if (TaskTransactionManager.LocalOpenedTransactionsCount == 0)
               {
                  result = GatewayCommandsFactory.CreateGatewayCommandOpenTransaction(ClientManager.Instance.LocalManager).Execute();
                  transactionOpened = true;
               }

               if (result.Success)
               {
                  SetDataToRuntimeParser();
                  RecordForDataViewToDataSource record = GetRecord();

                  while (record != null)
                  {
                     BuildCurrentValues(destinationRuntimeCursor, record);
                     result = GatewayCommandsFactory.CreateCursorInsertCommand(destinationRuntimeCursor, ClientManager.Instance.LocalManager, true).Execute();

                     if (!result.Success)
                     {
                        if (result.ErrorCode == GatewayErrorCode.DuplicateKey)
                        {
                           if (!insertMode)
                           {
                              //Build the ranges using unique key segments value.
                              BuildRanges(record, destinationRuntimeCursor);

                              //Open the cursor and apply the ranges.
                              result = GatewayCommandsFactory.CreateCursorOpenCommand(destinationRuntimeCursor, ClientManager.Instance.LocalManager, true).Execute();
                              if (result.Success)
                              {
                                 //Fetch the record
                                 result = GatewayCommandsFactory.CreateCursorFetchCommand(destinationRuntimeCursor, ClientManager.Instance.LocalManager).Execute();

                                 BuildCurrentValues(destinationRuntimeCursor, record);

                                 //If record found, that means record with same key value exists, so update the current record with destination record.
                                 if (result.Success)
                                 {
                                    result = GatewayCommandsFactory.CreateGatewayCommandCursorUpdateRecord(destinationRuntimeCursor, ClientManager.Instance.LocalManager, true).Execute();
                                 }
                                 else
                                 {
                                    if (!string.IsNullOrEmpty(ClientManager.Instance.ErrorToBeWrittenInServerLog))
                                    {
                                       ClientManager.Instance.ErrorToBeWrittenInServerLog += "\r\n";
                                    }

                                    ClientManager.Instance.ErrorToBeWrittenInServerLog += result.ErrorDescription;
                                 }

                                 //Close the cursor.
                                 GatewayCommandsFactory.CreateCursorCloseCommand(destinationRuntimeCursor, ClientManager.Instance.LocalManager).Execute();
                              }
                              else
                              {
                                 if (!string.IsNullOrEmpty(ClientManager.Instance.ErrorToBeWrittenInServerLog))
                                 {
                                    ClientManager.Instance.ErrorToBeWrittenInServerLog += "\r\n";
                                 }

                                 ClientManager.Instance.ErrorToBeWrittenInServerLog += result.ErrorDescription;
                              }
                           }
                           else
                           {
                              if (!string.IsNullOrEmpty(ClientManager.Instance.ErrorToBeWrittenInServerLog))
                              {
                                 ClientManager.Instance.ErrorToBeWrittenInServerLog += "\r\n";
                              }

                              ClientManager.Instance.ErrorToBeWrittenInServerLog += result.ErrorDescription;

                              record = GetRecord();

                              continue;
                           }
                        }
                        else
                        {
                           if (!string.IsNullOrEmpty(ClientManager.Instance.ErrorToBeWrittenInServerLog))
                           {
                              ClientManager.Instance.ErrorToBeWrittenInServerLog += "\r\n";
                           }

                           ClientManager.Instance.ErrorToBeWrittenInServerLog += result.ErrorDescription;

                           break;
                        }
                     }

                     record = GetRecord();
                  }

                  //If transaction is opened , then close the transaction. If any error occurs then abort the transation else do commit.
                  if (transactionOpened)
                  {
                     GatewayCommandCloseTransaction closeTransactionCommand = GatewayCommandsFactory.CreateGatewayCommandCloseTransaction(ClientManager.Instance.LocalManager);
                     if (result.Success)
                     {
                        closeTransactionCommand.TransactionModes = TransactionModes.Commit;
                     }
                     else
                     {
                        closeTransactionCommand.TransactionModes = TransactionModes.Abort;
                     }

                     closeTransactionCommand.Execute();
                  }

                  //Release the cursor and close the file.
                  GatewayCommandsFactory.CreateCursorReleaseCommand(destinationRuntimeCursor, ClientManager.Instance.LocalManager).Execute();

                  GatewayCommandsFactory.CreateFileCloseCommand(destinationDataSourceDefinition, ClientManager.Instance.LocalManager).Execute();
               }
               else
               {
                  ClientManager.Instance.ErrorToBeWrittenInServerLog = result.ErrorDescription;
               }
            }
            else
            {
               ClientManager.Instance.ErrorToBeWrittenInServerLog = result.ErrorDescription;
            }

         }
         else
         {
            ClientManager.Instance.ErrorToBeWrittenInServerLog = result.ErrorDescription;
         }

         return result;
      }

      /// <summary>
      /// Prepare mapping of Destination field to Source Field. 
      /// For that maintain a dictionary with key as destination field name and 
      /// value as index of the source field in the FieldsTable.
      /// </summary>
      /// <param name="record"></param>
      private void PrepareDestinationToSourceFieldIndexMapping(List<FieldDef>sourceVarList)
      {
         FieldsTable fieldsTab = ((DataView)task.DataView).GetFieldsTab();
         int sourceFieldIndex = 0;

         foreach (DBField dbfield in destinationColumnList)
         {
            FieldDef field = sourceVarList[sourceFieldIndex++];

            for (int fieldIndex = 0; fieldIndex < fieldsTab.getSize(); fieldIndex++)
            {
               if (field.getVarName() == fieldsTab.getField(fieldIndex).getVarName())
               {
                  destinationToSourceFieldIndexMapping.Add(dbfield.Name, fieldIndex);
                  break;
               }
            }
         }
      }


      /// <summary>
      /// Set DataViewContent to the Runtime Parser and set the current index after <DataViewToDataSource> Tag
      /// </summary>
      private void SetDataToRuntimeParser()
      {
         ClientManager.Instance.RuntimeCtx.Parser.setXMLdata(updateDataViewToDataSourceCommand.DataViewContent);
         int index = ClientManager.Instance.RuntimeCtx.Parser.getXMLdata().IndexOf(ConstInterface.MG_TAG_DATAVIEW) + ConstInterface.MG_TAG_DATAVIEW.Length + XMLConstants.TAG_CLOSE.Length;
         ClientManager.Instance.RuntimeCtx.Parser.setCurrIndex(index);
      }

      /// <summary>
      /// Get the record by parsing the dataviewcontent.
      /// </summary>
      /// <returns></returns>
      private RecordForDataViewToDataSource GetRecord()
      {
         RecordForDataViewToDataSource record = null;
         String foundTagName;
         bool isCurrRec;
         TaskBase task = (TaskBase)MGDataCollection.Instance.GetTaskByID(updateDataViewToDataSourceCommand.TaskTag);

         foundTagName = ClientManager.Instance.RuntimeCtx.Parser.getNextTag();

         if (foundTagName != null && foundTagName.Equals(ConstInterface.MG_TAG_REC))
         {
            record = new RecordForDataViewToDataSource((DataView)task.DataView, destinationColumnList);
            record.ValueInBase64 = true;
            isCurrRec = record.fillData();
         }

         return record;
      }

      /// <summary>
      /// Check if selected columns of destination datasource contains the Unique Key columns or not.
      /// </summary>
      /// <param name="datasourcedefinition"></param>
      /// <param name="columnList"></param>
      /// <returns></returns>
      private bool CheckDestinationColumnListContainUniqueKeyColumns()
      {
         bool segmentFound = false;

         if (uniqueKey != null)
         {
            foreach (DBSegment segment in uniqueKey.Segments)
            {
               segmentFound = false;

               foreach (DBField dbField in destinationColumnList)
               {
                  if (dbField.Name == segment.Field.Name)
                  {
                     segmentFound = true;
                     break;
                  }
               }

               if (!segmentFound)
                  break;
            }
         }

         return segmentFound;
      }

      /// <summary>
      /// Build the ranges using the unique key segments.
      /// </summary>
      /// <param name="record"></param>
      /// <param name="runtimeCursor"></param>
      /// <returns></returns>
      private void BuildRanges(RecordForDataViewToDataSource record, RuntimeCursor runtimeCursor)
      {
         int recordFieldIndex = 0;

         if (uniqueKey != null)
         {
            runtimeCursor.RuntimeCursorData.Ranges = new List<RangeData>();

            foreach (DBSegment segment in uniqueKey.Segments)
            {
               for (int fldIndex = 0; fldIndex < destinationColumnList.Count; fldIndex++)
               {
                  DBField dbField = destinationColumnList[fldIndex];

                  if (dbField.Equals(segment.Field))
                  {
                     RangeData rngData = new RangeData();
                     rngData.FieldIndex = dbField.IndexInRecord;

                     rngData.Max.Type = RangeType.RangeParam;
                     rngData.Max.Discard = false;

                     rngData.Min.Type = RangeType.RangeParam;
                     rngData.Min.Discard = false;

                     FieldValue fieldValue = new FieldValue();
                     destinationToSourceFieldIndexMapping.TryGetValue(dbField.Name, out recordFieldIndex);
                     fieldValue.Value = record.GetFieldValue(recordFieldIndex);

                     rngData.Max.Value = fieldValue;
                     rngData.Min.Value = fieldValue;

                     runtimeCursor.RuntimeCursorData.Ranges.Add(rngData);
                     break;
                  }

               }
            }
         }
      }

      /// <summary>
      /// Build Current values for Runtime cursor.
      /// </summary>
      /// <param name="runtimeCursor"></param>
      /// <param name="record"></param>
      private void BuildCurrentValues(RuntimeCursor runtimeCursor, RecordForDataViewToDataSource record)
      {
         for (int j = 0; j < destinationDataSourceDefinition.Fields.Count; j++)
         {
            DBField dbfield = destinationDataSourceDefinition.Fields[j];
            bool fieldExist = false;

            //Check if current Dbfield exist in the selected column list.
            foreach (DBField destinationDbField in destinationColumnList)
            {
               if(dbfield.Name == destinationDbField.Name)
               {
                  fieldExist = true;
                  break;
               }
            }

            //If field is selected then set the value of that field in the runtime cursor.
            if (fieldExist)
            {
               runtimeCursor.CursorDefinition.IsFieldUpdated[j] = true;

               //Convert values of fields of record.
               int recordFieldIndex;
               destinationToSourceFieldIndexMapping.TryGetValue(dbfield.Name, out recordFieldIndex);

               runtimeCursor.RuntimeCursorData.CurrentValues[j].IsNull = record.IsNull(recordFieldIndex);

               if (record.IsNull(recordFieldIndex))
               {
                  if (!dbfield.AllowNull)
                  {
                     runtimeCursor.RuntimeCursorData.CurrentValues[j].Value = dbfield.DefaultValue;
                     runtimeCursor.RuntimeCursorData.CurrentValues[j].IsNull = false;
                  }
               }
               else
               {
                  runtimeCursor.RuntimeCursorData.CurrentValues[j].Value = record.GetFieldValue(recordFieldIndex);
               }
            }
            else
            {
               runtimeCursor.CursorDefinition.IsFieldUpdated[j] = false;
               runtimeCursor.RuntimeCursorData.CurrentValues[j].IsNull = true;
            }
            
         }
      }
      
      /// <summary>
      /// Get Unique key of destination datasource definition.
      /// </summary>
      /// <returns></returns>
      private DBKey GetUniqueKey()
      {
         DBKey uniqueKey = null;

         if (destinationDataSourceDefinition != null)
         {
            if (destinationDataSourceDefinition.PositionIsn <= 0)
            {
               foreach (DBKey key in destinationDataSourceDefinition.Keys)
               {
                  if (key.CheckMask(KeyMasks.UniqueKeyModeMask))
                  {
                     uniqueKey = key;
                  }
               }
            }
            else
            {
               uniqueKey = destinationDataSourceDefinition.PositionKey;
            }
         }

         return uniqueKey;
      }


   }
}
