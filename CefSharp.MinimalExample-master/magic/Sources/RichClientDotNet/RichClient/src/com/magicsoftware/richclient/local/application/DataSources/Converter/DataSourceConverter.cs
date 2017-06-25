using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using com.magicsoftware.util.Xml;
using com.magicsoftware.richclient.util;
using com.magicsoftware.util;
using util.com.magicsoftware.util;
using com.magicsoftware.richclient.cache;
using com.magicsoftware.httpclient;
using com.magicsoftware.richclient.sources;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.gatewaytypes.data;
using com.magicsoftware.richclient.local.data.gateways;
using com.magicsoftware.richclient.local.data.gateways.commands;
using com.magicsoftware.richclient.local.data.cursor;
using com.magicsoftware.richclient.local.application.dataSources.converter.commands;
using com.magicsoftware.richclient.local.application.datasources.converter.convertValueStrategies;
using com.magicsoftware.unipaas;
using com.magicsoftware.unipaas.gui;

namespace com.magicsoftware.richclient.local.application.datasources.converter
{
   /// <summary>This class is responsible for handling conversions at data sources and data repository level.</summary>
   internal class DataSourceConverter
   {
      // Committing Tables: due to any error or any interruption like process termination during conversion,
      // it may happen that partial tables would get converted or there is a mismatch between table structure and its sources.
      // This may create a problem while working in offline mode. Hence the changes are committed after converting all tables.

      /// <summary>This list represents the modified data sources and the respective actions to be performed
      /// on the corresponding table, at the time of commit
      /// </summary>
      private List<IConvertCommand> _convertCommandList = new List<IConvertCommand>();

      /// <summary>
      /// reference to new data source repository's contents, maintained to check if repository is modified
      /// </summary>
      internal byte[] NewDataSourceRepositoryContents { private get; set; }

      enum ComparisonResult
      {
         Success = 1,
         Failed = 0
      }

      enum ConversionReason
      {
         NotNeeded = 0,
         DefinitionModified,
         TableDBNameModified,
         KeyDBNameModified
      }

      #region Nested type: FieldsMap
      /// <summary>
      /// Maintains map between destination and source fields.
      /// For newly added field, there will be no entry in map.
      /// </summary>
      class FieldsMap
      {
         Dictionary<int, int> fieldsMap;

         public FieldsMap(List<DBField> sourceFields, List<DBField> destinationFields)
         {
            int destinationFieldIndex;
            int sourceFieldIndex;

            fieldsMap = new Dictionary<int, int>();
            for (destinationFieldIndex = 0; destinationFieldIndex < destinationFields.Count; destinationFieldIndex++)
            {
               for (sourceFieldIndex = 0; sourceFieldIndex < sourceFields.Count; sourceFieldIndex++)
               {
                  if (destinationFields[destinationFieldIndex].Isn == sourceFields[sourceFieldIndex].Isn)
                  {
                     fieldsMap.Add(destinationFieldIndex, sourceFieldIndex);
                     break;
                  }
               }
            }
         }

         /// <summary>
         /// Get Source Field Index from fields map using destinationFieldIndex as a key
         /// </summary>
         /// <param name="destinationFieldIndex"></param>
         /// <returns></returns>
         public int GetSourceFieldIndex(int destinationFieldIndex)
         {
            int sourceFieldIndex = -1;

            if (fieldsMap.ContainsKey(destinationFieldIndex))
            {
               sourceFieldIndex = fieldsMap[destinationFieldIndex];
            }

            return sourceFieldIndex;
         }
      }

      #endregion //Nested type: FieldsMap

      /// <summary>
      /// Reads contents of a datasource indicated by sourceUrl using LocalCommandsProcessor.
      /// </summary>
      /// <param name="sourceUrl"></param>
      /// <returns></returns>
      private byte[] GetOldDataSourceBuffer(String sourceUrl)
      {
         return LocalCommandsProcessor.GetInstance().GetContent (sourceUrl, true);
      }

