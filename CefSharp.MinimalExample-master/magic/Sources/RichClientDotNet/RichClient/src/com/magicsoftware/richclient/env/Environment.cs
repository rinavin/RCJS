using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using com.magicsoftware.richclient.util;
using com.magicsoftware.unipaas;
using com.magicsoftware.unipaas.gui;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;
using Manager = com.magicsoftware.unipaas.Manager;
using com.magicsoftware.unipaas.management.env;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.unipaas.env;
using util.com.magicsoftware.util;
using com.magicsoftware.util.Xml;
using com.magicsoftware.richclient.sources;


#if PocketPC
using com.magicsoftware.richclient.mobile.util;
#endif

namespace com.magicsoftware.richclient.env
{
   /// <summary>
   ///   This class holds some of the Magic environment parameters which the client needs
   /// </summary>
   internal class Environment : IEnvironment
   {
      private readonly Hashtable _environments = new Hashtable();
      private long _contextInactivityTimeout;
      private int _toolitipTimeout;
      private long _contextUnloadTimeout;
      private bool _useWindowsXpThemes; //should we use XP themes
      private bool _accessTest; //property that enables the code for test tools 
      private bool _canReplaceDecimalSeparator;
      private bool _closeTasksOnParentActivate;
      private int _codePage;
      private char _dateSeparator; // date separator
      private char _decimalSeparator; // decimal separator
      private char _thousandsSeparator; // thousands separator 
      private char _timeSeparator; // time separator
      private int _defaultColor; //should we exit 'current' control when a non-parkable control is clicked
      private int _defaultFocusColor; //default focus color
      private String _guid; // guid of the application
      private String _controlsPersistencyPath;
      private bool _imeAutoOff; // JPN: IME support
      private char _language;
      private bool _localAs400Set;
      private String _localExtraGengo;
      private String _localFlags;
      private bool _lowHigh = true; //what is the order of bytes in expressions (LOW_HIGH or HIGH_LOW)
      private String _owner; // owner
      private int _debugMode = 0;
      private int _significantNumSize;
      private bool _specialAnsiExpression;
      private bool _specialShowStatusBarPanes;
      private bool _specialEditLeftAlign;
      private bool _specialSwfControlName;
      private bool _specialExitCtrl; //should we exit 'current' control when a non-parkable control is clicked
      private bool _specialTextSizeFactoring;
      private bool _specialFlatEditOnClassicTheme;
      private String _system;
      private int _terminal;
      private String _userId;
      private String _userInfo;
      private String _forwardSlash = Constants.ForwardSlashWebUsage; // refer to a forward slash as a relative web url.
      private String _dropUserFormats;

      public char Language
      {
         get { return _language; }
      }

      /// <summary>
      /// save special zorder
      /// </summary>
      public bool SpecialOldZorder
      {
         get;
         set;
      }
       
      /// <summary>
      /// This flag is only relevant for Online. Hence, return true in case of RC as it is the default value
      /// </summary>
      public bool SpecialNumpadPlusChar
      {
          get { return true; }
          set { }
      }

      public bool IgnoreReplaceDecimalSeparator { get; set; }

      public bool SpecialRestoreMaximizedForm
      {
         get
         {
            return false;
         }
         set { }
      }

      /// <summary>
      /// 
      /// </summary>
      public bool SpecialIgnoreBGinModify
      {
         get
         {
            return false;
         }
         set { }
      }

      /// <summary>instructs how to refer a forward slash - either as a relative web url, or as a file in the file system..</summary>
      public String ForwardSlashUsage
      {
         get { return _forwardSlash; }
         set { _forwardSlash = value; }
      }

      /// <summary>
      ///   Need part input String to relevant for the class data. end <env ...> tag
      /// </summary>
      internal void fillData()
      {
         XmlParser parser = ClientManager.Instance.RuntimeCtx.Parser;
         int endContext = parser.getXMLdata().IndexOf(XMLConstants.TAG_TERM, parser.getCurrIndex());
         if (endContext != -1 && endContext < parser.getXMLdata().Length)
         {
            //last position of its tag
            String tag = parser.getXMLsubstring(endContext);
            parser.add2CurrIndex(tag.IndexOf(ConstInterface.MG_TAG_ENV) + ConstInterface.MG_TAG_ENV.Length);
            List<string> tokensVector = XmlParser.getTokens(parser.getXMLsubstring(endContext), XMLConstants.XML_ATTR_DELIM);
            fillData(tokensVector);
            parser.setCurrIndex(endContext + XMLConstants.TAG_TERM.Length);
         }
         else
            Logger.Instance.WriteExceptionToLog("in Environment.FillData() out of string bounds");
      }

