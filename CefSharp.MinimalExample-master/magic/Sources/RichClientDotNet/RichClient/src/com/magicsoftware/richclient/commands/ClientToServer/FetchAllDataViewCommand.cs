using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.local.data;
using com.magicsoftware.richclient.data;

namespace com.magicsoftware.richclient.commands.ClientToServer
{
   //Delegate for on record fetch.
   internal delegate void OnRecordFetchDelegate(Record record);
   /// <summary>
   /// Command for fetching all the records.
   /// </summary>
   class FetchAllDataViewCommand : DataviewCommand
   {
      internal OnRecordFetchDelegate onRecordFetch;

      public FetchAllDataViewCommand()
         : base()
      {
         CommandType = DataViewCommandType.FetchAll;
      }

   }
}
