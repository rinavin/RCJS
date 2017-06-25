using System.Collections.Generic;
using com.magicsoftware.gatewaytypes.data;
using com.magicsoftware.gatewaytypes;
using util.com.magicsoftware.util;

namespace MgSqlite.src
{
   /// <summary>
   /// 
   /// </summary>
   public class GatewayCursor
   {
      SQLiteGateway SQLiteGateway;
      internal bool DirOrig;
      internal bool CheckForModifiedRow;
      internal bool InsertAllowed;
      internal bool DummyFetchWasDone;
      internal bool OuterJoin;
      internal bool JoinStmtBuiltWithInnerJoin;
      internal bool FirstFetchAfterOpen;
      internal bool InUse;

      internal List<bool> AllChkKeySegsInDataView;
      internal List<string> DbhPrefix;     // holds the prefix of all the tables of the cursors 
      internal List<int> SourceDbh;        // the number of the source dbh of the field in the cursor
      internal List<int> KeyArray;         // offset into output sqlda for view dbpos segs 
      internal List<int> NullIndicator;          // null   indicators used   by sqlvar->sqlind 
      internal List<string> StmtStartpos;

      internal Sql3Sqldata    Output;      // sqlda for select results 
      internal Sql3Sqldata    Input;       // sqlda for insert   
      internal Sql3Sqldata    Ranges;      // sqlda for ranges   
      internal Sql3Sqldata    SearchKey;   // sqlda for search key   
      internal Sql3Sqldata    Key;         // sqlda for retrieving key data 
      internal Sql3Sqldata    Update;      // sqlda for update   
      internal Sql3Sqldata    GcurrInput;  // sqlda for Gcurr Input
      internal Sql3Sqldata    GcurrOutput; // sqlda for Gcurr Output
      internal Sql3Sqldata    StartPos;    // sqlda for retrieval on full record
      
      internal DBKey PosKey;               // Key from   DBH->key table for poskey
      internal int   CRead;                // indexes to SQL3_CURSOR table
      internal int   CRange;
      internal int   CGcurr;
      internal int   CGkey;
      internal int   CInsert;
      internal int   SReadA;               // indexes to SQL3_STMT table
      internal int   sReadD;
      internal int   SRngA;
      internal int   SRngD;
      internal int   SStrt;
      internal int   SGCurr;
      internal int   SGCurrlock;
      internal int   SGKey;
      internal int   SInsert;
      internal int   SUpdate;
      internal int   SDelete;
      internal int   Rngs;              // # of   sqlvars   in range
      internal int   SqlRngs;           // # of   sqlvars   in Sql range 
      internal int   StrtposCnt;        // how many   startpos phrases 
      internal int   JoinRngs;          // the number of range parameters on link files during join 
      internal int   XtraSortkeyCnt;    // No of fields in the sortkey not in magic data view */
      internal int   XtraPoskeyCnt;
      internal int   NumOfBlobs;

      internal string StmtRanges;
      internal string StmtOrderBy;
      internal string StmtOrderByRev;
      internal string StmtInsert;
      internal string StmtDelete;
      internal string StmtUpdate;
      internal string StmtWhereKey;
      internal string StmtFields;
      internal string StmtKeyFields;
      internal string StmtExtraFields;
      internal string StmtAllTables;
      internal string StmtAllTablesUpdLock;
      internal string StmtAllTablesWithOtimizer;
      internal string StmtJoinCond;
      internal string StmtJoinRanges;
      internal string StmtSqlRng;
         
      internal byte[] DbPosBuf;

      internal DbPos LastPos;
      internal DbPos CurrPos;
      //internal List<BlobInfo> BlobInfo;