      /// <summary>
      /// Read old data source repository
      /// </summary>
      /// <param name="dataDefinitionIdsUrl"> repository url to read old repository </param>
      /// <returns>returns old data source repository only if it is modified</returns>
      private DataSourceDefinitionManager GetOldDataSourceDefinitions(String dataDefinitionIdsUrl)
      {
         DataSourceDefinitionManager oldDataSourceDefinitionManager = null;

         try
         {
            // Get old data source repository url from last offline response
            String oldDataDefinitionIdsUrl = GetLastDataSourcesIdUrl();

            // Last offline response can not be available if startup program is modified or in case of executing non-offline program.
            // In this case , read the repository file available in cache without remote time constraint
            if (oldDataDefinitionIdsUrl == null)
            {
               // split the url --> url and remote time
               string[] urlAndRemoteTimePair = HttpUtility.UrlDecode(dataDefinitionIdsUrl, Encoding.UTF8).Split('|');
               oldDataDefinitionIdsUrl = urlAndRemoteTimePair[0];
            }

            // read content form data source repository
            byte[] oldRepositoryContents = null;
            // the old (i.e. without the '_tmp' suffix) DbhDataIds may not be found in case this is the first access to the application.
            if (PersistentOnlyCacheManager.GetInstance().IsCompleteCacheRequestURLExistsLocally(oldDataDefinitionIdsUrl))
               oldRepositoryContents = LocalCommandsProcessor.GetInstance().GetContent(oldDataDefinitionIdsUrl, true);

            // if contents are modified then return old data source repository
            if (oldRepositoryContents != null && (oldRepositoryContents.Length != NewDataSourceRepositoryContents.Length ||
                !Misc.CompareByteArray(oldRepositoryContents, NewDataSourceRepositoryContents, NewDataSourceRepositoryContents.Length)))
            {
               // parse repository content and read data sources and build the data source definition collection
               oldDataSourceDefinitionManager = new DataSourceDefinitionManager();
               oldDataSourceDefinitionManager.DataSourceBuilder.DataSourceReader = GetOldDataSourceBuffer;
               new DataSourceDefinitionManagerSaxHandler(oldRepositoryContents, oldDataSourceDefinitionManager);
            }

            // new repository contents are not needed more for datasource converter
            NewDataSourceRepositoryContents = null;
         }
         catch (Exception ex)
         {
            Logger.Instance.WriteExceptionToLog(ex);
         }

         CommandsProcessorManager.SessionStatus = CommandsProcessorManager.SessionStatusEnum.Remote;

         return oldDataSourceDefinitionManager;
      }

      
      /// <summary>
      /// Compares the old and new repository and handles conversion of data sources
      /// </summary>
      /// <param name="newRepository"></param>
      /// <param name="dataDefinitionIdsUrl">repository url to read old repository</param>
      public void HandleRepositoryChanges(DataSourceDefinitionManager newRepository, String dataDefinitionIdsUrl)
      {
         Logger.Instance.WriteSupportToLog("HandleRepositoryChanges():>>>> ", true);

         DataSourceDefinitionManager oldRepository = GetOldDataSourceDefinitions(dataDefinitionIdsUrl);

         // Skip conversion if old repository is not exist or it is not old.
         if (oldRepository == null)
            return;

         // change mouse cursor to busy as converion may take time in case tables with larger data
         Commands.setCursor(MgCursors.WAITCURSOR);

         foreach (KeyValuePair<DataSourceId, DataSourceDefinition> definitionEntry in oldRepository.DataSourceDefinitions)
         {
            DataSourceDefinition oldDefinition = (DataSourceDefinition)definitionEntry.Value;
            DataSourceDefinition newDefinition = newRepository.GetDataSourceDefinition(oldDefinition.Id);

            // Check for deletion
            if (newDefinition == null)
            {
               // Chech if any other data sources in new repository refers to same table
               if (CanDeleteTable(oldDefinition.Name, newRepository))
               {
                  // Mark old table for delete
                  _convertCommandList.Add(new DeleteCommand(oldDefinition));
               }
            }
            else
            {
               // check if corresponding table is exist
               if (!IsTableExist(oldDefinition))
                  continue;

               // Check for conversion
               ConversionReason reason = NeedsConversion(oldDefinition, newDefinition);

               // todo: use startegy pattern instead of switch case of conversion reason and handling the corresponding operation
               switch (reason)
               {
                  case ConversionReason.DefinitionModified:
                     {
                        // Convert the old table to a temporary table using new definition
                        DataSourceDefinition convertedDefinition = ConvertDataSource(oldDefinition, newDefinition);

                        // Mark old table for delete
                        _convertCommandList.Add(new DeleteCommand(oldDefinition));

                        // Mark converted table to be renamed to original name
                        _convertCommandList.Add(new RenameCommand(convertedDefinition, newDefinition));
                     }
                     break;

                  case ConversionReason.KeyDBNameModified:
                  case ConversionReason.TableDBNameModified:
                     // Mark old table to be renamed
                     _convertCommandList.Add(new RenameCommand(oldDefinition, newDefinition));
                     break;

                  case ConversionReason.NotNeeded:
                     break; // Do nothing
               }
            }
         }

         Commands.setCursor(MgCursors.ARROW);

         Logger.Instance.WriteSupportToLog("HandleRepositoryChanges():<<<< ", true);
      }

