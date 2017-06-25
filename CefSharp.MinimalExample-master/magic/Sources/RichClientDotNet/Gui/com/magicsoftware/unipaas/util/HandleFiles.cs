using System;
using System.Text;
using System.IO;
using System.Threading;
using com.magicsoftware.win32;
using System.Collections.Generic;
#if !PocketPC
using OSEnvironment = com.magicsoftware.util.OSEnvironment;
#else
using OSEnvironment = com.magicsoftware.richclient.mobile.util.OSEnvironment;
#endif
using com.magicsoftware.util;
using util.com.magicsoftware.util;

namespace com.magicsoftware.unipaas.util
{
   public class HandleFiles
   {
      internal static char CONTENT_TYPE_ANSI = '0';
      public static char CONTENT_TYPE_SMALL_ENDIAN = '1';
      public static char CONTENT_TYPE_BIG_ENDIAN = '2';
      public static char CONTENT_TYPE_UTF8 = '3';

      internal static byte[] SMALL_ENDIAN_BOM_BYTE = new byte[] { (byte)0xFF, (byte)0xFE }; // BOM char which distinguishes file to be in small endian format
      internal static byte[] BIG_ENDIAN_BOM_BYTE = new byte[] { (byte)0xFE, (byte)0xFF }; // BOM char which distinguishes file to be in big endian format
      internal static byte[] UTF8_BOM_BYTE = new byte[] { (byte)0xEF, (byte)0xBB, (byte)0xBF }; // BOM char which distinguishes file to be in UTF8 format
      public static int endianLen = SMALL_ENDIAN_BOM_BYTE.Length;
      public static int utf8len = UTF8_BOM_BYTE.Length;

      private HandleFiles()
      {
      }

      /// <summary>Gets the size of the file in bytes</summary>
      /// <param name="filename">file</param>
      /// <returns>size of the file in bytes</returns>
      public static long getFileSize(String filename)
      {
         FileInfo fi = new FileInfo(filename);

         return (fi.Exists ? fi.Length : 0);
      }

      /// <returns> true if 'filename's last modification date equals 'lastModication'</returns>
      /// <param name="filename">java.lang.String the file name</param>
      public static String getFileTime(String filename)
      {
         String lastModicationTime = null;
         try
         {
            FileInfo f = new FileInfo(filename);
            lastModicationTime = DateTimeUtils.ToString(f.LastWriteTime, XMLConstants.CACHED_DATE_TIME_FORMAT);
         }
         catch (Exception ex)
         {
            Events.WriteExceptionToLog(ex);
         }
         return lastModicationTime;
      }

      /// <summary> Set the file last modification date.</summary>
      /// <returns> last modification date.</returns>
      /// <param name="filename">java.lang.String the file name</param>
      public static void setFileTime(String filename, String lastModicationTime)
      {
#if PocketPC
         FileInfo f = new FileInfo(filename);

         long lCreationTime = f.CreationTime.ToFileTime();
         long lAccessTime = f.LastAccessTime.ToFileTime();

         long lWriteDTime;
         try
         {
            // Topic #16 (MAGIC version 1.8 for WIN) RC - Cache security enhancements:
            lWriteDTime = DateTimeUtils.Parse(lastModicationTime).ToFileTime();
            IntPtr handle = user32.CreateFile(filename, user32.WRITE, FileShare.Read, IntPtr.Zero, FileMode.Open, user32.NORMAL, IntPtr.Zero);
            user32.SetFileTime(handle, ref lCreationTime, ref lAccessTime, ref lWriteDTime);
            user32.CloseHandle(handle);
         }
         catch (Exception ex)
         {
            Events.WriteExceptionToLog(ex);
         }
#else
         try
         {
            FileInfo f = new FileInfo(filename);
            f.LastWriteTime = DateTimeUtils.Parse(lastModicationTime);
         }
         catch (Exception ex)
         {
            Events.WriteExceptionToLog(ex);
         }
#endif
      }

