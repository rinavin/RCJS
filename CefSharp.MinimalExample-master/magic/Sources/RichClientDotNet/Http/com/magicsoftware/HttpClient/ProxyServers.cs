using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;
using com.magicsoftware.util;
using util.com.magicsoftware.util;
using System.Collections.Specialized;

namespace com.magicsoftware.httpclient
{
   /// <summary>This class manages proxy servers (0 or more).
   /// 
   /// If there are multiple proxy servers, this manager will return the first one.
   /// If the connection fails to this server, it will be marked as non-responsive.
   /// 
   /// Next time, the Proxies manager would return the next proxy server.
   /// 
   /// A retry to a previously unresponsive proxy will be done after 30 minutes,
   /// then after 1 hour from the previous try (always adding an extra 30 minutes).
   /// 
   /// If all proxies are down, all of them will be reset to responsive and the cycle will start again.
   /// </summary>
   internal class ProxyServers
   {
      /******************************************************************************/
      /*    Function Definition for Dynamic Proxy Handling  - Start                 */
      /******************************************************************************/

      private const int WINHTTP_ACCESS_TYPE_DEFAULT_PROXY = 0;
      private const int WINHTTP_ACCESS_TYPE_NO_PROXY = 1;
      private const int WINHTTP_AUTO_DETECT_TYPE_DHCP = 0x00000001;
      private const int WINHTTP_AUTO_DETECT_TYPE_DNS_A = 0x00000002;
      private const int WINHTTP_AUTOPROXY_CONFIG_URL = 0x00000002;

      private readonly List<Proxy> _proxies = new List<Proxy>();
      private readonly IntPtr _winhttpNoProxyBypass = IntPtr.Zero;
      private readonly IntPtr _winhttpNoProxyName = IntPtr.Zero;
      private bool _initialized;

      /// <summary>CTOR.</summary>
      internal ProxyServers()
      {
         _initialized = false;
      }

      [DllImport("winhttp.dll", SetLastError = true, CharSet = CharSet.Unicode)]
      internal static extern IntPtr WinHttpOpen(string pwszUserAgent, int dwAccessType, IntPtr pwszProxyName,
                                              IntPtr pwszProxyBypass, int dwFlags);

      [DllImport("winhttp.dll", SetLastError = true, CharSet = CharSet.Unicode)]
      internal static extern bool WinHttpCloseHandle(IntPtr hInternet);

      [DllImport("winhttp.dll", SetLastError = true, CharSet = CharSet.Unicode)]
      internal static extern bool WinHttpGetProxyForUrl(IntPtr hSession, string lpcwszUrl,
                                                      ref WINHTTP_AUTOPROXY_OPTIONS pAutoProxyOptions,
                                                      ref WINHTTP_PROXY_INFO pProxyInfo);

      /// <summary>in case of dynamic proxies, create Proxy objects for multi proxy servers.</summary>
      /// <param name="proxyAddresses">proxy server(s) address(es), delimited by ';'</param>
      internal void Add(String proxyAddresses)
      {
         String[] proxyAddr = StrUtil.tokenize(proxyAddresses, ";");
         for (int i = 0; i < proxyAddr.Length; i++)
         {
            var proxy = new Proxy(proxyAddr[i], null);
            _proxies.Add(proxy);
         }
      }

      /// <summary>create proxy object for the given WebProxy.</summary>
      /// <param name="address"></param>
      /// <param name="webProxy"></param>
      internal void Add(String address, WebProxy webProxy)
      {
         var proxy = new Proxy(address, webProxy);
         _proxies.Add(proxy);
      }

      /// <summary>return the count of the Proxy servers.</summary>
      internal int Count()
      {
         return _proxies.Count;
      }

