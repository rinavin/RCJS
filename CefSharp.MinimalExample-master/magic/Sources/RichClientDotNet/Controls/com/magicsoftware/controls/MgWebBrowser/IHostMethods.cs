using System.Runtime.InteropServices;

namespace com.magicsoftware.controls
{
   [ComVisible(true)]
   public interface IHostMethods
   {
      void MGExternalEvent([MarshalAs(UnmanagedType.BStr)] string param);
   }
}
