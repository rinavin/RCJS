using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SQLite;

namespace MgSqlite.src
{
   /// <summary>
   ///  This class represents a single connection to SQLite DB.
   /// </summary>
   public class SQL3Connection
   {
      internal string DbName;
      internal bool InTransaction;

#if SQLITE_CIPHER_CPP_GATEWAY      
      internal IntPtr ConnHandleUnmanaged;
      internal string DbPassword;
#else
      internal SQLiteConnection connectionHdl;      
      internal SQLiteException SqliteException;
      internal SQLiteTransaction Transaction;
      internal Encoding encoding;     
#endif

#if SQLITE_CIPHER_CPP_GATEWAY
      public SQL3Connection(string dbName, string dbPassword)
      {
         this.DbName = dbName;
         this.DbPassword = dbPassword;
      }
#else
      public SQL3Connection(string dbName)
      {
         this.DbName = dbName;
      }
#endif
   }
}
