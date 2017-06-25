using System;
using System.Collections.Specialized;
using com.magicsoftware.util;
using util.com.magicsoftware.util;
#if PocketPC
using System.IO;
#endif

namespace com.magicsoftware.unipaas.util
{
   /// <summary>parsing of the assemblies xml</summary>
   public class AssembliesSaxHandler : MgSAXHandler
   {
      public override void startElement(String elementName, NameValueCollection attributes)
      {
         if (elementName.Equals(XMLConstants.MG_TAG_ASSEMBLY))
         {
            String path = attributes[XMLConstants.MG_ATTR_ASSEMBLY_PATH];
            String fullName = attributes[XMLConstants.MG_ATTR_FULLNAME];
            bool useSpecific = attributes[XMLConstants.MG_ATTR_ISSPECIFIC] != null && attributes[XMLConstants.MG_ATTR_ISSPECIFIC].Equals("1", StringComparison.CurrentCultureIgnoreCase);
            String content = attributes[XMLConstants.MG_ATTR_ASSEMBLY_CONTENT];
            bool isGuiThreadExecution = attributes[XMLConstants.MG_ATTR_IS_GUI_THREAD_EXECUTION] != null && attributes[XMLConstants.MG_ATTR_IS_GUI_THREAD_EXECUTION].Equals("1", StringComparison.CurrentCultureIgnoreCase);
            if (path != null)
               ReflectionServices.AddAssembly(fullName, useSpecific, path, isGuiThreadExecution);
            else
            {
               // Cache not used - the assembly code was passed
#if !PocketPC
               ReflectionServices.AddAssembly(fullName, Base64.decodeToByte(content), isGuiThreadExecution);
#else
               BinaryWriter bw = new BinaryWriter(File.Open(fullName, FileMode.Create));
               byte[] decoded = Base64.decodeToByte(content);
               if (decoded != null)
                  bw.Write(decoded);
               bw.Close();
               ReflectionServices.AddAssembly(fullName, false, fullName, isGuiThreadExecution);
#endif
            }
         }
      }
   }
}
