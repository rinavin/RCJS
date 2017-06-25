using System;
using System.Reflection;
using System.IO;
using System.Text;
using System.Resources;
#if !PocketPC
using OSEnvironment = com.magicsoftware.util.OSEnvironment;
#else
using OSEnvironment = com.magicsoftware.richclient.mobile.util.OSEnvironment;
#endif
using com.magicsoftware.unipaas.util;

namespace com.magicsoftware.richclient.util
{
   /// <summary>Thus class is wrapper to System.Diagnostics.Process. It provides a convinient way 
   /// to spawn any process. The class uses System.Diagnostics.ProcessStartInfo. It includes 
   /// overloaded static methods to spawn process. These methods allows to control various 
   /// attributes of ProcessStartInfo.</summary>
   class Process
   {
      /// <summary>method spawns new process. Simplest overload to start a process.It only take the name
      /// of the assembly. Rest of the paramters of ProcessInfo will take default value
      /// i.e. 1) No arguemnts
      ///      2) Use Shell execute 
      /// </summary>
      /// <param name="assemblyName"> fullpath/name of the process to spawn </param>
      internal static void Start(string assemblyName)
      {
         Process.Start(assemblyName, "", true);
      }

      /// <summary>method spawns new process. This method allows to send arguments to a process. Other
      /// attributes of ProcessStartInfo will take default values
      /// i.e. 1) Use Shell execute 
      /// </summary>
      /// <param name="assemblyName"> fullpath/name of the process to spawn </param>
      /// <param name="arguments"> arguments passed to process </param>
      internal static void Start(string assemblyName, string arguments)
      {
         Process.Start(assemblyName, arguments, true);
      }

      /// <summary>Method spawns new process. This mehold allows user to control most of the  attributes of
      /// ProcessStartInfo. The window style will be default i.e. Normal.</summary>
      /// <param name="assemblyName"> fullpath/name of the process to spawn </param>
      /// <param name="arguments"> arguments passed to process </param>
      /// <param name="useShellExecute"> whether to use ShellExecute to start process </param>
      internal static void Start(string assemblyName, string arguments, bool useShellExecute)
      {
         //Set ProcesStartInfo.
         System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo();
         psi.FileName = assemblyName;
         psi.Arguments = arguments;
         psi.UseShellExecute = useShellExecute;
#if !PocketPC
         psi.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
#endif

         //Execute process.
         System.Diagnostics.Process process = System.Diagnostics.Process.Start(psi);
      }

      internal static void StartCurrentExecutable(String exeProps)
      {
#if !PocketPC
         Process.Start(Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName, exeProps, false);
#else
         StartCurrentExecutableRespawner(exeProps);
#endif
      }


      /// <summary> spawns the spawnRC with all it needs to wait for this process to end and
      /// launch a new one with the same command line
      /// </summary>
      /// <param name="args"></param>
      internal static void StartCurrentExecutableRespawner(String args)
      {
         StringBuilder arguments = new StringBuilder();

         // Add the current process ID, for the spawnRC to wait for
         arguments.Append(System.Diagnostics.Process.GetCurrentProcess().Id + " ");

         // Add the path and name of current executable
         arguments.Append(Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName + " ");

         // Add the command line arguments
         arguments.Append(args);

         // spawn the spawnRC
         System.Diagnostics.Process.Start(CreateRCSpawnerAssembly(), arguments.ToString());
      }

      /// <summary> Creates (on the disk) the executable used the launch the application after
      /// the current executable exits
      /// </summary>
      /// <returns> path to the newly created executable </returns>
      private static string CreateRCSpawnerAssembly()
      {
         String asmName = getSpawnerAssemblyPath();

         if (HandleFiles.isExists(asmName))
            HandleFiles.deleteFile(asmName);

         // Get the assmbly code from the resource
         Byte[] asmBuf;
#if PocketPC
         asmBuf = RichClient.Properties.Resources.MgxpaRIAMobile_spawner;
#else
         asmBuf = RichClient.Properties.Resources.MgxpaRIA_spawner;
#endif

         // Write the assembly code to the file
         BinaryWriter bw = new BinaryWriter(File.Open(asmName, FileMode.Create));
         bw.Write(asmBuf);
         bw.Close();

         return asmName;
      }

      /// <summary>returns spawner assembly path by appending to OS temp folder</summary>
      private static String getSpawnerAssemblyPath()
      {
         String assemblyName;

#if PocketPC
         assemblyName = OSEnvironment.getTempFolder() + @"\MgxpaRIAMobile spawner.exe";
#else
         assemblyName = OSEnvironment.get("TEMP") + @"\MgxpaRIA spawner.exe";
#endif

         return assemblyName;
      }

#if PocketPC
      /// <summary> On Mobile, a process can't spawn a new process from the same execute file. As a workaround,
      /// we copy the executable file with a new name, and execute it.
      /// </summary>
      /// <param name="exeProps"></param>
      internal static void RenameAndStartCurrentExecutable(String exeProps)
      {
         String exe = Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName;
         String finalExe;
         if (exe.EndsWith("Tmp.exe"))
            finalExe = exe.Replace("Tmp.exe", ".exe");
         else
         {
            finalExe = exe.Replace(".exe", "Tmp.exe");
            try
            {
               File.Copy(exe, finalExe, true);
            }
            catch
            {
            }
         }
         Process.Start(finalExe, exeProps, false);
      }
#endif

   }
}
