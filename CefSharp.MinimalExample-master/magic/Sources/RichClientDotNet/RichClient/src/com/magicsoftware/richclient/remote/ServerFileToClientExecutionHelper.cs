using System;
using System.Collections.Generic;
using System.Diagnostics;
using util.com.magicsoftware.util;

namespace com.magicsoftware.richclient.remote
{
   // This helper class supports retrieving multiple files from server
   internal class ServerFileToClientExecutionHelper
   {
      // On requesting URLs of multiple files (folder/wildcards) by client, the filenames returned by server are saved in this list.
      internal List<String> ServerFileNames = new List<String>();

      // The flag indicates whether client has issued the request for single file or multiple files      
      internal bool RequestedForFolderOrWildcard { get; set; }

      /// <summary>
      /// Get contents of files that returned by server on requesting URLs for matching files.
      /// </summary>
      internal void GetMultipleFilesFromServer()
      {
         // Note: This method retrieves contents of each file sequentially. In future, it may extended to support getting
         // content of multiples in one go.

         RemoteCommandsProcessor server = RemoteCommandsProcessor.GetInstance();
         foreach (string filename in ServerFileNames)
         {
            try
            {
               Debug.Assert(server.ServerFilesToClientFiles.ContainsKey(filename));

               String cachedUrl = server.ServerFilesToClientFiles[filename];
               
               //get the file from the server to local cache if still not cached. File will not be loaded into the memory. 
               server.DownloadContent(cachedUrl);
            }
            catch (System.Exception ex)
            {
               Logger.Instance.WriteExceptionToLog(ex);
            }
         }
      }
   }
}