      /// <summary>
      /// 
      /// </summary>
      internal void CrsrInit(GatewayAdapterCursor dbCrsr, SQLiteGateway gtwyObj)
      {
         SQL3Dbd pSQL3Dbd = null;
         int     segs = 0;

         PosKey = null;

         InUse = true;
         XtraPoskeyCnt = 0;
         XtraSortkeyCnt = 0;

         SQLiteGateway = gtwyObj;

         /* assign the gateway cursor id to the DB_CRSR structure */

         SQLiteGateway.DbdTbl.TryGetValue(dbCrsr.Definition.DataSourceDefinition, out pSQL3Dbd);

         Logger.Instance.WriteDevToLog(string.Format("CrsrInit(): >>>>> GTWY cursor cnt = %d", SQLiteGateway.GatewayCursorTbl.Count));

         Output = null;
         Input = null;
         Ranges = new Sql3Sqldata(SQLiteGateway);
         
         StartPos = new Sql3Sqldata(SQLiteGateway);

         SearchKey = null;
         Key = null;
         Update = null;
         
         GcurrInput = null;
         GcurrOutput = null;
         StmtRanges = null;

         /* for cascading startpos */
         /* not direct sql */
         this.PosKey = SQLiteGateway.FindPoskey(dbCrsr.Definition.DataSourceDefinition);

         Sql3PrepareForTimeStamp(dbCrsr);

         if (pSQL3Dbd != null)
         {
            /* if there is a key */
            if (dbCrsr.Definition.Key != null)
               segs = dbCrsr.Definition.Key.Segments.Count + 1;

            /*--------------------------------------------------------------*/
            /* fix bug #210900 - when no key selected - use the poskey segs */
            /*--------------------------------------------------------------*/
            else if (this.PosKey != null)
            {
               segs = PosKey.Segments.Count;
            }
            else
               segs = 1;

            Logger.Instance.WriteDevToLog(string.Format("CrsrInit(): segs in startpos = {0}", segs));
            StmtStartpos = new List<string>(segs);

            for (int i = 0; i < segs; i++)
               StmtStartpos.Add(null);
         }

         StmtOrderBy = null;
         StmtOrderByRev = null;
         StmtInsert = null;
         StmtDelete = null;
         StmtUpdate = null;
         StmtWhereKey = null;
         StmtFields = null;
         StmtKeyFields = null;
         StmtExtraFields = null;
         StmtAllTables = null;

         JoinStmtBuiltWithInnerJoin = false;
         StmtAllTablesUpdLock = null;

         StmtAllTablesWithOtimizer = null;

         SourceDbh = null;
         DbhPrefix = null;
         StmtJoinCond = null;
         StmtJoinRanges = null;
         StmtSqlRng = null;
         /* created for optimizer hints in join tables but will be use always */
         StmtAllTablesWithOtimizer = null;

         
         StmtJoinCond = null;
         StmtJoinRanges = null;
         StmtSqlRng = null;
         OuterJoin = false;

         if (dbCrsr.CursorType != CursorType.Join)
         {

            NullIndicator = new List<int>();
            for (int i = 0; i < dbCrsr.Definition.DataSourceDefinition.Fields.Count; i++)
               NullIndicator.Add(0);
         }

         /* sql3_stmt structures */
         /* Stmt allocation will be done on demand */

         SReadA = SqliteConstants.NULL_STMT; /*  sql3_stmt_alloc ("ReadA", crsr_hdl);  */
         sReadD = SqliteConstants.NULL_STMT; /*  sql3_stmt_alloc ("ReadD", crsr_hdl);  */
         SRngA = SqliteConstants.NULL_STMT; /*  sql3_stmt_alloc ("RngA", crsr_hdl);   */
         SRngD = SqliteConstants.NULL_STMT; /*  sql3_stmt_alloc ("RngD", crsr_hdl);   */
         SStrt = SqliteConstants.NULL_STMT; /*  sql3_stmt_alloc ("Strt", crsr_hdl);   */
         SGCurr = SqliteConstants.NULL_STMT; /*  sql3_stmt_alloc ("Gcurr", crsr_hdl);  */
         SGCurrlock = SqliteConstants.NULL_STMT;
         SGKey = SqliteConstants.NULL_STMT; /*  sql3_stmt_alloc ("Gkey", crsr_hdl);   */
         SInsert = SqliteConstants.NULL_STMT; /*  sql3_stmt_alloc ("Ins", crsr_hdl);    */
         SUpdate = SqliteConstants.NULL_STMT; /*  sql3_stmt_alloc ("Upd", crsr_hdl);    */
         SDelete = SqliteConstants.NULL_STMT; /*  sql3_stmt_alloc ("Del", crsr_hdl);    */

         /* indexes to SQL3_CURSOR 's */
         /* Cusror allocation will also be done on demand */

         CRead = SqliteConstants.NULL_CURSOR; /* sql3_cursor_alloc ("Read", crsr_hdl);  */
         CRange = SqliteConstants.NULL_CURSOR; /* sql3_cursor_alloc ("Range", crsr_hdl); */
         CGcurr = SqliteConstants.NULL_CURSOR; /* sql3_cursor_alloc ("Gcurr", crsr_hdl); */
         CGkey = SqliteConstants.NULL_CURSOR; /* sql3_cursor_alloc ("Gkey", crsr_hdl);  */
         CInsert = SqliteConstants.NULL_CURSOR;

         LastPos = new DbPos(true);
         CurrPos = new DbPos(true);

         Logger.Instance.WriteDevToLog("CrsrInit(): <<<<< ");
      }

      /// <summary>
      /// 
      /// </summary>
      internal void Sql3PrepareForTimeStamp(GatewayAdapterCursor dbCrsr)
      {

      }
   }
}
