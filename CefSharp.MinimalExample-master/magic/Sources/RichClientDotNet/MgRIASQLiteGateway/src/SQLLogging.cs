using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.gatewaytypes;
using util.com.magicsoftware.util;

namespace MgSqlite.src
{
   class SQLLogging
   {
      /// <summary>
      /// LogRngSlct()
      /// </summary>
      /// <param name="pSlct"></param>
      public static void LogRngSlct(RangeData range)
      {
         Logger.Instance.WriteDevToLog("RangeData");
         
         switch (range.Min.Type)
         {
            case RangeType.RangeNoVal:
               Logger.Instance.WriteDevToLog("min_typ - DB_RNG_NO_VAL");
               break;
            case RangeType.RangeParam:
               Logger.Instance.WriteDevToLog("min_typ - DB_RNG_PARAM");
               break;
            case RangeType.RangeMinMax:
               Logger.Instance.WriteDevToLog("min_typ - DB_RNG_MIN_MAX");
               break;
            default:
               Logger.Instance.WriteDevToLog("min_typ - UNDEFINED");
               break;
         }
         switch (range.Max.Type)
         {
            case RangeType.RangeNoVal:
               Logger.Instance.WriteDevToLog("max_typ - DB_RNG_NO_VAL");
               break;
            case RangeType.RangeParam:
               Logger.Instance.WriteDevToLog("max_typ - DB_RNG_PARAM");
               break;
            case RangeType.RangeMinMax:
               Logger.Instance.WriteDevToLog("max_typ - DB_RNG_MIN_MAX");
               break;
            default:
               Logger.Instance.WriteDevToLog("max_typ - UNDEFINED");
               break;
         }

         return;

      }

      /// <summary>
      /// SQL3LogSqldataOutput()
      /// </summary>
      /// <param name="sqlvar"></param>
      public static void SQL3LogSqldataOutput(Sql3SqlVar sqlVar)
      {
         int fldLen;

         if (sqlVar.NullIndicator == 1)
         {
            Logger.Instance.WriteToLog("\n\tRESULT: NULL", true);
         }
         else
         {
            switch (sqlVar.SqlType)
            {
               case Sql3Type.SQL3TYPE_STR:
               case Sql3Type.SQL3TYPE_DATE:
                  if (sqlVar.SqlLen < 40)
                  {
                     fldLen = sqlVar.SqlLen;
                     Logger.Instance.WriteToLog(string.Format("\n\tRESULT: {0}", sqlVar.SqlData), true);
                  }
                  else
                  {
                     Logger.Instance.WriteToLog(string.Format("\n\tRESULT TRUNCATED TO 40: {0}", sqlVar.SqlData), true);
                  }
                  break;
               case Sql3Type.SQL3TYPE_WSTR:
               case Sql3Type.SQL3TYPE_BSTR:
                  if (sqlVar.SqlLen < 40)
                  {
                     fldLen = (sqlVar.SqlLen / 2) - 1;
                     Logger.Instance.WriteToLog(string.Format("\n\tRESULT: {0}", sqlVar.SqlData), true);
                  }
                  else
                  {
                     Logger.Instance.WriteToLog(string.Format("\n\tRESULT TRUNCATED TO 40: {0}", sqlVar.SqlData), true);
                  }
                  break;
               case Sql3Type.SQL3TYPE_UI1:
               case Sql3Type.SQL3TYPE_I2:
               case Sql3Type.SQL3TYPE_I4:
                  switch (sqlVar.SqlLen)
                  {
                     case 1:
                        Logger.Instance.WriteToLog(string.Format(" \n\tRESULT: {0}",sqlVar.SqlData), true);
                        break;
                     case 2:
                        Logger.Instance.WriteToLog(string.Format(" \n\tRESULT: {0}", short.Parse(sqlVar.SqlData.ToString())), true);
                        break;
                     case 4:
                        Logger.Instance.WriteToLog(string.Format(" \n\tRESULT: {0}", long.Parse(sqlVar.SqlData.ToString())), true);
                        break;
                  }
                  break;
               case Sql3Type.SQL3TYPE_R4:
               case Sql3Type.SQL3TYPE_R8:
                  switch (sqlVar.SqlLen)
                  {
                     case 4:
                        Logger.Instance.WriteToLog(string.Format(" \n\tRESULT: {0}", float.Parse(sqlVar.SqlData.ToString())), true);
                        break;
                     case 8:
                        Logger.Instance.WriteToLog(string.Format(" \n\tRESULT: {0}", double.Parse(sqlVar.SqlData.ToString())), true);
                        break;
                  }
                  break;
               case Sql3Type.SQL3TYPE_BYTES:
                  Logger.Instance.WriteToLog(string.Format(" \n\tRESULT: {0}", sqlVar.SqlData), true);
                  break;
               case Sql3Type.SQL3TYPE_BOOL:
                  Logger.Instance.WriteToLog(string.Format(" \n\tRESULT: {0}", sqlVar.SqlData.ToString()), true);
                  break;
            }
         }

      }

