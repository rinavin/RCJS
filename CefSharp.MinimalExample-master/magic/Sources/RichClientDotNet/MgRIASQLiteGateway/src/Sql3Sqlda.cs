using System;
using System.Collections.Generic;
using com.magicsoftware.util;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.gatewaytypes.data;
using SQL3_CODE = System.Int32;
using util.com.magicsoftware.util;

namespace MgSqlite.src
{
   /// <summary>
   ///  This class holds the data and information required for SQLite DB. It creates a SqlVars for fetching the data from SQLite DB.
   ///  Also creates SqlVars to pass the data to SQLite DB for Insert/Update/Delete operations.
   /// </summary>
   public class Sql3Sqldata
   {
      internal SQLiteGateway  SQLiteGateway;
      internal string         name;

      internal int   Sqln;
      internal int   Sqld;
      internal List<Sql3SqlVar> SqlVars;

      /// <summary>
      ///  Constructor
      /// </summary>
      internal Sql3Sqldata(SQLiteGateway gatewayObj)
      {
         SQLiteGateway = gatewayObj;
      }

      /// <summary>
      ///  allocate no. of SqlVars
      /// </summary>
      public void SQL3SqldaAlloc(int numElems)
      {
         Logger.Instance.WriteDevToLog(string.Format("SQL3SqldaAlloc(): >>>>> number of elements = {0}", numElems));

         name = String.Format("SQLDA{0}", SQLiteGateway.SqldaNum);
         SQLiteGateway.SqldaNum++;

         Sqln = numElems;
         Sqld = numElems;

         SqlVars = new List<Sql3SqlVar>(numElems);
         for (int i = 0; i < numElems; i++) 
            SqlVars.Add(new Sql3SqlVar());

         Logger.Instance.WriteDevToLog(string.Format("SQL3SqldaAlloc(): <<<<< name - {0}", name));
      }

      /// <summary>
      ///  Free allocated SqlVars
      /// </summary>
      public SQL3_CODE SQL3SqldaFree()
      {
         SQL3_CODE retCode = SqliteConstants.RET_OK;

         if (SqlVars != null && SqlVars.Count == 0)
         {
            Logger.Instance.WriteDevToLog("SQL3SqldaFree(): >>>>> nothing to free");
            return retCode;
         }

         Logger.Instance.WriteDevToLog(string.Format("SQL3SqldaFree(): >>>>> sqlda name - {0}, elements - {1}", name, Sqln));
         
         if (SqlVars != null)
          SqlVars.Clear();

         Logger.Instance.WriteDevToLog(string.Format("SQL3SqldaFree(): <<<<< retcode - {0}", retCode));

         return retCode;
      }

      /// <summary>
      /// Prepare SqlVars to fetch the data.
      /// </summary>
      /// <param name="dbCrsr"></param>
      public void SQL3SqldaOutput(GatewayAdapterCursor dbCrsr)
      {
         GatewayCursor crsr;
         DBField        fld              = null;
         int            fldIdx           = 0;
         short          idx              = 0,
                        segIdx           = 0,
                        varIdx           = 0,
                        timeIdx;
         Sql3Field      sql3Field;
         DBKey          key              = null;
         DBSegment            seg              = null;
         DataSourceDefinition dbh = dbCrsr.Definition.DataSourceDefinition;
         SQL3Dbd        sql3Dbd        = null;
         String         fieldName        = null;

         SQLiteGateway.GatewayCursorTbl.TryGetValue(dbCrsr, out crsr);
         SQLiteGateway.DbdTbl.TryGetValue(dbh, out sql3Dbd);

         Logger.Instance.WriteDevToLog(string.Format("SQL3SqldaOutput(): >>>>> # of fields = {0}, sqlda - {1}", dbCrsr.Definition.FieldsDefinition.Count, name));
         
         sql3Field = new Sql3Field();
         
         // for all fields in the DB_CRSR field list 
         for (idx = 0; idx < dbCrsr.Definition.FieldsDefinition.Count; ++idx)
         {
            // get a pointer to the FLD
            fld = dbCrsr.Definition.FieldsDefinition[idx];
            fieldName = fld.DbName;
            
            if (fieldName.Length > SqliteConstants.MAX_FIELD_NAME_LEN)
            {
               sql3Field.Name = fieldName.Substring(0, SqliteConstants.MAX_FIELD_NAME_LEN);

               Logger.Instance.WriteDevToLog("SQL3SqldaOutput(): field name is too long, name trancated");
            }
            else
               sql3Field.Name = fieldName;

            // update the sql3_field struct for passing to SQL3SqlvarFill
            sql3Field.SqlVar = SqlVars[varIdx++];
            sql3Field.Fld = fld;
            sql3Field.GatewayAdapterCursor = dbCrsr;
            sql3Field.DataSourceDefinition = dbCrsr.Definition.DataSourceDefinition;

            sql3Field.AllowNull = fld.AllowNull;
            sql3Field.Storage = (FldStorage)fld.Storage;
            sql3Field.Whole = fld.Whole;
            sql3Field.Dec = fld.Dec;


            if (fld.Storage == FldStorage.DateString)
               // special case - direct SQL batch date fields magic sends len as 6 and should be 8
               sql3Field.FieldLen = 8;
            else
            {
               if (fld.IsBlob() &&
                  !SQLiteGateway.Sql3CheckDbtype (fld, "BINARY"))
                  sql3Field.SqlVar.IsBlob = true;

               sql3Field.FieldLen = fld.StorageFldSize();
               if (fld.Storage == FldStorage.AlphaZString)
                  sql3Field.FieldLen--;
               if (fld.IsUnicode())
               {
                  if (fld.Storage == FldStorage.UnicodeZString)
                     sql3Field.FieldLen -= sizeof(char);

                  //sql3_field.fieldLen = sql3_field.fieldLen / sizeof(Wchar);
               }
            }
            sql3Field.NullIndicator = crsr.NullIndicator[fldIdx];

            if (fld.PartOfDateTime != 0)
            {
               if (fld.Storage == FldStorage.TimeString)
                  sql3Field.PartOfDateTime = SqliteConstants.TIME_OF_DATETIME;
               else
               {
                  for (timeIdx = 0; timeIdx < dbCrsr.Definition.FieldsDefinition.Count; timeIdx++)
                  {
                     if (dbCrsr.Definition.FieldsDefinition[timeIdx].Isn == fld.PartOfDateTime)
                        break;
                  }
                  sql3Field.PartOfDateTime = timeIdx;
               }
            }
            else
               sql3Field.PartOfDateTime = SqliteConstants.NORMAL_OF_DATETIME;

            if (fld.IsBlob())
               sql3Field.SqlVar.IsBlob = true;

            Logger.Instance.WriteDevToLog(string.Format("SQL3SqldaOutput(): using sqlvar[{0}] for db_crsr field {1} length {2}", idx, sql3Field.Name, sql3Field.FieldLen));

            SQLiteGateway.SqlvarFill( ref sql3Field, false, false, false);
         }

         // add order by fields not in the db_crsr list if there is a key
         if (dbCrsr.Definition.Key != null && sql3Dbd != null && sql3Dbd.IsView)
         {
            key = dbCrsr.Definition.Key;
            
            // run thru the key segs
            
            for (idx = 0; idx < key.Segments.Count; idx++)
            {
               seg = key.Segments[idx];
               fld = seg.Field;
               // if seg is not in the magic view - add it
               if (!SQLiteGateway.Sql3IsDbCrsrField(dbCrsr, fld))
               {
                  // get a pointer to the FLD 
                  fieldName = fld.DbName;
                  sql3Field.Name = fieldName;

                  sql3Field.Storage = (FldStorage)fld.Storage;
                  sql3Field.Whole   = fld.Whole;
                  sql3Field.Dec     = fld.Dec;

                  Logger.Instance.WriteDevToLog(string.Format("SQL3SqldaOutput(): using sqlvar[%d] for extra field", varIdx));

                  sql3Field.SqlVar = SqlVars[varIdx++];
                  sql3Field.Fld = fld;
                  sql3Field.GatewayAdapterCursor = dbCrsr;
                  sql3Field.DataSourceDefinition = dbCrsr.Definition.DataSourceDefinition;
                  sql3Field.AllowNull = fld.AllowNull;
                  sql3Field.FieldLen = fld.StorageFldSize();

                  if (fld.Storage == FldStorage.AlphaZString)
                     sql3Field.FieldLen--;
                  if (fld.Storage == FldStorage.UnicodeZString)
                     sql3Field.FieldLen -= sizeof(char);

                  //TODO(Snehal): Handle it later. Now we dont have seg.fldIdx, only have seg.Field. Find the way to get correct fldIdx.
                  //sql3Field.NullBuf = crsr.NullBuf[seg.fldIdx];

                  if (fld.PartOfDateTime != 0)
                  {
                     if (fld.Storage == FldStorage.TimeString)
                        sql3Field.PartOfDateTime = SqliteConstants.TIME_OF_DATETIME;
                     else
                     {
                        for (timeIdx = 0; timeIdx < dbCrsr.Definition.FieldsDefinition.Count; timeIdx++)
                        {
                           if (dbCrsr.Definition.FieldsDefinition[timeIdx].Isn == fld.PartOfDateTime)
                              break;
                        }
                        sql3Field.PartOfDateTime = timeIdx;
                     }
                  }
                  else
                     sql3Field.PartOfDateTime = SqliteConstants.NORMAL_OF_DATETIME;

                  SQLiteGateway.SqlvarFill(ref sql3Field, false, false, false);
               }
            }
         }
         if (crsr.NumOfBlobs > 0)
         {
            //crsr.BlobInfo  = new List<BlobInfo> (crsr.NumOfBlobs); TODO
         }


         // If not batch , add position (TID or key)
         if (sql3Dbd != null)
         {
            if (!sql3Dbd.IsView)
            {
               // add one for the tid

               Logger.Instance.WriteDevToLog(string.Format("SQL3SqldaOutput(): using sqlvar[{0}] for TID", varIdx));

               sql3Field.SqlVar  = SqlVars[varIdx++];
               sql3Field.Name = SqliteConstants.SQL3_ROWID_ST_A;

               sql3Field.Storage = 0;
               sql3Field.Whole = 0;
               sql3Field.Dec = 0;
               sql3Field.GatewayAdapterCursor = dbCrsr;
               sql3Field.DataSourceDefinition = dbCrsr.Definition.DataSourceDefinition;
               sql3Field.AllowNull = false;
               sql3Field.FieldLen = SqliteConstants.SQL3_ROWID_LEN_EXTERNAL;
               sql3Field.PartOfDateTime = SqliteConstants.NORMAL_OF_DATETIME;

               if (dbCrsr.CursorType == CursorType.PartOfOuter)
                  sql3Field.NullIndicator = crsr.NullIndicator[varIdx - 1];
               else
                  sql3Field.NullIndicator = SQLiteGateway.SQL3NotNull;

               sql3Field.SqlVar.SqlType = Sql3Type.SQL3TYPE_ROWID;         
               sql3Field.SqlVar.IsBlob = false;
               sql3Field.SqlVar.DataSourceType = "SQL3TYPE_ROWID";
               sql3Field.SqlVar.typeAffinity = TypeAffinity.TYPE_AFFINITY_INTEGER;

               SQLiteGateway.SqlvarFill(ref sql3Field, false, false, false);
            }
            else
            {
               // view - add the db_pos key segments not in the db_crsr list
		         if (crsr.KeyArray == null)
                  SQLiteGateway.sql3MakeDbPosSegArray(dbCrsr);

		         key = crsr.PosKey;
		         

               // for all the dbpos key segments
               for (segIdx = 0; segIdx < key.Segments.Count; segIdx++)
               {
                  seg = key.Segments[segIdx];
                  // if the segment is not in the magic view
                  if (crsr.KeyArray[segIdx] >= dbCrsr.Definition.FieldsDefinition.Count)
                  {
                     // get a pointer to the FLD 
                     fld = seg.Field;

                     fieldName = fld.DbName;
                     sql3Field.Name = fieldName;

                     Logger.Instance.WriteDevToLog(string.Format("SQL3SqldaOutput(): using sqlvar[{0}] for dbpos field", varIdx));

                     sql3Field.Storage = (FldStorage)fld.Storage;
                     sql3Field.Whole = fld.Whole;
                     sql3Field.Dec = fld.Dec;
                     sql3Field.SqlVar = SqlVars[varIdx++];
                     sql3Field.Fld = fld;
                     sql3Field.GatewayAdapterCursor = dbCrsr;
                     sql3Field.DataSourceDefinition = dbCrsr.Definition.DataSourceDefinition;
                     sql3Field.AllowNull = fld.AllowNull;
                     sql3Field.FieldLen = fld.StorageFldSize();

                     if (fld.Storage == FldStorage.AlphaZString)
                        sql3Field.FieldLen--;
                     if (fld.Storage == FldStorage.UnicodeZString)
                        sql3Field.FieldLen -= sizeof(char);

                     //TODO(Snehal): Handle it later. Now we dont have seg.fldIdx, only have seg.Field. Find the way to get correct fldIdx.
                     //sql3Field.NullBuf = crsr.NullBuf[seg.fldIdx];

                     if (fld.PartOfDateTime != 0)
                     {
                        if (fld.Storage == FldStorage.TimeString)
                           sql3Field.PartOfDateTime = SqliteConstants.TIME_OF_DATETIME;
                        else
                           sql3Field.PartOfDateTime = varIdx;
                     }
                     else
                        sql3Field.PartOfDateTime = SqliteConstants.NORMAL_OF_DATETIME;

                     SQLiteGateway.SqlvarFill(ref sql3Field, false, false, false);
                  }
               }
            }
         }

         Logger.Instance.WriteDevToLog(string.Format("SQL3SqldaOutput(): <<<<< idx = {0}", varIdx));

         return;

      }