      /// <summary>
      /// Checks the need of conversion of table
      /// </summary>
      /// <param name="oldDefinition"></param>
      /// <param name="newDefinition"></param>
      /// <returns>returns conversion reason</returns>
      private ConversionReason NeedsConversion(DataSourceDefinition oldDefinition, DataSourceDefinition newDefinition)
      {
         // Check if table should be converted because the sources may have changed but there may not be a need for conversion.
         // Eg. if field name is changed but no change in its dbname

         if ((oldDefinition.Fields.Count != newDefinition.Fields.Count) ||
             (CompareRealKeyCount(oldDefinition.Keys, newDefinition.Keys) == ComparisonResult.Failed) ||
             (CompareFields(oldDefinition.Fields, newDefinition.Fields) == ComparisonResult.Failed) ||
             (CompareKeys(oldDefinition.Keys, newDefinition.Keys) == ComparisonResult.Failed) ||
             (oldDefinition.DBaseName != newDefinition.DBaseName))
            return ConversionReason.DefinitionModified;
         
         // If Table definition is not changed in terms of database related change then
         // check if Real key name is changed or only table is name changed, if so, rename is needed
         if (IsKeyRenamed(oldDefinition.Keys, newDefinition.Keys))
            return ConversionReason.KeyDBNameModified;

         if (!oldDefinition.Name.Equals(newDefinition.Name))
            return ConversionReason.TableDBNameModified;

         return ConversionReason.NotNeeded;
      }

      #region comparison members 

      /// <summary>
      /// compare real key count in both old and new definition keys
      /// </summary>
      /// <param name="oldDefinitionKeys"></param>
      /// <param name="newDefinitionKeys"></param>
      /// <returns>returns success if no mismatch found</returns>
      private ComparisonResult CompareRealKeyCount(List<DBKey> oldDefinitionKeys, List<DBKey> newDefinitionKeys)
      {
         int oldRealKeyCount = 0;
         int newRealKeyCount = 0;

         for (int i = 0; i < oldDefinitionKeys.Count; i++)
            if (oldDefinitionKeys[i].CheckMask(KeyMasks.KeyTypeReal))
               oldRealKeyCount++;

         for (int i = 0; i < newDefinitionKeys.Count; i++)
            if (newDefinitionKeys[i].CheckMask(KeyMasks.KeyTypeReal))
               newRealKeyCount++;

         return ((oldRealKeyCount != newRealKeyCount) ? ComparisonResult.Failed : ComparisonResult.Success);
      }

      /// <summary>
      /// compares fields of old and new definition
      /// </summary>
      /// <param name="oldDefinitionFields"></param>
      /// <param name="newDefinitionFields"></param>
      /// <returns>returns success if no mismatch found</returns>
      private ComparisonResult CompareFields(List<DBField> oldDefinitionFields, List<DBField> newDefinitionFields)
      {
         for (int idx = 0; idx < oldDefinitionFields.Count; idx++)
         {
            DBField oldField = oldDefinitionFields[idx];
            DBField newField = newDefinitionFields[idx];

            if (CompareField(oldField, newField) ==  ComparisonResult.Failed)
               return ComparisonResult.Failed;
         }

         return ComparisonResult.Success;
      }


