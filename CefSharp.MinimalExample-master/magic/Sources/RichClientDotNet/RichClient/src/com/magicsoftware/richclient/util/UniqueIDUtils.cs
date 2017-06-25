using System;
using System.Text;
using System.Net;
using util.com.magicsoftware.util;
#if !PocketPC
using System.Management;
#endif

namespace com.magicsoftware.richclient.util
{
	internal class UniqueIDUtils
	{
		/// <summary>Generate unique machine id.</summary>
		/// <returns>String containing the ID of the 1st processor, or MAC address, or host:port</returns>
		internal static String GetUniqueMachineID()
		{
         string hashedString;

         StringBuilder uniqueID = new StringBuilder();
			string platformIndicator;
#if !PocketPC
			platformIndicator = "D";
			uniqueID.Append("{" + System.Diagnostics.Process.GetCurrentProcess().SessionId + "}");
#else
			platformIndicator = "M";
			uniqueID.Append(Assembly.GetExecutingAssembly().ManifestModule.Name);
#endif

         try
         {
				// Processor ID
				ManagementClass mc = new ManagementClass("Win32_Processor");
				ManagementObjectCollection moc = mc.GetInstances();
				ManagementObjectCollection.ManagementObjectEnumerator enumator = moc.GetEnumerator();
				enumator.MoveNext();
				uniqueID.Append(((ManagementObject)enumator.Current).Properties["ProcessorId"].Value.ToString());
            AppendNetInfo(uniqueID);
         }
         catch (Exception ex)
			{
            Logger.Instance.WriteExceptionToLog(ex);
         }
         finally
			{
            hashedString = String.Format("{0:X}", uniqueID.ToString().GetHashCode());
            hashedString = hashedString.PadLeft(16, '0');
            hashedString = String.Format("{0}_{1}", hashedString, platformIndicator);
         }

         return hashedString;
		}

      /// <summary>Appends the host name and possibly the IP address of the current machine into 'uniqueID'.</summary>
      /// <param name="uniqueID">Unique ID to be further appended. [REF]</param>
      private static void AppendNetInfo(StringBuilder uniqueID)
		{
			String localHostName = Dns.GetHostName();
			IPHostEntry hostInfo = Dns.GetHostEntry(localHostName);
			StringBuilder sourceString = new StringBuilder();
			sourceString.Append(hostInfo.HostName);
			if (hostInfo.AddressList != null && hostInfo.AddressList.Length > 0)
				sourceString.Append(hostInfo.AddressList[0]);
			uniqueID.Append(System.Convert.ToString(sourceString.ToString().GetHashCode()));
 		}
	}
}