      /// <summary>Creates the file and all its parent directories and returns a File pointer to the file.</summary>
      /// <returns> File a pointer to the file or null if fails;</returns>
      /// <param name="fullPath">java.lang.String the file to get</param>
      public static FileInfo getFile(String fullPath)
      {
         FileInfo file = null;

         try
         {
            file = new FileInfo(fullPath);

            if (!file.Exists)
            {
               /** create the directories */
               String dirName = file.DirectoryName;
               if (dirName != null)
                  Directory.CreateDirectory(dirName);

               FileStream fs = file.Create();
               fs.Close();

               file = new FileInfo(fullPath);
            }
         }
         catch (Exception ex)
         {
            Events.WriteExceptionToLog(ex);
         }

         return file;
      }

      /// <summary>Copy the file from Source to destination.</summary>
      /// <param name="srcPath">path of the file to be copied</param>
      /// <param name="dstPath">path of the destination file</param>
      /// <param name="override">overwite the file or not</param>
      /// <param name="bCreateDstFolders">if folder does not exists, then should create folder or not</param>
      /// <returns>true if file is successfuly copied to desination</returns>
      public static bool copy(String srcPath, String dstPath, bool overrideDst, bool createDstFolders)
      {
         try
         {
            if (File.Exists(dstPath))
            {
               if (!overrideDst)
                  return true;
            }
            else if (createDstFolders)
            {
               getFile(dstPath);
               overrideDst = true;
            }

            File.Copy(srcPath, dstPath, overrideDst);

            return true;
         }
         catch (Exception ex)
         {
            Events.WriteExceptionToLog(ex);
            return false;
         }
      }

      /// <summary>Checks if the file exists.</summary>
      /// <param name="fullFilename">full path of the file or the directory</param>
      /// <returns>true - if the file or directory exists</returns>
      public static bool isExists(String fullFilename)
      {
         return (File.Exists(fullFilename) || Directory.Exists(fullFilename));
      }

      /// <summary> Return the file as a byte array.</summary>
      /// <returns> byte[] the file data; null if fails.</returns>
      /// <param name="filename">the file name</param>
      public static byte[] readToByteArray(String filename, String relativePath)
      {
         byte[] buffer = null;

         try
         {
            // if the request is for a higher path
            if (checkRelativePath(relativePath))
               return null;

            using (FileStream fis = File.OpenRead(filename))
            {
               buffer = new byte[fis.Length];
               fis.Read(buffer, 0, buffer.Length);
               fis.Close();
            }
         }
         catch (Exception ex)
         {
            Events.WriteExceptionToLog(ex);
            buffer = null;
         }

         return buffer;
      }

      /// <param name="filePath"></param>
      /// <returns></returns>
      private static bool checkRelativePath(String filePath)
      {
         int rootPathIndex = 0;
         char pathChar = Path.DirectorySeparatorChar;

         for (int i = 0; i < filePath.Length; i++)
         {
            char currentChar = filePath[i];

            // handling './' and '../'
            if (currentChar == '.')
            {
               // handling only combination when '/' was entered before
               if (i == 0 || (filePath[i - 1] == pathChar))
               {
                  if (filePath.Length > i + 1 && filePath[i + 1] == pathChar)
                  {
                     // skipping the './' pair
                     i++;
                  }
                  // handling '../'
                  else if (filePath.Length > i + 2 && filePath[i + 1] == '.' && filePath[i + 2] == pathChar)
                  {
                     rootPathIndex--;
                     i += 2;
                  }
                  else
                  {
                     // do nothing
                     ;
                  }
                  while (filePath.Length > i + 1 && filePath[i + 1] == pathChar)
                     i++;
               }
            }
            // handling '/' and '//': (/)+
            else if (currentChar == pathChar)
            {
               rootPathIndex++;
               while (filePath.Length > i + 1 && filePath[i + 1] == pathChar)
                  i++;
            }
         }
         return rootPathIndex < 0;
      }