      /// <summary>
      ///   Make initialization of private elements by found tokens
      /// </summary>
      /// <param name = "tokensVector">found tokens, which consist attribute/value of every found element</param>
      private void fillData(List<String> tokensVector)
      {
         String attribute;
         String valueStr;
         Int32 hashKey;
         EnvironmentDetails env;
         UtilImeJpn utilImeJpn = Manager.UtilImeJpn;

         env = new EnvironmentDetails();

         for (int j = 0; j < tokensVector.Count; j += 2)
         {
            attribute = (tokensVector[j]);
            valueStr = (tokensVector[j + 1]);
            valueStr = XmlParser.unescape(valueStr);

            switch (attribute)
            {
               case ConstInterface.MG_ATTR_THOUSANDS:
                  _thousandsSeparator = valueStr[0];
                  break;
               case ConstInterface.MG_ATTR_DECIMAL_SEPARATOR:
                  _decimalSeparator = valueStr[0];
                  break;
               case ConstInterface.MG_ATTR_DATE:
                  _dateSeparator = valueStr[0];
                  DisplayConvertor.Instance.setDateChar(_dateSeparator);
                  break;
               case ConstInterface.MG_ATTR_TIME:
                  _timeSeparator = valueStr[0];
                  break;
               case ConstInterface.MG_ATTR_OWNER:
                  _owner = valueStr;
                  break;
               case ConstInterface.MG_ATTR_SIGNIFICANT_NUM_SIZE:
                  _significantNumSize = XmlParser.getInt(valueStr);
                  break;
               case ConstInterface.MG_ATTR_DEBUG_MODE:
                  _debugMode = XmlParser.getInt(valueStr);
                  break;
               case ConstInterface.MG_ATTR_POINT_TRANSLATION:
                  _canReplaceDecimalSeparator = XmlParser.getBoolean(valueStr);
                  break;
               case ConstInterface.MG_ATTR_SPECIAL_EXITCTRL:
                  _specialExitCtrl = XmlParser.getBoolean(valueStr);
                  break;
               case ConstInterface.MG_ATTR_USE_WINDOWS_XP_THEAMS:
                  _useWindowsXpThemes = XmlParser.getBoolean(valueStr);
                  break;
               case ConstInterface.MG_ATTR_LOWHIGH:
                  _lowHigh = XmlParser.getBoolean(valueStr);
                  break;
               case ConstInterface.MG_ATTR_ACCESS_TEST:
                  _accessTest = XmlParser.getBoolean(valueStr);
                  break;
               case ConstInterface.MG_ATTR_SPECIAL_TEXT_SIZE_FACTORING:
                  _specialTextSizeFactoring = XmlParser.getBoolean(valueStr);
                  break;

               case ConstInterface.MG_ATTR_SPECIAL_FLAT_EDIT_ON_CLASSIC_THEME:
                  _specialFlatEditOnClassicTheme = XmlParser.getBoolean(valueStr);
                  break;

               case ConstInterface.MG_ATTR_ENCODING:
                  if (!valueStr.Equals(" "))
                     _codePage = XmlParser.getInt(valueStr);
                  break;
               case ConstInterface.MG_ATTR_SYSTEM:
                  _system = XmlParser.unescape(valueStr);
                  break;
               case ConstInterface.MG_ATTR_COMPONENT:
                  env.CompIdx = XmlParser.getInt(valueStr);
                  break;
               case ConstInterface.MG_ATTR_DATEMODE:
                  env.DateMode = valueStr[0];
                  break;
               case ConstInterface.MG_ATTR_CENTURY:
                  env.Century = XmlParser.getInt(valueStr);
                  break;
               case ConstInterface.MG_ATTR_IDLETIME:
                  env.IdleTime = XmlParser.getInt(valueStr);
                  //handle the default idle time value
                  if (env.IdleTime == 0)
                     env.IdleTime = 1;
                  break;
               case ConstInterface.MG_ATTR_UPD_IN_QUERY:
                  env.UpdateInQueryMode = XmlParser.getBoolean(valueStr);
                  break;
               case ConstInterface.MG_ATTR_CRE_IN_MODIFY:
                  env.CreateInModifyMode = XmlParser.getBoolean(valueStr);
                  break;
               case ConstInterface.MG_ATTR_DEFAULT_COLOR:
                  _defaultColor = XmlParser.getInt(valueStr);
                  break;
               case ConstInterface.MG_ATTR_DEFAULT_FOCUS_COLOR:
                  _defaultFocusColor = XmlParser.getInt(valueStr);
                  break;
               case ConstInterface.MG_ATTR_CONTEXT_INACTIVITY_TIMEOUT:
                  _contextInactivityTimeout = XmlParser.getInt(valueStr);
                  break;
               case ConstInterface.MG_ATTR_TOOLTIP_TIMEOUT:
                  _toolitipTimeout = XmlParser.getInt(valueStr);
                  break;
               case ConstInterface.MG_ATTR_CONTEXT_UNLOAD_TIMEOUT:
                  _contextUnloadTimeout = XmlParser.getInt(valueStr);
                  break;
               case ConstInterface.MG_ATTR_IME_AUTO_OFF:
                  _imeAutoOff = XmlParser.getBoolean(valueStr);
                  break;
               case ConstInterface.MG_ATTR_LOCAL_AS400SET:
                  _localAs400Set = XmlParser.getBoolean(valueStr);
                  break;
               case ConstInterface.MG_ATTR_LOCAL_EXTRA_GENGO:
                  _localExtraGengo = valueStr;
                  UtilDateJpn.getInstance().addExtraGengo(_localExtraGengo);
                  break;
               case ConstInterface.MG_ATTR_LOCAL_FLAGS:
                  _localFlags = valueStr;
                  break;
               case ConstInterface.MG_ATTR_SPEACIAL_ANSI_EXP:
                  _specialAnsiExpression = XmlParser.getBoolean(valueStr);
                  break;
               case ConstInterface.MG_ATTR_SPECIAL_SHOW_STATUSBAR_PANES:
                  _specialShowStatusBarPanes = XmlParser.getBoolean(valueStr);
                  break;
               case ConstInterface.MG_ATTR_SPECIAL_SPECIAL_EDIT_LEFT_ALIGN:
                 _specialEditLeftAlign = XmlParser.getBoolean(valueStr);
                  break;
                    
               case ConstInterface.MG_ATTR_SPEACIAL_SWF_CONTROL_NAME:
                  _specialSwfControlName = XmlParser.getBoolean(valueStr);
                  break;
               case ConstInterface.MG_ATTR_LANGUAGE:
                  _language = valueStr[0];
                  break;
               case ConstInterface.MG_ATTR_USERID:
                  _userId = valueStr;
                  break;
               case ConstInterface.MG_TAG_USERNAME:
                  ClientManager.Instance.setUsername(valueStr);
                  break;
               case ConstInterface.MG_ATTR_TERMINAL:
                  _terminal = XmlParser.getInt(valueStr);
                  break;
               case ConstInterface.MG_ATTR_USERINFO:
                  _userInfo = valueStr;
                  break;
               case ConstInterface.MG_ATTR_GUID:
                  _guid = valueStr;
                  break;
               case ConstInterface.MG_ATTR_CONTROLS_PERSISTENCY_PATH:
                  _controlsPersistencyPath = valueStr;
                  break;
               case ConstInterface.MG_ATTR_PROJDIR:
                  env.ProjDir = valueStr;
                  break;
               case ConstInterface.MG_ATTR_CLOSE_TASKS_ON_PARENT_ACTIVATE:
                  _closeTasksOnParentActivate = XmlParser.getBoolean(valueStr);
                  break;
               case ConstInterface.MG_ATTR_DROP_USERFORMATS:
                  _dropUserFormats = valueStr;
                  break;
               default:
                  Logger.Instance.WriteExceptionToLog("in Environment.fillData(): unknown attribute: " + attribute);
                  break;
            }
         }
         hashKey = env.CompIdx;
         _environments[hashKey] = env;

         //if (_accessTest)
         //   Commands.addAsync(CommandType.SET_ENV_ACCESS_TEST, null, 0, _accessTest);

         //if (_toolitipTimeout > 0)
         //   Commands.addAsync(CommandType.SET_ENV_TOOLTIP_TIMEOUT, (object)null, (object)_toolitipTimeout);

         //if (_specialTextSizeFactoring)
         //   Commands.addAsync(CommandType.SET_ENV_SPECIAL_TEXT_SIZE_FACTORING, null, 0, _specialTextSizeFactoring);

         //if (_specialFlatEditOnClassicTheme)
         //   Commands.addAsync(CommandType.SET_ENV_SPECIAL_FLAT_EDIT_ON_CLASSIC_THEME, null, 0, _specialFlatEditOnClassicTheme);

         //if (_language != ' ')
         //   Commands.addAsync(CommandType.SET_ENV_LAMGUAGE, null, 0, (int)_language);
      }

