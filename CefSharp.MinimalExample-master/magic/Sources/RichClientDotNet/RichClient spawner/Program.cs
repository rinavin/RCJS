using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

// This spawner is spawned by MgxpaRIA* process for 2 purposes :
// 1. For version updating i.e updating MgxpaRIA* directory content with newer files.
// 2. On Mobile devices to launch a new instance of MgxpaRIAMobile when the old one exits.
//    Due to the single instance policy on .NetCF, we can't have 2 instances of MgxpaRIAMobile running at the same time.
//    The executable created here should be copied to the RichClient Resources directory, so it will be embeded in
//    the executable as a resource.

namespace RichClient_spawner
{
	class Program
	{
		const String UpdateFrom = "<UpdateFrom=";
		const int MgxpaRIAExitTimeout = 10000;

		// 1st arg: process ID of launching process: the spawner waits till this process exits before launching 
		// the new process
		// 2nd (optionally more) arg: spawned assembly (full path). The path may be broken if there are spaces in the path, 
		// e.g. "\storage card\..." will be received as: arg[1] = "\storage", arg[2] = "card\...", so we need to rebuild the full path
		//
		// For version updating : the next 2 arguments after the ".exe" of the 2nd argument (in case there're no spaces 
		// in the 2nd argument, the next 2 arguments will be the #3rd & 4th arguments)
		// If spawner is spawned for version updating, then 3rd argument will be new-files directory path and 4th will be execution properties
		// In this case, 3rd argument will be received as: arg[1] = "<UpdateFrom=", arg[2] = "directory path", arg[3] = "/>"
		// 
		// For launching new instance of MgxpaRIAMobile : 3rd argument will be execution properties
		static void Main(string[] args)
		{
			// get launching process ID and wait for it to exit
			int pid = Convert.ToInt32(args[0]);

			try
			{
				Process proc = Process.GetProcessById(pid);
				proc.WaitForExit(MgxpaRIAExitTimeout);    // wait for 10 seconds for the process to exit

				if (!proc.HasExited)             // if the process is still running then kill it
					proc.Kill();
			}
			catch { } // the process specified by the processId parameter may not be running anymore.
			// in any case, the spawner shouldn't be allowed to linger.

			StringBuilder RCexec = new StringBuilder(args[1]);
			StringBuilder RCargs = new StringBuilder();

			int i = 2;
			// 2nd arg: spawned assembly (full path). The path may be broken if there are spaces in the path, e.g. "\storage card\..."
			// will be received as: arg[1] = "\storage", arg[2] = "card\...", so we need to rebuild the full path
			while (!RCexec.ToString().EndsWith(".exe"))
				RCexec.Append(" " + args[i++]);

			// For version updating : the next 2 arguments after the ".exe" of the 2nd argument 
			// (in case there're no spaces in the 2nd argument, the next 2 arguments will be the #3rd & 4th arguments)
			if (args[i].Equals(UpdateFrom))
			{
				// this process is spawned for updating MgxpaRIA*.exe with that on server. 
				// Extract the directory path whose content needs to copied

				++i;     // exclude UpdateFrom tag after which the directory path begins

				StringBuilder RIAModulesPath = new StringBuilder(args[i++]);

				while (args[i] != "/>")          // continue till UpdateFrom end of tag is not encountered
					RIAModulesPath.Append(" " + args[i++]);

				++i;     // exclude UpdateFrom end of tag

				// extract RC executable name and directory name.
				String RCexecPath = RCexec.ToString();
				int idx = RCexecPath.LastIndexOf(Path.DirectorySeparatorChar);
				String MgxpaRIAdir = RCexecPath.Substring(0, ++idx);
				String MgxpaRIAexe = RCexecPath.Substring(idx);

				try
				{
					// Overwrite old files with updated files for version update
					File.Copy(RIAModulesPath + MgxpaRIAexe, RCexecPath, true);
#if !PocketPC
               const String controlsDLL = "MgControls.dll";
               const String nativeWrapperDLL = "MgNative.dll";
               const String guiDLL = "MgGui.dll";
               const String utilsDLL = "MgUtils.dll";

					File.Copy(RIAModulesPath + controlsDLL,			MgxpaRIAdir + controlsDLL,			true);
					File.Copy(RIAModulesPath + nativeWrapperDLL,	MgxpaRIAdir + nativeWrapperDLL,	true);
					File.Copy(RIAModulesPath + guiDLL,				MgxpaRIAdir + guiDLL,				true);
					File.Copy(RIAModulesPath + utilsDLL,			MgxpaRIAdir + utilsDLL,			true);
#endif
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand, MessageBoxDefaultButton.Button1);
					return;     // return from here, else old MgxpaRIA*.exe process will be spawned again 
				}
			}

			// the remains of the args are the command line arguments
			for (; i < args.Length; i++)
				RCargs.Append(args[i] + " ");

			Process.Start(RCexec.ToString(), RCargs.ToString());
		}
	}
}
