using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.tasks;

namespace com.magicsoftware.richclient.rt
{
   /// <summary>
   /// link factory
   /// </summary>
   internal class DataviewHeaderFactory
   {
      internal DataviewHeaderBase CreateDataviewHeaders(Task task, int tableIndex)
      {
         bool isTableLocal = task.IsTableLocal(tableIndex);
         DataviewHeaderBase dataviewHeader = (isTableLocal ? new LocalDataviewHeader(task, tableIndex) : (DataviewHeaderBase)new RemoteDataviewHeader(task) );
         return dataviewHeader;
      }

     
   }
}
