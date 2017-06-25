using System;
using System.Security;
using System.Text;

namespace MgSqlite.src
{
   /// <summary>
   /// Represents the return values for sqlite_exec() and sqlite_step()
   /// </summary>
   internal enum SqliteError : int
   {
      /// <value>Successful result</value>
      Ok = 0,
      /// <value>SQL error or missing database</value>
      Error = 1,
      /// <value>An internal logic error in SQLite</value>
      Internal = 2,
      /// <value>Access permission denied</value>
      Perm = 3,
      /// <value>Callback routine requested an abort</value>
      Abort = 4,
      /// <value>The database file is locked</value>
      Busy = 5,
      /// <value>A table in the database is locked</value>
      Locked = 6,
      /// <value>A malloc() failed</value>
      Nomem = 7,
      /// <value>Attempt to write a readonly database</value>
      ReadOnly = 8,
      /// <value>Operation terminated by public const int interrupt()</value>
      Interrupt = 9,
      /// <value>Some kind of disk I/O error occurred</value>
      Ioerr = 10,
      /// <value>The database disk image is malformed</value>
      Corrupt = 11,
      /// <value>(Internal Only) Table or record not found</value>
      NOTFOUND = 12,
      /// <value>Insertion failed because database is full</value>
      Full = 13,
      /// <value>Unable to open the database file</value>
      CANTOPEN = 14,
      /// <value>Database lock protocol error</value>
      PROTOCOL = 15,
      /// <value>(Internal Only) Database table is empty</value>
      EMPTY = 16,
      /// <value>The database schema changed</value>
      SCHEMA = 17,
      /// <value>Too much data for one row of a table</value>
      TOOBIG = 18,
      /// <value>Abort due to contraint violation</value>
      Constraint = 19,
      /// <value>Data type mismatch</value>
      MISMATCH = 20,
      /// <value>Library used incorrectly</value>
      MISUSE = 21,
      /// <value>Uses OS features not supported on host</value>
      NOLFS = 22,
      /// <value>Authorization denied</value>
      AUTH = 23,
      /// <value>Auxiliary database format error</value>
      FORMAT = 24,
      /// <value>2nd parameter to sqlite_bind out of range</value>
      RANGE = 25,
      /// <value>File opened that is not a database file</value>
      NOTADB = 26,
      /// <value>sqlite_step() has another row ready</value>
      ROW = 100,
      /// <value>sqlite_step() has finished executing</value>
      DONE = 101
   }
}
