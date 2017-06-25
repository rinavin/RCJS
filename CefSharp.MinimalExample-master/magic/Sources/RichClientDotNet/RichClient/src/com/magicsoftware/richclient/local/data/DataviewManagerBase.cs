using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.richclient.local.data.gateways;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.data;

namespace com.magicsoftware.richclient.local.data
{
   /// <summary>
   ///  dataview manager base 
   /// </summary>
   internal abstract class DataviewManagerBase
   {
      internal Task Task { get; set;} // the task that owns the current data view.
      internal Transaction Transaction { get; set; }

      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="task"></param>
      internal DataviewManagerBase(Task task) 
      {
         this.Task = task;
      }

      /// <summary>
      /// !!
      /// </summary>
      /// <param name="command"></param>
      internal virtual ReturnResult Execute(IClientCommand command)
      {
          return new ReturnResult();
      }   

      internal virtual GatewayResult CreateDataControlViews(List<IDataSourceViewDefinition> dataControlSourceDefintions)
      {
         return new GatewayResult();
      }

      internal virtual int GetDbViewRowIdx()
      {
         int idx = ((DataView)Task.DataView).getCurrDBViewRowIdx();
         return idx;
      }
      
   }
}