      /// <summary>
      /// compares old field with new field
      /// </summary>
      /// <param name="oldField"></param>
      /// <param name="newField"></param>
      /// <returns>returns success if no mismatch found</returns>
      private ComparisonResult CompareField (DBField oldField, DBField newField)
      {
         if ((oldField.Storage != newField.Storage) ||
             (!oldField.DbName.Equals(newField.DbName)) ||
             (!oldField.DbType.Equals(newField.DbType, StringComparison.OrdinalIgnoreCase)) ||
             (!oldField.DbDefaultValue.Equals(newField.DbDefaultValue)) ||
             (oldField.PartOfDateTime != newField.PartOfDateTime) ||
             (!oldField.UserType.Equals(newField.UserType)) ||
             (oldField.AllowNull != newField.AllowNull) ||
             (!oldField.DbInfo.Equals(newField.DbInfo)) ||
             (oldField.DefaultStorage != newField.DefaultStorage) ||
             (oldField.Length != newField.Length))
            return ComparisonResult.Failed;

         return ComparisonResult.Success;
      }

      /// <summary>
      /// compare keys of old and new definition
      /// </summary>
      /// <param name="oldDefinitionKeys"></param>
      /// <param name="newDefinitionKeys"></param>
      /// <returns>returns success if no mismatch found</returns>
      private ComparisonResult CompareKeys(List<DBKey> oldDefinitionKeys, List<DBKey> newDefinitionKeys)
      {
         // Compare key properties
         for (int idx = 0; idx < oldDefinitionKeys.Count; idx++)
         {
            if (idx < newDefinitionKeys.Count)
            {
               DBKey oldKey = oldDefinitionKeys[idx];
               DBKey newKey = newDefinitionKeys[idx];

               if (CompareKey(oldKey, newKey) == ComparisonResult.Failed)
                  return ComparisonResult.Failed;
            }
         }

         return ComparisonResult.Success;
      }


      /// <summary>
      /// compares old key with new key
      /// </summary>
      /// <param name="oldKey"></param>
      /// <param name="newKey"></param>
      /// <returns>returns success if no mismatch found</returns>
      private ComparisonResult CompareKey(DBKey oldKey, DBKey newKey)
      {

         if (oldKey.CheckMask(KeyMasks.KeyTypeVirtual) && newKey.CheckMask(KeyMasks.KeyTypeVirtual))
            return ComparisonResult.Success;                 

         if (oldKey.Segments.Count != newKey.Segments.Count)
            return ComparisonResult.Failed;

         if ((oldKey.CheckMask(KeyMasks.DuplicateKeyModeMask) && newKey.CheckMask(KeyMasks.UniqueKeyModeMask)) ||
            (newKey.CheckMask(KeyMasks.DuplicateKeyModeMask) && oldKey.CheckMask(KeyMasks.UniqueKeyModeMask)))
            return ComparisonResult.Failed;

         if ((oldKey.CheckMask(KeyMasks.KeyTypeVirtual) && newKey.CheckMask(KeyMasks.KeyTypeReal)) ||
             (newKey.CheckMask(KeyMasks.KeyTypeVirtual) && oldKey.CheckMask(KeyMasks.KeyTypeReal)))
            return ComparisonResult.Failed;

         if ((oldKey.CheckMask(KeyMasks.KeyPrimaryMask) && !newKey.CheckMask(KeyMasks.KeyPrimaryMask)) ||
             (newKey.CheckMask(KeyMasks.KeyPrimaryMask) && !oldKey.CheckMask(KeyMasks.KeyPrimaryMask)))
            return ComparisonResult.Failed;

         // compare segments of real keys
         for (int idx = 0; idx < oldKey.Segments.Count; idx++)
         {
            DBSegment oldSegment = oldKey.Segments[idx];
            DBSegment newSegment = newKey.Segments[idx];

            if (CompareSegment(oldSegment, newSegment) == ComparisonResult.Failed)
               return ComparisonResult.Failed;
         }
         
         return ComparisonResult.Success;
      }

      /// <summary>
      /// compares old segment with new segment
      /// </summary>
      /// <param name="oldSegment"></param>
      /// <param name="newSegment"></param>
      /// <returns>returns success if no mismatch found</returns>
      private ComparisonResult CompareSegment(DBSegment oldSegment, DBSegment newSegment)
      {
         if (oldSegment.Field.Isn != newSegment.Field.Isn)
            return ComparisonResult.Failed;

         if ((oldSegment.CheckMask(SegMasks.SegDirAscendingMask) && newSegment.CheckMask(SegMasks.SegDirDescendingMask)) ||
             (newSegment.CheckMask(SegMasks.SegDirAscendingMask) && oldSegment.CheckMask(SegMasks.SegDirDescendingMask)))
            return ComparisonResult.Failed;

         return ComparisonResult.Success;
      }

