using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using com.magicsoftware.win32;
using com.magicsoftware.unipaas.gui;
using com.magicsoftware.unipaas.management.tasks;
using com.magicsoftware.util;

namespace com.magicsoftware.unipaas.util
{
   public class ProcessLauncher
   {
      /// <summary>
      /// </summary>
      /// <param name = "command">OsCommand to be executed</param>
      /// <param name = "wait">wait property</param>
      /// <param name = "show">show property</param>
      /// <param name = "errMsg">hold error message string, if command execution fails.</param>
      /// <param name = "exitCode">exit code returned by process</param>
      /// <returns>this returns errcode, if command fails to execute. </returns>
      public static int InvokeOS(String command, bool wait, CallOsShow show, ref String errMsg, ref int exitCode)
      {
         String[] commands = null;
         int retcode = -1;

         if (command == null)
            command = "";

         if (command.Length > 0)
            commands = SeparateParams(command);

         if (commands != null && commands.Length > 0)
         {
            try
            {
               var processInfo = new ProcessStartInfo(commands[0], commands[1]);
#if !PocketPC

               switch (show)
               {
                  case CallOsShow.Hide:
                     processInfo.WindowStyle = ProcessWindowStyle.Hidden;
                     break;
                  case CallOsShow.Normal:
                     processInfo.WindowStyle = ProcessWindowStyle.Normal;
                     break;
                  case CallOsShow.Maximize:
                     processInfo.WindowStyle = ProcessWindowStyle.Maximized;
                     break;
                  case CallOsShow.Minimize:
                     processInfo.WindowStyle = ProcessWindowStyle.Minimized;
                     break;
               }
#endif
               System.Diagnostics.Process process = System.Diagnostics.Process.Start(processInfo);
               retcode = 0;

               if (wait)
               {
                  process.WaitForExit();
                  if (process.HasExited)
                     exitCode = process.ExitCode;
               }
            }
            catch (Win32Exception e)
            {
               retcode = e.NativeErrorCode;  // Return the error code returned if process start fails.
               if (e.NativeErrorCode == (int)Win32ErrorCode.ERROR_ACCESS_DENIED)
                  errMsg = "Permission Denied";
               else if (e.NativeErrorCode == (int)Win32ErrorCode.ERROR_FILE_NOT_FOUND)
                  errMsg = "File not found: " + commands[0];
               else
                  errMsg = e.Message;
            }
            catch (Exception)
            {
               errMsg = "Loading error: " + commands[0];
               retcode = -1;  // Return -1 in case of any other exception.
            }
         }

         return retcode;
      }

      /// <summary>
      ///   Separates the executable file and arguments from a command string
      /// </summary>
      /// <param name = "cmd">the command</param>
      /// <returns>returns argument list specified in command</returns>
      internal static String[] SeparateParams(String cmd)
      {
         const char DOUBLE_QUOTE = '"';
         const char SPACE_CHAR = ' ';
         var parameters = new String[2];
         String appName = null;
         String param = null;
         int startIdx = 0;
         int endIdx = -1;

         cmd = cmd.Trim();

         startIdx = 0;
         endIdx = -1;

         /* if the first char is double quote, look for the matching double quote. */
         if (cmd[startIdx] == DOUBLE_QUOTE)
            endIdx = cmd.IndexOf(DOUBLE_QUOTE, startIdx + 1);

         /* if there is no double qoute, search for the first space character. */
         if (endIdx == -1)
            endIdx = cmd.IndexOf(SPACE_CHAR, startIdx);

         /* if there is no space char as well, this means that we have only one param. */
         if (endIdx == -1)
            endIdx = cmd.Length - 1;

         // Read the application name
         appName = cmd.Substring(startIdx, (endIdx + 1) - (startIdx));
         parameters[0] = appName.Trim();

         // Read the arguments
         param = cmd.Substring(endIdx + 1, (cmd.Length) - (endIdx + 1));
         parameters[1] = param.Trim();

         return parameters;
      }


   }
}