      /// <summary>
      ///   fill the file mapping when passed through the cache: placed into the cache by the runtime-engine, passed
      ///   as '<fileurl val = "/..."'
      /// </summary>
      /// <param name = "TAG_URL"></param>
      internal void fillFromUrl(String tagName)
      {
         XmlParser parser = ClientManager.Instance.RuntimeCtx.Parser;
         String XMLdata = parser.getXMLdata();

         int endContext = parser.getXMLdata().IndexOf(XMLConstants.TAG_TERM, parser.getCurrIndex());
         if (endContext != -1 && endContext < XMLdata.Length)
         {
            // find last position of its tag
            String tagAndAttributes = parser.getXMLsubstring(endContext);
            parser.add2CurrIndex(tagAndAttributes.IndexOf(tagName) + tagName.Length);

            List<String> tokensVector = XmlParser.getTokens(parser.getXMLsubstring(endContext), XMLConstants.XML_ATTR_DELIM);
            Debug.Assert((tokensVector[0]).Equals(XMLConstants.MG_ATTR_VALUE));
            String cachedFileUrl = (tokensVector[1]);

            if (cachedFileUrl.Trim() == "") // might happen in case the xpa server failed to write to its cache folder (e.g. the cache folder is invalid, due to a configuration mistake, in which case the current error message will be matched with an error in the xpa server's log file (GeneralErrorLog=)).
               Logger.Instance.WriteErrorToLog(string.Format("Empty cached file URL: '{0}'", tagAndAttributes.Trim()));
            else
            {
               // environment params are not metadata - they're a reflection of the server's environment params, and shouldn't be handled as a source (specifically shouldn't be collected and be accessed to the server upon switching from session stats local to remote).
               byte[] Content = (tagName != ConstInterface.MG_TAG_ENV_PARAM_URL
                                    ? ApplicationSourcesManager.GetInstance().ReadSource(cachedFileUrl, true)
                                    : CommandsProcessorManager.GetContent(cachedFileUrl, true));
               try
               {
                  switch (tagName)
                  {
                     case ConstInterface.MG_TAG_COLORTABLE_URL:
                        Manager.GetColorsTable().FillFrom(Content);
                        break;

                     case ConstInterface.MG_TAG_FONTTABLE_URL:
                        Manager.GetFontsTable().FillFrom(Content);
                        break;

                     case ConstInterface.MG_TAG_KBDMAP_URL:
                        ClientManager.Instance.getKbdMap().fillKbdMapTable(Content);
                        break;

                     case ConstInterface.MG_TAG_ENV_PARAM_URL:
                        //TODO: MerlinRT :We should parse Environment Table using MgSAXParser instead of XMLParser (the same way like we do for
                        //Font, Color and KeyBoardMappping Table). If we do this for env, then we will have to do it for globalParamChanges as well.
                        XmlParser innerXmlParser = new XmlParser(Encoding.UTF8.GetString(Content, 0, Content.Length));
                        while (ClientManager.Instance.getEnvParamsTable().mirrorFromXML(innerXmlParser.getNextTag(), innerXmlParser))
                        {
                        }
                        break;
                  }
               }
               catch (SystemException ex)
               {
                  switch (tagName)
                  {
                     case ConstInterface.MG_TAG_COLORTABLE_URL:
                        Logger.Instance.WriteExceptionToLog(string.Format("Colors Table: '{0}'{1}{2}", cachedFileUrl, OSEnvironment.EolSeq, ex.Message));
                        break;

                     case ConstInterface.MG_TAG_FONTTABLE_URL:
                        Logger.Instance.WriteExceptionToLog(string.Format("Fonts Table: '{0}'{1}{2}", cachedFileUrl, OSEnvironment.EolSeq, ex.Message));
                        break;

                     case ConstInterface.MG_TAG_KBDMAP_URL:
                        Logger.Instance.WriteExceptionToLog(string.Format("Keyboard Mapping: '{0}'{1}{2}", cachedFileUrl, OSEnvironment.EolSeq, ex.Message));
                        break;
                  }
               }
            }

            endContext = XMLdata.IndexOf(XMLConstants.TAG_OPEN, endContext);
            if (endContext != -1)
               parser.setCurrIndex(endContext);
         }
      }

