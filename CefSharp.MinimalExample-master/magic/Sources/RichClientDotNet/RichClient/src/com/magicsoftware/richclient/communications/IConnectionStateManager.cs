using System;

namespace com.magicsoftware.richclient.communications
{
   /// <summary>
   /// Interface of a class that has knowledge of the current 
   /// connection state.<br/>
   /// The specifics of detecting disconnection is left to the implementing 
   /// class.
   /// </summary>
   public interface IConnectionStateManager
   {
      void ConnectionEstablished();
      void ConnectionDropped();
   }
}