      /// <summary>
      /// check if key is renamed
      /// </summary>
      /// <param name="oldDefinitionKeys"></param>
      /// <param name="newDefinitionKeys"></param>
      /// <returns>true if renamed</returns>
      private bool IsKeyRenamed(List<DBKey> oldDefinitionKeys, List<DBKey> newDefinitionKeys)
      {
         // Compare dbname of real keys
         for (int idx = 0; idx < oldDefinitionKeys.Count; idx++)
         {
            DBKey oldKey = oldDefinitionKeys[idx];
            DBKey newKey = null;

            for (int i = 0; i < newDefinitionKeys.Count; i++)
               if (newDefinitionKeys[i].Isn == oldKey.Isn)
               {
                  newKey = newDefinitionKeys[i];
                  break;
               }

            if (oldKey.CheckMask(KeyMasks.KeyTypeReal) && newKey != null)
               if (!oldKey.KeyDBName.Equals(newKey.KeyDBName))
                  return true;
         }

         return false;
      }

      #endregion //comparion members

      /// <summary>
      /// Checks if table can be deleted, by checking its references in all data sources in repository
      /// </summary>
      /// <param name="dbName">table name</param>
      /// <param name="dataSourceRepository">repository</param>
      /// <returns></returns>
      private bool CanDeleteTable(String dbName, DataSourceDefinitionManager dataSourceRepository)
      {
         foreach (KeyValuePair<DataSourceId, DataSourceDefinition> definitionEntry in dataSourceRepository.DataSourceDefinitions)
         {
            DataSourceDefinition dataSourceDefinition = (DataSourceDefinition)definitionEntry.Value;
            if (dataSourceDefinition.Name.Equals(dbName))
               return false;
         }

         return true;
      }

      /// <summary>
      /// Checks if correspondng table of data source is exist at backend
      /// </summary>
      /// <param name="dataSourceDefinition"></param>
      /// <returns>returns true if table exists</returns>
      private bool IsTableExist(DataSourceDefinition dataSourceDefinition)
      {
         GatewayCommandFileExist fileExistCommand = GatewayCommandsFactory.CreateFileExistCommand(dataSourceDefinition.Name, dataSourceDefinition,
                                                                                                  ClientManager.Instance.LocalManager);
         GatewayResult result = fileExistCommand.Execute();

         return (result.Success);
      }