      /// <summary>
      ///   get EnvironmentDetails with the corresponding compIdx
      /// </summary>
      /// <param name = "compIdx">of the task</param>
      /// <returns> reference to the current EnvironmentDetails</returns>
      private EnvironmentDetails getEnvDet(int compIdx)
      {
         EnvironmentDetails env = null;
         Int32 hashKey = compIdx;
         env = (EnvironmentDetails)_environments[hashKey];
         if (env == null)
            Logger.Instance.WriteExceptionToLog("in Environment.getEnvDet() there is no env");

         return env;
      }

      /// <summary>
      ///   return the date mode
      /// </summary>
      /// <param name = "compIdx">of the task</param>
      /// <returns> DateMode</returns>
      public char GetDateMode(int compIdx)
      {
         return getEnvDet(compIdx).DateMode;
      }

      /// <summary>
      ///   return the Thousands
      /// </summary>
      /// <returns> Thousands</returns>
      public char GetThousands()
      {
         return _thousandsSeparator;
      }

      /// <summary>
      ///   return the Decimal
      /// </summary>
      /// <returns> Decimal</returns>
      public char GetDecimal()
      {
         return _decimalSeparator;
      }

      /// <summary>
      ///   sets the Decimal
      /// </summary>
      internal void setDecimalSeparator(char value)
      {
         _decimalSeparator = value;
      }