      /// <summary>
      /// Prepase SqlVars to pass the data to SQLite DB for various operations like Insert/Update/Delete.
      /// </summary>
      /// <param name="dbCrsr"></param>
      internal void SQL3SqldaInput(GatewayAdapterCursor dbCrsr, bool isPrepared)
      {
         GatewayCursor crsr;
         DBField fld = null;
         short          idx      = 0,
                        varIdx   = 0;
         Sql3Field      sql3Field;
         DataSourceDefinition dbh = dbCrsr.Definition.DataSourceDefinition;
         String         fieldName = String.Empty;
         SQL3Dbd sql3Dbd;
         SQL3Connection sql3Connection = null;
         Sql3Stmt       sql3Stmt  = null;

         SQLiteGateway.GatewayCursorTbl.TryGetValue(dbCrsr, out crsr);
         SQLiteGateway.DbdTbl.TryGetValue(dbh, out sql3Dbd);
         sql3Connection = SQLiteGateway.ConnectionTbl[sql3Dbd.DatabaseName];
         sql3Stmt = SQLiteGateway.StmtTbl[crsr.SInsert];

         Logger.Instance.WriteDevToLog(string.Format("SQL3SqldaInput(): >>>>> # of fields = {0}, sqlda - {1}", dbCrsr.Definition.FieldsDefinition.Count, name));

         sql3Field = new Sql3Field();
         
         // for all fields in the DB_CRSR field list 
         for (idx = 0; idx < dbCrsr.Definition.FieldsDefinition.Count; ++idx)
         {
            // get a pointer to the FLD
            fld = dbCrsr.Definition.FieldsDefinition[idx];
            if ((fld.Storage == FldStorage.TimeString) &&
                (fld.PartOfDateTime != 0))
               continue;

            // not to put IDENTITY column name into INSERT statement 
            if (SQLiteGateway.Sql3CheckDbtype(fld, SqliteConstants.IDENTITY_STR))
               continue;

            // Skip TIMESTAMP fld in an Insert stmt as we cannot insert an explicit value into it.
            if (SQLiteGateway.Sql3CheckDbtype(fld, "TIMESTAMP"))
               continue;

            if (!dbCrsr.Definition.IsFieldUpdated[idx])
               continue;

            
            sql3Field.SqlVar = SqlVars[varIdx++];

            if(!isPrepared)
            {
               fieldName = fld.DbName;
               sql3Field.Name = fieldName;

               // update the sql3_field struct for passing to SQL3SqlvarFill 
               sql3Field.Fld = fld;
               sql3Field.GatewayAdapterCursor = dbCrsr;
               sql3Field.DataSourceDefinition = dbCrsr.Definition.DataSourceDefinition;

               sql3Field.AllowNull = fld.AllowNull;
               sql3Field.Storage = (FldStorage)fld.Storage;
               sql3Field.Whole = fld.Whole;
               sql3Field.Dec = fld.Dec;

               if (fld.Storage == FldStorage.DateString)
                  // special case - direct SQL batch date fields magic sends len as 6 and should be 8
                  sql3Field.FieldLen = 8;
               else
               {
                  sql3Field.FieldLen = fld.StorageFldSize();
                  if (fld.Storage == FldStorage.AlphaZString)
                     sql3Field.FieldLen--;
                  if (fld.Storage == FldStorage.UnicodeZString)
                     sql3Field.FieldLen -= sizeof(char);

                  if (sql3Field.IsBlob())
                  {
                     if(!dbCrsr.CurrentRecord.IsNull(idx))
                     {
                        GatewayBlob blobType = (GatewayBlob)(dbCrsr.CurrentRecord.GetValue(idx));
                        sql3Field.FieldLen = (int)blobType.BlobSize;
                     }
                     else
                     {
                        sql3Field.FieldLen = 0;
                     }
                  }
               }

               sql3Field.NullIndicator = crsr.NullIndicator[idx];

               sql3Field.PartOfDateTime = SqliteConstants.NORMAL_OF_DATETIME;

               Logger.Instance.WriteDevToLog(string.Format("SQL3SqldaInput(): using sqlvar[{0}] for db_crsr field {1} length {2}",
                                             idx, sql3Field.Name, sql3Field.FieldLen));

               SQLiteGateway.SqlvarFill(ref sql3Field, true, true, true);
            }
            else
            {
               sql3Field.SqlVar.NullIndicator = crsr.NullIndicator[idx];
            }
            
         }

         if (!sql3Dbd.IsView)
         {
            // add one for the tid
            Logger.Instance.WriteDevToLog(string.Format("SQL3SqldaInput(): using sqlvar[{0}] for TID", varIdx));

            SqlVars[varIdx++].SQL3SqlvarTid();
         }

         Logger.Instance.WriteDevToLog(string.Format("SQL3SqldaInput(): <<<<< idx = {0}", varIdx));

         return;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="db_crsr"></param>
      /// <param name="whNullbuf"></param>
      /// <param name="prepared"></param>
      /// <returns></returns>
      public SQL3_CODE SQL3SqldaUpdate(GatewayAdapterCursor dbCrsr, int []whNullbuf, bool prepared)
      {
         GatewayCursor  crsr;
         SQL3Dbd        pSQL3Dbd;
         DBField        fld;
         int            idx = 0,
                        varIdx = 0;

         string         fieldName  = string.Empty;
         Sql3Field      sql3_field = new Sql3Field();
         DataSourceDefinition dbh = dbCrsr.Definition.DataSourceDefinition;
         SQL3_CODE      errcode     = SqliteConstants.SQL3_OK; 
         bool           overflow     = true;
         int            blobIdx     = 0;
         GatewayBlob     blobTypes;
         bool           dateFl;
         string         numeric = string.Empty; // 1 for the Decimal place + 1 for '\0'
         int            timeIdx = 0;
         string         fullDate;

         SQLiteGateway.GatewayCursorTbl.TryGetValue(dbCrsr, out crsr);
         SQLiteGateway.DbdTbl.TryGetValue(dbCrsr.Definition.DataSourceDefinition, out pSQL3Dbd);

         Logger.Instance.WriteDevToLog("SQL3SqldaUpdate(): >>>>>");

         /* for all fields in db_crsr field list */
         for (idx = 0; idx < dbCrsr.Definition.FieldsDefinition.Count; ++idx)
         {
            fld = dbCrsr.Definition.FieldsDefinition[idx];

            if (!dbCrsr.Definition.IsFieldUpdated[idx])
            {
               if (fld.IsBlob())
                  blobIdx++;
               continue;
            }

            if (fld.AllowNull && dbCrsr.CurrentRecord.IsNull(idx))
               continue;

            /* not to put IDENTITY column name into UPDATE statement */
            if (SQLiteGateway.Sql3CheckDbtype (fld, SqliteConstants.IDENTITY_STR))
               continue;

            if ((fld.Storage == FldStorage.DateString && SQLiteGateway.Sql3DateType (dbh, fld) != DateType.DATE_TO_SQLCHAR) || 
                (fld.Storage == FldStorage.TimeString && SQLiteGateway.Sql3DateType (dbh, fld) == DateType.DATE_TO_DATE))
               dateFl = true;
            else
               dateFl = false;

      /**/
		      sql3_field.SqlVar     = SqlVars[varIdx++];
            if (! prepared)
            {
               fieldName = fld.DbName;
               sql3_field.Name = fieldName;

               Logger.Instance.WriteDevToLog(string.Format("SQL3SqldaUpdate(): using sqlvar[{0}] for {1}", varIdx, fieldName));

               sql3_field.FieldLen = fld.StorageFldSize();
			      if (fld.Storage == FldStorage.AlphaZString)
				      sql3_field.FieldLen --;
			      sql3_field.GatewayAdapterCursor   = dbCrsr;
               sql3_field.Whole      = fld.Whole;
               sql3_field.Dec        = fld.Dec;
			      sql3_field.AllowNull  = fld.AllowNull;
			      sql3_field.Storage    = fld.Storage;
			      sql3_field.NullIndicator   = crsr.NullIndicator[idx];
            }
            else
               sql3_field.SqlVar.NullIndicator = crsr.NullIndicator[idx];

		      sql3_field.DataSourceDefinition = dbCrsr.Definition.DataSourceDefinition;
		      sql3_field.Fld    = fld;
            sql3_field.Buf    = dbCrsr.CurrentRecord.GetValue(idx);
            sql3_field.PartOfDateTime = SqliteConstants.NORMAL_OF_DATETIME;
      /**/
      //
            //Fill up the sqlvar for blob.
            //Also the updated buffer will be copied here into sqlvar->sqldata as we are going to bind the blob
            //directly with the update statement.
            if (fld.IsBlob() && !dbCrsr.CurrentRecord.IsNull(idx)) 
            {
               blobTypes = (GatewayBlob)dbCrsr.CurrentRecord.GetValue(idx);

               sql3_field.FieldLen = blobTypes.BlobSize;

               if (!prepared)
               {
                  SQLiteGateway.SqlvarFill(ref sql3_field, true, true, true);
               }

               sql3_field.SqlVar.SqlData = blobTypes.Blob;
               blobIdx++;
               continue;
            }

            if (fld.PartOfDateTime != 0)
            {
               if (fld.Storage == FldStorage.DateString)
               {
                  for (timeIdx = 0; timeIdx < dbCrsr.Definition.FieldsDefinition.Count; timeIdx++)
                  {
                     if (dbCrsr.Definition.FieldsDefinition[timeIdx].Isn == fld.PartOfDateTime)
                        break;
                  }

                  sql3_field.PartOfDateTime = timeIdx;
               }
               else
                  sql3_field.PartOfDateTime = SqliteConstants.TIME_OF_DATETIME;
            }

            if (dateFl)
            {
               if (! prepared)
               {
                  SQLiteGateway.SqlvarFill (ref sql3_field, true, true, false);            
               }
               if (fld.PartOfDateTime != 0)
               {

                  SQLiteGateway.Sql3DateTimeToInternal ((string)sql3_field.Buf, (string)dbCrsr.CurrentRecord.GetValue(timeIdx), out fullDate, fld.StorageFldSize());
               }
               else
               {
                  if (fld.Storage == FldStorage.TimeString)
                  {
                     SQLiteGateway.Sql3TimeToInternal ((string)sql3_field.Buf, out fullDate);
                  }
                  else
                  {
                     SQLiteGateway.Sql3DateToInternal((string)sql3_field.Buf, out fullDate, fld.StorageFldSize());
                  }
               }

               sql3_field.SqlVar.SqlData = fullDate;
            }
            else
            {
               SQLiteGateway.SqlvarValFill (sql3_field.SqlVar, sql3_field, varIdx - 1, prepared, SqliteConstants.QUOTES_TRUNC);

               if (dbCrsr.Definition.FieldsDefinition[idx].DiffUpdate == 'Y')
               {
                  if (sql3_field.SqlVar.SqlType == Sql3Type.SQL3TYPE_NUMERIC || sql3_field.SqlVar.SqlType == Sql3Type.SQL3TYPE_DECIMAL)
                  {
                     numeric = (string)sql3_field.Buf;
                     
                     numeric.Trim();
                     
                     for (int i = 0; i < numeric.Length; i++)
                        if (numeric[i] != '0')
                           overflow = false;   
                     if (overflow)
                     {
                        SQLiteGateway.LastErr = "SQL3 Gateway: An Arithmetic Overflow Occured in UPDATE operation.";
                        
                     }
                  }
               }
            }

            if (errcode != SqliteConstants.SQL3_OK)
            {
               Logger.Instance.WriteToLog(string.Format("SQL3SqldaUpdate(): <<<<< idx = {0}, Arithmetic OverFlow", varIdx), true);
               return errcode;
            }
         }

         varIdx += crsr.Update.SQL3SqldaFromDbpos(dbCrsr, varIdx, dbCrsr.Definition.CurrentPosition, false, prepared);

         Logger.Instance.WriteDevToLog(string.Format("SQL3SqldaUpdate(): <<<<< sqlvars = {0}", varIdx));

         return errcode;
      }

      public int SQL3SqldaFromAllView(GatewayAdapterCursor dbCrsr, int sqlvarIdx, int []whNullBuf,
                                      bool withnull, bool prepared)
      {
         CursorDefinition     dbd    = dbCrsr.Definition;
         DataSourceDefinition dbh   = dbCrsr.Definition.DataSourceDefinition;;
         DBField              fld;
         DBKey                key = null;
         GatewayCursor        crsr;
         SQL3Dbd              pSQL3_dbd ;
         int                  idx ,
                              seg_idx,
                              time_idx = 0;
         string               name = string.Empty;
         bool                 []field_in_pos;
         string               full_date;
         bool                 date_fl;
         Sql3Field            sql3_field = new Sql3Field();
         int                  cnt            = 0;

         Logger.Instance.WriteDevToLog(string.Format("SQL3SqldaFromAllView(): >>>>> sqlvarIdx = {0}", sqlvarIdx));

         /* allocate an indications array for adding only one time the fields which are in the
            db_pos and also in the data view */

         SQLiteGateway.GatewayCursorTbl.TryGetValue(dbCrsr, out crsr);
         SQLiteGateway.DbdTbl.TryGetValue(dbh, out pSQL3_dbd);
         field_in_pos = new bool[dbCrsr.Definition.FieldsDefinition.Count];
   
         if (pSQL3_dbd.IsView == true)
         {
            if (crsr.KeyArray == null)
            {
               SQLiteGateway.Sql3MakeDbpoSegArray(dbCrsr);
            }

            key = crsr.PosKey;
            /* for all the segs in the dbpos key */
            for (seg_idx = 0; seg_idx < key.Segments.Count; seg_idx++)
            {
               /* if the segment is in the magic view */
               if (crsr.KeyArray[seg_idx] < dbCrsr.Definition.FieldsDefinition.Count)
                  field_in_pos[crsr.KeyArray[seg_idx]] = true;
            }
         }
         // WHERE CLAUSE : 1. Include the DbPos fields first.
         //cnt = SQL3SqldaFromDbpos1 (db_crsr, sqlda, sqlvar_idx, db_crsr->curr_pos, withnull, prepared);

         // WHERE CLAUSE : 2. Include the remaining fields (other than DbPos) depending upon del_upd_mode strategy.
         //if (db_crsr->del_upd_mode != UPD_POS_CHECK)
         {
            /* add the data view fields that are not in the db_pos */
            for (idx = 0; idx < dbCrsr.Definition.FieldsDefinition.Count; idx++)
            {
               /* The field should not be included in where clause if ,
                  1. Delete/Update mode is Position & Updated Fields but , fld_update is FALSE 
                  2. fld_update is TRUE but Update style of Field is Differential*/
               if (//(db_crsr->del_upd_mode == UPD_CHANGED_CHECK && ! db_crsr.Definition.IsFieldUpdated[idx]) ||
                   (dbCrsr.Definition.IsFieldUpdated[idx] && dbCrsr.Definition.DifferentialUpdate[idx]))
                  continue ;

               fld = dbCrsr.Definition.FieldsDefinition[idx];
               if (fld.AllowNull && dbCrsr.OldRecord.GetValue(idx) == null)
               {
                  whNullBuf[idx] = 0;
                  continue;
               }

               /* if the field isn't in the db_pos and it isn't a long field */
               if ((! field_in_pos[idx])) //&& ! Sql3IsLongFld (dbd, fld_idx))
               {
                  if ((fld.Storage == FldStorage.DateString && SQLiteGateway.Sql3DateType (dbh, fld) != DateType.DATE_TO_SQLCHAR) ||
                      (fld.Storage == FldStorage.TimeString && SQLiteGateway.Sql3DateType(dbh, fld) == DateType.DATE_TO_DATE))
                     date_fl = true;
                  else
                     date_fl = false;

				      sql3_field.SqlVar     = SqlVars[sqlvarIdx++];
                  if (!prepared)
                  {
                     name = fld.DbName;
                     sql3_field.FieldLen = fld.StorageFldSize();
                     if (fld.Storage == FldStorage.AlphaZString)
                        sql3_field.FieldLen--;

                     sql3_field.Storage = fld.Storage;
                     sql3_field.Whole = fld.Whole;
                     sql3_field.Dec = fld.Dec;
                     sql3_field.AllowNull = fld.AllowNull;
                     sql3_field.GatewayAdapterCursor = dbCrsr;
                     sql3_field.NullIndicator = whNullBuf[idx];
                  }
                  else
                  {
                     sql3_field.SqlVar.NullIndicator = whNullBuf[idx];
                  }

				      sql3_field.DataSourceDefinition        = dbCrsr.Definition.DataSourceDefinition;
				      sql3_field.Fld    = fld;
                  sql3_field.Buf = dbCrsr.OldRecord.GetValue(idx);

                  if (fld.PartOfDateTime > 0)
                  {
                     if (fld.Storage == FldStorage.DateString)
                     {
                        for (time_idx = 0; time_idx < dbCrsr.Definition.FieldsDefinition.Count; time_idx++)
                        {
                           if (dbCrsr.Definition.FieldsDefinition[time_idx].Isn == fld.PartOfDateTime)
                              break;
                        }
                        sql3_field.PartOfDateTime = time_idx;
                     }
                     else
                     {
                        sql3_field.PartOfDateTime = SqliteConstants.TIME_OF_DATETIME;
                     }
                  }
                  else
                  {
                     sql3_field.PartOfDateTime = SqliteConstants.NORMAL_OF_DATETIME;
                  }
                  if (date_fl)
                  {
                     if (!prepared)
                     {
                        SQLiteGateway.SqlvarFill(ref sql3_field, true, true, false);
                     }
                     if (fld.PartOfDateTime != 0)
                     {
                        SQLiteGateway.Sql3DateTimeToInternal ((string)dbCrsr.OldRecord.GetValue(idx), (string)dbCrsr.OldRecord.GetValue(time_idx), out full_date, fld.StorageFldSize());
                        sql3_field.SqlVar.SqlData = full_date;
                     }
                     else
                     {
                        if (fld.Storage == FldStorage.TimeString)
                        {  

                           SQLiteGateway.Sql3TimeToInternal ((string)dbCrsr.OldRecord.GetValue(idx), out full_date);
                           sql3_field.SqlVar.SqlData = full_date;
                        }
                        else
                        {
                           SQLiteGateway.Sql3DateToInternal((string)dbCrsr.OldRecord.GetValue(idx), out full_date, fld.StorageFldSize()); ;
                           sql3_field.SqlVar.SqlData = full_date;
				               cnt++;
                        }
                     }
                  }
                  else
                  {
                     SQLiteGateway.SqlvarValFill (sql3_field.SqlVar, sql3_field, sqlvarIdx - 1, prepared, SqliteConstants.QUOTES_TRUNC);
                  }

				      cnt++;
               }
            } // for loop
         } // if (db_crsr->del_upd_mode != UPD_POS_CHECK)

         Logger.Instance.WriteDevToLog(string.Format("SQL3SqldaFromAllView(): <<<<< sqlvar cnt = {0}", cnt));
         return cnt;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="dbCrsr"></param>
      public int SQL3SqldaAllRanges(GatewayAdapterCursor dbCrsr, GatewayCursor crsr, bool prepared)
      {
         int cnt;

         if (dbCrsr.CursorType != CursorType.Join || !crsr.OuterJoin)
            cnt = SQL3SqldaRange(dbCrsr, SqliteConstants.ALL_RANGES_RNG, prepared);
         else
         {
            cnt = SQL3SqldaRange(dbCrsr, SqliteConstants.ONLY_LINKS_RNG, prepared);
            cnt += SQL3SqldaRange(dbCrsr, SqliteConstants.ONLY_MAIN_RNG, prepared);
         }

         return (cnt);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="dbCrsr"></param>
      /// <param name="crsrRngType"></param>
      /// /// <param name="prepared"></param>
      public int SQL3SqldaRange(GatewayAdapterCursor dbCrsr, int crsrRngType, bool prepared)
      {
         GatewayCursor        crsr;
         DataSourceDefinition dbh = dbCrsr.Definition.DataSourceDefinition;
         List<RangeData>      ranges = dbCrsr.Ranges;
         RangeData            range = null;
         RangeData            dateRange = null;
         RangeData            timeRange = null;
         Sql3Field            sql3Field = new Sql3Field();
         DBField              fld;
         int                  len = 0;
         int                  sqlvar_idx = 0;
         string               name = string.Empty;
         string               minFullDate = string.Empty;
         string               maxFullDate = string.Empty;
         int                  fldNum;
         string               maxOrg = string.Empty;
         bool                 dateFl;
         int                  datetimeRange = SqliteConstants.DATE_RNG;
         int                  dtIdx;
         char                 rangeType;
         char                 timeRangeType = SqliteConstants.NULL_CHAR;
         bool                 valueSet,
                              minDate,
                              maxDate;
         string               valMin;
         string               valMax;
         short                cnt = 0;
         bool                 isTimeRange = false;
         bool                 slctModified = false;
         bool                 isDBTypeDBTime = false;

         SQLiteGateway.GatewayCursorTbl.TryGetValue(dbCrsr, out crsr);

         Logger.Instance.WriteDevToLog("SQL3SqldaRange(): >>>>>");

         /* run through all the ranges */
         for (int idx = 0; idx < dbCrsr.Ranges.Count; ++idx)
         {
            range = ranges[idx];
            fld = dbCrsr.Definition.FieldsDefinition[range.FieldIndex];

            datetimeRange = SqliteConstants.DATE_RNG;

            if (fld.PartOfDateTime != 0)
            {
               dateFl = true;
               if (fld.Storage == FldStorage.DateString)
               {
                  datetimeRange = SqliteConstants.DATETIME_RNG;
                  if (range.DatetimeRangeIdx> 0)
                  {
                     timeRange = dbCrsr.Ranges[range.DatetimeRangeIdx - 1];

                     if (range.Min.Type == RangeType.RangeMinMax && !range.Min.Discard)
                     {
                        if (timeRange.Min.Type == RangeType.RangeParam || timeRange.Max.Type == RangeType.RangeParam)
                        {
                           if (!timeRange.Min.Discard && timeRange.Max.Discard)
                           {
                              range.Min.Type = range.Max.Type = RangeType.RangeParam;
                              slctModified = true;
                           }
                        }
                     }
                  }
               }
               else
               {
                  if (range.DatetimeRangeIdx == 0)
                  {
                     datetimeRange = SqliteConstants.TIME_RNG;
                  }
                  else if (fld.Storage == FldStorage.TimeString)
                  {

                     isTimeRange = false;
                     for (dtIdx = 0; dtIdx < dbCrsr.Ranges.Count; ++dtIdx)
                     {
                        dateRange = dbCrsr.Ranges[dtIdx];
                        if (dbCrsr.Definition.FieldsDefinition[dateRange.FieldIndex].Isn == fld.PartOfDateTime)
                        {
                           if ((dateRange.Min.Type == RangeType.RangeNoVal && dateRange.Min.Type == RangeType.RangeParam) ||
                               (dateRange.Max.Type == RangeType.RangeNoVal && dateRange.Max.Type == RangeType.RangeParam))
                           {
                              datetimeRange = SqliteConstants.TIME_RNG;
                              isTimeRange = true;
                              break;
                           }
                        }
                     }
                     if (isTimeRange == false)
                        continue;
                  }
                  else
                     continue;
               }
            }

            if (dbCrsr.CursorType == CursorType.Join)
            {
               // when building ranges for links and if the field belongs to main file, skip it
               fldNum = SQLiteGateway.Sql3GetDbCrsrIndex(dbCrsr, fld);
               if (crsrRngType == SqliteConstants.ONLY_LINKS_RNG && crsr.SourceDbh[fldNum] == 0 ||
                   crsrRngType == SqliteConstants.ONLY_MAIN_RNG && crsr.SourceDbh[fldNum] != 0)
               {
                  if (slctModified)
                  {
                     range.Min.Type = RangeType.RangeMinMax; //restore min_type
                     range.Max.Type = RangeType.RangeMinMax; //restore max_type
                  }
                  continue;
               }
            }

            if ((fld.Storage == FldStorage.DateString && SQLiteGateway.Sql3DateType(dbh, fld) != DateType.DATE_TO_SQLCHAR) ||
                (fld.Storage == FldStorage.TimeString && SQLiteGateway.Sql3DateType(dbh, fld) == DateType.DATE_TO_DATE))
            {
               dateFl = true;
               if (fld.PartOfDateTime == 0)
                  datetimeRange = SqliteConstants.DATE_RNG;
            }
            else
            {
               if (fld.PartOfDateTime > 0 && fld.Storage == FldStorage.TimeString && datetimeRange == SqliteConstants.TIME_RNG)
                  dateFl = true;
               else
                  dateFl = false;

            }

            if (fld.DbType == "TIME")
               isDBTypeDBTime = true;

            len = fld.StorageFldSize();
            valueSet = false;
            minDate = false;
            maxDate = false;
            valMin = null;
            valMax = null;

            if (!prepared)
            {
               name = fld.DbName;
               sql3Field.Name = name;

               sql3Field.FieldLen = fld.StorageFldSize();
               if (fld.Storage == FldStorage.AlphaZString)
                  sql3Field.FieldLen--;

               sql3Field.Storage = (FldStorage)fld.Storage;
               sql3Field.Whole = fld.Whole;
               sql3Field.Dec = fld.Dec;
               sql3Field.AllowNull = fld.AllowNull;
               sql3Field.GatewayAdapterCursor = dbCrsr;
               sql3Field.NullIndicator = SQLiteGateway.SQL3NotNull;
               sql3Field.PartOfDateTime = SqliteConstants.NORMAL_OF_DATETIME;
            }

            sql3Field.DataSourceDefinition = dbCrsr.Definition.DataSourceDefinition;
            sql3Field.Fld = fld;

            if (dbh.CheckMask(DbhMask.BinaryTableMask))
            {
               if (fld.Attr == (char)StorageAttributeType.Alpha)
               {
                  if (range.Max != null)
                  {
                     maxOrg = (string)range.Max.Value.Value;
                     SQLiteGateway.SQL3ChangeToMaxSortChar((string)range.Max.Value.Value, len);
                  }
               }
            }

            if (dateFl)
            {
               switch (datetimeRange)
               {
                  case SqliteConstants.DATE_RNG:
                     if (range.Min.Value.Value != null && !range.Min.Value.IsNull)
                     {
                        if (fld.Storage == FldStorage.TimeString)
                        {
                           SQLiteGateway.Sql3TimeToInternal((string)range.Min.Value.Value, out minFullDate);
                        }
                        else
                        {
                           SQLiteGateway.Sql3DateToInternal((string)range.Min.Value.Value, out minFullDate, len);
                        }
                     }

                     if (range.Max.Value.Value != null && !range.Max.Value.IsNull)
                     {
                        if ((range.Max.Value.Value.ToString() == "99991231") ||
                            (fld.Storage == FldStorage.TimeString && range.Max.Value.Value.ToString() == "235959"))
                        {
                           range.Max.Discard = true;
                        }
                        else
                        {
                           if (fld.Storage == FldStorage.TimeString)
                           {
                              SQLiteGateway.Sql3TimeToInternal((string)range.Max.Value.Value, out maxFullDate);
                           }
                           else
                           {
                              SQLiteGateway.Sql3DateToInternal((string)range.Max.Value.Value, out maxFullDate, len);
                           }
                        }
                     }
                     break;

                  case SqliteConstants.TIME_RNG:
                     if (fld.PartOfDateTime > 0)
                     {
                        if (range.Min.Value.Value != null && !range.Min.Value.IsNull)
                        {
                           SQLiteGateway.Sql3TimepartToInternal((string)range.Min.Value.Value, out minFullDate, SqliteConstants.SQL3_DATETIME_LEN + 1);
                        }

                        if (range.Max.Value.Value != null && !range.Max.Value.IsNull)
                        {
                           SQLiteGateway.Sql3TimepartToInternal((string)range.Max.Value.Value, out maxFullDate, SqliteConstants.SQL3_DATETIME_LEN + 1);
                        }

                     }
                     else
                     {
                        if (range.Min.Value.Value != null && !range.Min.Value.IsNull)
                        {
                           SQLiteGateway.Sql3ConvertToFullTime((string)range.Min.Value.Value, out minFullDate, SqliteConstants.SQL3_DATETIME_LEN + 1);
                        }

                        if (range.Max.Value.Value != null && !range.Max.Value.IsNull)
                        {
                           SQLiteGateway.Sql3ConvertToFullTime((string)range.Max.Value.Value, out maxFullDate, SqliteConstants.SQL3_DATETIME_LEN + 1);
                        }
                     }
                     break;

                  case SqliteConstants.DATETIME_RNG:
                     // FROM
                     if (range.Min.Value.Value != null && !range.Min.Discard)
                     {
                        if (!range.Min.Value.IsNull)
                        {
                           if (timeRange != null && timeRange.Min.Value.Value != null && !timeRange.Min.Value.IsNull && !timeRange.Min.Discard)
                           {
                              SQLiteGateway.Sql3DateTimeToInternal((string)range.Min.Value.Value, (string)timeRange.Min.Value.Value, out minFullDate, len);
                           }
                           else
                           {
                              SQLiteGateway.Sql3DateTimeToInternal((string)range.Min.Value.Value, "000000", out minFullDate, len);
                           }

                        }
                        else
                        {
                           if (timeRange != null)
                           {
                              if (timeRange.Min.Value.Value != null && !timeRange.Min.Value.IsNull && !timeRange.Min.Discard)
                              {
                                 SQLiteGateway.Sql3ConvertToFullTime((string)timeRange.Min.Value.Value, out minFullDate, SqliteConstants.SQL3_DATETIME_LEN + 1);
                              }
                           }
                        }
                     }

                     // TO
                     if (range.Max.Value.Value != null && !range.Max.Discard && !range.Max.Value.IsNull)
                     {
                        if (range.Max.Value.Value.ToString() == "99991231")
                        {
                           range.Max.Discard = true;
                        }
                     }

                     if (range.Max.Value.Value != null && !range.Max.Discard)
                     {
                        if (!range.Max.Value.IsNull)
                        {
                           if (timeRange != null && timeRange.Max != null && !timeRange.Max.Value.IsNull && !timeRange.Max.Discard)
                           {
                              SQLiteGateway.Sql3DateTimeToInternal((string)range.Max.Value.Value, (string)timeRange.Max.Value.Value, out maxFullDate, len);
                              
                           }
                           else
                           {
                              SQLiteGateway.Sql3DateTimeToInternal((string)range.Max.Value.Value, "235959", out maxFullDate, len);
                              
                           }

                        }
                        else
                        {
                           if (timeRange != null)
                           {
                              if(timeRange.Max.Value.Value != null && !timeRange.Max.Value.IsNull && !timeRange.Max.Discard)
                              {
                                 SQLiteGateway.Sql3ConvertToFullTime((string)timeRange.Max.Value.Value, out maxFullDate,  SqliteConstants.SQL3_DATETIME_LEN + 1);
                              }
                           }
                        }
                     }
                     break;

                  default:
                     break;
               }
            }

            rangeType = SQLiteGateway.Sql3GetFieldRangeType(dbCrsr, range, fld, dateFl, (char)datetimeRange);

            if ((rangeType != SqliteConstants.NO_RNG) && (rangeType != SqliteConstants.NULL_RNG))
               sql3Field.SqlVar = SqlVars[sqlvar_idx];

            if (slctModified)
            {
               range.Min.Type = RangeType.RangeMinMax; //restore min_type
               range.Max.Type = RangeType.RangeMinMax; //restore max_type
            }

            if (datetimeRange == SqliteConstants.DATETIME_RNG)
               timeRangeType = rangeType;

            switch (rangeType)
            {
               case (char)SqliteConstants.MIN_RNG:
                  cnt++;
                  if (datetimeRange != SqliteConstants.DATETIME_RNG)
                  {
                     if (dateFl)
                     {
                        valueSet = true;
                        minDate = true;
                     }
                     else
                        valMin = range.Min.Value.Value.ToString();
                  }
                  else
                  {
                     switch (timeRangeType)
                     {
                        case (char)SqliteConstants.MIN_RNG:
                        case (char)SqliteConstants.NO_RNG:
                           valueSet = true;
                           minDate = true;
                           break;

                        case (char)SqliteConstants.MAX_RNG:
                        case (char)SqliteConstants.MIN_AND_MAX_RNG:
                        case (char)SqliteConstants.MIN_EQ_MAX_RNG:
                           valueSet = true;
                           minDate = true;
                           maxDate = true;
                           cnt++;
                           break;

                        case (char)SqliteConstants.NULL_RNG:
                        case (char)SqliteConstants.MIN_AND_NULL_RNG:
                        case (char)SqliteConstants.NULL_AND_MAX_RNG:
                           valueSet = true;
                           minDate = true;
                           break;

                        default:
                           break;
                     }
                  }
                  break;

               case (char)SqliteConstants.MAX_RNG:
                  cnt++;
                  if (datetimeRange != SqliteConstants.DATETIME_RNG)
                  {
                     if (dateFl)
                     {
                        valueSet = true;
                        maxDate = true;
                     }
                     else
                        valMax = range.Max.Value.Value.ToString();
                  }
                  else
                  {
                     switch (timeRangeType)
                     {
                        case (char)SqliteConstants.MIN_RNG:
                           valueSet = true;
                           minDate = true;
                           maxDate = true;
                           cnt++;
                           break;

                        case (char)SqliteConstants.MAX_RNG:
                        case (char)SqliteConstants.NO_RNG:
                           valueSet = true;
                           maxDate = true;
                           break;

                        case (char)SqliteConstants.MIN_AND_MAX_RNG:
                        case (char)SqliteConstants.MIN_EQ_MAX_RNG:
                           valueSet = true;
                           minDate = true;
                           maxDate = true;
                           cnt++;
                           break;

                        case (char)SqliteConstants.NULL_RNG:
                        case (char)SqliteConstants.MIN_AND_NULL_RNG:
                        case (char)SqliteConstants.NULL_AND_MAX_RNG:
                           valueSet = true;
                           maxDate = true;
                           break;

                        default:
                           break;
                     }
                  }
                  break;

               case (char)SqliteConstants.MIN_AND_MAX_RNG:
                  cnt++; cnt++;
                  if (datetimeRange != SqliteConstants.DATETIME_RNG)
                  {
                     if (dateFl)
                     {
                        if (datetimeRange == SqliteConstants.DATE_RNG)
                        {
                           valueSet = true;
                           minDate = true;
                           maxDate = true;
                        }
                        else
                        {
                           valueSet = true;
                           minDate = true;
                           maxDate = true;
                        }
                     }
                     else
                     {
                        valMin = range.Min.Value.Value.ToString();
                        valMax = range.Max.Value.Value.ToString();
                     }
                  }
                  else
                  {
                     switch (timeRangeType)
                     {
                        case (char)SqliteConstants.MIN_RNG:
                           valueSet = true;
                           minDate = true;
                           maxDate = true;
                           break;

                        case (char)SqliteConstants.MAX_RNG:
                        case (char)SqliteConstants.MIN_AND_MAX_RNG:
                        case (char)SqliteConstants.MIN_EQ_MAX_RNG:
                        case (char)SqliteConstants.NO_RNG:
                           valueSet = true;
                           minDate = true;
                           maxDate = true;
                           break;

                        case (char)SqliteConstants.NULL_RNG:
                        case (char)SqliteConstants.MIN_AND_NULL_RNG:
                        case (char)SqliteConstants.NULL_AND_MAX_RNG:
                           valueSet = true;
                           minDate = true;
                           cnt--;
                           break;

                        default:
                           break;
                     }
                  }
                  break;

               case (char)SqliteConstants.MIN_EQ_MAX_RNG:
                  cnt++;
                  if (datetimeRange != SqliteConstants.DATETIME_RNG)
                  {
                     if (dateFl)
                     {
                        valueSet = true;
                        minDate = true;
                     }
                     else
                        valMin = range.Min.Value.Value.ToString();
                  }
                  else
                  {
                     switch (timeRangeType)
                     {
                        case (char)SqliteConstants.MIN_RNG:
                           valueSet = true;
                           minDate = true;
                           maxDate = true;
                           cnt++;
                           break;

                        case (char)SqliteConstants.MAX_RNG:
                        case (char)SqliteConstants.MIN_AND_MAX_RNG:
                           valueSet = true;
                           minDate = true;
                           maxDate = true;
                           cnt++;
                           break;

                        case (char)SqliteConstants.MIN_EQ_MAX_RNG:
                        case (char)SqliteConstants.NO_RNG:
                           valueSet = true;
                           minDate = true;
                           break;

                        case (char)SqliteConstants.NULL_RNG:
                        case (char)SqliteConstants.MIN_AND_NULL_RNG:
                        case (char)SqliteConstants.NULL_AND_MAX_RNG:
                           valueSet = true;
                           minDate = true;
                           break;

                        default:
                           break;
                     }
                  }

                  break;

               case (char)SqliteConstants.NULL_RNG:
                  if (datetimeRange != SqliteConstants.DATETIME_RNG)
                  {
                     ; // IS NULL
                  }
                  else
                  {
                     switch (timeRangeType)
                     {
                        case (char)SqliteConstants.MIN_RNG:
                        case (char)SqliteConstants.MIN_AND_MAX_RNG:
                        case (char)SqliteConstants.MIN_EQ_MAX_RNG:
                        case (char)SqliteConstants.MIN_AND_NULL_RNG:
                           valueSet = true;
                           minDate = true;
                           cnt++;
                           break;

                        case (char)SqliteConstants.MAX_RNG:
                        case (char)SqliteConstants.NULL_AND_MAX_RNG:
                           valueSet = true;
                           maxDate = true;
                           cnt++;
                           break;

                        case (char)SqliteConstants.NULL_RNG:
                        case (char)SqliteConstants.NO_RNG:
                           // IS NULL
                           break;

                        default:
                           break;
                     }
                  }
                  break;

               case (char)SqliteConstants.MIN_AND_NULL_RNG:
                  cnt++;
                  if (dateFl)
                  {
                     valueSet = true;
                     minDate = true;
                  }
                  else
                     valMin = range.Min.Value.Value.ToString();
                  break;

               case (char)SqliteConstants.NULL_AND_MAX_RNG:
                  cnt++;

                  if (dateFl)
                  {
                     valueSet = true;
                     maxDate = true;
                  }
                  else
                     valMax = range.Max.Value.Value.ToString();
                  break;

               case (char)SqliteConstants.NO_RNG:
                  if (datetimeRange == SqliteConstants.DATETIME_RNG)
                  {
                     cnt++;
                     switch (timeRangeType)
                     {
                        case (char)SqliteConstants.MIN_RNG:
                           valueSet = true;
                           minDate = true;
                           break;

                        case (char)SqliteConstants.MAX_RNG:
                           valueSet = true;
                           maxDate = true;
                           break;

                        case (char)SqliteConstants.MIN_AND_MAX_RNG:
                           valueSet = true;
                           minDate = true;
                           maxDate = true;
                           cnt++;
                           break;

                        case (char)SqliteConstants.MIN_EQ_MAX_RNG:
                           valueSet = true;
                           minDate = true;
                           break;

                        case (char)SqliteConstants.NULL_RNG:
                           cnt--; // IS NULL
                           break;

                        case (char)SqliteConstants.MIN_AND_NULL_RNG:
                           valueSet = true;
                           minDate = true;
                           break;

                        case (char)SqliteConstants.NULL_AND_MAX_RNG:
                           valueSet = true;
                           maxDate = true;
                           break;

                        case (char)SqliteConstants.NO_RNG:
                        default:
                           break;
                     }
                  }
                  break;

               default:
                  break;
            }  // switch (range_fl)

            if (valueSet)
            {
               if (minDate)
               {
                  SQL3SqldaDateTimeRange(sql3Field, prepared, (char)datetimeRange, minFullDate, SqliteConstants.SQL3_DATETIME_LEN + 1);
                  sqlvar_idx++;
               }

               if (maxDate)
               {
                  sql3Field.SqlVar = SqlVars[sqlvar_idx];
                  SQL3SqldaDateTimeRange(sql3Field, prepared, (char)datetimeRange, maxFullDate, SqliteConstants.SQL3_DATETIME_LEN + 1);
                  sqlvar_idx++;
               }
            }
            else
            {
               if (valMin != null)
               {
                  sql3Field.Buf = valMin;
                  sql3Field.SqlVar.IsMinRange = true;
                  if (isDBTypeDBTime && sql3Field.Storage == FldStorage.TimeString)
                  {
                     SQL3SqldaTimeRange(sql3Field, prepared, (string)sql3Field.Buf, sql3Field.SqlVar.IsMinRange);
                  }
                  else
                  {
                     SQLiteGateway.SqlvarValFill(sql3Field.SqlVar, sql3Field, sqlvar_idx, prepared, SqliteConstants.QUOTES_TRUNC);
                  }
                  sqlvar_idx++;
               }
               if (valMax != null)
               {
                  sql3Field.SqlVar = SqlVars[sqlvar_idx];
                  sql3Field.Buf = valMax;
                  sql3Field.SqlVar.IsMinRange = false;
                  if (isDBTypeDBTime && sql3Field.Storage == FldStorage.TimeString)
                     SQL3SqldaTimeRange(sql3Field, prepared, (string)sql3Field.Buf, sql3Field.SqlVar.IsMinRange);
                  else
                     SQLiteGateway.SqlvarValFill(sql3Field.SqlVar, sql3Field, sqlvar_idx, prepared, SqliteConstants.QUOTES_TRUNC);
                  sqlvar_idx++;
               }

            }

            if (maxOrg != null)
            {
               range.Max.Value.Value = maxOrg;
            }
         }  // for loop

         Logger.Instance.WriteDevToLog("SQL3SqldaRange(): <<<<< ");

         return cnt;
      }

      public void SQL3SqldaDateTimeRange(Sql3Field pSQL3Field, bool prepared, char DTRange, string val, long val_len)
      {
         if (!prepared)
            SQLiteGateway.SqlvarFill(ref pSQL3Field, true, true, false);

         if (DTRange == SqliteConstants.TIME_RNG)
         {
            pSQL3Field.SqlVar.SqlLen = SqliteConstants.TIME_FORMAT.Length + 1;



            SQLiteGateway.Sql3AddValSqldata(out pSQL3Field.SqlVar.SqlData, pSQL3Field.SqlVar.SqlLen, pSQL3Field.SqlVar.SqlType,
                                               pSQL3Field.SqlVar.SqlLen, pSQL3Field.Fld, (string)val,
                                               SqliteConstants.QUOTES_TRUNC, pSQL3Field.SqlVar.IsMinRange);

         }
         else
         {
            pSQL3Field.SqlVar.SqlData = val;
         }
      }

      public void SQL3SqldaTimeRange(Sql3Field pSQL3Field, bool prepared, string val, bool isMinRange)
      {
         if (!prepared)
            SQLiteGateway.SqlvarFill(ref pSQL3Field, true, true, false);


         SQLiteGateway.Sql3AddValSqldata(out pSQL3Field.SqlVar.SqlData, pSQL3Field.SqlVar.SqlLen, pSQL3Field.SqlVar.SqlType,
                                               pSQL3Field.SqlVar.SqlLen, pSQL3Field.Fld, (string)val,
                                               SqliteConstants.QUOTES_TRUNC, isMinRange);

      }
      public int SQL3SqldaSqlRange(GatewayAdapterCursor dbCrsr, Sql3SqlVar sqlvar, bool prepared)
      {
         return 0;
      }

      /// <summary>
      /// Copy Sqlvars into dest sqlVars from startIdx
      /// </summary>
      /// <param name="dest"></param>
      /// <param name="startIdx"></param>
      public int SQL3SqldaNoNullCopy(List<Sql3SqlVar> dest, int startIdx)
      {
         int i = 0;
         int j = startIdx;
         Sql3SqlVar sqlvar;
         object sqldataPtr;
         string dataArr = string.Empty;  // a buff sufficient to avoid frequent mallocs.

         Logger.Instance.WriteDevToLog("SqldaNoNullCopy(): >>>>> ");

         // The following loop copies non-null key sqlvars (src) to the startpos sqlvars (dest) and 
         // also format the data (select-list data), which will get used as i/p data (in where-clause).
         for (i = 0; i < SqlVars.Count; i++)
         {
            sqlvar = SqlVars[i];
            if (sqlvar.NullIndicator != 1)
            {
               dest[j] = sqlvar;

               // when we bind the date value (which is in char format), we need to 
               // bind it as a CHAR, unlike fetch-case where we define it as TIMESTAMP.
               if (dest[j].dateType == DateType.DATETIME_TO_CHAR ||
                   dest[j].dateType == DateType.DATETIME4_TO_CHAR)
               {
                  dest[j].DataSourceType = "SQL3TYPE_CHAR";
                  dest[j].SqlType = Sql3Type.SQL3TYPE_STR;

                  sqldataPtr = dest[j].SqlData;
                  dest[j].SqlData = sqldataPtr;
                  dest[j].SqlLen += 1; // for STR types
               }

               if (dest[j].SqlLen > dataArr.Length)
                  sqldataPtr = string.Empty;
               else
                  sqldataPtr = dataArr;

               sqldataPtr = dest[j].SqlData;

               SQLiteGateway.Sql3AddValSqldata(out dest[j].SqlData, dest[j].SqlLen, dest[j].SqlType,
                                               dest[j].SqlLen, dest[j].Fld, sqldataPtr.ToString(),
                                               SqliteConstants.QUOTES_TRUNC, false);

               j++;
            }
         }

         Logger.Instance.WriteDevToLog("SqldaNoNullCopy(): <<<<< ");

         return j;
      }


      public int SQL3SqldaFromDbpos(GatewayAdapterCursor db_crsr, int sqlVarIdx, DbPos dbPos, bool withnull, bool prepared)
      {
         int         idx = 0,
                     offset = 0,
                     cnt = 0;
         short       plen = 0;
         DataSourceDefinition dbh = db_crsr.Definition.DataSourceDefinition;
         GatewayCursor crsr;

         SQL3Dbd pSQL3_dbd;
         DBField fld = null;
         DBKey key = null;
         List<DBSegment> segments = null;
         DBSegment seg = null;

         string name = string.Empty;
         Sql3Field sql3Field = new Sql3Field();
         //SQL3_SQLVAR  *sqlvar  = sqlda->sqlvar;

         string fullDate = string.Empty,
                strDate = string.Empty;
         bool dateFl;
         DBSegment TimeSeg = null;
         int TimeIdx;
         string TimeBuf = string.Empty;
         int TimeLen = 0,
             TimeOffset = 0;

         SQLiteGateway.GatewayCursorTbl.TryGetValue(db_crsr, out crsr);
         SQLiteGateway.DbdTbl.TryGetValue(dbh, out pSQL3_dbd);

         Logger.Instance.WriteDevToLog("SQL3SqldaFromDbpos(): >>>>> ");

         if (dbPos != null)
            crsr.DbPosBuf = (Byte[])dbPos.Get().Clone();

         Logger.Instance.WriteDevToLog("SQL3SqldaFromDbpos(): after DB_POS_GET ");

         if (pSQL3_dbd.IsView)
         {
            key = crsr.PosKey;
            segments = key.Segments;


            for (idx = 0; idx < key.Segments.Count; idx++)
            {
               seg = segments[idx];

               name = seg.Field.DbName;

               Logger.Instance.WriteDevToLog(string.Format("SQL3SqldaFromDbpos(): field {0}", name));

               plen = BitConverter.ToInt16(SQLiteGateway.GetBytes(crsr.DbPosBuf, offset, sizeof(short)), 0);

               offset += sizeof(short);

               fld = seg.Field;

               if (fld.PartOfDateTime != 0)
               {
                  if (fld.Storage == FldStorage.TimeString)
                  {
                     sql3Field.PartOfDateTime = SqliteConstants.TIME_OF_DATETIME;
                  }
                  else
                  {
                     TimeOffset = 0;
                     for (TimeIdx = 0; TimeIdx < key.Segments.Count; TimeIdx++)
                     {
                        TimeSeg = key.Segments[TimeIdx];

                        byte[] buf = SQLiteGateway.GetBytes(crsr.DbPosBuf, TimeOffset, sizeof(short));
                        TimeLen = BitConverter.ToInt16(buf, 0); 

                        TimeOffset += sizeof(short);

                        buf = new byte[crsr.DbPosBuf.Length];
                        buf = SQLiteGateway.GetBytes(crsr.DbPosBuf, TimeOffset, crsr.DbPosBuf.Length - TimeOffset);
                        TimeBuf = SQLiteGateway.ConvertFromBytes(buf, StorageAttribute.ALPHA).ToString();
                        TimeOffset += TimeLen;


                        if (TimeSeg.Field.Isn == fld.PartOfDateTime)
                           break;
                     }
                     sql3Field.PartOfDateTime = TimeIdx;
                  }
               }
               else
                  sql3Field.PartOfDateTime = SqliteConstants.NORMAL_OF_DATETIME;

               /* if field is null */
               if (plen == 0)
               {
                  if (withnull)
                  {
                     sql3Field.SqlVar = SqlVars[sqlVarIdx++];
                     if (!prepared)
                     {

                        sql3Field.Name = name;

                        sql3Field.FieldLen = fld.StorageFldSize();
                        if (fld.Storage == FldStorage.AlphaZString)
                           sql3Field.FieldLen--;
                        sql3Field.GatewayAdapterCursor = db_crsr;
                        sql3Field.Whole = fld.Whole;
                        sql3Field.Dec = fld.Dec;
                        sql3Field.AllowNull = fld.AllowNull;
                        sql3Field.Storage = (FldStorage)fld.Storage;
                        sql3Field.NullIndicator = crsr.NullIndicator[db_crsr.GetFieldIndex(seg.Field)];
                     }
                     else
                     {
                        sql3Field.SqlVar.NullIndicator = crsr.NullIndicator[db_crsr.GetFieldIndex(seg.Field)];
                     }

                     sql3Field.DataSourceDefinition = db_crsr.Definition.DataSourceDefinition;
                     sql3Field.Fld = seg.Field;
                     sql3Field.Buf = crsr.DbPosBuf;

                     SQLiteGateway.SqlvarValFill(sql3Field.SqlVar, sql3Field, sqlVarIdx - 1, prepared, SqliteConstants.QUOTES_TRUNC);
                     cnt++;

                     Logger.Instance.WriteDevToLog(string.Format("SQL3SqldaFromDbpos(): doing a NULL segment %S", name));
                  }
                  else
                  {
                     Logger.Instance.WriteDevToLog(string.Format("SQL3SqldaFromDbpos(): doing nothing for a NULL segment {0}", name));
                  }
               }
               else
               {
                  Logger.Instance.WriteDevToLog(string.Format("SQL3SqldaFromDbpos(): doing a NON NULL segment {0}", name));

                  dateFl = false;
                  sql3Field.SqlVar = SqlVars[sqlVarIdx++];
                  if (!prepared)
                  {
                     sql3Field.FieldLen = fld.StorageFldSize();
                     switch (fld.Storage)
                     {
                        case FldStorage.AlphaZString:
                           sql3Field.FieldLen--;
                           break;

                        case FldStorage.UnicodeZString:
                           sql3Field.FieldLen -= 2;
                           break;
                     }

                     sql3Field.Name = name;

                     sql3Field.GatewayAdapterCursor = db_crsr;
                     sql3Field.Storage = (FldStorage)fld.Storage;
                     sql3Field.Whole = fld.Whole;
                     sql3Field.Dec = fld.Dec;
                     sql3Field.AllowNull = fld.AllowNull;
                     sql3Field.NullIndicator = SQLiteGateway.SQL3NotNull;
                  }
                  else
                  {
                     sql3Field.SqlVar.NullIndicator = SQLiteGateway.SQL3NotNull;
                  }

                  sql3Field.DataSourceDefinition = db_crsr.Definition.DataSourceDefinition;
                  sql3Field.Fld = seg.Field;

                  byte[] buf = SQLiteGateway.GetBytes(crsr.DbPosBuf, offset, sql3Field.FieldLen);

                  StorageAttribute destinationAttribute = (StorageAttribute)sql3Field.Fld.Attr;
                  if (sql3Field.Fld.Storage == FldStorage.NumericString || sql3Field.Fld.Storage == FldStorage.TimeString)
                  {
                     destinationAttribute = StorageAttribute.ALPHA;
                  }

                  sql3Field.Buf = SQLiteGateway.ConvertFromBytes(buf, destinationAttribute);

                  if (sql3Field.Fld.PartOfDateTime != 0 &&
                      sql3Field.Fld.Storage == FldStorage.DateString)
                  {
                     dateFl = true;
                  }
                  else
                     dateFl = false;

                  if (dateFl)
                  {
                     if (!prepared)
                     {
                        SQLiteGateway.SqlvarFill(ref sql3Field, true, true, false);
                     }

                     byte[] buf1 = SQLiteGateway.GetBytes(crsr.DbPosBuf, offset, crsr.DbPosBuf.Length - offset);

                     SQLiteGateway.SQLiteLow.LibDateCrack(SQLiteGateway.ConvertFromBytes(buf1, StorageAttribute.ALPHA), out fullDate, fullDate.Length, plen, null);

                     strDate = string.Format("{0}{1}{2}", fullDate.Substring(0, 4), fullDate.Substring(5, 2), fullDate.Substring(8, 2));
                     SQLiteGateway.Sql3DateTimeToInternal(strDate, TimeBuf, out fullDate, 8);

                     sql3Field.SqlVar.SqlData = fullDate;

                  }
                  else
                  {
                     SQLiteGateway.SqlvarValFill(sql3Field.SqlVar, sql3Field, sqlVarIdx - 1, prepared, SqliteConstants.QUOTES_TRUNC);

                  }

                  offset += plen;
                  cnt++;
               }
            }
         }
         else //Build the sqlda using rowid.
         {
            SqlVars[sqlVarIdx].SQL3SqlvarTid();
            byte []buf = SQLiteGateway.GetBytes(crsr.DbPosBuf, sizeof(int), SqliteConstants.SQL3_ROWID_LEN_EXTERNAL);
            SqlVars[sqlVarIdx].SqlData = BitConverter.ToInt32(buf, 0);
            cnt++;
         }

         Logger.Instance.WriteDevToLog(string.Format("SQL3SqldaFromDbpos(): <<<<< sqlvar cnt = {0}", cnt));

         return cnt;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="db_crsr"></param>
      public void SQL3BuffToSqlda(GatewayAdapterCursor dbCrsr)
      {
         DBField      fld      = null;
         int          fldIdx    = 0,
                      idx       = 0;
         int          fldLen   = 0;
         string       buf      = string.Empty;
         string       yearBuf = string.Empty;

         Logger.Instance.WriteDevToLog("SQL3BuffToSqlda(): >>>>>");

         /* for all fields in the DB_CRSR field list */
         for (idx = 0 ; idx < dbCrsr.Definition.FieldsDefinition.Count ; ++idx)
         {
            fld = dbCrsr.Definition.FieldsDefinition[idx];

            fldLen = fld.StorageFldSize();
            if (fld.Storage == FldStorage.AlphaZString)
               fldLen--;
            if (fld.Storage == FldStorage.UnicodeZString)
               fldLen-= 2;

            if (!fld.IsBlob() && !dbCrsr.CurrentRecord.IsNull(idx))
            {
               /* get a pointer to the result buffer */
               buf = dbCrsr.CurrentRecord.GetValue(idx).ToString();

               Logger.Instance.WriteDevToLog(string.Format("SQL3BuffToSqlda(): Using non varchar for {0}", fldIdx));

               {
                  if  (SQLiteGateway.Sql3FieldInfoFlag (dbCrsr.Definition.DataSourceDefinition, fld, "MAGICKEY"))
                  {
                     SqlVars[idx].SqlData = SQLiteGateway.Sql3GetUniqueKey();

                     Logger.Instance.WriteDevToLog(string.Format("SQL3BuffToSqlda(): Generated random number {0}", SqlVars[idx].SqlData));
                  }
                  else
                  {
                     if (fldLen == 6 && SqlVars[idx].SqlLen == 8)
                     {
                        yearBuf = yearBuf.Substring(0, 2);

                        if (Int32.Parse(yearBuf) < 50)
                        {
                           SqlVars[idx].SqlData = "20";
                        }
                        else
                        {
                           SqlVars[idx].SqlData = "19";
                        }

                        SqlVars[idx].SqlData += buf;
                     }
                     else
                     {
                        SqlVars[idx].SqlData = buf;
                     }
                  }
               }
            }
         }

         Logger.Instance.WriteDevToLog(string.Format("SQL3BuffToSqlda(): <<<<< idx = {0}", idx));
      }

      /// <summary>
      /// Copy the data from SqlVars to dbCrsr.
      /// </summary>
      /// <param name="dbCrsr"></param>
      public void SQL3SqldaToBuff(GatewayAdapterCursor dbCrsr)
      {
         GatewayCursor crsr;
         DBField fld = null;
         int idx = 0;
         int fldLen = 0;
         object dataBuf = string.Empty;
         string tempDate;
         string fullDate = string.Empty;
         FldStorage Storage;
         SQL3Dbd sql3dbd = null;

         SQLiteGateway.GatewayCursorTbl.TryGetValue(dbCrsr, out crsr);

         Logger.Instance.WriteDevToLog(string.Format("SQL3SqldaToBuff(): >>>>> : # of fields = {0}", dbCrsr.Definition.FieldsDefinition.Count));

         /* for all fields in the DB_CRSR field list */
         for (idx = 0; idx < dbCrsr.Definition.FieldsDefinition.Count; ++idx)
         {
            /* get offset into the field table */

            fld = dbCrsr.Definition.FieldsDefinition[idx];

            fldLen = fld.Length;
            Storage = (FldStorage)fld.Storage;
            if (Storage == FldStorage.AlphaZString)
               fldLen--;
            if (Storage == FldStorage.UnicodeZString)
               fldLen -= 2;


            if (!SQLiteGateway.DbdTbl.TryGetValue(dbCrsr.Definition.DataSourceDefinition, out sql3dbd))
            {
               
               if (SqlVars[idx].SqlLen == 8 && fldLen == 6 && fld.Storage != FldStorage.UnicodeZString)
               {
                  /* convert the YYYYMMDD from the database to YYMMDD */

                  SQLiteGateway.SQLiteLow.LibDateCrack(SqlVars[idx].SqlData, out fullDate, fullDate.Length, SqlVars[idx].SqlLen, null);
                  tempDate = string.Format("{0}{1}{2}", fullDate.Substring(2, 2), fullDate.Substring(5, 2), fullDate.Substring(8, 2));
                  dataBuf = tempDate;

                  Logger.Instance.WriteDevToLog(string.Format("SQL3SqldaToBuff(): -1- converted database date - to magic date - {0}", dataBuf));
                  
               }
               else if (/* sqlvar[idx].sqltype == SQL3TYPE_DBTIMESTAMP  && */
                  (fld.Storage == FldStorage.DateString || fld.Storage == FldStorage.TimeString))
               {
                  // GATS
                  SQLiteGateway.SQLiteLow.LibDateCrack(SqlVars[idx].SqlData, out fullDate, fullDate.Length, SqlVars[idx].SqlLen, null);
                  //GATS
                  if (fldLen == 8)
                  {
                     tempDate = string.Format("{0}{1}{2}", fullDate.Substring(0,4), fullDate.Substring(5,2), fullDate.Substring(8,2));
                     dataBuf = tempDate;

                     Logger.Instance.WriteDevToLog(string.Format("SQL3SqldaToBuff(): -2- converted database date - to magic date - {0}", dataBuf));
                  }
                  else if (fldLen == 6)
                  {
                     if (fld.Storage == FldStorage.TimeString)
                     {
                        tempDate = string.Format("{0}{1}{2}", fullDate.Substring(0, 2), fullDate.Substring(2, 2), 
                                                              fullDate.Substring(4, 2));
                        dataBuf = tempDate;

                        Logger.Instance.WriteDevToLog(string.Format("SQL3SqldaToBuff(): -2- converted database date - to magic date - {0}", dataBuf));

                     }
                     else
                     {
                        tempDate = string.Format("{0}{1}{2}", fullDate.Substring(2, 2), fullDate.Substring(5, 2), fullDate.Substring(8, 2));
                        dataBuf = tempDate;

                        Logger.Instance.WriteDevToLog(string.Format("SQL3SqldaToBuff(): -2- converted database date - to magic date - {0}", dataBuf));
                     }

                  }
                  else
                  {
                     dataBuf = fullDate;

                     Logger.Instance.WriteDevToLog(string.Format("SQL3SqldaToBuff(): -2- converted database date - to magic char - {0}", dataBuf));
                  }
               }
               else
               {
                  if (fld.IsBlob())
                  {

                  }
                  else
                  {
                     if (SqlVars[idx].SqlType == Sql3Type.SQL3TYPE_BOOL)
                     {
                        if (fld.Attr == (char)StorageAttributeType.Numeric)
                        {
                           if ((int)SqlVars[idx].SqlData == -1)
                              (SqlVars[idx].SqlData) = 1;
                           else
                              (SqlVars[idx].SqlData) = 0;
                        }
                        else
                        {
                           if ((int)(SqlVars[idx].SqlData) == 1)
                              (SqlVars[idx].SqlData) = 1;
                           else
                              (SqlVars[idx].SqlData) = 0;
                        }
                     }

                     dataBuf = SqlVars[idx].SqlData;

                     Logger.Instance.WriteDevToLog(string.Format("SQL3SqldaToBuff(): copying {0} bytes NON VARCHAR field to BUFF {1}", fldLen, idx));
                     
                  }
               }
            }
            else
            {
               if ((fld.Storage == FldStorage.DateString || fld.Storage == FldStorage.TimeString) &&
                  (SqlVars[idx].SqlType == Sql3Type.SQL3TYPE_DBTIMESTAMP || SqlVars[idx].SqlType == Sql3Type.SQL3TYPE_DBDATE
                   || SqlVars[idx].SqlType == Sql3Type.SQL3TYPE_DBTIME))
               {
                  if (SqlVars[idx].SqlType == Sql3Type.SQL3TYPE_DBTIMESTAMP)
                  {
                     fullDate = (string)SqlVars[idx].SqlData;
                     if (fldLen == 8)
                     {
                        tempDate = string.Format("{0}{1}{2}", fullDate.Substring(0, 4), fullDate.Substring(5, 2), fullDate.Substring(8, 2));
                        dataBuf = tempDate;
                        Logger.Instance.WriteDevToLog(string.Format("SQL3SqldaToBuff(): -2- converted database date - to magic date - {0}", dataBuf));
                     }
                     else if (fldLen == 6)
                     {
                        if (fld.Storage == FldStorage.TimeString)
                        {
                           tempDate = string.Format("{0}{1}{2}", fullDate.Substring(11, 2), fullDate.Substring(14, 2), fullDate.Substring(17, 2));
                           dataBuf = tempDate;
                           Logger.Instance.WriteDevToLog(string.Format("SQL3SqldaToBuff(): -2- converted database date - to magic time - {0}", dataBuf));

                        }
                        else
                        {
                           tempDate = string.Format("{0}{1}{2}", fullDate.Substring(2, 2), fullDate.Substring(5, 2), fullDate.Substring(8, 2));
                           dataBuf = tempDate;
                           Logger.Instance.WriteDevToLog(string.Format("SQL3SqldaToBuff(): -2- converted database date - to magic date - {0}", dataBuf));
                        }
                     }
                     else
                     {
                        dataBuf = fullDate;
                        Logger.Instance.WriteDevToLog(string.Format("SQL3SqldaToBuff(): -2- converted database date - to magic char - {0}", dataBuf));
                     }
                  }
                  else
                  {
                     if (SqlVars[idx].SqlType == Sql3Type.SQL3TYPE_DBDATE)
                     {
                        SQLiteGateway.SQLiteLow.LibDateCrack(SqlVars[idx].SqlData, out fullDate, fullDate.Length, SqlVars[idx].SqlLen, null);

                        if (fldLen == 8)
                        {
                           
                           string [] strarr =   fullDate.Split('-');
                           tempDate = string.Concat(strarr);
                           dataBuf = tempDate;

                           Logger.Instance.WriteDevToLog(string.Format("SQL3SqldaToBuff(): -2- converted database date - to magic date - {0}", dataBuf));
                        }
                        else if (fldLen == 6)
                        {
                           if (SqlVars[idx].NullIndicator == 1)
                           {
                              dataBuf = string.Empty;
                           }
                           else
                           {
                              string year = fullDate.Substring(2, 2);
                              int next = 5;

                              string month = fullDate.Substring(next, 2);
                              next = 8;

                              string day = fullDate.Substring(next, 2);

                              tempDate = string.Format("{0}{1}{2}", year, month, day);
                              dataBuf = tempDate;
                           }

                           Logger.Instance.WriteDevToLog(string.Format("SQL3SqldaToBuff(): -2- converted database date - to magic date - {0}", dataBuf));
                        }
                     }
                     else
                     {
                        if (SqlVars[idx].SqlType == Sql3Type.SQL3TYPE_DBTIME)
                        {
                           SQLiteGateway.Sql3DBTime((string)SqlVars[idx].SqlData, (string)dataBuf, fldLen);
                        }
                     }
                  }
               }
               else
               {
                  if (fld.IsBlob())
                  {
                     // sqldata contain the result of DATALENGTH on the blob field
                     dataBuf = new GatewayBlob();
                     ((GatewayBlob)dataBuf).Blob = SqlVars[idx].SqlData;
                     if (SqlVars[idx].NullIndicator == 0)
                     {
                        if (SqlVars[idx].SqlType == Sql3Type.SQL3TYPE_BYTES)
                           ((GatewayBlob)dataBuf).BlobSize = ((byte[])SqlVars[idx].SqlData).Length;
                        else if (SqlVars[idx].SqlType == Sql3Type.SQL3TYPE_WSTR)
                        {
                           ((GatewayBlob)dataBuf).BlobSize = ((string)SqlVars[idx].SqlData).Length * 2;
                        }
                        else
                        {
                           ((GatewayBlob)dataBuf).BlobSize = ((string)SqlVars[idx].SqlData).Length;
                        }
                     }
                     Logger.Instance.WriteDevToLog("SQL3SqldaToBuff(): copying 4 bytes contains the image length");
                  }
                  else
                  {
                     if (SqlVars[idx].SqlType == Sql3Type.SQL3TYPE_BOOL)
                     {

                        if (Convert.ToInt32(SqlVars[idx].SqlData) == 1)
                           (SqlVars[idx].SqlData) = (short)1;
                        else
                           (SqlVars[idx].SqlData) = (short)0;

                     }

                     if (fld.DbType.Contains("REAL") && fldLen == 8)
                     {
                        double dbl = (double)SqlVars[idx].SqlData;
                        dataBuf = dbl.ToString();
                     }
                     else
                     {
                        dataBuf = SqlVars[idx].SqlData;
                     }

                     Logger.Instance.WriteDevToLog(string.Format("SQL3SqldaToBuff(): copying {0} bytes NON VARCHAR field to BUFF {1}", fldLen, idx));
                  }
               }
            }

            dbCrsr.CurrentRecord.SetValue(idx, dataBuf);
         }

         Logger.Instance.WriteDevToLog(string.Format("SQL3SqldaToBuff(): <<<<< idx = {0}", idx));
      }

      /// <summary>
      ///  SQL3SqlindToBuff ()
      /// </summary>
      /// <param name="crsr"></param>
      /// <param name="dbCrsr"></param>
      /// <param name="using_cursor"></param>
      /// <returns>int</returns>
      public void SQL3SqlindToBuff(GatewayAdapterCursor dbCrsr, bool usingCursor)
      {
         int idx;
         DBField fld = null;

         Logger.Instance.WriteDevToLog(string.Format("SQL3SqlindToBuff(): >>>>> # of fields = {0}", dbCrsr.Definition.FieldsDefinition.Count));

         for (idx = 0; idx < dbCrsr.Definition.FieldsDefinition.Count; ++idx)
         {
            fld = dbCrsr.Definition.FieldsDefinition[idx];

            if (fld.AllowNull)
            {
               if ((usingCursor && SqlVars[idx].NullIndicator == 1) ||
                   (!usingCursor && SqlVars[idx].NullIndicator == -1))
               {
                  dbCrsr.CurrentRecord.SetNull(idx, true);

                  Logger.Instance.WriteDevToLog(string.Format("SQL3SqlindToBuff(): crsr_idx {0} is null", idx));

                  /*------------------------------------------------------------*/
                  /* insert 0 to the blob length in case datalength return null */
                  /*------------------------------------------------------------*/
                  if (fld.IsBlob())
                  {
                     ((GatewayBlob)dbCrsr.CurrentRecord.GetValue(idx)).BlobSize = 0;
                  }

                  Logger.Instance.WriteDevToLog("SQL3SqlindToBuff(): blob size set to zero");
               }
               else
               {
                  dbCrsr.CurrentRecord.SetNull(idx, false);
               }
            }
            else
            {
               dbCrsr.CurrentRecord.SetNull(idx, false);
            }

         }

         Logger.Instance.WriteDevToLog(string.Format("SQL3SqlindToBuff(): <<<<< fld_idx = {0}", dbCrsr.GetFieldIndex(fld)));

         return;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="sqlda"></param>
      /// <param name="db_crsr"></param>
      public void SQL3SqldaCurrOutput(GatewayAdapterCursor dbCrsr)
      {
         GatewayCursor crsr;
         DBField fld;
         int idx = 0,
             varIdx = 0;

         string fieldName = string.Empty;

         Sql3Field sql3Field = new Sql3Field();
         DataSourceDefinition dbh = dbCrsr.Definition.DataSourceDefinition;
         short TimeIdx;

         SQLiteGateway.GatewayCursorTbl.TryGetValue(dbCrsr, out crsr);

         Logger.Instance.WriteDevToLog("SQL3SqldaCurrOutput(): >>>>>");

         /* for all fields in the DB_CRSR field list */
         for (idx = 0; idx < dbCrsr.Definition.FieldsDefinition.Count; ++idx)
         {
            /* get a pointer to the FLD */
            fld = dbCrsr.Definition.FieldsDefinition[idx];

            fieldName = fld.DbName;
            sql3Field.Name = fieldName;

            /* update the sql3_field struct for passing to SqlvarFill */
            sql3Field.SqlVar = SqlVars[varIdx++];
            sql3Field.Fld = fld;
            sql3Field.GatewayAdapterCursor = dbCrsr;
            sql3Field.DataSourceDefinition = dbh;

            sql3Field.Storage = fld.Storage;
            sql3Field.AllowNull = fld.AllowNull;
            sql3Field.Whole = fld.Whole;
            sql3Field.Dec = fld.Dec;

            if (fld.IsBlob())
            {
               //sql3_field.field_len = LONG_MAX;   /* Ora8i: Specify the Max size of data. */
               ///*   sql3_field.sqlvar->sqldata = SQL3_blob_tbl[0].buf; */
               //sql3_field.sqlvar->sqldata = SQL3_blob_tbl[which_blob][0].buf;
               //which_blob++;
               //sql3_field.sqlvar->blob = TRUE;
            }
            else
            {
               sql3Field.FieldLen = fld.StorageFldSize();
               sql3Field.SqlVar.IsBlob = false;
            }

            sql3Field.NullIndicator = crsr.NullIndicator[idx];
            /*in the date sqlvar DTIdx we're storing time-sqlvar's idx*/
            if (fld.PartOfDateTime != 0)
            {
               if (fld.Storage == FldStorage.DateString)
               {
                  for (TimeIdx = 0; TimeIdx < dbCrsr.Definition.FieldsDefinition.Count; TimeIdx++)
                  {
                     if (dbCrsr.Definition.FieldsDefinition[TimeIdx].Isn == fld.PartOfDateTime)
                     {
                        break;
                     }
                  }
                  sql3Field.SqlVar.PartOfDateTime = TimeIdx;
               }
               else /*timefld*/
               {
                  sql3Field.SqlVar.PartOfDateTime = SqliteConstants.TIME_OF_DATETIME;
               }
            }

            Logger.Instance.WriteDevToLog(string.Format("SQL3SqldaCurrOutput(): using sqlvar[{0}] for db_crsr field {1} length {2}", idx, sql3Field.Name, sql3Field.FieldLen));

            SQLiteGateway.SqlvarFill(ref sql3Field, false, false, false);

            if (fld.IsBlob() && sql3Field.SqlVar.IsBlob)
            {
               //sql3_field.SqlVar.SqlLen = ULONG_MAX;   /* Ora8i: Specify the Max size of data for BLOB.(4GB) */
            }
         }


         Logger.Instance.WriteDevToLog(string.Format("SQL3SqldaCurrOutput(): <<<<< idx = {0}", idx));
         return;
      }

   }
}
