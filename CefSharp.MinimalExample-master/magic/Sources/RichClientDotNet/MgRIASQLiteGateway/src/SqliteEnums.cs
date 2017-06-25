using System;
using System.Collections.Generic;
using System.Text;

namespace MgSqlite.src
{

   /// <summary>
   ///  This file contains various enums required for SQLite Gateway.
   /// </summary>
   public enum Sql3Type
   {
      SQL3TYPE_EMPTY = 0,
      SQL3TYPE_NULL = 1,
      SQL3TYPE_I1 = 2,
      SQL3TYPE_I2 = 3,
      SQL3TYPE_I4 = 4,
      SQL3TYPE_I8 = 5,
      SQL3TYPE_UI1 = 6,
      SQL3TYPE_UI2 = 7,
      SQL3TYPE_UI4 = 8,
      SQL3TYPE_UI8 = 9,
      SQL3TYPE_R4 = 10,
      SQL3TYPE_R8 = 11,
      SQL3TYPE_BSTR = 12,
      SQL3TYPE_ERROR = 13,
      SQL3TYPE_BOOL = 14,
      SQL3TYPE_IUNKNOWN = 15,
      SQL3TYPE_DECIMAL = 16,
      SQL3TYPE_BYTES = 17,
      SQL3TYPE_STR = 18,
      SQL3TYPE_WSTR = 19,
      SQL3TYPE_NUMERIC = 20,
      SQL3TYPE_DATE = 21,
      SQL3TYPE_TIME = 22,
      SQL3TYPE_DATETIME = 23,
      SQL3TYPE_DBTIMESTAMP = 24,
      SQL3TYPE_DBDATE = 25,
      SQL3TYPE_DBTIME = 26,
      SQL3TYPE_ROWID = 27
   };

   public enum TypeAffinity
   {
      TYPE_AFFINITY_TEXT = 1,
      TYPE_AFFINITY_NUMERIC = 2,
      TYPE_AFFINITY_INTEGER = 3,
      TYPE_AFFINITY_REAL = 4,
      TYPE_AFFINITY_NONE = 5
   };

   public enum DateType
   {
      NORMAL_TYPE = 0,
      DATE_TO_DATE = 1,
      DATETIME_TO_CHAR = 2,
      DATETIME4_TO_CHAR = 3,
      DATE_TO_SQLCHAR = 4
   }

   public enum MagicFileType
   {
     MAGIC_DATA_FILE = ' ',
     MAGIC_SORT_FILE = 'S',
     MAGIC_TEMP_FILE = 'T',
   }

   public enum IndexingMode
   {
      REINDEX_CLOSE_MODE = 0,
      CREATE_MODE,       
      REINDEX_OPEN_MODE   
   }

   public enum DropObject
   {
      SQL3_DROP_TABLE = 1,
      SQL3_DROP_INDEX = 2,
      SQL3_DROP_VIEW  = 3,
      SQL3_DROP_PRMKEY = 4

   }

   public enum FundamentalDatatypes
   {
      SQLITE_INTEGER = 1,
      SQLITE_FLOAT = 2,
      SQLITE_TEXT = 3,
      SQLITE_BLOB = 4,
      SQLITE_NULL = 5
   }
}