      internal void setDateSeparator(char value)
      {
         _dateSeparator = value;
      }

      internal void setTimeSeparator(char value)
      {
         _timeSeparator = value;
      }

      /// <summary>
      ///   return the Date Separator
      /// </summary>
      /// <returns> Date Separator</returns>
      public char GetDate()
      {
         return _dateSeparator;
      }

      /// <summary>
      ///   return the IdleTime
      /// </summary>
      /// <param name = "compIdx">of the task</param>
      /// <returns> IdleTime</returns>
      internal int getIdleTime(int compIdx)
      {
         return getEnvDet(compIdx).IdleTime;
      }

      /// <summary>
      ///   return the owner
      /// </summary>
      /// <returns> owner</returns>
      internal String getOwner()
      {
         return _owner;
      }

      /// <summary>
      ///   return the time separator
      /// </summary>
      /// <returns> time seperator</returns>
      public char GetTime()
      {
         return _timeSeparator;
      }

      /// <summary>
      ///   return the century
      /// </summary>
      /// <param name = "compIdx">of the task</param>
      /// <returns>century</returns>
      public int GetCentury(int compIdx)
      {
         return getEnvDet(compIdx).Century;
      }

      /// <summary>
      ///   return TRUE if update is allowed in query mode
      /// </summary>
      /// <param name = "compIdx">of the task</param>
      /// <returns> UpdateInQueryMode</returns>
      internal bool allowUpdateInQueryMode(int compIdx)
      {
         return getEnvDet(compIdx).allowUpdateInQueryMode();
      }

