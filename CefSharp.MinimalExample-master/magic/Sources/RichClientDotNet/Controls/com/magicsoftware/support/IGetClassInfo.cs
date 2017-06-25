using System;
using System.Collections.Generic;
using System.Text;

namespace Controls.com.magicsoftware.support
{
   /// <summary>
   /// interface to allow getting the class name to be displayed without using the ICustomTypeDescriptor.
   /// Used by the document outline code in the new studio
   /// </summary>
   public interface IGetClassInfo
   {
      string GetClassName(InfoType infoType);
   }

   public enum InfoType
   {
      Component,
      ComponentAndClass
   }

}
