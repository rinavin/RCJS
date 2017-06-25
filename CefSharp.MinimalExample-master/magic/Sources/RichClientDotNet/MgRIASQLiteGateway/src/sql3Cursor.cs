using System;
using System.Collections.Generic;
using System.Text;

namespace MgSqlite.src
{
   /// <summary>
   ///  This class represents a single cursor operation read/insert/range.
   /// </summary>
   public class SQL3Cursor
   {
      internal string Name;
      internal bool   InUse;
      internal bool   IsStartPos;
      internal bool   IsRange;
      internal bool   DoOpen;
      internal bool   DoDummyFetchBlob;
      internal bool   DoDummyFetch;

      internal int    StartPosLevel;
      internal int    StmtIdx;            // Index in SQL3_stmt_tbl

      internal Sql3Sqldata InputSqlda;
      internal Sql3Sqldata OutputSqlda;   
   }
}