      /// <summary>
      ///   return TRUE if create is allowed in modify mode
      /// </summary>
      /// <param name = "compIdx">of the task</param>
      /// <returns> allowCreateInModifyMode</returns>
      internal bool allowCreateInModifyMode(int compIdx)
      {
         return getEnvDet(compIdx).allowCreateInModifyMode();
      }

      /// <summary>
      ///   return the significant num size
      /// </summary>
      /// <returns> the significant num size</returns>
      public int GetSignificantNumSize()
      {
         return _significantNumSize;
      }

      /// <summary>
      ///   set the significant num size
      /// </summary>
      /// <returns></returns>
      internal void setSignificantNumSize(int newSize)
      {
         _significantNumSize = newSize;
      }

      /// <summary>
      ///   return the code page
      /// </summary>
      /// <returns> the code page
      /// </returns>
      internal int getCodePage()
      {
         return _codePage;
      }


      /// <summary>
      ///   get charset of the current document
      /// </summary>
      public Encoding GetEncoding()
      {
         Encoding encoding;

         try
         {
            encoding = Encoding.GetEncoding(_codePage);
         }
         catch (SystemException)
         {
            encoding = null;
         }

         return encoding;
      }

      /// <summary>
      ///   return the system name
      /// </summary>
      /// <returns> the system name
      /// </returns>
      internal String getSystem()
      {
         return _system;
      }

      /// <summary>
      ///   returns the debug level
      /// </summary>
      /// <returns> 0 no debug 1 debug 2 debug and data transfers in hex ( not in base64)
      /// </returns>
      public int GetDebugLevel()
      {
         return _debugMode;
      }

      /// <summary>
      ///   returns if '.' should be changed to the decimal separator
      /// </summary>
      public bool CanReplaceDecimalSeparator()
      {
         if (IgnoreReplaceDecimalSeparator)
            return false;

         return _canReplaceDecimalSeparator;
      }

      /// <summary>
      ///   return the ContextInactivityTimeout
      /// </summary>
      /// <returns> the ContextInactivityTimeout</returns>
      internal long getContextInactivityTimeout()
      {
         return _contextInactivityTimeout;
      }

      /// <summary>
      ///   return the ContextUnloadTimeout
      /// </summary>
      internal long getContextUnloadTimeout()
      {
         return _contextUnloadTimeout;
      }

      /// <summary>
      ///   return the tooltip timeout
      /// </summary>
      /// <returns> the tooltipTimeout</returns>
      public int GetTooltipTimeout()
      {
         return _toolitipTimeout;
      }

        /// <summary>
        /// return the SpecialEditLeftAlign
        /// </summary>
        /// <returns></returns>
        public bool GetSpecialEditLeftAlign()
        {
            return _specialEditLeftAlign;
        }
        
        
      /// <summary>
      ///   returns TRUE if the bytes sequence for numbers in expressions is from low to high
      /// </summary>
      internal bool getLowHigh()
      {
         return _lowHigh;
      }

      /// <summary>
      ///   get default color
      /// </summary>
      /// <returns></returns>
      public int GetDefaultColor()
      {
         return _defaultColor;
      }

      /// <summary>
      /// Get default focus color
      /// </summary>
      /// <returns></returns>
      public int GetDefaultFocusColor()
      {
         return _defaultFocusColor;
      }

      /// <summary>
      ///   returns if we should exit 'current' control when a non-parkable control is clicked
      /// </summary>
      internal bool getSpecialExitCtrl()
      {
         return _specialExitCtrl;
      }

      /// <summary>
      ///   returns if we should use XP themes
      /// </summary>
      internal bool getUseWindowsXPThemes()
      {
         return _useWindowsXpThemes;
      }

      /// <summary>
      ///   return true if IMEAutoOff = Y 
      ///   (JPN: IME support)
      /// </summary>
      public bool GetImeAutoOff()
      {
         return _imeAutoOff;
      }

      /// <summary>
      ///   return true if As400Set flag = Y
      ///   (DBCS Support)
      /// </summary>
      public bool GetLocalAs400Set()
      {
         return _localAs400Set;
      }