      /// <summary>
      /// Convert DataSource from old DataSourceDefinition to new DataSourceDefinition. It create temporaryDataSource definition and perform all operations
      /// on temporary DataSourcedefinition and return it.
      /// </summary>
      /// <param name="fromDataSourceDefinition"></param>
      /// <param name="toDataSourceDefinition"></param>
      /// <returns>Temporary DataSourceDefinition</returns>
      private DataSourceDefinition ConvertDataSource(DataSourceDefinition fromDataSourceDefinition, DataSourceDefinition toDataSourceDefinition)
      {
         Logger.Instance.WriteSupportToLog("convertDataSource():>>>> ", true);
         Logger.Instance.WriteSupportToLog(String.Format("convertDataSource(): converting table {0}",fromDataSourceDefinition.Name), true);

         GatewayResult result = null;

         string temporaryTableName = GetTemporaryTableName(fromDataSourceDefinition.Id);

         DataSourceDefinition temporaryDataSourceDefinition = (DataSourceDefinition)toDataSourceDefinition.Clone();
         temporaryDataSourceDefinition.Name = temporaryTableName;

         // In order to genearte temporary key name for the temporary dbh set magic key mask on the keys.
         for (int keyIndex = 0; keyIndex < temporaryDataSourceDefinition.Keys.Count; keyIndex++)
         {
            DBKey key = temporaryDataSourceDefinition.Keys[keyIndex];
            key.SetMask(KeyMasks.MagicKeyMask);
         }

         // Delete temporary table, if exists
         result = GatewayCommandsFactory.CreateFileDeleteCommand(temporaryTableName, temporaryDataSourceDefinition, ClientManager.Instance.LocalManager).Execute();
         if (!result.Success && result.ErrorCode != GatewayErrorCode.FileNotExist)
            throw new DataSourceConversionFailedException(fromDataSourceDefinition.Name, result.ErrorDescription);

         // Open source and temporary table
         temporaryDataSourceDefinition.SetMask(DbhMask.CheckExistMask);
         result = GatewayCommandsFactory.CreateFileOpenCommand(temporaryTableName, temporaryDataSourceDefinition, Access.Write, ClientManager.Instance.LocalManager).Execute();
         if (result.Success)
            result = GatewayCommandsFactory.CreateFileOpenCommand(fromDataSourceDefinition.Name, fromDataSourceDefinition, Access.Read, ClientManager.Instance.LocalManager).Execute();

         if (!result.Success)
            throw new DataSourceConversionFailedException(fromDataSourceDefinition.Name, result.ErrorDescription);

         //Convert values of source table and insert it into temporary table
         ConvertAndInsertValues(fromDataSourceDefinition, temporaryDataSourceDefinition);

         //Close source and temporary table
         result = GatewayCommandsFactory.CreateFileCloseCommand(temporaryDataSourceDefinition, ClientManager.Instance.LocalManager).Execute();
         if (result.Success)
            GatewayCommandsFactory.CreateFileCloseCommand(fromDataSourceDefinition, ClientManager.Instance.LocalManager).Execute();

         if (!result.Success)
            throw new DataSourceConversionFailedException(fromDataSourceDefinition.Name, result.ErrorDescription);

         Logger.Instance.WriteSupportToLog("convertDataSource():<<<< ", true);

         return temporaryDataSourceDefinition;
      }

      #region conversion members
      /// <summary>
      /// Fetech the values from source datasource, convert fetched values and insert values in destination dataSource.
      /// </summary>
      /// <param name="fromDataSourceDefinition"></param>
      /// <param name="toDataSourceDefinition"></param>
      private void ConvertAndInsertValues(DataSourceDefinition fromDataSourceDefinition, DataSourceDefinition toDataSourceDefinition)
      {
         GatewayResult result = null;

         MainCursorBuilder cursorBuilder = new MainCursorBuilder(null);

         RuntimeCursor fromRuntimeCursor = cursorBuilder.Build(fromDataSourceDefinition, Access.Read);
         RuntimeCursor toRuntimeCursor = cursorBuilder.Build(toDataSourceDefinition, Access.Write);

         //Prepare and open source and destnation runtime cursor.
         PrepareAndOpenCursor(fromRuntimeCursor, true);
         PrepareAndOpenCursor(toRuntimeCursor, false);

         //Create fetch command
         GatewayCommandFetch fetchCommand = GatewayCommandsFactory.CreateCursorFetchCommand(fromRuntimeCursor, ClientManager.Instance.LocalManager);

         while (true)
         {
            //Fetch record from source table
            result = fetchCommand.Execute();
            if (!result.Success)
            {
               if (result.ErrorCode == GatewayErrorCode.NoRecord)
                  break;
               else
                  throw new DataSourceConversionFailedException(fromDataSourceDefinition.Name, result.ErrorDescription);
            }

            //Convert values of fields of source table.
            ConvertFields(fromDataSourceDefinition, toDataSourceDefinition, fromRuntimeCursor.RuntimeCursorData.CurrentValues, toRuntimeCursor.RuntimeCursorData.CurrentValues);

            for (int i = 0; i < toDataSourceDefinition.Fields.Count; i++)
            {
               toRuntimeCursor.CursorDefinition.IsFieldUpdated[i] = true;
            }

            //Insert converted values into temporary table.
            result = GatewayCommandsFactory.CreateCursorInsertCommand(toRuntimeCursor, ClientManager.Instance.LocalManager).Execute();
         }

         // release and close source and destnation runtime cursor.
         ReleaseAndCloseCursor(fromRuntimeCursor, true);
         ReleaseAndCloseCursor(toRuntimeCursor, false);
      }