      /// <summary> Write the file content as String.</summary>
      /// <returns> boolean</returns>
      /// <param name="path">String</param>
      /// <param name="data">String</param>
      /// <param name="bNewLine">boolean add new line at the end of the data</param>
      /// <param name="bAppend">boolean append the data to existing data</param>
      internal static bool writeToFile(String path, String data, bool bNewLine, bool bAppend)
      {
         bool success = false;

         try
         {
            FileMode fileMode = bAppend ? FileMode.Append : FileMode.Create;

            using (FileStream fs = new FileStream(path, fileMode, FileAccess.Write))
            {
               if (bNewLine)
                  data = data + OSEnvironment.EolSeq;

               byte[] bytes = Encoding.Default.GetBytes(data);

               fs.Write(bytes, 0, bytes.Length);
            }

            success = true;
         }
         catch (Exception ex)
         {
            Events.WriteExceptionToLog(ex);
         }

         return success;
      }

      /// <summary> Delete recursivly the directory and all its files.</summary>
      /// <returns> boolean</returns>
      /// <param name="fullPath">String</param>
      public static bool deleteDir(String fullPath)
      {
         bool result = false;
         try
         {
            Directory.Delete(fullPath, true);
            result = true;
         }
         catch (Exception ex)
         {
            Events.WriteExceptionToLog(ex);
         }
         return result;
      }

      /// <summary>Write the byte array to file</summary>
      public static bool writeToFile(String filename, byte[] content)
      {
         FileInfo finfo = new FileInfo(filename);

         return (writeToFile(finfo, content, false, false));
      }

      /// <param name="file"></param>
      /// <param name="b"></param>
      /// <param name="append"></param>
      /// <param name="doLock"></param>
      public static bool writeToFile(FileInfo file, byte[] b, bool append, bool doLock)
      {
         return writeToFile(file, b, append, doLock, 10);
      }

      /// <param name="file"></param>
      /// <param name="b"></param>
      /// <param name="append"></param>
      /// <param name="doLock"></param>
      /// <param name="maxRetryTime">Time in seconds for retrying to access the file.</param>
      public static bool writeToFile(FileInfo file, byte[] b, bool append, bool doLock, int maxRetryTime)
      {

         bool success = false;
         FileStream fos = null;

         try
         {
            FileMode mode;

            if (append)
               mode = FileMode.Append;
            else
               mode = FileMode.Create;

            if (doLock)
            {
               bool fileOpened = false;
               long endOfRetryTime = Misc.getSystemMilliseconds() + maxRetryTime * 1000;

               while (!fileOpened)
               {
                  try
                  {
                     fos = new FileStream(file.FullName, mode, FileAccess.Write, FileShare.Read);
                     fileOpened = true;
                  }
                  catch (Exception ex)
                  {
                     long systemMilliseconds = Misc.getSystemMilliseconds();
                     if (systemMilliseconds > endOfRetryTime)
                     {
                        Events.WriteDevToLog(string.Format("writeToFile({0}, append={1}, doLock={2}, maxRetryTime={3}sec): failed to open file (wait timeout is over). Exiting with exception: {4} : {5}{6}",
                                                               file.FullName, append.ToString(), doLock.ToString(), maxRetryTime, ex.GetType(), OSEnvironment.EolSeq, ex.Message));
                        throw ex;
                     }
                     else
                     {
                        Events.WriteDevToLog(string.Format("writeToFile({0}, append={1}, doLock={2}, maxRetryTime={3}sec): waiting for timeout",
                                                               file.FullName, append.ToString(), doLock.ToString(), maxRetryTime));
                        Thread.Sleep(1000);
                     }
                  }
               }
            }
            else
            {
               fos = new FileStream(file.FullName, mode, FileAccess.Write, FileShare.ReadWrite);
            }

            fos.Write(b, 0, b.Length);
            fos.Close();
            fos = null;
            success = true;
         }
         catch (Exception ex)
         {
            Events.WriteExceptionToLog(string.Format("writeToFile({0}, append={1}, doLock={2}, maxRetryTime={3}sec): failed with exception: {4} : {5}{6}{7}{8}",
                                                         file.FullName, append.ToString(), doLock.ToString(), maxRetryTime, ex.GetType(), OSEnvironment.EolSeq,
                                                         ex.StackTrace, OSEnvironment.EolSeq, ex.Message));
         }
         return success;
      }

