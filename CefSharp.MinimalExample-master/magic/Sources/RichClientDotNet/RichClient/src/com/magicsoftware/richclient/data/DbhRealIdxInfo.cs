using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using com.magicsoftware.util;
using com.magicsoftware.gatewaytypes.data;

namespace com.magicsoftware.richclient.data
{
   /// <summary>
   /// This class is introduced to hold all dbh's ctl information, real idx in order to support data source literal.
   /// Store the dbh's ctl information i.e. for each ctl and for all it's available data sources : ctlIdx, realIdx, dbh->ctlIdx, dbh->isn.
   /// At client, each data source have dbh->ctlIdx and dbh->isn. So, In order to get any dbh using DSOURCE Literal at client,
   /// component info is needed. So providing map of task's ctlIdx, realIdx, dbh->ctlIdx, dbh->isn.
   /// At client, using current task's ctlIdx and literal value (which is actually real idx for that ctl), we will get 
   /// dbh->ctlIdx and dbh->isn. And using dbh->ctlIdx and dbh->isn, we get a correct dbh (dbh->ctlIdx + dbh->isn).

   /// </summary>
   internal class DbhRealIdxInfo
   {
      private readonly Hashtable dbhRealIdxInfoTab;

      /// <summary>
      /// c'tor
      /// </summary>
      internal DbhRealIdxInfo()
      {
         dbhRealIdxInfoTab = new Hashtable();
      }

      /// <summary>
      /// parse the dbh's ctl information.
      /// </summary>
      /// <param name="dbhRealIdxs"></param>
      internal void fillDbhRealIdxs(String dbhRealIdxs)
      {
         // clear the  dbhRealIdxsTab.

         dbhRealIdxInfoTab.Clear();
         if (dbhRealIdxs.Trim().Length > 0)
         {
            string[] dbhRealIdxStr = dbhRealIdxs.Split(";".ToCharArray());

            ////first is blank entry skip it

            for (int i = 1; i < dbhRealIdxStr.Length; i++)
            {
               String[] dbhRealIdxVal = dbhRealIdxStr[i].Split(",".ToCharArray());

               System.Diagnostics.Debug.Assert(dbhRealIdxVal.Length == 4);

               DataSourceIdKey dataSourceIdKey = new DataSourceIdKey(Convert.ToInt32(dbhRealIdxVal[0]), Convert.ToInt32(dbhRealIdxVal[1]));

               DataSourceId dataSourceId = new DataSourceId(Convert.ToInt32(dbhRealIdxVal[2]), Convert.ToInt32(dbhRealIdxVal[3]));

               dbhRealIdxInfoTab.Add(dataSourceIdKey, dataSourceId);
            }
         }
      }

      /// <summary>
      /// Get DataSourceId using taskCtlIdx and realIdx.
      /// </summary>
      /// <param name="taskCtlIdx"></param>
      /// <param name="realIdx"></param>
      internal DataSourceId GetDataSourceId(int taskCtlIdx, int realIdx)
      {
         DataSourceIdKey dataSourceIdKey = new DataSourceIdKey(taskCtlIdx, realIdx);
         return (DataSourceId)(dbhRealIdxInfoTab[dataSourceIdKey]);
      }
      
   }
}
