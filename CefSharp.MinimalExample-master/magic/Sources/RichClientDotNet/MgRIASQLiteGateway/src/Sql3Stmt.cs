using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SQLite;

namespace MgSqlite.src
{
   /// <summary>
   ///  This class holds Stmt details for single operation.
   /// </summary>
   public class Sql3Stmt
   {
      internal int Idx;
      internal string Name;
      internal string Buf;
      internal string StmtWithValues;

      internal bool IsPrepared;
      internal bool InUse;
      internal bool IsOpen;

#if SQLITE_CIPHER_CPP_GATEWAY
      internal IntPtr StmtHandleUnmanaged;
#else
      internal SQLiteDataReader DataReader;
      internal SQLiteCommand sqliteCommand;     
#endif
   }
}