      /// <summary>Deletes the given file.</summary>
      /// <param name="filename">file to be deleted</param>
      /// <returns>true if file is successfully deleted</returns>
      public static bool deleteFile(String filename)
      {
         bool tmpBool = false;

         try
         {
            if (File.Exists(filename))
            {
               File.Delete(filename);
               tmpBool = true;
            }
         }
         catch (Exception ex)
         {
            Events.WriteExceptionToLog(ex);
            tmpBool = false;
         }

         return tmpBool;
      }

      /// <summary>Renames the file.</summary>
      /// <param name="i_currName">the source name</param>
      /// <param name="i_newName">the new name</param>
      /// <returns>true if file is successfully renamed</returns>
      public static bool renameFile(String i_currName, String i_newName)
      {
          bool done = true;
          if (i_currName != i_newName)
          {
              try
              {
                  // http://msdn.microsoft.com/en-us/library/system.io.file.move.aspx:
                  // .. You cannot use the Move method to overwrite an existing file.
                  if (File.Exists(i_newName))
                      File.Delete(i_newName);
                  File.Move(i_currName, i_newName);
              }
              catch (Exception ex)
              {
                  Events.WriteExceptionToLog(ex);
                  done = false;
              }
          }
          return done;
      }

      /// <param name="i_fullDirName"></param>
      /// <returns></returns>
      public static bool createDir(String i_fullDirName)
      {
         bool bRez = false;
         try
         {
            if (!isDirectory(i_fullDirName))
            {
               Directory.CreateDirectory(i_fullDirName);
            }
            bRez = true;
         }
         catch (Exception)
         {
         }
         return bRez;
      }

      /// <summary> Retuen the file content as a String.</summary>
      /// <returns> java.lang.String the file content or null;</returns>
      /// <param name="filename">java.lang.String the file to read</param>
      public static String readToString(String filename)
      {
         return readToString(filename, null);
      }

      /// <summary> Retuen the file content as a String.</summary>
      /// <returns> java.lang.String the file content or null;</returns>
      /// <param name="filename"></param>
      /// <param name="charset">java.lang.String the file to read</param>
      public static String readToString(String filename, Encoding encoding)
      {
         String retStr = null;
         byte[] bytes;

         try
         {
            bytes = readToByteArray(filename, "");

            if (encoding == null)
               encoding = Encoding.Default;

            retStr = encoding.GetString(bytes, 0, bytes.Length);
         }
         catch (Exception ex)
         {
            Events.WriteExceptionToLog(ex);
         }

         return retStr;
      }

      /// <summary> Check if the file exists.</summary>
      /// <returns> boolean TRUE - If the file exists</returns>
      /// <param name="filename">java.lang.String - the file full path</param>
      internal static bool isDirectory(String fullFilename)
      {
         return Directory.Exists(fullFilename);
      }

      /// <summary> Reads the file and gets its content type: SMALL_ENDIAN, BIG_ENDIAN, UTF8 or ANSI</summary>
      /// <param name="filename"></param>
      /// <returns></returns>
      public static char GetFileContentType(String filename)
      {
         char contentType = CONTENT_TYPE_ANSI; // Content type of file which is returned to caller
         byte[] readFileBuf = new byte[3]; // Actual file is read in the byte buffer

         try
         {
            // Read the file into the byte buffer
            using (FileStream fileInputStream = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
               fileInputStream.Read(readFileBuf, 0, readFileBuf.Length);
            }

            // Check whether the file is UTF8
            if (Misc.CompareByteArray(readFileBuf, UTF8_BOM_BYTE, utf8len))
               contentType = CONTENT_TYPE_UTF8;
            else
            {
               //Check whether the file is unicode
               if (Misc.CompareByteArray(readFileBuf, SMALL_ENDIAN_BOM_BYTE, endianLen))
                  contentType = CONTENT_TYPE_SMALL_ENDIAN;
               else if (Misc.CompareByteArray(readFileBuf, BIG_ENDIAN_BOM_BYTE, endianLen))
                  contentType = CONTENT_TYPE_BIG_ENDIAN;
            }
         }
         catch (Exception ex)
         {
            Events.WriteExceptionToLog(ex);
            throw new ApplicationException(ex.Message);
         }
         return contentType;
      }


