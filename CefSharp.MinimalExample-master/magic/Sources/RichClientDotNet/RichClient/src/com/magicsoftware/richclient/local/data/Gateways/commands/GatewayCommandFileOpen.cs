using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.local.application.datasources;
using com.magicsoftware.util;
using com.magicsoftware.richclient.util;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;
using com.magicsoftware.gatewaytypes.data;

namespace com.magicsoftware.richclient.local.data.gateways.commands
{
   public class GatewayCommandFileOpen : GatewayCommandBase
   {
      #region Properties
      public String FileName { get; set; }
      public Access Access { get; set; }
      #endregion
      static XmlSerializer dataSourceSerializer;
      static XmlSerializer serializer;
      public String SerializeDatasource(DataSourceDefinition d)
      {
         String result = null;
#if DEBUG
         try
#endif
         {
            if (dataSourceSerializer == null)
               dataSourceSerializer = new XmlSerializer(typeof(DataSourceDefinition));
            MemoryStream stream = new MemoryStream();
            dataSourceSerializer.Serialize(stream, d);

            result = Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Length);
            stream = new MemoryStream( Encoding.UTF8.GetBytes(result));

            object cursor = dataSourceSerializer.Deserialize(stream);
         }
#if DEBUG
         catch (Exception e)
         {

            MessageBox.Show(e.ToString());

         }
#endif


         return result;
      }


      public String Serialize()
      {
         String result = null;
         try
         {
            if (serializer == null)
               serializer = new XmlSerializer(GetType());
            MemoryStream stream = new MemoryStream();
            serializer.Serialize(stream, this);

            result = Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Length);
            stream = new MemoryStream(Encoding.UTF8.GetBytes(result));

            GatewayCommandBase gatewayCommandBase = (GatewayCommandBase)serializer.Deserialize(stream);
            LocalManager localManager = new LocalManager();
            DatabaseDefinition localDatabase = new DatabaseDefinition { DatabaseType = 10, Location = "SqliteTest", Name = "Local" };
            localManager.ApplicationDefinitions.DatabaseDefinitionsManager.Add("LOCAL", localDatabase);
            localManager.GatewaysManager = this.LocalManager.GatewaysManager;
            gatewayCommandBase.LocalManager = localManager;
            gatewayCommandBase.Execute();
         }
         catch (Exception )
         {
//#if DEBUG
//            MessageBox.Show(e.ToString());
//#endif
         }


         return result;
      }



      internal override GatewayResult Execute()
      {
         Record();
         GatewayResult result = new GatewayResult();
         RTDataSource rtDataSource = GatewayAdapter.GetDataSource(DataSourceDefinition);
         //SerializeDatasource(DataSourceDefinition);
         //Serialize();
         if (rtDataSource == null)
         {
            rtDataSource = new RTDataSource(DataSourceDefinition);
            rtDataSource.GatewayAdapter = GatewayAdapter;
         }

         // TODO: Access (HDLINFO hdl_info_tbl_.datatbl)
         // TODO: Foreign keys
         // share & mode are ignored

         if (!rtDataSource.IsOpened)
         {
            try
            {
               DatabaseDefinition dbDefinition = (DatabaseDefinition)DbDefinition.Clone();
               UpdateDataBaseLocation(dbDefinition);

               result.ErrorCode = GatewayAdapter.Gateway.FileOpen(DataSourceDefinition, dbDefinition, FileName, Access,
                                                                  DbShare.Write, DbOpen.Normal, null);
            }
            catch (FileNotFoundException ex)
            {
               throw new ApplicationException("The SQLite database couldn't be opened.", ex);
            }
         }

         if (result.Success)
            rtDataSource.Open();
         SetErrorDetails(result);
         return result;
      }
   }
}
