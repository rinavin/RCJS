using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.local.data.gateways;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.commands.ClientToServer;

namespace com.magicsoftware.richclient.local.data.commands
{
   class InitDataControlLocalViewsCommand : LocalDataViewCommandBase
   {
      public InitDataControlLocalViewsCommand(DataviewCommand command)
         : base(command)
      {

      }

      internal override ReturnResultBase Execute()
      {         
         List<IDataSourceViewDefinition> dataControlsSourceDefs = DataSourceDefinitionsBuilder.BuildDataControlSourceDefinitions(Task);
         GatewayResult result = LocalDataviewManager.CreateDataControlViews(dataControlsSourceDefs);

         return result;
      }

   }
}
