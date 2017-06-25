using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.util;
using System.IO;
using com.magicsoftware.unipaas;
using System.Diagnostics;
using com.magicsoftware.unipaas.management.gui;

#if PocketPC
using com.magicsoftware.richclient.mobile.util;
using com.magicsoftware.richclient;
#endif


namespace Gui.com.magicsoftware.unipaas.management.gui
{
   public class EnvControlsPersistencyPath
   {
      public const string PreffixControlsPersistencyProgramDirectory = "prg_";
      public const string PreffixControlsPersistencyFileName = "CP_";

      private static EnvControlsPersistencyPath _instance; //singleton

      private readonly String _defaultPath;
      private string ControlsPersistencyPath
      {
         get
         {
            String retControlsPersistencyPath = Manager.Environment.GetControlsPersistencyPath();
            retControlsPersistencyPath = retControlsPersistencyPath.Trim();

            if (String.IsNullOrEmpty(retControlsPersistencyPath))
               retControlsPersistencyPath = _defaultPath;

            retControlsPersistencyPath.TrimEnd();

            retControlsPersistencyPath = Events.TranslateLogicalName(retControlsPersistencyPath);

            // add \\ in the end of the current dir if needed 
            if (!(retControlsPersistencyPath.EndsWith("\\")))
               retControlsPersistencyPath += Path.DirectorySeparatorChar;

            if (!IsAbsoluteUri(retControlsPersistencyPath))
            {
               string currentDir = System.IO.Directory.GetCurrentDirectory();

               if (!(retControlsPersistencyPath.StartsWith("\\")))
                  currentDir += Path.DirectorySeparatorChar;
               
               // add the current dir 
               retControlsPersistencyPath = retControlsPersistencyPath.Insert(0, currentDir);

               if (!(retControlsPersistencyPath.EndsWith("\\")))
                  retControlsPersistencyPath += Path.DirectorySeparatorChar;
            }

            return retControlsPersistencyPath;
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="path"></param>
      /// <returns></returns>
      private bool IsAbsoluteUri(string path)
      {
         bool isAbsoluteUri = false;
         Uri uri;
         if (Uri.TryCreate(path, UriKind.RelativeOrAbsolute, out uri))
            isAbsoluteUri = uri.IsAbsoluteUri;        

         return isAbsoluteUri;
      }
      /// <summary>
      ///   CTOR
      /// </summary>
      private EnvControlsPersistencyPath()
      {

#if PocketPC
         _defaultPath = OSEnvironment.getAssemblyFolder();
#else

         //1.	If the path is not defined (default) then the files will be created in the (same folder as the form state persistency file.)
         //•	In xp: C:\Documents and Settings\<user>\Application Data\MSE\<APP GUI ID>\
         //•	In Win 7 and Win 8:  C:\Users\<user>\AppData\Roaming\MSE\<APP GUI ID>         
         _defaultPath = OSEnvironment.get("APPDATA")
                    + Path.DirectorySeparatorChar
                    + (UtilStrByteMode.isLocaleDefLangJPN()
                          ? "MSJ"
                          : "MSE")
                    + Path.DirectorySeparatorChar;
#endif
      }

      public static EnvControlsPersistencyPath GetInstance()
      {
         if (_instance == null)
         {
            lock (typeof(EnvControlsPersistencyPath))
            {
               if (_instance == null)
                  _instance = new EnvControlsPersistencyPath();
            }
         }
         return _instance;
      }


      /// <summary>
      /// 
      /// </summary>
      public static void DeleteInstance()
      {
         Debug.Assert(_instance != null);
         _instance = null;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      public String GetFullPath(String guid)
      {
         String fulPath = ControlsPersistencyPath;

         //3.	Relative path should be supported. 
         //a.	Online - it will be relative to the project (As any other relative paths)
         //b.	RIA – it will be relative to the cache folder.

         fulPath += guid;
         fulPath += Path.DirectorySeparatorChar;

         return fulPath;
      }
      /// <summary>
      /// get the full file name
      /// </summary>
      /// <param name="form"></param>
      /// <returns></returns>
      public String GetFullControlsPersistencyFileName(MgFormBase form)
      {
         String fullControlsPersistencyPath = GetFullPath(form._task.ApplicationGuid);

         //4.	The file names will be:
         //GUID \ prg_xxxxx \ CP_taskID_FormID &’.xml’.
         //Where the GUID is the ID of the host or component.
         //•	For example: 123-4354\prg_2\CP_12_1.xml
         //•	In the default folder, the file will be: C:\Users\erozenberg\AppData\Roaming\MSE\123-4354\prg_2\CP_12_1.xml
         //•	The GUID will not be added twice. It will not be: C:\Users\erozenberg\AppData\Roaming\MSE\123-4354\123-4354\prg_2\CP_12_1.xml    

         // If ControlsPersistencyPath = "xxx: the file will be: xxx\123-4354\prg_2\CP_12_1.xml

         fullControlsPersistencyPath += PreffixControlsPersistencyProgramDirectory + form._task.ProgramIsn + Path.DirectorySeparatorChar +
                                         PreffixControlsPersistencyFileName +
                                        form._task.TaskIsn + "_" + form.FormIsn + ".xml";

         return fullControlsPersistencyPath;
      }
   }
}