      /// <summary>return WebProxy to be used.</summary>
      internal WebProxy Select()
      {
         Debug.Assert(_proxies.Count > 0);

         Proxy proxy = GetProxyForConnection();

         // If there is no responsive proxy (i.e. all proxies are down),
         // set all to responsive irrespective of their last retry time 
         // and start from the first one again.
         if (proxy == null)
         {
            SetAllResponsive();
            proxy = GetProxyForConnection();
         }

         WebProxy webProxy = proxy.GetWebProxy();
         //If it was previously determined that the proxy supports integrated authentication,
         //set the appropriate property of the WebProxy object.
         if (SupportsIntegratedAuthentication(webProxy))
            webProxy.Credentials = CredentialCache.DefaultCredentials;
         // Get credentials from cache and set it in WebProxy that we are going to return.
         // In case connection is direct (without proxy), there can not be any proxy credentials
         else if (webProxy.Address != null)
            webProxy.Credentials = CredentialsCache.GetInstance().GetCredentials(webProxy.Address);

         return webProxy;
      }

      /// <summary>get a proxy to be used.
      /// It always starts from the first proxy server.
      /// If the proxy server is responsive, it is used.
      /// Otherwise, if the waiting time is elapsed, it is still used.
      /// </summary>
      /// <returns></returns>
      private Proxy GetProxyForConnection()
      {
         Proxy proxy = null;

         for (int i = 0; i < _proxies.Count; i++)
         {
            Proxy tmpProxy = _proxies[i];

            if (tmpProxy.IsResponsive() ||
                (DateTime.Now >= tmpProxy.GetNextRetryTime()))
            {
               // if we are checking a proxy again after its retry time then set
               // it responsive.
               if (!tmpProxy.IsResponsive())
                  tmpProxy.SetResponsive(true);
               proxy = tmpProxy;
               Logger.Instance.WriteDevToLog("INFO: The current proxy server is " + tmpProxy.GetProxyAddress());
               break;
            }
            Logger.Instance.WriteDevToLog("INFO: The proxy server " + tmpProxy.GetProxyAddress() +
                                                 " was identified as non-responsive; the next retry time is " +
                                                 tmpProxy.GetNextRetryTime());
         }

         return proxy;
      }

      /// <summary>set all proxy servers as responsive</summary>
      private void SetAllResponsive()
      {
         for (int i = 0; i < _proxies.Count; i++)
         {
            Proxy proxy = _proxies[i];
            proxy.SetResponsive(true);
         }
      }

      /// <summary>get the Proxy corresponding to the given webProxy</summary>
      /// <param name="webProxy"></param>
      /// <returns></returns>
      private Proxy GetProxy(WebProxy webProxy)
      {
         Proxy proxy = null;

         for (int i = 0; i < _proxies.Count; i++)
         {
            Proxy tmpProxy = _proxies[i];
            if (tmpProxy.GetWebProxy() == webProxy)
            {
               proxy = tmpProxy;
               break;
            }
         }

         Debug.Assert(proxy != null);

         return proxy;
      }

      /// <summary>Set the credentials in the Cache.</summary>
      /// <param name="webProxy"></param>
      /// <param name="networkCredentials"></param>
      internal void SetCredentials(WebProxy webProxy, NetworkCredential networkCredentials)
      {
         CredentialsCache.GetInstance().SetCredentials(webProxy.Address, networkCredentials);
      }

      /// <summary>set a Proxy (corresponding to the given webProxy) to responsive or non-responsive</summary>
      /// <param name="webProxy"></param>
      /// <param name="responsive"></param>
      internal void SetResponsive(WebProxy webProxy, bool responsive)
      {
         Proxy proxy = GetProxy(webProxy);
         Debug.Assert(proxy != null);
         proxy.SetResponsive(responsive);
      }

      /// <summary>
      /// For a specified proxy, true if the proxy requested authentication and 
      /// informed (Proxy-Authenticate header) that it supports integrated authentication.
      /// </summary>
      /// <param name="webProxy"></param>
      /// <param name="supportsIntegratedAuthentication"></param>
      internal void SupportsIntegratedAuthentication(WebProxy webProxy, bool supportsIntegratedAuthentication)
      {
         Proxy proxy = GetProxy(webProxy);
         Debug.Assert(proxy != null);

         proxy.SupportsIntegratedAuthentication = supportsIntegratedAuthentication;
      }

