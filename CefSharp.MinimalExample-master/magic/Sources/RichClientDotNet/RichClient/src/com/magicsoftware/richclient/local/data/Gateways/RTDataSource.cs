using com.magicsoftware.gatewaytypes.data;

namespace com.magicsoftware.richclient.local.data.gateways
{
   /// <summary>
   /// we need this class in GatewayAdapter for to manage opened data sources
   /// </summary>
   internal class RTDataSource
   {
      internal GatewayAdapter GatewayAdapter { get; set; }
      private DataSourceDefinition dataSourceDefinition;

      private int openCounter { get; set; } // will need it to know if we need to close the file
      internal bool IsOpened
      {
         get
         {
            return openCounter > 0;
         }
      }
      internal bool IsLast
      {
         get
         {
            return openCounter == 1;
         }
      }

      /// <summary>
      /// CTOR
      /// </summary>
      internal RTDataSource(DataSourceDefinition dataSourceDefinition)
      {
         this.dataSourceDefinition = dataSourceDefinition;
      }

      /// <summary>
      /// If it is a new DataSource adds it to the opened dictionary and increases the counter
      /// </summary>
      internal void Open()
      {
         if (!IsOpened)
            GatewayAdapter.AddDataSource(dataSourceDefinition, this);
         openCounter++;
      }

      /// <summary>
      /// Decreases the counter and remove from the dictionary if it was the last opened data source.
      /// </summary>
      internal void Close()
      {
         if (IsLast)
            GatewayAdapter.RemoveDataSource(dataSourceDefinition);
         openCounter--;
      }
   }
}
