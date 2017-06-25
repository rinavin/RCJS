using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.IO;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.local.data.recording;

namespace com.magicsoftware.richclient.local.data.gateways
{
   /// <summary>
   /// Manages gateways (we have only SQL lite for now)
   /// </summary>
   internal class GatewaysManager
   {
      internal const int DATA_SOURCE_DATATYPE_LOCAL = 10;

      Dictionary<int, GatewayAdapter> GatewayAdapters { get; set; }
      internal GatewayCommandsRecorder Recorder { get;  set; }
      internal GatewayDataRecorder DataRecorder { get; set; }



      /// <summary>
      /// CTOR
      /// </summary>
      internal GatewaysManager()
      {
         GatewayAdapters = new Dictionary<int, GatewayAdapter>();
      }

      /// <summary>
      /// Returns the gateway adapter for the corresponding database according to its index
      /// </summary>
      /// <param name="dbName"></param>
      /// <returns></returns>
      internal GatewayAdapter GetGatewayAdapter(int dbType)
      {
         if (!GatewayAdapters.ContainsKey(dbType))
         {
            ISQLGateway gateway = null;
#if !PocketPC
            String mgSqliteDllPath = null;
            try
            {
               mgSqliteDllPath = Path.GetFullPath(ConstInterface.SQLITE_DLL_NAME);
               Assembly mgSqliteDll = Assembly.LoadFile(mgSqliteDllPath);
               Type mgSqliteGatewayClass = mgSqliteDll.GetType("MgSqlite.src.SQLiteGateway");
               if (mgSqliteGatewayClass != null)
                  gateway = (ISQLGateway)Activator.CreateInstance(mgSqliteGatewayClass);
            }
            catch (Exception ex)
            {
               throw new ApplicationException(String.Format("The SQLite gateway couldn't be loaded: \"{0}\"", mgSqliteDllPath), ex);
            }
#endif
            GatewayAdapters.Add(dbType, new GatewayAdapter(gateway) { Recorder = Recorder, DataRecorder = DataRecorder});
         }
         return (GatewayAdapters[dbType]);
      }
   }
}