      /// <summary>
      /// For a specified proxy, returns true if the proxy requested authentication 
      /// and informed (Proxy-Authenticate header) that is supports integrated authentication.
      /// </summary>
      /// <param name="webProxy"></param>
      /// <returns></returns>
      internal bool SupportsIntegratedAuthentication(WebProxy webProxy)
      {
         Proxy proxy = GetProxy(webProxy);
         Debug.Assert(proxy != null);

         return proxy.SupportsIntegratedAuthentication;
      }

      /// <summary>initialize</summary>
      /// <param name="serverURL">the url of the web-server with which the RC will interact</param>
      internal void Initialize(String serverURL, NameValueCollection outgoingHeaders)
      {
         if (!_initialized)
         {
#if !PocketPC
            string proxyAddress = GetDynamicProxy(serverURL, outgoingHeaders);
            if (proxyAddress != null)
            {
               if (proxyAddress.Equals("DIRECT"))
               {
                  Logger.Instance.WriteServerToLog("INFO: dynamic proxy - DIRECT");
                  Add("DIRECT", new WebProxy());
               }
               else
               {
                  Logger.Instance.WriteServerToLog("INFO: dynamic proxy");
                  Add(proxyAddress);
               }
            }
            else
            {
               Logger.Instance.WriteServerToLog("INFO: static proxy");
#pragma warning disable 0618
               Add("DEFAULT", WebProxy.GetDefaultProxy());
#pragma warning restore 0618
            }

#else //PocketPC
            Add("DIRECT", new WebProxy());
#endif
            _initialized = true;
         }
      }

      /// <summary>get the address(s) of proxy servers from Control Panel / Internet Options</summary>
      /// <param name="serverURL">the url of the web-server with which the RC will interact</param>
      private string GetDynamicProxy(string serverURL, NameValueCollection outgoingHeaders)
      {
         var byteArray = new byte[10000];
         var chrArray = new char[20000];
         Encoding encoding = Encoding.UTF8;

         //Open http session
         IntPtr hSession = WinHttpOpen("Test App", WINHTTP_ACCESS_TYPE_DEFAULT_PROXY, _winhttpNoProxyName,
                                       _winhttpNoProxyBypass, 0);
         var wao = new WINHTTP_AUTOPROXY_OPTIONS();
         var wpi = new WINHTTP_PROXY_INFO();
         wao.dwFlags = WINHTTP_AUTOPROXY_CONFIG_URL;
         wao.dwAutoDetectFlags = 0;
         wao.fAutoLoginIfChallenged = true;
         // Get the proxy config file (pac).
         wao.lpszAutoConfigUrl =
            (String)
            Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\Currentversion\\Internet Settings").GetValue(
               "AutoConfigURL");
         wao.dwAutoDetectFlags = (WINHTTP_AUTO_DETECT_TYPE_DHCP | WINHTTP_AUTO_DETECT_TYPE_DNS_A);

         if (wao.lpszAutoConfigUrl != null)
            wao.lpszAutoConfigUrl = wao.lpszAutoConfigUrl.Trim();

         bool result = WinHttpGetProxyForUrl(hSession, serverURL, ref wao, ref wpi);

