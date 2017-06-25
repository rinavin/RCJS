namespace com.magicsoftware.unipaas.gui.low
{
   public class Styles
   {
      //styles that call from style2style
      //-------------------------------------
      internal const int PROP_STYLE_BUTTON_PUSH = 0x00000001;
      internal const int PROP_STYLE_BUTTON_IMAGE = 0x00000002;
      internal const int PROP_STYLE_BUTTON_HYPERTEXT = 0x00000004;
      internal const int PROP_STYLE_BUTTON_TEXT_ON_IMAGE = 0x00000008;

      internal const int PROP_STYLE_HOR_RIGHT = 0x00000010;
      public const int PROP_STYLE_RIGHT_TO_LEFT = 0x00000020;
      internal const int PROP_STYLE_HORIZONTAL_SCROLL = 0x00000040;
      internal const int PROP_STYLE_VERTICAL_SCROLL = 0x00000080;
      internal const int PROP_STYLE_TITLE = 0x00000100;
      internal const int PROP_STYLE_CLOSE = 0x00000200;
      internal const int PROP_STYLE_MIN = 0x00000400;
      internal const int PROP_STYLE_MAX = 0x00000800;
      internal const int PROP_STYLE_BORDER = 0x00001000;
      internal const int PROP_STYLE_BORDER_STYLE_THIN = 0x00002000;
      internal const int PROP_STYLE_BORDER_STYLE_THICK = 0x00004000;
      internal const int PROP_STYLE_READ_ONLY = 0x00008000;
      internal const int PROP_STYLE_APPEARANCE_CHECK = 0x00010000;
      internal const int PROP_STYLE_APPEARANCE_BUTTON = 0x00020000;
      internal const int PROP_STYLE_WORD_WRAP = 0x00040000;
      internal const int PROP_STYLE_STYLE_FLAT = 0x00080000;
      internal const int PROP_STYLE_TAB_SIDE_TOP = 0x04000000;
      internal const int PROP_STYLE_TAB_SIDE_BOTTOM = 0x08000000;
      internal const int PROP_STYLE_NO_BACKGROUND = 0x10000000;
      internal const int PROP_STYLE_SHOW_LINES = 0x20000000;
      public const int PROP_STYLE_HORIZONTAL = 0x40000000;
      public const int PROP_STYLE_VERTICAL = unchecked((int) 0x80000000);
      //-------------------------------------   
      //confirmation box
      public const int MSGBOX_BUTTON_OK = 0x00000000;
      public const int MSGBOX_BUTTON_OK_CANCEL = 0x00000001;
      public const int MSGBOX_BUTTON_ABORT_RETRY_IGNORE = 0x00000002;
      public const int MSGBOX_BUTTON_YES_NO_CANCEL = 0x00000003;
      public const int MSGBOX_BUTTON_YES_NO = 0x00000004;
      public const int MSGBOX_BUTTON_RETRY_CANCEL = 0x00000005;

      public const int MSGBOX_ICON_ERROR = 0x00000010;
      public const int MSGBOX_ICON_QUESTION = 0x00000020;
      public const int MSGBOX_ICON_EXCLAMATION = 0x00000030;
      public const int MSGBOX_ICON_INFORMATION = 0x00000040;
      public const int MSGBOX_ICON_WARNING = MSGBOX_ICON_EXCLAMATION;

      public const int MSGBOX_DEFAULT_BUTTON_1 = 0x00000000;
      public const int MSGBOX_DEFAULT_BUTTON_2 = 0x00000100;
      public const int MSGBOX_DEFAULT_BUTTON_3 = 0x00000200;

      //message box result
      // TODO: move these constants to enum
      public const int MSGBOX_RESULT_OK = 1;
      public const int MSGBOX_RESULT_CANCEL = 2;
      public const int MSGBOX_RESULT_ABORT = 3;
      public const int MSGBOX_RESULT_RETRY = 4;
      public const int MSGBOX_RESULT_IGNORE = 5;
      public const int MSGBOX_RESULT_YES = 6;
      public const int MSGBOX_RESULT_NO = 7;

      //for Startup Mode
      public const int WINDOW_STATE_RESTORE = 1;
      public const int WINDOW_STATE_MAXIMIZE = 2;
      public const int WINDOW_STATE_MINIMIZE = 3;
   }
}