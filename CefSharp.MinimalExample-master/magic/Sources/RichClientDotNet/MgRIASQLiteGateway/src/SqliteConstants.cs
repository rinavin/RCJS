using System;
using System.Collections.Generic;
using System.Text;
using SQL3_ROWID = System.Int32;

namespace MgSqlite.src
{
   /// <summary>
   ///  This file contains constants required for SQLite Gateway.
   /// </summary>
   public class SqliteConstants
   {
      public const int SQLITE_OK = 0;   /* Successful result */
      public const int RET_OK = 0;
      public const int SQLITE_ROW = 100;
      public const int SQLITE_NOTADB = 26;
      public const int SQL3_MAX_CHAR_LEN    = 8000;
      public const int SQL3_MAX_NCHAR_LEN   = 4000;
      public const int SQL3_MAX_READ_BUFFER = 65534;
      public const int SQL3_MAX_BINARY_LEN  = 8000;
      public const int MAX_FIELD_NAME_LEN   = 255;
      public const int NULL_PRIMARY_KEY     = 9999;
      public const int WRONG_USER_PWD       = 9996;
      public const int SERVER_NAME_ERROR    = 9995;
      public const int NO_DATABASE_NAME_GIVEN  = 9998;
      public const int UNABLE_TO_OPEN_DATABASE = 14;
      public const int ALREADY_CONNECTED_TO_A_DATABASE = 9997;
      public const int SQL3_OK = 0;
      public const int SQL3_SQL_NOTFOUND = 101;
      public const bool CREATE_MODE = true;
      public const bool REINDEX_CLOSE_MODE = false;
      public const int REINDEX_OPEN_MODE = 2;
      public const string DBMS_name = "SQLite";

      public const long SQL_NO_AUTOCOMMIT           = (1L<<0);        /* not in use yet */
      public const long SQL_NO_INSERT_IN_DSQL       = (1L<<1);
      public const long SQL_NO_DB_PREFIX_IN_DSQL    = (1L<<2);
      public const long SQL_NO_GW_SORT              = (1L<<3);
      public const long SQL_NO_CHG_RNG              = (1L<<4);
      public const long SQL_NO_LIMIT_TO             = (1L<<5);
      public const long SQL_OPEN_FILE_IN_TRAN       = (1L<<6);
      public const long SQL_NO_ROW_LEVEL_LOCKING    = (1L<<7);
      public const long SQL_NO_UPDATE_WITH_ORDER_BY = (1L<<8);
      public const long SQL_NO_UPDATE_WITH_JOIN     = (1L<<9);
      public const long SQL_NO_ROW_ID               = (1L<<10);
      public const long SQL_CAN_SORT_OUTER_JOIN     = (1L<<11);
      public const long SQL_SUPPORT_UNICODE         = (1L << 12);

      public const int SQL3_BUF_PER_FIELD = 128;
      public const int NULL_ARRAY_SIZE = -1;
      public const int NULL_IDX = -1;
      public const string IDENTITY_STR = "INTEGER PRIMARY KEY";

      public const char DB_INFO_SEP = '=';
      public const string FM_SRT_MARK = "SRT";
      public const string FM_TMP_MARK = "TMP";
      public const string SQL3_PHYSICAL_LOCK = "SQL_PHYSICAL_LOCKING";
      public const char NULL_CHAR = ((char)0xff);
      public const int NULL_SHRT = ((int)0xffff);
      public const Int32 SQL3_ROWID_LEN_EXTERNAL = sizeof(SQL3_ROWID);

      public const int TIME_OF_DATETIME = -2;
      public const int NORMAL_OF_DATETIME = -1;
      public const string SQL3_ROWID_ST_A = "rowid";
      public const string NO_DEFAULT_STR = "NO DEFAULT";
      public const int NULL_INDEX = 9999;
      public const string DB_STR_FORKEY = "FKY";
      public const int SQL3_MAX_OBJECTNAME = 260;  // should be equal to FILE_NAME_SIZE
      public const bool NO_PREFIX = false;
      public const int NULL_STMT = 9999;
      public const int NULL_CURSOR = 9999;
      public const char DB_SHARE_NONE = 'N';
      public const int SQLITE_DONE = 101;
      public const int NULL_LEVEL = 9999;
      public const int SQL3_REOPEN = -2;
      public const int SQL3_TRANS_ERROR = -4;

      public const int QUOTES = 1;
      public const int NO_QUOTES = 2;
      public const int QUOTES_TRUNC = 3;
      public const int NO_QUOTES_TRUNC = 4;
      public const int SQL3_TIME_LEN = 16;
      public const int INVALID_RANGE = -1;
      public const int START_POS_4_BUF = 15;      /* Tell the gateway to read the start pos values from the buf  */

      public const int ALL_RANGES_RNG = 0;
      public const int  ONLY_LINKS_RNG = 1;
      public const int ONLY_MAIN_RNG = 2;

      public const int DATE_RNG = 1;
      public const int TIME_RNG = 2;
      public const int DATETIME_RNG = 3;

      public const int SQL3_DATETIME_LEN = 40;

      public const int MIN_RNG = 1;
      public const int MAX_RNG = 2;
      public const int  MIN_AND_MAX_RNG = 3;
      public const int  MIN_EQ_MAX_RNG = 4;
      public const int  NULL_RNG = 5;
      public const int  MIN_AND_NULL_RNG = 6;
      public const int  NULL_AND_MAX_RNG = 7;
      public const int NO_RNG = 8;

      public const string TIME_FORMAT = "HH:MM:SS:000";
      public const string DATETIME_LITERAL = "DATETIME(?)";
      public const string TIME_LITERAL = "TIME(?)";
      public const string DATE_LITERAL = "DATE(?)";

      public const string TIME_NAME = "TIME({0}{1})";

      public const int DB_SQL_RANGE_VALUE_IND = 0x01;
      public const int NO_RECORDS_UPDATED = 10;
      public const int S_False = 1;
      public const string SQL3_DUP_INDEX_ERR = "not unique";
      public const string SQL3_DUP_INDEX_ERR_UNMANAGED = "UNIQUE constraint failed";
   }
}