         // If we didn't succeed to read the pac file maybe its an ins file
         if (!result && !string.IsNullOrEmpty(wao.lpszAutoConfigUrl))
         {
            try
            {
               int usedLen;
               int charLen = 0;
               Decoder decoder = encoding.GetDecoder();

               // Get the content of the file
               var requesterURLCon = (HttpWebRequest)WebRequest.Create(wao.lpszAutoConfigUrl);
               requesterURLCon.AllowAutoRedirect = true;
               requesterURLCon.Method = "GET";
               requesterURLCon.Credentials = CredentialCache.DefaultCredentials;
               if (outgoingHeaders.Count > 0 && Logger.Instance.LogLevel == Logger.LogLevels.Basic)
                  requesterURLCon.Headers.Add(outgoingHeaders.GetKey(0), outgoingHeaders.GetValues(0)[0]);

               // read the answer
               WebResponse webResponse = requesterURLCon.GetResponse();
               var inpStreamReader = new StreamReader(webResponse.GetResponseStream(), encoding);
               Stream stream = inpStreamReader.BaseStream;
#if !PocketPC
               stream = new BufferedStream(stream);
#endif
               while ((usedLen = stream.Read(byteArray, 0, byteArray.Length)) > 0)
               {
                  // Make sure we have enough space for the growing message
                  if (charLen + usedLen > chrArray.Length)
                  {
                     var newArray = new char[chrArray.Length + 10000];
                     chrArray.CopyTo(newArray, 0);
                     chrArray = newArray;
                  }
                  charLen += decoder.GetChars(byteArray, 0, usedLen, chrArray, charLen);
               }
               var answer = new String(chrArray, 0, charLen);
               Logger.Instance.WriteServerToLog("pac file" + answer);

               // Get the pac file from the correct entry in the ins file
               int startIndex = answer.IndexOf("AutoConfigJSURL");
               Logger.Instance.WriteServerToLog("pac file" + startIndex);
               if (startIndex != -1)
               {
                  int endIndex = answer.IndexOf(".pac", startIndex);
                  Logger.Instance.WriteServerToLog("pac file" + endIndex);
                  string str = answer.Substring(startIndex + 16, endIndex - startIndex - 16 + 4);
                  Logger.Instance.WriteServerToLog("str" + str);
                  wao.lpszAutoConfigUrl = str;
                  WinHttpGetProxyForUrl(hSession, serverURL, ref wao, ref wpi);
               }
            }
            catch (System.Exception ex)
            {
               Logger.Instance.WriteExceptionToLog(ex, string.Format("PAC file \"{0}\" was not found!", wao.lpszAutoConfigUrl));
            }
         }

         //Close http session
         WinHttpCloseHandle(hSession);

         if (wpi.dwAccessType == WINHTTP_ACCESS_TYPE_NO_PROXY)
            return "DIRECT";
         return wpi.lpszProxy;
      }

      #region Nested type: CredentialsCache

      /// <summary>The implementation of ProxyServers is such that, it finds out all avaliable proxies for accessing a perticular 
      /// URL. So if there are multiple URLS like srv1\MagicScripts, srv1\MgxpaRIAcache, srv2\images then we will need multiple
      /// objects of proxyServers.HttpManager.urlAccessDetailsTable we maintain a cache of protocol://server key and ProxyServers
      /// object. So for each new URL we have different ProxyServer instance.
      /// 
      /// Now if more than one URL is using proxy,for example for host Srv1 there is say Proxy1 then srv1\MagicScripts and srv1\MgxpaRIAcache
      /// will be using same Proxy, but in Access details cache (urlAccessDetailsTable) there will be different proxyServers 
      /// Object for them and if Crdentails are stored on ProxyServers.Proxy Object then while accessing MagicScripts and MgxpaRIACache
      /// authentication will be asked twice.  As well when we fail over to another proxy during proxy recovery we must pass
      /// the credentials to next proxy.
      /// 
      /// To avoid the problem and achive passing credentials this cache is intrduced. It holds a collection of Address of proxy 
      /// and its credentials. So when ProxyServers selects proxy, it queries this cache for credentials if any against the 
      /// address of the proxy. While when HttpClient accepts the credentials they are saved/updated in this cache against the
      /// proxy address.
      /// 
      /// CredentialsCache also passes last used credentials to next proxy when we fail over during proxy recovery. It maintains 
      /// last used credentials. When ever credentials are updated last used are also updated with latest value. While getting
      /// credentials if the address is not found in the table, a new record is added in the table with last used crdentails.
      /// This is to make an attempt to reuse previous credentials for a proxy server that wasn't accessed yet
      /// </summary>
      private class CredentialsCache
      {
         private static CredentialsCache _instance;
         private readonly Dictionary<Uri, NetworkCredential> _table;
         //As name suggest, it is the last used credentials. It is used to pass credentials to next proxy when we 
         //fall over to another proxy during proxy recovery. 
         private NetworkCredential _lastUsedCredentials;

         //Singleton pattern so private CTOR.
         private CredentialsCache()
         {
            _table = new Dictionary<Uri, NetworkCredential>();
         }