      /// <summary>
      /// Convert fields according to the storage attribute. For newly created field assign default value to that field.
      /// </summary>
      /// <param name="sourceDataSourceDefinition"></param>
      /// <param name="destinationDataSourceDefinition"></param>
      /// <param name="sourceValues"></param>
      /// <param name="destinationValues"></param>
      private void ConvertFields(DataSourceDefinition sourceDataSourceDefinition, DataSourceDefinition destinationDataSourceDefinition,
                                  FieldValues sourceValues, FieldValues destinationValues)
      {
         FieldsMap fieldsMap = new FieldsMap(sourceDataSourceDefinition.Fields, destinationDataSourceDefinition.Fields);
         DBField sourceField = null;
         FieldValue sourceValue = null;

         for (int destinationFieldIndex = 0; destinationFieldIndex < destinationDataSourceDefinition.Fields.Count; destinationFieldIndex++)
         {
            int sourceFieldIndex = fieldsMap.GetSourceFieldIndex(destinationFieldIndex);

            DBField destinationField = destinationDataSourceDefinition.Fields[destinationFieldIndex];
            FieldValue destinationValue = destinationValues[destinationFieldIndex];

            // If source field exists and source and destination types are comapatible then convert field values.
            // Else assign default value to destination field.
            if (sourceFieldIndex != -1)
            {
               sourceField = sourceDataSourceDefinition.Fields[sourceFieldIndex];
               sourceValue = sourceValues[sourceFieldIndex];

               if (StorageAttributeCheck.IsTypeCompatibile((StorageAttribute)sourceField.Attr, (StorageAttribute)destinationField.Attr))
               {
                  ConvertFieldValue(sourceField, destinationField, sourceValue, destinationValue);
               }
               else
               {
                  destinationValue.Value = destinationField.DefaultValue;
                  destinationValue.IsNull = destinationField.DefaultNull;
               }
            }
            else
            {
               destinationValue.Value = destinationField.DefaultValue;
               destinationValue.IsNull = destinationField.DefaultNull;
            }
         }
      }

      /// <summary>
      /// Convert field's value.
      /// </summary>
      /// <param name="sourceField"></param>
      /// <param name="destinationField"></param>
      /// <param name="sourceValue"></param>
      /// <param name="destinationValue"></param>
      public void ConvertFieldValue(DBField sourceField, DBField destinationField, FieldValue sourceValue, FieldValue destinationValue)
      {
         destinationValue.IsNull = sourceValue.IsNull;

         //If Source value id null and destination field do not allow null values then set default value
         //to destination field.
         if (sourceValue.IsNull)
         {
            if (!destinationField.AllowNull)
            {
               destinationValue.Value = destinationField.DefaultValue;
               destinationValue.IsNull = false;
            }
            return;
         }

         IConvertValueStrategy convertValueStrategy = null;

         switch ((StorageAttribute)sourceField.Attr)
         {
            case StorageAttribute.ALPHA:
               convertValueStrategy = new ConvertAlphaStrategy();
               break;
            case StorageAttribute.UNICODE:
               convertValueStrategy = new ConvertUnicodeStrategy();
               break;
            case StorageAttribute.NUMERIC:
               convertValueStrategy = new ConvertNumericStrategy();
               break;
            case StorageAttribute.BOOLEAN:
               convertValueStrategy = new ConvertBooleanStrategy();
               break;
            case StorageAttribute.DATE:
               convertValueStrategy = new ConvertDateStrategy();
               break;
            case StorageAttribute.TIME:
               convertValueStrategy = new ConvertTimeStrategy();
               break;
            case StorageAttribute.BLOB:
               convertValueStrategy = new ConvertBlobStrategy();
               break;
         }

         convertValueStrategy.Convert(sourceField, destinationField, sourceValue, destinationValue);
      }

      /// <summary>
      /// Prepare and Open runtime cursor
      /// </summary>
      /// <param name="runtimeCursor"></param>
      private void PrepareAndOpenCursor(RuntimeCursor runtimeCursor, bool openTransaction)
      {
         GatewayResult result = null;

         runtimeCursor.CursorDefinition.StartPosition = new DbPos(true);
         runtimeCursor.CursorDefinition.CurrentPosition = new DbPos(true);

         result = GatewayCommandsFactory.CreateCursorPrepareCommand(runtimeCursor, ClientManager.Instance.LocalManager).Execute();
         if (result.Success)
         {
            if (openTransaction)
            {
               result = GatewayCommandsFactory.CreateGatewayCommandOpenTransaction(ClientManager.Instance.LocalManager).Execute();
            }
            if (result.Success)
            {
               result = GatewayCommandsFactory.CreateCursorOpenCommand(runtimeCursor, ClientManager.Instance.LocalManager).Execute();
            }
         }

         if (!result.Success)
            throw new DataSourceConversionFailedException(runtimeCursor.CursorDefinition.DataSourceDefinition.Name, result.ErrorDescription);
      }

