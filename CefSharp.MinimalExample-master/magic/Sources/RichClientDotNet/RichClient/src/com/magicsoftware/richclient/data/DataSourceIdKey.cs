using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.util;

namespace com.magicsoftware.richclient.data
{
   internal struct DataSourceIdKey
   {
      private int taskCtlIdx;
      private int realIdx;

      private const int PRIME_NUMBER = 37;
      private const int SEED = 23;

      internal DataSourceIdKey(int taskCtlIdx, int realIdx)
      {
         this.taskCtlIdx = taskCtlIdx;
         this.realIdx = realIdx;
      }
   }
}
