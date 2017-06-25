using System;
using System.Collections.Generic;
using System.Text;

namespace com.magicsoftware.richclient.local.data
{
   internal enum DataViewCommandType
   {
      Init, 
      Clear, 
      Prepare, 
      FirstChunk, 
      RecomputeUnit, 
      ExecuteLocalUpdates,
      InitDataControlViews,
      FetchAll,

      OpenTransaction,
      CloseTransaction,      
      SetTransactionState,
      AddUserRange,
      ResetUserRange,
      DbDisconnect,
      AddUserLocate,
      ResetUserLocate,
      AddUserSort,
      ResetUserSort,
      DataViewToDataSource,
      DbDelete,
      ControlItemsRefresh,
      SQLExecute
   };
}
