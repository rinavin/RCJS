using System;
using System.Diagnostics;
using System.Net;
using System.Windows.Forms;
using System.IO;
using util.com.magicsoftware.util;

namespace com.magicsoftware.httpclient
{
   /// <summary>
   /// Used to handle the authentication with RSA server.
   /// </summary>
   internal class RSAAuthenticationHandler : ISpecialAuthenticationHandler
   {
      private const string RSA_INITIAL_COOKIE_PROP = "InitialCookieName";
      private const string RSA_AUTHENTICATION_COOKIE_PROP = "AuthenticationCookieName";
      private const string RSA_DOMAIN_NAME_PROP = "DomainName";
      private readonly string _domainName;
      private readonly string _eRgvCookieName;
      private readonly string _rsaCsrfCookieName;
      private bool _isPermissionGranted; //the current status of the handler - whether the last authentication attempt was successful
      private string _requiredCookies = ""; //name=value pairs
      private bool _wasAuthCookieSwitched;
      private const string _rsaLoginTitleString = "<TITLE>RSA SecurID : Log In</TITLE>";
      private const int _rsaLoginTitleSearchableHeaderLength = 100;

      /// <summary>
      /// Initializes the class data members.
      /// </summary>
      internal RSAAuthenticationHandler()
      {
         _isPermissionGranted = false;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="rsaCookieNames">Should contain the names of cookies required by the handler
      /// to handle web responses. This string should be contained in a property in execution.properties
      /// file which should look like this: '<property key="RSACookies" val="InitialCookieName=rsa-csrf;AuthenticationCookieName=cookie4eRGV"/>'</param>
      internal RSAAuthenticationHandler(string rsaCookieNames) : this()
      {
         _rsaCsrfCookieName = ExtractValueFromPairs(rsaCookieNames, RSA_INITIAL_COOKIE_PROP);
         _eRgvCookieName = ExtractValueFromPairs(rsaCookieNames, RSA_AUTHENTICATION_COOKIE_PROP);
         _domainName = ExtractValueFromPairs(rsaCookieNames, RSA_DOMAIN_NAME_PROP);

         if (String.IsNullOrEmpty(_rsaCsrfCookieName) ||
             String.IsNullOrEmpty(_eRgvCookieName) ||
             String.IsNullOrEmpty(_domainName))
            throw new Exception("One or more of the following sub-properties are not set: " +
                                RSA_INITIAL_COOKIE_PROP + ", " + RSA_AUTHENTICATION_COOKIE_PROP + ", " + RSA_DOMAIN_NAME_PROP +
                                ". They have to be defined inside a value string of the property RSACookies in execution.properties");
      }

      #region ISpecialAuthenticationHandler Members

      /// <summary>
      /// Determines whether the current handler is responsible of handling the HTTP response ("httpWebResponse").
      /// The current handler identifies responses that it should handle by searching for
      /// a "Set-Cookie:" header and a cookie named "rsa-csrf" inside it. If it is there,
      /// we conclude that the response is an authentication HTML form 
      /// from the RSA server, and that's what this handler is for.
      /// </summary>
      /// <param name="httpWebResponse"></param>
      /// <returns>whether the current implementation of the ISpecialAuthenticationHandler interface
      ///          is responsible for handling the given response</returns>
      public bool ShouldHandle(object response, byte[] contentFromServer)
      {
         Logger.Instance.WriteServerToLog("RSAAuthenticationHandler.ShouldHandle() started");

         bool result = false;
         HttpWebResponse httpWebResponse = response as HttpWebResponse;
         string responseSetCookie;
         Debug.Assert(httpWebResponse != null, "webServerResponse != null");
         if (!String.IsNullOrEmpty(responseSetCookie = httpWebResponse.GetResponseHeader("Set-Cookie")))
         {
            Logger.Instance.WriteServerToLog(
               String.Format("RSAAuthenticationHandler.ShouldHandle(): found 'Set-Cookie':{0} header in the response",
                             responseSetCookie));

            string cookie; //NameValuePair (name=value)

            // if the rsa-csrf cookie comes with the response, we know that authentication is required 
            // and we reset the current values of this cookie in the handler
            if ((cookie = ExtractNameValuePair(responseSetCookie, _rsaCsrfCookieName)) != null)
            {
               Logger.Instance.WriteServerToLog("RSAAuthenticationHandler.ShouldHandle(): Found new " + cookie
                                              +
                                              ". This means that the server wants to re-authenticate. Resetting the state of the handler.");
               OnShouldHandle();
               //_requiredCookies = "";
               //ResetAuthenticationStatus();

               result = true;
            }
               // handle the eRGVCookie renewal initiated by the server
               // "else" because there's no way the RSA server will offer a new cookie4eRGV if re-authentication is required!
               // if the eRGV cookie exists, we save it for future use - we will attach it to every outgoing HTTP request in the future.
            else if ((cookie = ExtractNameValuePair(responseSetCookie, _eRgvCookieName)) != null)
            {
               // replace the previous eRGV cookie: rsa-csrf=AAAAAA;cookie4eRGV=BBBBBBB ==> rsa-csrf=AAAAAA;cookie4eRGV=CCCCCCCCC
               // requiredCookies = requiredCookies.Replace(ExtractNameValuePair(requiredCookies, eRGVCookieName), cookie);
               _requiredCookies = AddOrReplaceNameValuePair(_requiredCookies, _eRgvCookieName, cookie);

               _wasAuthCookieSwitched = true;
               Logger.Instance.WriteServerToLog(
                  string.Format("RSAAuthenticationHandler.ShouldHandle(): requiredCookies = {0}", _requiredCookies));
            }
         }

         // Determine if the RSA server requires authentication.
         // Usually the server sends a rsa-csrf cookie (located in the header) to signal that, 
         // but not always. If not, an additional check (by content) needs to be performed, like in this case.
         // We will make this check only if previous checks yielded false results.
         if (!result && !_wasAuthCookieSwitched && ShouldHandleByContent(contentFromServer))
         {
            Logger.Instance.WriteServerToLog("RSAAuthenticationHandler.ShouldHandle(): Found the following in the server response: " + _rsaLoginTitleString + ". This means that the server wants to re-authenticate. Resetting the state of the handler.");
            OnShouldHandle();
            result = true;
         }

         Logger.Instance.WriteServerToLog("RSAAuthenticationHandler.ShouldHandle() finished. result = " + result);

         return result;
      }

      /// <summary>
      /// Actions to take if determined that this handler is responsible for handling the response.
      /// </summary>
      /// <returns></returns>
      private void OnShouldHandle()
      {
         _requiredCookies = "";
         ResetAuthenticationStatus();
      }

      /// <summary>
      /// Determines if the RSA server requires an authentication by the response content.
      /// If the response contains a certain string, we know that authentication is required.
      /// To see the value of this string, please refer to the definition of the _rsaLoginTitleString constant.
      /// </summary>
      /// <param name="contentFromServer">the HTML or XML payload taken from the web server response</param>
      /// <returns></returns>
      private bool ShouldHandleByContent(byte[] contentFromServer)
      {
         bool shouldHandle = false;
#if !PocketPC
         // to improve performance, we'll use only the first several bytes of the response content buffer.
         byte[] buffer = new byte[_rsaLoginTitleSearchableHeaderLength];
         System.Array.Copy(contentFromServer, buffer, Math.Min(buffer.Length, contentFromServer.Length));
         String responseDocBeginning = System.Text.Encoding.UTF8.GetString(buffer);

         int indexOfLoginTitle = responseDocBeginning.IndexOf(_rsaLoginTitleString);

         if (indexOfLoginTitle > 0)
            shouldHandle = true;
#endif
         return shouldHandle;
      }

      /// <summary>
      /// Determines whether the current HTTP response is a successful authentication response from the RSA server
      /// by analyzing the response.
      /// Later we can inquire the status using the property WasPermissionGranted
      /// </summary>
      /// <param name="browserObject">WebBrowser</param>
      /// <returns></returns>
      public bool IsPermissionGranted(object browserObject)
      {
         Logger.Instance.WriteServerToLog("RSAAuthenticationHandler.IsPermissionGranted() started");
         DateTime expiration = DateTime.UtcNow - TimeSpan.FromDays(1);

#if !PocketPC
         //irrelevant for EuroClear in mobile
         if (browserObject != null)
         {
            var webBrowser = browserObject as WebBrowser;
            if (webBrowser != null)
            {
               HtmlDocument doc = webBrowser.Document;
               Debug.Assert(doc != null, "doc != null");
               string documentCookies = String.Copy(doc.Cookie);
               Logger.Instance.WriteServerToLog(
                  "RSAAuthenticationHandler.IsPermissionGranted(): webBrowser != null, documentCookies = "
                  + documentCookies);

               if (!String.IsNullOrEmpty(documentCookies))
               {
                  Logger.Instance.WriteServerToLog(
                     "RSAAuthenticationHandler.IsPermissionGranted(): !String.IsNullOrEmpty(documentCookies), documentCookies = "
                     + documentCookies);

                  string eRGVCookie;
                  string rsaCsrfCookie;

                  // if the rsa-csrf cookie exists, we save it for future use - we will attach it to every outgoing HTTP request in the future.
                  // we cannot expire it in browser's state, because it may be used for subsequent authentication retries (e.g. when wrong credentials
                  // have been entered for the first time).
                  if ((rsaCsrfCookie = ExtractNameValuePair(documentCookies, _rsaCsrfCookieName)) != null)
                  {
                     Logger.Instance.WriteServerToLog("RSAAuthenticationHandler.IsPermissionGranted(): found "
                                                    + rsaCsrfCookie);
                     _requiredCookies = AddOrReplaceNameValuePair(_requiredCookies, _rsaCsrfCookieName, rsaCsrfCookie);
                  }
                  else
                     Logger.Instance.WriteServerToLog(
                        "RSAAuthenticationHandler.IsPermissionGranted(): did NOT find rsa-csrf expiredCookie in the browser session state!");

                  // If cookie4eRGV exists in the browser state, it means that authentication is successful.
                  // We save it in requiredCookies for use by RC and clean both cookies from the browser state,
                  // since the browser will be closed in a moment anyway and the next time it will open (e.g. on timeout)
                  // a new set of cookies will be relevant instead of the old one.
                  if ((eRGVCookie = ExtractNameValuePair(documentCookies, _eRgvCookieName)) != null)
                  {
                     Logger.Instance.WriteServerToLog("RSAAuthenticationHandler.IsPermissionGranted(): eRGVCookie = "
                                                    + eRGVCookie);

                     _isPermissionGranted = true;

                     //For testing, remove 'Secure'
                     doc.Cookie = String.Format("{0}=;expires={1};path=/;domain={2};Secure", _eRgvCookieName,
                                                expiration.ToString("R"), _domainName);
                     doc.Cookie = String.Format("{0}=;expires={1};path=/;Secure", _rsaCsrfCookieName,
                                                expiration.ToString("R"));

                     Logger.Instance.WriteServerToLog(
                        "RSAAuthenticationHandler.IsPermissionGranted(): just expired both cookies. Current doc.Cookie = "
                        + doc.Cookie);
                     _requiredCookies = AddOrReplaceNameValuePair(_requiredCookies, _eRgvCookieName, eRGVCookie);
                  }
                  else
                     Logger.Instance.WriteServerToLog(
                        "RSAAuthenticationHandler.IsPermissionGranted(): did NOT find eRGVCookie in the browser session state!");
               }
            }
         }

         if (_isPermissionGranted)
         {
            Debug.Assert(!String.IsNullOrEmpty(_requiredCookies));
            PermissionInfo = _requiredCookies;
         }
#endif
         Logger.Instance.WriteServerToLog(
            "RSAAuthenticationHandler.IsPermissionGranted() finished. isPermissionGranted = " + _isPermissionGranted
            + "; requiredCookies = " + _requiredCookies + "; doc.Cookie = null");

         return _isPermissionGranted;
      }

      /// <summary>
      /// Sets internal authentication status to false
      /// </summary>
      public void ResetAuthenticationStatus()
      {
         _isPermissionGranted = false;
         _wasAuthCookieSwitched = false;
      }

      /// <summary>
      /// Specifies if the RSA server has granted permission previously in this session
      /// </summary>
      /// <returns></returns>
      public bool WasPermissionGranted
      {
         get { return _isPermissionGranted; }
      }

      /// <summary>
      /// Specifies whether the permission cookies were updated by the server recently
      /// </summary>
      public bool WasPermissionInfoUpdated
      {
         get { return _wasAuthCookieSwitched; }
      }

      /// <summary>
      /// In this case, a string containing all the cookies needed for subsequent communication with
      /// the RTE server behind the RSA server.
      /// </summary>
      public object PermissionInfo
      {
         get { return _requiredCookies; }
         set { _requiredCookies = value as string; }
      }

      #endregion

      /// <summary>
      /// Adds the new pair to the list if the key does not exist there or replaces the existing pair if it does
      /// For pairsList = "a1=a2,b1=b2" AddOrReplaceNameValuePair(pairsList, "c1", "c1=c2") will yield "a1=a2,b1=b2,c1=c2"
      /// For pairsList = "a1=a2,b1=b2" AddOrReplaceNameValuePair(pairsList, "a1", "a1=a3") will yield "a1=a3,b1=b2"
      /// </summary>
      /// <param name="pairsList"></param>
      /// <param name="name"></param>
      /// <param name="newNameValuePair"></param>
      /// <returns></returns>
      private string AddOrReplaceNameValuePair(string pairsList, string name, string newNameValuePair)
      {
         if (String.IsNullOrEmpty(pairsList))
            pairsList = newNameValuePair;
         else
         {
            string oldNameValuePair = ExtractNameValuePair(pairsList, name);

            if (oldNameValuePair == null)
               pairsList += "," + newNameValuePair;
            else
               pairsList = pairsList.Replace(ExtractNameValuePair(pairsList, name), newNameValuePair);
         }
         return pairsList;
      }

      /// <summary>
      /// Extracts the pair name=value as one string from a given string. 
      /// Delimiter between pairs can be ';' or ','.
      /// </summary>
      /// <param name="nameValuePairs">a string to extract from</param>
      /// <param name="nameOfPairToExtract">self-descriptive</param>
      /// <returns>the string "name=value"</returns>
      private string ExtractNameValuePair(string nameValuePairs, string nameOfPairToExtract)
      {
         Logger.Instance.WriteServerToLog("RSAAuthenticationHandler.ExtractNameValuePair() started. nameValuePairs="
                                        + nameValuePairs + ", nameOfPairToExtract=" + nameOfPairToExtract);

         int nameStartIndex;
         string pairToReturn = null;

         if ((nameStartIndex = nameValuePairs.IndexOf(nameOfPairToExtract)) > -1)
         {
            //name1=value1;name2=value2 => name1=value1 (for a given nameOfPairToExtract = 'name1')
            int pairEndIndex = nameValuePairs.IndexOf(';', nameStartIndex);
            pairEndIndex = pairEndIndex > -1
                              ? pairEndIndex
                              : nameValuePairs.IndexOf(',', nameStartIndex);
            pairEndIndex = pairEndIndex > -1
                              ? pairEndIndex
                              : nameValuePairs.Length;
            int cookieLength = pairEndIndex - nameStartIndex;
            pairToReturn = nameValuePairs.Substring(nameStartIndex, cookieLength);
         }

         Logger.Instance.WriteServerToLog("RSAAuthenticationHandler.ExtractNameValuePair() finished, pairToReturn = "
                                        + pairToReturn);
         return pairToReturn;
      }

      /// <summary>
      /// Extracts the value from a pair name=value
      /// </summary>
      /// <param name="nameValuePair">pair looking like this: name=value</param>
      /// <returns>value</returns>
      private string ExtractValueFromPair(string nameValuePair)
      {
         string value = null;

         int valueStartIndex = nameValuePair.IndexOf('=');

         if (valueStartIndex > -1)
            value = nameValuePair.Substring(valueStartIndex + 1).Trim();

         return value;
      }

      /// <summary>
      /// Extracts a value from a string name1=value1;name2=value2 (delimiter can be ';' or ',')
      /// by a given name.
      /// </summary>
      /// <param name="nameValuePairs"></param>
      /// <param name="nameOfPairToExtract"></param>
      /// <returns></returns>
      private string ExtractValueFromPairs(string nameValuePairs, string nameOfPairToExtract)
      {
         string value = null;

         string pair = ExtractNameValuePair(nameValuePairs, nameOfPairToExtract);
         if (!String.IsNullOrEmpty(pair))
            value = ExtractValueFromPair(pair);

         return value;
      }
   }
}