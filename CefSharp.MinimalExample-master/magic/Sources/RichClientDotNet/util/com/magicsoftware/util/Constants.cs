using System;
using System.Collections.Generic;
using System.Text;

namespace com.magicsoftware.util
{
   //This class contains all the constants which are used in MgxpaRIA.exe as well as in MgGui.dll.
   public class Constants
   {
      /// <summary> Null Arithmetic values</summary>
      public const char NULL_ARITH_NULLIFY = 'N';
      public const char NULL_ARITH_USE_DEF = 'U';

      /// <summary> select program : select mode property</summary>
      public const char SELPRG_MODE_BEFORE = 'B';
      public const char SELPRG_MODE_AFTER = 'A';
      public const char SELPRG_MODE_PROMPT = 'P';

      /// <summary> move in View</summary>
      public const char MOVE_UNIT_TABLE = 'T';
      public const char MOVE_UNIT_PAGE = 'P';
      public const char MOVE_UNIT_ROW = 'R';
      public const char MOVE_UNIT_TREE_NODE = 'E';

      public const char MOVE_DIRECTION_NONE = ' ';
      public const char MOVE_DIRECTION_BEGIN = 'B';
      public const char MOVE_DIRECTION_PREV = 'P';
      public const char MOVE_DIRECTION_NEXT = 'N';
      public const char MOVE_DIRECTION_END = 'E';
      public const char MOVE_DIRECTION_PARENT = 'A';
      public const char MOVE_DIRECTION_FIRST_SON = 'F';
      public const char MOVE_DIRECTION_NEXT_SIBLING = 'X';
      public const char MOVE_DIRECTION_PREV_SIBLING = 'V';

      /// <summary> refresh types for a task form</summary>
      public const char TASK_REFRESH_FORM = 'F';
      public const char TASK_REFRESH_TABLE = 'T';
      public const char TASK_REFRESH_TREE_AND_FORM = 'R';
      public const char TASK_REFRESH_CURR_REC = 'C';
      public const char TASK_REFRESH_NONE = 'N';

      public const char TASK_MODE_QUERY = 'E';
      public const char TASK_MODE_MODIFY = 'M';
      public const char TASK_MODE_CREATE = 'C';
      public const char TASK_MODE_DELETE = 'D';
      public const char TASK_MODE_NONE = ' ';

      /// <summary> task level</summary>
      public const char TASK_LEVEL_NONE = ' ';
      public const char TASK_LEVEL_TASK = 'T';
      public const char TASK_LEVEL_RECORD = 'R';
      public const char TASK_LEVEL_CONTROL = 'C';

   
      /// <summary> special records constants</summary>
      public const int MG_DATAVIEW_FIRST_RECORD = Int32.MinValue;
      public const int MG_DATAVIEW_LAST_RECORD = Int32.MaxValue;

      //Index of the message pane shown in status bars.
      public const int SB_MSG_PANE_LAYER = 0;

      //Width of the message pane shown in status bar.
      public const int SB_MSG_PANE_WIDTH = 0;

      /// <summary> action states for keyboard mapping</summary>
      public const int ACT_STT_TBL_SCREEN_MODE = 0x0001;
      public const int ACT_STT_TBL_LEFT_TO_RIGHT = 0x0002;
      public const int ACT_STT_TBL_SCREEN_TOP = 0x0004;
      public const int ACT_STT_TBL_SCREEN_END = 0x0008;
      public const int ACT_STT_TBL_ROW_START = 0x0010;
      public const int ACT_STT_TBL_ROW_END = 0x0020;
      public const int ACT_STT_EDT_LEFT_TO_RIGHT = 0x0040;
      public const int ACT_STT_EDT_FORM_TOP = 0x0080;
      public const int ACT_STT_EDT_FORM_END = 0x0100;
      public const int ACT_STT_EDT_LINE_START = 0x0200;
      public const int ACT_STT_EDT_LINE_END = 0x0400;
      public const int ACT_STT_EDT_EDITING = 0x0800;
      public const int ACT_STT_TREE_PARK = 0x1000;
      public const int ACT_STT_TREE_EDITING = 0x2000;

      public const String ForwardSlashWebUsage = "web";  // refer to a forward slash as a relative web url.
      public const string HTTP_PROTOCOL  = "http://";
      public const string HTTPS_PROTOCOL = "https://";
      public const string FILE_PROTOCOL  = "file://";

      /// <summary>threads constants</summary>
      internal const String MG_GUI_THREAD = "MG_GUI_THREAD";
      internal const String MG_WORK_THREAD = "MG_WORK_THREAD";
      internal const String MG_TIMER_THREAD = "MG_TIMER_THREAD";

      /// <summary>
      /// property name for the runtime designer
      /// </summary>
      public const string ConfigurationFilePropertyName = "Configuration file";
      public const string WinPropLeft = "Left";
      public const string WinPropTop = "Top";
      public const string WinPropWidth = "Width";
      public const string WinPropHeight = "Height";
      public const string WinPropBackColor = "BackColor";
      public const string WinPropForeColor = "ForeColor";
      public const string WinPropFont = "Font";
      public const string WinPropText = "Text";
      public const string WinPropLayer = "Layer";
      public const string WinPropX1 = "X1";
      public const string WinPropX2 = "X2";
      public const string WinPropY1 = "Y1";
      public const string WinPropY2 = "Y2";
      public const string WinPropIsTransparent = "IsTransparent";
      public const string WinPropName = "Name";
      public const string WinPropVisible = "Visible";
      public const string WinPropGradientStyle = "GradientStyle";
      public const string WinPropVisibleLayerList = "VisibleLayerList";
      public const string TabOrderPropertyTermination = "ForTabOrder";
   }
}