      /// <summary>
      /// Close and Relaese runtime cursor.
      /// </summary>
      /// <param name="runtimeCursor"></param>
      private void ReleaseAndCloseCursor(RuntimeCursor runtimeCursor, bool closeTransaction)
      {
         GatewayResult result = null;
         result = GatewayCommandsFactory.CreateCursorCloseCommand(runtimeCursor, ClientManager.Instance.LocalManager).Execute();
         if (result.Success)
         {
            if (closeTransaction)
            {
               result = GatewayCommandsFactory.CreateGatewayCommandCloseTransaction(ClientManager.Instance.LocalManager).Execute();
            }
            if (result.Success)
            {
               result = GatewayCommandsFactory.CreateCursorReleaseCommand(runtimeCursor, ClientManager.Instance.LocalManager).Execute();
            }
         }

         if (!result.Success)
            throw new DataSourceConversionFailedException(runtimeCursor.CursorDefinition.DataSourceDefinition.Name, result.ErrorDescription);
      }

      #endregion //conversion members

      /// <summary>
      /// commit changes that made to data sources
      /// </summary>
      internal void Commit()
      {
         SourcesSyncStatus sourcesSyncStatus = ApplicationSourcesManager.GetInstance().SourcesSyncStatus;

         if (_convertCommandList.Count > 0)
         {
            // It is about to start committing the tables. So save the status. If process get terminated during commit,
            // in next execution, client will get to know the status of data sources synchronization and take action accordingly.
            // This flag will be set to false after commiting sources successfully.
            sourcesSyncStatus.TablesIncompatibleWithDataSources = true;
            sourcesSyncStatus.SaveToFile();
         }

         try
         {
            foreach (IConvertCommand command in _convertCommandList)
               command.Execute();
         }
         catch (Exception e)
         {
            String errorMessage = ClientManager.Instance.getMessageString(MsgInterface.RC_ERROR_INCOMPATIBLE_DATASOURCES);
            throw new InvalidSourcesException(errorMessage, e);
         }
      }

      /// <summary>
      /// Roll back modified tables
      /// </summary>
      internal void RollBack()
      {
         _convertCommandList.Clear();
      }

      /// <summary>
      /// get last data repository url from initial response
      /// </summary>
      /// <returns></returns>
      private string GetLastDataSourcesIdUrl()
      {
         string dataDefinitionIdsUrl = null;

         if (LocalCommandsProcessor.GetInstance().CanStartWithoutNetwork)
         {
            String lastOfflineInitialResponse = LocalCommandsProcessor.GetInstance().GetLastOfflineInitialResponse();
            XmlParser parser = new XmlParser(lastOfflineInitialResponse);

            parser.setCurrIndex(parser.getXMLdata().IndexOf(ConstInterface.MG_TAG_DBH_DATA_IDS_URL, parser.getCurrIndex()) + ConstInterface.MG_TAG_DBH_DATA_IDS_URL.Length + 1);

            int endContext = parser.getXMLdata().IndexOf(XMLConstants.TAG_CLOSE, parser.getCurrIndex());
            List<string> tokensVector = XmlParser.getTokens(parser.getXMLsubstring(endContext), XMLConstants.XML_ATTR_DELIM);
            Debug.Assert(tokensVector[0].Equals(XMLConstants.MG_ATTR_VALUE));

            dataDefinitionIdsUrl = XmlParser.unescape(tokensVector[1]);
         }

         return dataDefinitionIdsUrl;
      }

      /// <summary>
      /// Return the temporary table name
      /// </summary>
      /// <param name="id"></param>
      /// <returns></returns>
      private string GetTemporaryTableName(DataSourceId id)
      {
         return string.Format("cnv_{0}_{1}", id.Isn, id.CtlIdx);
      }

   }
}
