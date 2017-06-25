using com.magicsoftware.gatewaytypes.data;

namespace MgSqlite.src
{
   /// <summary>
   ///  This class holds data and information of single fld selected .It is used for fetching or binding data.
   /// </summary>
    public class Sql3SqlVar
   {
      internal string DataSourceType;
      internal string SqlName;

      internal Sql3Type SqlType;

      internal int SqlLen;
      internal int NullIndicator;
      internal int PartOfDateTime;  // '-1' - Normal Field, '-2' - Time part of Datetime, 'N' - idx of Time part for Date part of Datetime 
      internal int DataPrecision;
      internal int DataScale;

      internal DBField Fld;          // used to build a stmt with values.

      internal object SqlData;
#if !SQLITE_CIPHER_CPP_GATEWAY 
      internal object SqlData1; //h_sqldata
#endif

      internal DateType dateType;       // NORMAL Or DATETIME/DATETIME4 Type 

      internal bool IsMinRange;
      internal bool IsBlob;

      internal TypeAffinity typeAffinity;

      public void SQL3SqlvarTid ()
      {
         SqlType          = Sql3Type.SQL3TYPE_ROWID;
         DataSourceType   = "SQL3TYPE_ROWID";
         SqlLen           = SqliteConstants.SQL3_ROWID_LEN_EXTERNAL;
         typeAffinity     = TypeAffinity.TYPE_AFFINITY_INTEGER;
         NullIndicator    = 0;
         PartOfDateTime   = SqliteConstants.NORMAL_OF_DATETIME;
         IsBlob           = false;
         SqlName          = SqliteConstants.SQL3_ROWID_ST_A;
      }
   }
}
