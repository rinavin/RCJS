using System;
using System.Collections.Generic;
using System.Text;

namespace com.magicsoftware.richclient.data
{
   /// <summary>
   /// 
   /// </summary>
   interface IRecordsTable
   {
      int GetSize();
      Record GetRecByIdx(int idx);
      void RemoveAll();
   } 
}
