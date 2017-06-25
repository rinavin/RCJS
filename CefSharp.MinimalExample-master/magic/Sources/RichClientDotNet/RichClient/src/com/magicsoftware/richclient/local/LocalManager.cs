using com.magicsoftware.richclient.local.data.gateways;
using com.magicsoftware.richclient.local.application.datasources.converter;

namespace com.magicsoftware.richclient.local
{
   /// <summary>
   /// Includes application definitions and dynamic parts (as GatewayManager)
   /// </summary>
   internal class LocalManager
   {
      // include all definitions of the local application
      internal ApplicationDefinitions ApplicationDefinitions { get; set; }
      internal GatewaysManager GatewaysManager { get;  set; }
      internal DataSourceConverter DataSourceConverter { get; set; }

      internal LocalManager()
      {
         ApplicationDefinitions = new ApplicationDefinitions();
         GatewaysManager = new GatewaysManager();
      }
   }
}
