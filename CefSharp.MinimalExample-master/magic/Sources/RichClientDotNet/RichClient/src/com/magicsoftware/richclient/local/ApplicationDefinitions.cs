using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.local.application;
using com.magicsoftware.richclient.local.application.datasources;
using com.magicsoftware.richclient.local.application.Databases;

namespace com.magicsoftware.richclient.local
{
   /// <summary>
   /// include all Definitions about local Application 
   /// </summary>
   internal class ApplicationDefinitions
   {
      /// <summary>
      /// data for local 
      /// </summary>
      internal TaskDefinitionIdsManager TaskDefinitionIdsManager { get; private set; }
      internal OfflineSnippetsManager OfflineSnippetsManager { get; private set; }
      internal DataSourceDefinitionManager DataSourceDefinitionManager { get; private set; }
      internal DatabaseDefinitionsManager DatabaseDefinitionsManager { get; private set; }

      internal ApplicationDefinitions()
      {
         Init();
      }

      internal void Init()
      {
         TaskDefinitionIdsManager = new TaskDefinitionIdsManager();
         OfflineSnippetsManager = new OfflineSnippetsManager();
         DataSourceDefinitionManager = new DataSourceDefinitionManager();
         DatabaseDefinitionsManager = new DatabaseDefinitionsManager();
      }

   }
}
