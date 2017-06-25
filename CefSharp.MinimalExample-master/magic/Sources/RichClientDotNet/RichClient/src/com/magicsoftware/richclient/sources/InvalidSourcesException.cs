using System;

namespace com.magicsoftware.richclient.sources
{
   internal class InvalidSourcesException : ApplicationException
   {
      internal InvalidSourcesException(String message, Exception innerException) : base(message, innerException)
      {
      }
   }
}