using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using com.magicsoftware.unipaas.gui;
using System.Reflection;

namespace com.magicsoftware.richclient.mobile.util
{
   /// <summary></summary>
   class Beep
   {
      [DllImport("coredll.dll")]
      internal static extern int PlaySound(
          string szSound,
          IntPtr hModule,
          int flags);

      static internal void Play()
      {
         PlaySound(@"\Windows\Voicbeep", IntPtr.Zero, 0);
      }
   }

   /// <summary>set/get OS level environment variables (name, value)</summary>
   class OSEnvironment
   {
      internal static readonly String EolSeq = "\n\r";
	  internal static readonly String TabSeq = "\t";

      /// <summary>set OS level environment variable (name, value)</summary>
      static internal void set(string name, string value)
      {
         throw new NotImplementedException();
      }

      /// <summary>get OS level environment variable (name, value)</summary>
      static internal string get(string name)
      {
         Debug.Assert(false, "Not Implemented in the Compact Framework");
         return String.Empty;
      }

      internal static string getStackTrace()
      {
         return ""; // not supported in CF
      }

      /// <summary>Return the folder of the RC assembly</summary>
      /// <returns></returns>
      internal static string getAssemblyFolder()
      {
         string fullyQualifiedName = Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName;
         int lastPathSeparator = fullyQualifiedName.LastIndexOf('\\');
         return fullyQualifiedName.Remove(lastPathSeparator + 1, fullyQualifiedName.Length - lastPathSeparator - 1);
      }
      
      /// <summary> Get the temp dir on the executable's disk</summary>
      /// <returns></returns>
      internal static string getTempFolder()
      {
         string fullyQualifiedName = Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName;
         int secondPathSeparator = fullyQualifiedName.IndexOf('\\', 1);
         return fullyQualifiedName.Remove(secondPathSeparator + 1, fullyQualifiedName.Length - secondPathSeparator - 1) + "TEMP";
      }
   }

   /// <summary>Handles: (1) the Status Bar animation while calling the server. (2) the displayed cursor.
   /// The animation will start after a short delay in order to prevent animations for any short term server access.</summary>
   internal class ExternalAccessAnimator
   {
      internal void Start() 
      {
         GUIManager.Instance.setCurrentCursor(MgCursors.WAITCURSOR);
      }
      internal void Stop() 
      {
         GUIManager.Instance.setCurrentCursor(MgCursors.ARROW);
      }
   }

}