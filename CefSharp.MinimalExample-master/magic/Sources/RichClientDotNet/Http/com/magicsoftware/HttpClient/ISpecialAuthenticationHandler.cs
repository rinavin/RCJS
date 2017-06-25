namespace com.magicsoftware.httpclient
{
   /// <summary>
   /// Used to handle special authentication issues (e.g. with external security systems) 
   /// that arise during the operation of RC client, particularly upon the initial request.
   /// </summary>
   public interface ISpecialAuthenticationHandler
   {
      bool ShouldHandle(object response, byte[] contentFromServer);
      bool IsPermissionGranted(object param);
      void ResetAuthenticationStatus();

      object PermissionInfo { get; set; }
      bool WasPermissionGranted { get; }
      bool WasPermissionInfoUpdated { get; }
   }
}
