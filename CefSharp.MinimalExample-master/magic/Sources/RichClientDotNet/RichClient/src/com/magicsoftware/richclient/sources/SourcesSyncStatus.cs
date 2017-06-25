using System;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.richclient.cache;
using util.com.magicsoftware.util;

namespace com.magicsoftware.richclient.sources
{
   /// <summary>This class is responsible for maintaining the status of sources syncronization</summary>
   internal class SourcesSyncStatus
   {
      // TablesIncompatibleWithDataSources flag: structures for one or more tables in local database doesn't match with their sources.
      internal bool TablesIncompatibleWithDataSources { get; set; }

      // InvalidSources flag: sources are not synchronized.
      internal bool InvalidSources { get; set; }

      // This file containing the status will be saved only in case of failure.
      private String StatusFileName { get; set; }
      
      internal SourcesSyncStatus()
      {
         TablesIncompatibleWithDataSources = false;
         InvalidSources = false;

         String cachFoldername = CacheUtils.GetCacheFolderName();
         String fileName = ClientManager.Instance.getAppName() + "_SourcesSyncStatus";
         StatusFileName = cachFoldername + "\\" + fileName;

         // Read the synchronization status of sources in previous execution, if exist.
         // If file does not exist, this means in previous execution sources were synchronized without any failure.
         if (HandleFiles.isExists(StatusFileName))
            ReadFromFile();
      }

      /// <summary>
      /// Read sources syncronisation status in previous execution 
      /// </summary>
      private void ReadFromFile()
      {
         System.IO.StreamReader file = new System.IO.StreamReader(StatusFileName);

         String line;
         int count = 1;

         while ((line = file.ReadLine()) != null && count <= 2)
         {
            count++;
            String[] nameValuePair = line.Split('=');
            if (nameValuePair.Length == 2)
            {
               if (nameValuePair[0].Trim().Equals("TablesIncompatibleWithDataSources") && nameValuePair[1].Trim().Equals("Y"))
                  TablesIncompatibleWithDataSources = true;
               else if (nameValuePair[0].Trim().Equals("InvalidSources") && nameValuePair[1].Trim().Equals("Y"))
                  InvalidSources = true;
            }
         }

         file.Close();
      }

      /// <summary>
      /// Save sources syncronisation status in current execution to status file
      /// </summary>
      public void SaveToFile ()
      {
         System.IO.StreamWriter file = new System.IO.StreamWriter(StatusFileName, false);

         if (TablesIncompatibleWithDataSources)
            file.WriteLine("TablesIncompatibleWithDataSources=Y");
         if (InvalidSources)
            file.WriteLine("InvalidSources=Y");

         file.Close();
      }

      /// <summary>
      /// Clear status
      /// </summary>
      public void Clear()
      {
         TablesIncompatibleWithDataSources = false;
         InvalidSources = false;
         HandleFiles.deleteFile(StatusFileName);
      }        
   
   }
}
