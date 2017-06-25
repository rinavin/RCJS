using System;

using System.Collections.Generic;
using System.Text;

namespace com.magicsoftware.richclient.mobile.util
{
   /// <summary>
   /// Exception thrown by threads when a thread is
   /// intrrupted while in wait state
   /// </summary>
   internal class ThreadInterruptedException : SystemException
   {
      /// <summary>
      /// Initializes a new instance of the ThreadInterruptedException class with a 
      /// specified error message.
      /// </summary>
      /// <param name="message">The error message that explains the reason for the exception.</param>
      internal ThreadInterruptedException(string message) :
         base(message)
      {
      }
   }
}
