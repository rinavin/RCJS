using System;
using System.Collections.Generic;
using com.magicsoftware.richclient.util;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;
using com.magicsoftware.richclient.http;
using System.Text;
using util.com.magicsoftware.util;
using com.magicsoftware.util.Xml;

namespace com.magicsoftware.richclient.security
{
   //This class is used to manage the user details on the client side.
   //The user details are sent from the server.
   internal class UserDetails
   {
      internal String UserName { get; private set; } // user name obtained from the server.
      internal String UserID { get; private set; } // userID obtained from the server
      internal String UserInfo { get; private set; } // user info obtained from the server
      internal String Password { get; private set; } // Password obtained from server.

      private static UserDetails _instance;
      internal static UserDetails Instance
      {
         get
         {
            if (_instance == null)
            {
               lock (typeof(UserDetails))
               {
                  if (_instance == null)
                     _instance = new UserDetails();
               }
            }
            return _instance;
         }
      }

      /// <summary>Private CTOR as part of making this class a singleton/// </summary>
      private UserDetails()
      {
         UserName = String.Empty;
         UserID = String.Empty;
         UserInfo = String.Empty;
      }

      /// <summary>
      /// This function gets the xml tag containing user info.
      /// </summary>
      internal void fillData()
      {
         XmlParser parser = ClientManager.Instance.RuntimeCtx.Parser;
         List<String> tokensVector;

         int endContext = parser.getXMLdata().IndexOf(XMLConstants.TAG_TERM, parser.getCurrIndex());
         if (endContext != -1 && endContext < parser.getXMLdata().Length)
         {
            // last position of its tag
            String tag = parser.getXMLsubstring(endContext);
            parser.add2CurrIndex(tag.IndexOf(ConstInterface.MG_TAG_USER_DETAILS) + ConstInterface.MG_TAG_USER_DETAILS.Length);
            tokensVector = XmlParser.getTokens(parser.getXMLsubstring(endContext), XMLConstants.XML_ATTR_DELIM);
            initElements(tokensVector);
            parser.setCurrIndex(endContext + XMLConstants.TAG_TERM.Length); // to delete ">" too
         }
         else
            Logger.Instance.WriteExceptionToLog("in UserDetails.fillData(): out of bounds");
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="tokensVector"></param>
      /// <returns></returns>
      private bool initElements(List<String> tokensVector)
      {
         String attribute, valueStr;
         bool isSuccess = true;
         int j;

         for (j = 0; j < tokensVector.Count; j += 2)
         {
            attribute = ((String)tokensVector[j]);
            valueStr = ((String)tokensVector[j + 1]);

            switch (attribute)
            {
               case ConstInterface.MG_TAG_USERNAME:
                  UserName = valueStr;
                  break;
               case ConstInterface.MG_ATTR_USERID:
                  UserID = valueStr;
                  ClientManager.Instance.setUsername(UserID);
                  break;
               case ConstInterface.MG_ATTR_USERINFO:
                  UserInfo = valueStr;
                  break;
               case ConstInterface.MG_TAG_PASSWORD:
                  Password = valueStr;
                  byte[] passwordDecoded = Base64.decodeToByte(Password);
                  string encryptedPassword = Encoding.UTF8.GetString(passwordDecoded, 0, passwordDecoded.Length);
                  string decryptedPassword = Scrambler.UnScramble(encryptedPassword, 0, encryptedPassword.Length - 1);
                  // At server side spaces are added at the end if the length of password is less than 4 characters.
                  // Remove these extra spaces before setting the password.
                  decryptedPassword = StrUtil.rtrim(decryptedPassword);
                  // if the password is empty set a password containing one " ", as for userId, where if no userId 
                  // then a " " set to userId.
                  if (String.IsNullOrEmpty(decryptedPassword))
                     decryptedPassword = " ";
                  ClientManager.Instance.setPassword(decryptedPassword);
                  break;
               default:
                  Logger.Instance.WriteExceptionToLog("in UserDetails.initElements(): unknown attribute: " + attribute);
                  isSuccess = false;
                  break;
            }
         }
         return isSuccess;
      }
   }
}