      /// <summary>
      /// SQL3LogSqlda()
      /// </summary>
      /// <param name="sqlda"></param>
      /// <param name="desc"></param>
      public static void SQL3LogSqlda(Sql3Sqldata sqlda, string desc)
      {
         int    i = 0;
         int    fldLen;
         string fullDate = string.Empty;
         string tmp;

         Logger.Instance.WriteToLog(string.Format("SQLDA {0}", desc), true);
         Logger.Instance.WriteToLog(string.Format("name = {0}", sqlda.name), true);
         Logger.Instance.WriteToLog(string.Format("sqln = {0}", sqlda.Sqln), true);
         Logger.Instance.WriteToLog(string.Format("sqld = {0}", sqlda.Sqld), true);
         
         for (i = 0; i < sqlda.Sqln; i++)
         {
            Logger.Instance.WriteToLog(string.Format("sqlvar[{0}].sqlname.data = {1} ---------------------------", i, sqlda.SqlVars[i].SqlName), true);
            Logger.Instance.WriteToLog(string.Format("sqlvar[{0}].sqltype = {1}", i, sqlda.SqlVars[i].SqlType), true);
            Logger.Instance.WriteToLog(string.Format("sqlvar[{0}].sqllen = {1}", i, sqlda.SqlVars[i].SqlLen), true);
            tmp = sqlda.SqlVars[i].SqlData == null ? "null" : sqlda.SqlVars[i].SqlData.ToString();

            switch (sqlda.SqlVars[i].SqlType)
            {

               case Sql3Type.SQL3TYPE_DATE:
                  if ((sqlda.SqlVars[i].dateType != DateType.DATETIME_TO_CHAR &&
                         sqlda.SqlVars[i].dateType != DateType.DATETIME4_TO_CHAR))
                  {
                     /*  olga for right print of internal date  11.05.97*/
                     /* Bugfix - 420702 - esqlc_date_crack should be called to convert date*/

                      sqlda.SQLiteGateway.SQLiteLow.LibDateCrack(tmp, out fullDate, fullDate.Length, sqlda.SqlVars[i].SqlLen, null);
                     Logger.Instance.WriteToLog(string.Format("sqlvar[{0}].sqldata = {1}", i, fullDate), true);
                  }

                  break;
               case Sql3Type.SQL3TYPE_STR:
                  if (sqlda.SqlVars[i].SqlLen < 40)
                  {
                     fldLen = (int)sqlda.SqlVars[i].SqlLen;
                     Logger.Instance.WriteToLog(string.Format("sqlvar[{0}].sqldata = {1}", i, sqlda.SqlVars[i].SqlData), true);
                  }
                  else
                  {
                     Logger.Instance.WriteToLog(string.Format("sqlvar[{0}].sqldata = {1}", i, sqlda.SqlVars[i].SqlData), true);
                  }
                  break;
               case Sql3Type.SQL3TYPE_WSTR:
               case Sql3Type.SQL3TYPE_BSTR:
                  
                  if (sqlda.SqlVars[i].SqlLen < 40)
                  {
                     fldLen = (int)sqlda.SqlVars[i].SqlLen;
                      Logger.Instance.WriteToLog(string.Format("sqlvar[{0}].sqldata = {1}", i, tmp), true);

                  }
                  else
                  {
                     fldLen = sqlda.SqlVars[i].SqlLen > 40 ? 40 : sqlda.SqlVars[i].SqlLen;
                     Logger.Instance.WriteToLog(string.Format("sqlvar[{0}].sqldata = {1} (TRUNCATED TO 40)", i, tmp), true);
                  }
                  break;
               case Sql3Type.SQL3TYPE_UI1:
               case Sql3Type.SQL3TYPE_I2:
               case Sql3Type.SQL3TYPE_I4:
                  {
                     switch (sqlda.SqlVars[i].SqlLen)
                     {
                        case 1:
                           Logger.Instance.WriteToLog(string.Format("sqlvar[{0}].sqldata = :{1}: (integer)", i, tmp), true);
                           break;
                        case 2:
                           Logger.Instance.WriteToLog(string.Format("sqlvar[{0}].sqldata = :{1}: (integer)", i, tmp), true);
                           break;
                        case 4:
                           Logger.Instance.WriteToLog(string.Format("sqlvar[{0}].sqldata = :{1}: (integer)", i, tmp), true);
                           break;
                     }
                  }
                  break;
               case Sql3Type.SQL3TYPE_R4:
               case Sql3Type.SQL3TYPE_R8:
                  {
                     switch (sqlda.SqlVars[i].SqlLen)
                     {
                        case 4:
                             Logger.Instance.WriteToLog(string.Format("sqlvar[{0}].sqldata = :{1}: (float)", i, tmp), true);
                           break;
                        case 8:
                           Logger.Instance.WriteToLog(string.Format("sqlvar[{0}].sqldata = :{1}: (double)", i, tmp), true);
                           break;
                     }
                  }
                  break;
            }

         }

      }