      /// <summary> </summary>
      /// <param name="directory"></param>
      /// <param name="fileFilter"></param>
      /// <param name="searchSubDir"></param>
      /// <returns></returns>
      /// TODO: Check if this function works correctly (one thing that is missing now is to avoid same file
      /// occurring multiple times due to multiple filters)
      public static List<String> getFileList(String directory, String fileFilter, bool searchSubDir)
      {
         List<String> fileList = new List<String>();
         List<String> directoryList = new List<String>();

         //Split the filter if there are multiple filters separated by '|' 
         String[] splitFilter = fileFilter.Split('|');
         bool directoryExists = Directory.Exists(directory);

         if (directoryExists)
         {
            directoryList.Add(directory);

            if (searchSubDir)
            {
               processSubDirectory(directory, directoryList);
            }

            foreach (String Dir in directoryList)
            {
               for (int j = 0; j < splitFilter.Length; j++)
                  fileList.AddRange(Directory.GetFiles(Dir, splitFilter[j].Trim()));
            }

            fileList = removeDuplicateEntries(fileList);
         }
         
         return fileList;
      }

      /// <summary>Gets the List of all SubDirectories inside Current Directory</summary>
      /// <param name="currDirectory">The Directory to be processed</param>
      /// <param name="directoryList">Updated with list of SubDirectories</param>
      internal static void processSubDirectory(String currDirectory, List<String> directoryList)
      {
         String[] SubDirectories = Directory.GetDirectories(currDirectory);
         if (SubDirectories != null)
         {
            directoryList.AddRange(SubDirectories);
            foreach (String SubDirectory in SubDirectories)
               processSubDirectory(SubDirectory, directoryList);
         }
      }

      /// <summary>Removes the duplicate entries from the List</summary>
      /// <param name="originalList">Original list</param>
      /// <returns>List without duplicate entries. Also if original list is empty, it is returned as such.</returns>
      internal static List<String> removeDuplicateEntries(List<String> originalList)
      {
         List<String> newList = null;

         if (originalList.Count > 0)
         {
            newList = new List<String>();
            bool ResultExist = false;

            //add first entry as it is
            newList.Add(originalList[0]);

            //check whether the entry is repeated...if not then add it to new list
            for (int i = 1; i < originalList.Count; i++)
            {
               ResultExist = false;
               for (int j = 0; j < newList.Count; j++)
               {
                  if (String.Compare(originalList[i], newList[j]) == 0)
                  {
                     ResultExist = true;
                     break;
                  }
               }
               if (!ResultExist)
                  newList.Add(originalList[i]);
            }
         }
         else
            newList = originalList;

         return newList;
      }

      /// <summary>Determines if local time and remote time are same</summary>
      /// <returns></returns>
      public static Boolean equals(String localTimeString, String remoteTimeString)
      {
         Boolean timeStampEqual = false;

         try
         {
            // Topic #16 (MAGIC version 1.8 for WIN) RC - Cache security enhancements:
            DateTime localTime = DateTimeUtils.Parse(localTimeString);
            DateTime remoteTime = DateTimeUtils.Parse(remoteTimeString);

#if PocketPC
            // Problem while copying file from NTFS to FAT file system.
            // If file time stamp contained odd value of second, then HandleFiles.setFileTime rounded this 
            // odd value to previous or next even value (http://support.microsoft.com/kb/127830/en-us )
            // WIN CE uses FAT file system ( http://msdn.microsoft.com/en-us/library/ms900336.aspx)

            // Round remoteTime to even number of seconds and then compare with localtime
            DateTime tmpRemoteTime;
            // get previous even value of second
            tmpRemoteTime = getPreviousEvenValueOfSecond(remoteTime);
            if (localTime == tmpRemoteTime)
               timeStampEqual = true;
            else
            {
               // get next even value of second
               tmpRemoteTime = getNextEvenValueOfSecond(remoteTime);
               if (localTime == tmpRemoteTime)
                  timeStampEqual = true;
            }
#else
            if (localTime == remoteTime)
               timeStampEqual = true;
#endif
         }
         catch (FormatException ex)
         {
            Events.WriteExceptionToLog(ex);
         }

         return timeStampEqual;
      }

