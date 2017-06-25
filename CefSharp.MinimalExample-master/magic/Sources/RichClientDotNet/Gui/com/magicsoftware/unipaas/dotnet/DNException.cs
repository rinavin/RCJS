using System;

namespace com.magicsoftware.unipaas.dotnet
{
   /// <summary>
   /// keep a track of last exception
   /// </summary>
   public class DNException : Exception
   {
      private Exception _lastExceptionOccured;

      /// <summary>
      /// CTOR
      /// </summary>
      internal DNException()
      {
         reset();
      }

      /// <summary>
      /// resets the last exception that was thrown
      /// </summary>
      internal void reset()
      {
         _lastExceptionOccured = null;
      }

      /// <summary>
      /// sets a new exception object
      /// </summary>
      /// <param name="exception"></param>
      public void set(Exception exception)
      {
         if (exception is DNException)
            _lastExceptionOccured = ((DNException)exception).get();
         else
            _lastExceptionOccured = exception;
      }

      /// <summary>
      /// gets the last exception that was thrown
      /// </summary>
      /// <returns></returns>
      public Exception get()
      {
         return _lastExceptionOccured;
      }

      /// <summary>
      /// checks if an exception was thrown
      /// </summary>
      /// <returns></returns>
      internal bool hasExceptionOcurred()
      {
         return (_lastExceptionOccured != null);
      }
   }
}
