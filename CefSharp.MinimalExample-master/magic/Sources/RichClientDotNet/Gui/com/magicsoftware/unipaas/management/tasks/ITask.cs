namespace com.magicsoftware.unipaas.management.tasks
{
   /// <summary>
   /// functionality required by the GUI namespace from the TaskBase class.
   /// </summary>
   public interface ITask
   {
      /// <summary>
      ///   return the component idx
      /// </summary>
      int getCompIdx();

      /// <summary>
      ///   get task id
      /// </summary>
      string getTaskTag();

      /// <summary>
      ///   returns scheme for null arithmetic
      /// </summary>
      char getNullArithmetic();

      /// <summary>
      ///   get the task mode
      /// </summary>
      char getMode();
   }
}
