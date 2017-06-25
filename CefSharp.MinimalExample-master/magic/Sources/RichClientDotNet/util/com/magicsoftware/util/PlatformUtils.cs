using System;

namespace com.magicsoftware.util
{
   /// <summary>set/get OS level environment variables (name, value)</summary>
   public class OSEnvironment
   {
      public static readonly String EolSeq = System.Environment.NewLine;
	  public static readonly String TabSeq = "\t";

      /// <summary>set OS level environment variable (name, value)</summary>
      static internal void set(string name, string value)
      {
         Environment.SetEnvironmentVariable(name, value, EnvironmentVariableTarget.Process);
      }

      /// <summary>get OS level environment variable (name, value)</summary>
      static public string get(string name)
      {
         return Environment.GetEnvironmentVariable(name);
      }

      /// <summary>return the stack trace, after removing the path and namespace of called classes</summary>
      /// <returns></returns>
      public static string getStackTrace()
      {
         return (OSEnvironment.EolSeq + System.Environment.StackTrace);
      }
   }
}