      /// <summary>
      /// SQL3LogNumberFld()
      /// </summary>
      /// <param name="sqlvar"></param>
      public static void SQL3LogNumberFld(Sql3SqlVar sqlvar)
      {
         short shortVal = 0;
         long longVal = 0;
         int printFieldLen;
         float floatVal = 0.0F;

         switch (sqlvar.SqlType)
         {
            case Sql3Type.SQL3TYPE_I2:
            case Sql3Type.SQL3TYPE_I4:
            case Sql3Type.SQL3TYPE_R4:
            case Sql3Type.SQL3TYPE_R8:
               if (sqlvar.SqlLen == 2)
               {
                  shortVal = short.Parse(sqlvar.SqlData.ToString());
                  Logger.Instance.WriteToLog(string.Format("SQL3LogNumberFld(): sqlvar.SqlData = {0}", shortVal), true);
               }
               else if (sqlvar.SqlLen == 4)
               {
                  if (sqlvar.typeAffinity == TypeAffinity.TYPE_AFFINITY_REAL)
                  {
                     floatVal = float.Parse(sqlvar.SqlData.ToString());
                     Logger.Instance.WriteToLog(string.Format("SQL3LogNumberFld():sqlvar.SqlData = {0}", floatVal), true);
                  }
                  else
                  {
                     longVal = long.Parse(sqlvar.SqlData.ToString());
                     Logger.Instance.WriteToLog(string.Format("SQL3LogNumberFld():sqlvar.SqlData = {0}", longVal), true);
                  }
               }
               else
               {
                  Logger.Instance.WriteToLog(string.Format("SQL3LogNumberFld(): sqlvar.SqlData = {0}", sqlvar.SqlData), true);
               }
               break;

            default:
               if (sqlvar.SqlLen <= 40)
               {
                  printFieldLen = (int)sqlvar.SqlLen;
                  Logger.Instance.WriteToLog(string.Format("SQL3LogNumberFld(): sqlvar.SqlData = {0}", sqlvar.SqlData), true);

               }
               else
               {
                  Logger.Instance.WriteToLog("SQL3LogNumberFld(): sqlvar->sqllen bigger than 40, db_crsr->buf is not printed", true);
               }
               break;
         }

      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="b"></param>
      /// <param name="len"></param>
      /// <returns></returns>
      public string Sql3DbgBinaryToStr(string b, long len)
      {
         return string.Empty;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="s"></param>
      /// <param name="sSizeInChars"></param>
      /// <param name="b"></param>
      /// <param name="len"></param>
      /// <returns></returns>
      public string Sql3BinaryToStr(string s, int sSizeInChars, string b, long len)
      {
         return string.Empty;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="s"></param>
      /// <param name="b"></param>
      /// <param name="len"></param>
      /// <returns></returns>
      public string Sql3BinaryToStrLogical(string s, string b, long len)
      {
         return string.Empty;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="dest"></param>
      /// <param name="source"></param>
      /// <param name="len"></param>
      /// <returns></returns>
      public string Sql3PartBinaryToStr2(string dest, string source, long len)
      {
         return string.Empty;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="s"></param>
      /// <param name="sSizeInChars"></param>
      /// <param name="b"></param>
      /// <param name="len"></param>
      /// <returns></returns>
      public long Sql3VarbinaryToStr(string s, long sSizeInChars, string b, long len)
      {
         return 0;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="dest"></param>
      /// <param name="source"></param>
      /// <param name="len"></param>
      /// <returns></returns>
      long Sql3PartVarbinaryToStr2(string dest, string source, long len)
      {
         return 0;
      }

   }
}