         /// <summary>Singleton pattern. A method to return instance of the class.</summary>
         /// <returns></returns>
         internal static CredentialsCache GetInstance()
         {
            return _instance ?? (_instance = new CredentialsCache());
         }

         /// <summary>Update credentials for an existing proxy or insert a new record.</summary>
         /// <param name="address"></param>
         /// <param name="credentials"></param>
         internal void SetCredentials(Uri address, NetworkCredential credentials)
         {
            Debug.Assert(address != null);

            // If address is present in the cache, update it otherwise insert it,
            _table[address] = credentials;
            //Always update the last used credentials with latest credentials.
            _lastUsedCredentials = credentials;
         }

         /// <summary>Returns credentials for a proxy represented by address, if address not found it inserts
         /// address with last used credentials in the table. This allows to pass credentials to next proxy
         /// when we fail over in proxy recovery</summary>
         /// <param name="address"></param>
         /// <returns></returns>
         internal NetworkCredential GetCredentials(Uri address)
         {
            Debug.Assert(address != null);

            NetworkCredential credentials;

            if (!_table.TryGetValue(address, out credentials))
            {
               // If address is not found in the table, add it in the table with lastUsedCredentials
               // This ensures that the crdentails are passed to another proxy when we fail over.
               credentials = _lastUsedCredentials;
               _table[address] = credentials;
            }

            return credentials;
         }
      }

      #endregion

      #region Nested type: Proxy

      /// <summary>
      /// 
      /// </summary>
      private class Proxy
      {
         private readonly String _address;
         private DateTime _nextRetryTime;
         private bool _responsive = true;
         private int _retryCount;
         private WebProxy _webProxy;

         /// <summary>Constructor</summary>
         /// <param name="address"></param>
         /// <param name="webProxy"></param>
         internal Proxy(String address, WebProxy webProxy)
         {
            _address = address;
            _webProxy = webProxy;
         }

         internal bool SupportsIntegratedAuthentication { get; set; }

         /// <summary>return the proxy address</summary>
         /// <returns></returns>
         internal String GetProxyAddress()
         {
            return _address;
         }

         /// <summary>return the webProxy.If null, create and return.</summary>
         /// <returns></returns>
         internal WebProxy GetWebProxy()
         {
            return _webProxy ?? (_webProxy = new WebProxy(_address));
         }

         /// <summary>return responsive</summary>
         /// <returns></returns>
         internal bool IsResponsive()
         {
            return _responsive;
         }

         /// <summary>return nextRetryTime</summary>
         /// <returns></returns>
         internal DateTime GetNextRetryTime()
         {
            return _nextRetryTime;
         }

         /// <summary>Sets a Proxy either responsive or non-responsive.
         /// In case of non-responsive, it also sets the time when the server can be re-tried.
         /// A retry to a previously unresponsive proxy will be done after 30 minutes,
         ///   then after 1 hour from the previous try (always adding an extra 30 minutes).
         /// </summary>
         /// <param name="responsive"></param>
         internal void SetResponsive(bool responsive)
         {
            _responsive = responsive;

            if (_responsive)
               _retryCount = 0;
            else
            {
               _retryCount++;
               _nextRetryTime = DateTime.Now.AddMinutes(_retryCount * 30);
            }
         }
      }

      #endregion

      #region Nested type: WINHTTP_AUTOPROXY_OPTIONS

      [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
      internal struct WINHTTP_AUTOPROXY_OPTIONS
      {
         [MarshalAs(UnmanagedType.U4)]
         internal int dwFlags;
         [MarshalAs(UnmanagedType.U4)]
         internal int dwAutoDetectFlags;
         internal string lpszAutoConfigUrl;
         internal IntPtr lpvReserved;
         [MarshalAs(UnmanagedType.U4)]
         internal int dwReserved;
         internal bool fAutoLoginIfChallenged;
      }

      #endregion

      #region Nested type: WINHTTP_PROXY_INFO

      [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
      internal struct WINHTTP_PROXY_INFO
      {
         [MarshalAs(UnmanagedType.U4)]
         internal int dwAccessType;
         internal string lpszProxy;
         internal string lpszProxyBypass;
      }

      #endregion
   }
}
