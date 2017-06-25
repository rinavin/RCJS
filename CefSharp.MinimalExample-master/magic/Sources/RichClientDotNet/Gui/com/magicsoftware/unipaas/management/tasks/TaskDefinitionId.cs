using System;

namespace com.magicsoftware.unipaas.management.tasks
{
   /// <summary></summary>
   public class TaskDefinitionId
   {
      public const int PRIME_NUMBER = 37;
      public const int SEED = 23;


      public int CtlIndex { get; set; }
      public int ProgramIsn { get; set; }
      public int TaskIsn { get; set; }
      public bool IsProgram { get; set; }

      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="ctlIndex"></param>
      /// <param name="programIsn"></param>
      /// <param name="taskIsn"></param>
      /// <param name="isProgram"></param>
      public TaskDefinitionId(int ctlIndex, int programIsn, int taskIsn, bool isProgram)
      {
         this.CtlIndex = ctlIndex;
         this.ProgramIsn = programIsn;
         this.IsProgram = isProgram;
         this.TaskIsn = (isProgram ? 0 : taskIsn);
      }

      /// <summary>
      /// CTOR without params - for unserializing
      /// </summary>
      public TaskDefinitionId()
      {
      }

      /// <summary>
      /// return true if this task definition id define main program
      /// </summary>
      /// <returns></returns>
      public bool IsMainProgram()
      {
         return (IsProgram && ProgramIsn == 1);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="obj"></param>
      /// <returns></returns>
      public override bool Equals(object obj)
      {
         if (obj == null || !(obj is TaskDefinitionId))
            return false;
         return GetHashCode() == obj.GetHashCode();        
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      public override int GetHashCode()
      {      
         string strHashCode = HashCodeString();
         return strHashCode.GetHashCode();
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      private string HashCodeString()
      {
         string strHashCode = CtlIndex.ToString() + "." + IsProgram.ToString() + "." + ProgramIsn.ToString() + "." + TaskIsn.ToString();
         return strHashCode;
      }

      public override string ToString()
      {
         return String.Format("{{Task ID: {0} ctl {1}/prg {2}{3} (hash: {4})}}",
            IsProgram ? "Program" : "Task",
            CtlIndex,
            ProgramIsn,
            IsProgram ? "" : String.Format(" Task {0}", TaskIsn),
            HashCodeString());
      }
   }
}
