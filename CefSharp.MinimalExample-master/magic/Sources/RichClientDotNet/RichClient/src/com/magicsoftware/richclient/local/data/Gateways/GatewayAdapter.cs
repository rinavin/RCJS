using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.gatewaytypes.data;
using com.magicsoftware.richclient.local.data.cursor;
using com.magicsoftware.richclient.local.data.gateways.storage;
using com.magicsoftware.richclient.local.data.recording;

namespace com.magicsoftware.richclient.local.data.gateways
{
   /// <summary>
   /// Is used for the connection between RunTime and the specific gateway (FM)
   /// </summary>
   public class GatewayAdapter
   {      

      internal ISQLGateway Gateway { get; private set; }
      internal StorageConverter StorageConvertor { get; private set; }
      internal GatewayCommandsRecorder Recorder { get; set; }
      internal GatewayDataRecorder DataRecorder { get; set; }

      // is transaction was opened in the gateway (we will use it in the close\rollback transaction.)
      internal bool TransactionWasOpned { get; set; }

      // it is hdl_info_tbl_ in FM (C++)
      private Dictionary<DataSourceDefinition, RTDataSource> openedDataSources;

      // it is crsr_info_tbl_ in FM (C++)
      private Dictionary<RuntimeCursor, GatewayAdapterCursor> gatewayAdapterCursors;

      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="gateway"></param>
      internal GatewayAdapter(ISQLGateway gateway)
      {
         Gateway = gateway;
         StorageConvertor = (new StorageConverterBuilder()).Build();
         openedDataSources = new Dictionary<DataSourceDefinition, RTDataSource>();
         gatewayAdapterCursors = new Dictionary<RuntimeCursor, GatewayAdapterCursor>();
      }

      /// <summary>
      /// Adds the specific data source to the opened data sources dictionary
      /// </summary>
      /// <param name="dataSourceDefinition"></param>
      /// <param name="rtDataSource"></param>
      internal void AddDataSource (DataSourceDefinition dataSourceDefinition, RTDataSource rtDataSource)
      {
         Debug.Assert(!openedDataSources.ContainsKey(dataSourceDefinition));
         openedDataSources.Add(dataSourceDefinition, rtDataSource);
      }

      /// <summary>
      /// Removes the specific data source from the opened data sources dictionary
      /// </summary>
      /// <param name="dataSourceDefinition"></param>
      internal void RemoveDataSource(DataSourceDefinition dataSourceDefinition)
      {
         Debug.Assert(openedDataSources.ContainsKey(dataSourceDefinition));
         openedDataSources.Remove(dataSourceDefinition);
      }

      /// <summary>
      /// Returns RTDataSource according to its definition
      /// </summary>
      /// <param name="dataSourceDefinition"></param>
      /// <returns></returns>
      internal RTDataSource GetDataSource(DataSourceDefinition dataSourceDefinition)
      {
         if (openedDataSources.ContainsKey(dataSourceDefinition))
            return openedDataSources[dataSourceDefinition];
         return null;
      }

      /// <summary>
      /// Adds the specific cursor to the cursors dictionary
      /// </summary>
      /// <param name="runtimeCursor"></param>
      /// <param name="gatewayAdapterCursor"></param>
      internal void AddCursor(RuntimeCursor runtimeCursor, GatewayAdapterCursor gatewayAdapterCursor)
      {
         Debug.Assert(!gatewayAdapterCursors.ContainsKey(runtimeCursor));
         gatewayAdapterCursors.Add(runtimeCursor, gatewayAdapterCursor);
      }

      /// <summary>
      /// Removes the specific cursor from the cursors dictionary
      /// </summary>
      /// <param name="runtimeCursor"></param>
      internal void RemoveCursor(RuntimeCursor runtimeCursor)
      {
         Debug.Assert(gatewayAdapterCursors.ContainsKey(runtimeCursor));
         gatewayAdapterCursors.Remove(runtimeCursor);
      }

      /// <summary>
      /// Returns gateway adapter cursor according to its runtime cursor
      /// </summary>
      /// <param name="runtimeCursor"></param>
      /// <returns></returns>
      internal GatewayAdapterCursor GetCursor(RuntimeCursor runtimeCursor)
      {
         if (gatewayAdapterCursors.ContainsKey(runtimeCursor))
         {
            GatewayAdapterCursor gatewayAdapterCursor = gatewayAdapterCursors[runtimeCursor];

            //This change is done to execute unit test. Because for unit test each command refers different runtime cursor.
            // So to get correct Cursor definition copy CursorDefinition from RunTimeCursor To Gateway cursor.
            if (!runtimeCursor.CursorDefinition.Equals(gatewayAdapterCursor.Definition))
            {
               gatewayAdapterCursor.Definition = runtimeCursor.CursorDefinition;
            }

            return gatewayAdapterCursor;
         }

         return null;
      }

      /// <summary>
      /// return true if we have data source definition 
      /// </summary>
      /// <returns></returns>
      internal bool  HasDataSourceDefinition()
      {
         return openedDataSources.Count > 0;
      }

       /// <summary>
       /// return first data source definition
       /// </summary>
       /// <returns></returns>
      internal DataSourceDefinition GetFirstDataSourceDefinition()
      {
          DataSourceDefinition[] arr = new DataSourceDefinition[openedDataSources.Count];
          openedDataSources.Keys.CopyTo(arr, 0);
          return arr[0];
      }
   }
}