      /// <summary>Function returning additional Gengo value.
      /// </summary>
      public String getLocalExtraGengo()
      {
         return _localExtraGengo;
      }

      /// <summary>Function to check whether flag f is in the local flags.
      /// </summary>
      public bool GetLocalFlag(char f)
      {
          return (_localFlags != null && _localFlags.IndexOf (f) >= 0);
      }

      /// <summary>
      /// </summary>
      /// <returns> true if SpecialAnsiExpression flag = Y</returns>
      internal bool getSpecialAnsiExpression()
      {
         return _specialAnsiExpression;
      }

      /// <summary>
      /// </summary>
      /// <returns> true if SpecialAnsiExpression flag = Y</returns>
      internal bool getSpecialShowStatusBarPanes()
      {
         return _specialShowStatusBarPanes;
      }
    
        
      /// <summary>
      ///   Function returns the terminal number.
      /// </summary>
      /// <returns>terminal number.</returns>
      internal int getTerminal()
      {
         return _terminal;
      }

      /// <summary>
      ///   Function returns user ID of currently logged in user.
      /// </summary>
      /// <returns>user id.</returns>
      internal String getUserID()
      {
         return _userId;
      }

      /// <summary>
      ///   Function returning user info of currently logged in user.
      /// </summary>
      /// <returns>user info</returns>
      internal String getUserInfo()
      {
         return _userInfo;
      }

      /// <summary>gets gui Id of the application</summary>
      /// <returns></returns>
      public String GetGUID()
      {
         return _guid;
      }

      /// <summary>
      /// Get ControlsPersistencyPath
      /// </summary>
      /// <returns></returns>
      public String GetControlsPersistencyPath()
      {
         return _controlsPersistencyPath;
      }
      /// <summary>
      ///   return the project's dir
      /// </summary>
      /// <param name="compIdx">component index.</param>
      /// <returns></returns>
      internal String getProjDir(int compIdx)
      {
         return getEnvDet(compIdx).ProjDir;
      }

      /// <summary>
      /// flag SpecialENGLogon is not available for RC so return false.
      /// </summary>
      /// <returns></returns>
      public bool GetSpecialEngLogon()
      { 
         return false; 
      }

      /// <summary>
      /// flag SpecialENGLogon is not available for RC so return false.
      /// </summary>
      /// <returns></returns>
      public bool GetSpecialIgnoreButtonFormat()
      {
         return false;
      }

      /// <summary>
      /// returns value of SpecialSWFControlNameProperty.
      /// </summary>
      /// <returns></returns>
      public bool GetSpecialSwfControlNameProperty()
      {
         return _specialSwfControlName;
      }

      /// <summary>
      /// returns value of SpecialLogInternalExceptions.
      /// </summary>
      /// <returns></returns>
      public bool GetSpecialLogInternalExceptions()
      {
         return true;
      }
      
      /// <summary>
      ///   sets the closeTasksOnParentActivate flag value.
      /// </summary>
      /// <returns></returns>
      internal void setCloseTasksOnParentActivate(bool val)
      {
         _closeTasksOnParentActivate = val;
      }

      /// <summary>
      ///   return the closeTasksOnParentActivate flag value.
      /// </summary>
      /// <returns></returns>
      internal bool CloseTasksOnParentActivate()
      {
         return _closeTasksOnParentActivate;
      }

      /// <summary>
      ///   Set the owner
      /// </summary>
      /// <param name = "val"></param>
      internal void setOwner(string val)
      {
         _owner = val;
      }

      /// <summary>
      ///   Set the date mode in the component's environment
      /// </summary>
      /// <param name = "compIdx"></param>
      /// <param name = "val"></param>
      internal void setDateMode(int compIdx, char val)
      {
         getEnvDet(compIdx).DateMode = val;
      }

      /// <summary>
      ///   Set the century in the component's environment
      /// </summary>
      /// <param name = "compIdx"></param>
      /// <param name = "val"></param>
      internal void setCentury(int compIdx, int val)
      {
         getEnvDet(compIdx).Century = val;
      }

      /// <summary>
      ///   Set the idle time in the component's environment
      /// </summary>
      /// <param name = "compIdx"></param>
      /// <param name = "val"></param>
      internal void setIdleTime(int compIdx, int val)
      {
         getEnvDet(compIdx).IdleTime = val;
      }

