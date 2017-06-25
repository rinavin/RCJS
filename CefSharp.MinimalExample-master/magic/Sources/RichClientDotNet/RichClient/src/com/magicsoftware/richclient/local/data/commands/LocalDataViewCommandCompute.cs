using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.local.data.gateways;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.commands.ClientToServer;
using System.Diagnostics;

namespace com.magicsoftware.richclient.local.data.commands
{
   /// <summary>
   /// compute operation
   /// </summary>
   class LocalDataViewCommandCompute : LocalDataViewCommandBase
   {
      /// <summary>
      /// record 
      /// </summary>
      internal IRecord Record { get; set; }

      private bool refreshSubforms { get; set; }

      public LocalDataViewCommandCompute(ComputeEventCommand command)
         : base(command)
      {
         refreshSubforms = command.Subforms;
      }


      internal override ReturnResultBase Execute()
      {
         GatewayResult result = new GatewayResult();

         TaskViews.RecordComputer.Compute(Record, false, false, true);
         if (refreshSubforms)
            Task.RefreshSubforms();

         return result;
      }

   }
}

