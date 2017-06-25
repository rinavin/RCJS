using System;

namespace com.magicsoftware.unipaas
{
   public class GuiConstants
   {
      public const int DEFAULT_LIST_VALUE = -1;
      public const int DEFAULT_VALUE_INT = -999999;
      public const int ALL_LINES = -1;
      public const int NO_ROW_SELECTED = Int32.MinValue; //no row selected - for empty dataview

      /// <summary>RC mobile unsupported controls message string</summary>
      internal const string RIA_MOBILE_UNSUPPORTED_CONTROL_ERROR = "control is not supported for mobile RIA deployment";
      internal const string RIA_MOBILE_UNSUPPORTED_CONTROL_ERROR_WINDOW_6 = "control is not supported for mobile RIA deployment on Window 6 standard";

      internal const int TOOL_HEIGHT = 17;
      internal const int TOOL_WIDTH = 16;

      /// <summary>Data Binding constants</summary>
      internal const String STR_DISPLAY_MEMBER = "DisplayMember";
      internal const String STR_VALUE_MEMBER = "ValueMember";

      /// <summary>parent type</summary>
      public const char PARENT_TYPE_TASK = 'T';
      public const char PARENT_TYPE_FORM = 'F';
      public const char PARENT_TYPE_CONTROL = 'C';

      /// <summary>properties</summary>
      public const int PROP_DEF_HYPER_TEXT_COLOR = 39;
      public const int PROP_DEF_VISITED_COLOR = 40;
      public const int PROP_DEF_HOVERING_COLOR = 41;
      public const int GENERIC_PROPERTY_BASE_ID = 10000;

      /// <summary>tabbing cycle</summary>
      public const char TABBING_CYCLE_REMAIN_IN_CURRENT_RECORD = 'R';
      public const char TABBING_CYCLE_MOVE_TO_NEXT_RECORD = 'N';
      public const char TABBING_CYCLE_MOVE_TO_PARENT_TASK = 'P';
      
      /// <summary>blobs</summary>
      public const int BLOB_PREFIX_ELEMENTS_COUNT = 5;

      /// <summary>key codes</summary>
      public const int KEY_DOWN = 40;
      public const int KEY_RIGHT = 39;
      public const int KEY_UP = 38;
      public const int KEY_LEFT = 37;
      public const int KEY_HOME = 36;
      public const int KEY_END = 35;
      public const int KEY_PG_DOWN = 34;
      public const int KEY_PG_UP = 33;
      public const int KEY_SPACE = 32;
      internal const int KEY_F12 = 123;
      internal const int KEY_F1 = 112;
      internal const int KEY_9 = 57;
      internal const int KEY_0 = 48;
      internal const int KEY_Z = 90;
      internal const int KEY_A = 65;
      internal const int KEY_DELETE = 46;
      public   const int KEY_INSERT = 45;
      internal const int KEY_BACKSPACE = 8;
      public   const int KEY_TAB = 9;
      internal const int KEY_RETURN = 13;
      public   const int KEY_ESC = 27;
      internal const int KEY_CAPS_LOCK = 20;
      internal const int KEY_POINT1 = 190;
      internal const int KEY_POINT2 = 110;
      internal const int KEY_COMMA = 188;

#if PocketPC
      internal const int KEY_ACCUMULATED = 245; // Special key code, used by a MgTextBox with accumulated text
#endif

      // authentication
      public const String LOGON_CAPTION = "Logon - ";
#if !PocketPC
      public const String ENTER_UID_TTL = "Please enter your user ID and password.";
#else
      public const String ENTER_UID_TTL = "Please enter credentials.";
#endif

      public const int GROUP_BOX_TITLE_MAX_SIZE = 62;
      public const int MSG_CAPTION_MAX_SIZE = 56;
      public const int LABEL_CAPTION_MAX_SIZE = 9;
      public const int PASSWORD_CAPTION_MAX_SIZE = 19;

      public const int REPLACE_ALL_TEXT = -1;

      //Indexes of the panes shown in status bars.
      public const int SB_TASKMODE_PANE_LAYER = 2;
      public const int SB_ZOOMOPTION_PANE_LAYER = 3;
      public const int SB_WIDEMODE_PANE_LAYER = 4;
      public const int SB_INSMODE_PANE_LAYER = 5;
      public const int TOTAL_TASKINFO_PANES = 4;

      //Width of the panes (in pixels) shown in status bar.
      public const int SB_TASKMODE_PANE_WIDTH = 100;
      public const int SB_ZOOMOPTION_PANE_WIDTH = 63;
      public const int SB_WIDEMODE_PANE_WIDTH = 55;
      public const int SB_INSMODE_PANE_WIDTH = 50;

      // execution property names used for customizing logon screen
      public const String STR_LOGON_RTL = "LogonRTL";
      public const String STR_LOGO_WIN_ICON_URL = "LogonWindowIconURL";
      public const String STR_LOGON_IMAGE_URL = "LogonImageURL";
      public const String STR_LOGON_WIN_TITLE = "LogonWindowTitle";
      public const String STR_LOGON_GROUP_TITLE = "LogonGroupTitle";
      public const String STR_LOGON_MSG_CAPTION = "LogonMessageCaption";
      public const String STR_LOGON_USER_ID_CAPTION = "LogonUserIDCaption";
      public const String STR_LOGON_PASS_CAPTION = "LogonPasswordCaption";
      public const String STR_LOGON_OK_CAPTION = "LogonOKCaption";
      public const String STR_LOGON_CANCEL_CAPTION = "LogonCancelCaption";
   }
}
