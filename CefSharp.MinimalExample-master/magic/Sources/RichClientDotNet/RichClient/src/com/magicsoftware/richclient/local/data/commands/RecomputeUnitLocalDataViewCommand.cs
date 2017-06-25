using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.local.data.gateways;
using com.magicsoftware.richclient.local.data.view.RecordCompute;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.commands.ClientToServer;
using com.magicsoftware.richclient.data;

namespace com.magicsoftware.richclient.local.data.commands
{
   /// <summary>
   /// locate in query command
   /// </summary>
   internal class RecomputeUnitLocalDataViewCommand : LocalDataViewCommandBase
   {
      /// <summary>
      /// id of link dataview header to get
      /// </summary>
      internal RecomputeId UnitId { get; set; }

      /// <summary>
      /// record 
      /// </summary>
      internal IRecord Record { get; set; }

      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="command"></param>
      public RecomputeUnitLocalDataViewCommand(RecomputeUnitDataviewCommand command)
         : base(command)
      {
         UnitId = command.UnitId;
         
      }


      internal override ReturnResultBase Execute()
      {
         GatewayResult result = new GatewayResult();
         RecordComputer recordComputer = TaskViews.RecordComputer;
         recordComputer.RecomputeUnit(UnitId, Record);
         return result;
      }
   }
}
