using com.magicsoftware.richclient.local.application.Databases;
using com.magicsoftware.richclient.local.data.cursor;
using com.magicsoftware.gatewaytypes.data;
using com.magicsoftware.gatewaytypes;
using System.Xml.Serialization;
using com.magicsoftware.richclient.local.data.recording;
using System;
using com.magicsoftware.richclient.cache;

namespace com.magicsoftware.richclient.local.data.gateways.commands
{
   /// <summary>
   /// Abstract base class for gateway commands
   /// </summary>
   public abstract class GatewayCommandBase
   {

      public GatewayCommandBase()
      {
         ShouldUpdateDataBaseLocation = true;
      }

      private DataSourceDefinition dataSourceDefinition;
      
      public bool ValueInGatewayFormat { get; set; }

      [XmlIgnore]
      public DataSourceDefinition DataSourceDefinition
      {
         get { return dataSourceDefinition; }
         set
         {
            dataSourceDefinition = value;
            DataSourceId = dataSourceDefinition == null ? null : dataSourceDefinition.Id;
         }
      }

      public DataSourceId DataSourceId { get; set; }
      
      private RuntimeCursor runtimeCursor;

      public RuntimeCursor RuntimeCursor
      {
         get { return runtimeCursor; }
         set
         {
            runtimeCursor = value;
            if ( runtimeCursor.CursorDefinition.DataSourceDefinition != null)
               DataSourceDefinition = runtimeCursor.CursorDefinition.DataSourceDefinition;
         }
      }

      internal GatewayCommandsRecorder Recorder { get { return this.GatewayAdapter.Recorder; } }
      internal GatewayDataRecorder DataRecorder { get { return this.GatewayAdapter.DataRecorder; } }
      internal LocalManager LocalManager { get; set; }
      internal ApplicationDefinitions ApplicationDefinitions
      {
         get
         {
            return LocalManager.ApplicationDefinitions;
         }
      }

      DatabaseDefinition dbDefinition;
      protected DatabaseDefinition DbDefinition
      {
         get
         {
            if (dbDefinition == null)
            {
               DatabaseDefinitionsManager dbDefManager = ApplicationDefinitions.DatabaseDefinitionsManager;
               dbDefinition = dbDefManager[DataSourceDefinition.DBaseName];
            }
            return dbDefinition;
         }
      }

      int databaseType = -1;
      internal int DatabaseType
      {
         get
         {
            if (databaseType == -1)
            {
               DatabaseDefinitionsManager dbDefManager = ApplicationDefinitions.DatabaseDefinitionsManager;
               dbDefinition = dbDefManager[DataSourceDefinition.DBaseName];
               databaseType = dbDefinition.DatabaseType;
            }
            return databaseType;
         }
         set
         {
            databaseType = value;
         }
      }

      GatewayAdapter gatewayAdapter;
      protected GatewayAdapter GatewayAdapter
      {
         get
         {
            if (gatewayAdapter == null)
               gatewayAdapter = LocalManager.GatewaysManager.GetGatewayAdapter(DatabaseType);
            return gatewayAdapter;
         }
      }

      public bool ShouldUpdateDataBaseLocation { get; set; }

      internal abstract GatewayResult Execute();


      /// <summary>
      /// fm_flds_mg_2_db
      /// Converts a record from magic presentation to gateway 
      /// </summary>
      protected void ConvertToGateway(GatewayAdapterCursor gatewayAdapterCursor)
      {
         FieldValues currentRecord = gatewayAdapterCursor.CurrentRecord;
         FieldValues runtimeRecord = RuntimeCursor.RuntimeCursorData.CurrentValues;

        
         for (int i = 0; i < currentRecord.Count; i++)
         {
            currentRecord[i].IsNull = runtimeRecord.IsNull(i);
            if (currentRecord[i].IsNull)
               currentRecord[i].Value = null;
            else
            {
               DBField dbField = gatewayAdapterCursor.Definition.FieldsDefinition[i];
               if (ValueInGatewayFormat)
               {
                  if (dbField.IsBlob())
                  {
                     GatewayBlob blob = new GatewayBlob();
                     blob.Blob = runtimeRecord.GetValue(i);

                     if (dbField.Storage == magicsoftware.util.FldStorage.Blob)
                         blob.BlobSize = ((Byte[])runtimeRecord.GetValue(i)).Length;
                     else
                         blob.BlobSize = ((string)runtimeRecord.GetValue(i)).Length;

                     currentRecord[i].Value = blob;
                  }
                  else
                  {
                     currentRecord[i].Value = runtimeRecord.GetValue(i);
                  }
               }
               else
               {
                  string trimValue = ((string)runtimeRecord.GetValue(i)).TrimEnd();
                  currentRecord[i].Value = GatewayAdapter.StorageConvertor.ConvertRuntimeFieldToGateway(dbField, trimValue);
               }
            }

         }
      }


      /// <summary>
      /// set error details on the command
      /// </summary>
      /// <param name="result"></param>
      protected virtual void SetErrorDetails(GatewayResult result)
      {
         string errorString = ""; ;
         int errorCode = 0;

         if (!result.Success)
         {
            // TODO: Error handling.
            // Temporary !!!

            DatabaseDefinition databaseDefinition = (DatabaseDefinition)DbDefinition.Clone();
            UpdateDataBaseLocation(databaseDefinition);

            GatewayAdapter.Gateway.LastError(databaseDefinition, false, ref errorCode, ref errorString);
            result.ErrorParams = new object[] { DataSourceDefinition.Name, errorString };
         }
      }

      protected void Record()
      {
         if (Recorder != null)
            Recorder.Record(this);
      }

      protected void RecordData()
      {
         if (DataRecorder != null)
            DataRecorder.Record(GatewayAdapter.GetCursor(RuntimeCursor));
      }

      /// <summary>
      /// check if the flag set 
      /// </summary>
      /// <param name="cursorProperties"></param>
      /// <returns></returns>
      protected GatewayResult CheckIsFlagSet(CursorProperties cursorProperties)
      {
         GatewayResult gatewayResult = new GatewayResult();

         if (!RuntimeCursor.CursorDefinition.IsFlagSet(cursorProperties))
         {
            gatewayResult.ErrorCode = GatewayErrorCode.ReadOnly;
            gatewayResult.ErrorParams = new string[] { RuntimeCursor.CursorDefinition.DataSourceDefinition.Name, "" };
         }

         return gatewayResult;
      }

      /// <summary>
      ///  Update the location with translated logical name.
      /// </summary>
      /// <param name="dbDefinition"></param>
      protected void UpdateDataBaseLocation(DatabaseDefinition dbDefinition)
      {
         if (ShouldUpdateDataBaseLocation)
         {
            string localDatabaseLocation = ClientManager.Instance.getEnvParamsTable().translate(dbDefinition.Location);
            if (String.IsNullOrEmpty(localDatabaseLocation))
               dbDefinition.Location = null;
            else
               dbDefinition.Location = CacheUtils.ServerFileToLocalFileName(localDatabaseLocation);
         }
      }

   }

  
}