      /// <summary>
      ///   Set the allow-update-in-qury-mode flag in the component's environment
      /// </summary>
      /// <param name = "compIdx"></param>
      /// <param name = "val"></param>
      internal void setAllowUpdateInQueryMode(int compIdx, bool val)
      {
         getEnvDet(compIdx).UpdateInQueryMode = val;
      }

      /// <summary>
      ///   Set the allow-create-in-modify-mode flag in the component's environment
      /// </summary>
      /// <param name = "compIdx"></param>
      /// <param name = "val"></param>
      internal void setAllowCreateInModifyMode(int compIdx, bool val)
      {
         getEnvDet(compIdx).CreateInModifyMode = val;
      }

      /// <summary>
      ///   set the context timeout
      /// </summary>
      /// <param name = "val"></param>
      internal void setContextInactivityTimeout(long val)
      {
         _contextInactivityTimeout = val;
      }

      /// <summary>
      ///   set the context unload timeout
      /// </summary>
      /// <param name = "val"></param>
      internal void setContextUnloadTimeout(long val)
      {
         _contextUnloadTimeout = val;
      }

      /// <summary>
      ///   set the terminal number
      /// </summary>
      /// <param name = "val"></param>
      internal void setTerminal(int val)
      {
         _terminal = val;
      }

      /// <summary>
      ///  Get the UserFormats for Drag & Drop Operation.
      /// </summary>
      /// <returns></returns>
      public String GetDropUserFormats()
      {
         return _dropUserFormats;
      }

      /// <summary>
      /// get GetSpecialTextSizeFactoring flag
      /// </summary>
      /// <returns></returns>
      public bool GetSpecialTextSizeFactoring()
      {
         return _specialTextSizeFactoring;
      }

      /// <summary>
      /// get GetSpecialFlatEditOnClassicTheme flag
      /// </summary>
      /// <returns></returns>
      public bool GetSpecialFlatEditOnClassicTheme()
      {
         return _specialFlatEditOnClassicTheme;
      }

      /// <summary>
      /// return special zorder
      /// </summary>
      /// <returns></returns>
      public bool GetSpecialOldZorder()
      {
         return false;
      }

      /// <summary>
      /// return SpecialSwipeFlickeringRemoval
      /// </summary>
      /// <returns></returns>
      public bool GetSpecialSwipeFlickeringRemoval()
      {
          EnvParamsTable env = ClientManager.Instance.getEnvParamsTable();
          bool result = String.Equals("Y", env.get("[MAGIC_SPECIALS]SpecialSwipeFlickeringRemoval"),
             StringComparison.CurrentCultureIgnoreCase);
          return result;
      }

      public bool GetSpecialDisableMouseWheel()
      {
         throw new NotImplementedException();
      }
      
      #region Nested type: EnvironmentDetails

      /// <summary>
      ///   This class holds details for single Environment (per Component)
      /// </summary>
      private class EnvironmentDetails
      {
         private bool _createInModifyMode; //allow create in modify mode ?
         private bool _updateInQueryMode; //allow update in query mode ?

         /// <summary>
         ///   constructor
         /// </summary>
         internal EnvironmentDetails()
         {
            CompIdx = 0;
         }

         internal int CompIdx { set; get; }

         internal char DateMode { set; get; }

         internal int Century { set; get; }

         internal int IdleTime { set; get; }

         internal bool UpdateInQueryMode
         {
            set { _updateInQueryMode = value; }
            //				get { return updateInQueryMode; }
         }

         internal bool CreateInModifyMode
         {
            set { _createInModifyMode = value; }
            //				get { return createInModifyMode; }
         }

         internal String ProjDir { set; get; }

         /// <summary>
         ///   return TRUE if update is allowed in query mode
         /// </summary>
         internal bool allowUpdateInQueryMode()
         {
            return _updateInQueryMode;
         }

         /// <summary>
         ///   return TRUE if update is allowed in query mode
         /// </summary>
         internal bool allowCreateInModifyMode()
         {
            return _createInModifyMode;
         }
      }

      #endregion








  
   }
}