      /// <summary>
      /// This function will split initialPath into directory name and file name
      /// </summary>
      /// <param name="initialPath"></param>
      /// <param name="directoryName"></param>
      /// <param name="fileName"></param>
      public static void splitFileAndDir(String initialPath, out String directoryName, out String fileName)
      {
         directoryName = "";
         fileName = "";

         if (!String.IsNullOrEmpty(initialPath) && !isDirectory(initialPath))
         {
            // If initialPath is not a directory, split initialPath into two strings with last index of '\' 
            // 1. path before last '\' will be the directory name.
            // 2. name after last '\' will be set to file name.
            // 3. If '\' not found, set the file name = initialPath.

            int idx = initialPath.LastIndexOf('\\');

            if (idx == -1)
               fileName = initialPath;
            else
            {
               directoryName = initialPath.Substring(0, idx);
               // move the idx to next position (i.e. after last '\')
               idx++;
               fileName = initialPath.Substring(idx, initialPath.Length - idx);
            }
         }
         else
            // if initialPath is an existing directory, set directoryName = initialPath
            directoryName = initialPath;
      }

      /// <summary>
      /// prefix file:// protocol to the given file name (only if not already containing a protocol).
      /// </summary>
      /// <param name="fileName"></param>
      static internal void PrefixFileProtocol(ref string fileName)
      {
         if (!string.IsNullOrEmpty(fileName) &&
             !fileName.StartsWith(Constants.HTTPS_PROTOCOL, StringComparison.CurrentCultureIgnoreCase) &&
             !fileName.StartsWith(Constants.HTTP_PROTOCOL, StringComparison.CurrentCultureIgnoreCase) &&
             !fileName.StartsWith(Constants.FILE_PROTOCOL, StringComparison.CurrentCultureIgnoreCase))
            fileName = Constants.FILE_PROTOCOL + fileName;
      }

      /// <summary>
      /// truncate the file:// protocol (if exists) from the given file name (file:// is allowed by Window's Files Explorer, but not by the .Net Framework ....)
      /// </summary>
      /// <param name="fileName"></param>
      static internal void TruncateFileProtocol(ref string fileName)
      {
         if (fileName.StartsWith(Constants.FILE_PROTOCOL))
            fileName = fileName.Substring(Constants.FILE_PROTOCOL.Length);
      }

#if PocketPC
      /// <summary> Returns Previous even value of second for odd valued second
      /// If filetime is "11/03/2009 17:59:57" then change it to "11/03/2009 17:59:56"</summary>
      /// <param name="filetime"></param>
      /// <returns></returns>
      internal static DateTime getPreviousEvenValueOfSecond(DateTime filetimeVal)
      {
         if (filetimeVal.Second % 2 != 0)       //contains odd value of second, so change it to previous even value
            filetimeVal = filetimeVal.AddSeconds(-1);

         return filetimeVal;
      }

      /// <summary> Returns next even value of second for odd valued second
      /// If filetime is "11/03/2009 17:59:57" then change it to "11/03/2009 17:59:58"</summary>
      /// <returns></returns>
      internal static DateTime getNextEvenValueOfSecond(DateTime filetimeVal)
      {
         if (filetimeVal.Second % 2 != 0)       //contains odd value of second, so change it to next even value
            filetimeVal = filetimeVal.AddSeconds(1);

         return filetimeVal;
      }
#endif

   }
}
