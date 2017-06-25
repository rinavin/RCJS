using System;
using System.ComponentModel;
#if PocketPC
using com.magicsoftware.mobilestubs;
#endif

namespace com.magicsoftware.util
{
   /// <summary>
   /// the class used to provide localized strings for enums
   /// </summary>
   [AttributeUsage(AttributeTargets.Field)]
   public sealed class DisplayStringAttribute : Attribute
   {
      private readonly string value;
      public string Value
      {
         get { return value; }
      }

      public string ResourceKey { get; set; }

      public DisplayStringAttribute(string v)
      {
         this.value = v;
      }

      public DisplayStringAttribute()
      {
      }
   }

   public enum Priority
   {
      LOWEST = 1,
      LOW = 2,
      HIGH = 3
   }

   public enum TableBehaviour
   {
      LimitedItems = 1,
      UnlimitedItems = 2
   }


   public enum MgControlType
   {
      CTRL_TYPE_BUTTON = 'B',
      CTRL_TYPE_CHECKBOX = 'C',
      CTRL_TYPE_RADIO = 'R',
      CTRL_TYPE_COMBO = 'D',
      CTRL_TYPE_LIST = 'E',
      CTRL_TYPE_TEXT = 'T',
      CTRL_TYPE_GROUP = 'G',
      CTRL_TYPE_TAB = 'J',
      CTRL_TYPE_TABLE = 'A',
      CTRL_TYPE_COLUMN = 'K',
      CTRL_TYPE_LABEL = 'L',
      CTRL_TYPE_IMAGE = 'I',
      CTRL_TYPE_SUBFORM = 'F',
      CTRL_TYPE_BROWSER = 'W',
      CTRL_TYPE_STATUS_BAR = '1',
      CTRL_TYPE_SB_LABEL = '2',
      CTRL_TYPE_SB_IMAGE = '3',
      CTRL_TYPE_TREE = 'N',
      CTRL_TYPE_FRAME_SET = 'P',
      CTRL_TYPE_CONTAINER = 'Q',
      CTRL_TYPE_FRAME_FORM = 'U',
      CTRL_TYPE_RICH_EDIT = 'S',
      CTRL_TYPE_RICH_TEXT = 'V',
      CTRL_TYPE_LINE = 'X',
      CTRL_TYPE_DOTNET = 'Y'
   }  

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum DitType
   {
      None = 1,
      Edit,
      [DisplayString(ResourceKey = "PushButton_s")]
      Button,
      [DisplayString(ResourceKey = "ComboBox_s")]
      Combobox,
      [DisplayString(ResourceKey = "ListBox_s")]
      Listbox,
      [DisplayString(ResourceKey = "Radiobutton_s")]
      Radiobox,
      Tab,
      [DisplayString(ResourceKey = "CheckBox_s")]
      Checkbox,
      Image,
      Static,
      Line,
      Group,
      RichEdit,
      RichText,
      Table,
      Slider,
      Ole,
      Hotspot,
      StaticTable,
      Sound,
      Html,
      Java,
      Activex,
      [DisplayString(ResourceKey = "IFrame_s")]
      Frame,
      Subform,
      Hypertext,
      Browser,
      Dotnet,
      Opaque
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum BorderType
   {
      Thin = 1,
      Thick = 2,
      NoBorder = 3
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum GradientStyle
   {
      None = 1,
      Horizontal,
      HorizontalSymmetric,
      HorizontalWide,
      Vertical,
      VerticalSymmetric,
      VerticalWide,
      DiagonalLeft,
      DiagonalLeftSymmetric,
      DiagonalRight,
      DiagonalRightSymmetric,
      CornerTopLeft,
      CornerTopRight,
      CornerBottomLeft,
      CornerBottomRight,
      Center
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum AlignmentTypeHori
   {
      Left = 1,
      Center,
      Right
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum TabbingOrderType
   {
      [DisplayString(ResourceKey = "True_s")]
      Automatically = 1,
      [DisplayString(ResourceKey = "False_s")]
      Manual
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum AllowedDirectionType
   {
      [DisplayString(ResourceKey = "BothDirections_s")]
      Both = 1,
      [DisplayString(ResourceKey = "ForwardOnly_s")]
      Foreword,
      [DisplayString(ResourceKey = "BackwordOnly_s")]
      Backward
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum AlignmentTypeVert
   {
      Top = 1,
      Center,
      Bottom
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum HtmlAlignmentType
   {
      TextVertTop = 1,
      TextVertCenter,
      TextVertBottom,
      TextHoriLeft,
      TextHoriRight
   }


   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum SideType
   {
      Top = 1,
      Right,
      Bottom,
      Left
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum SelprgMode
   {
      Before = 'B',
      After = 'A',
      Prompt = 'P'
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum WinCptn
   {
      Half = 1,
      On,
      Off
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum WinHtmlType
   {
      Get = 1, //should be 1
      Post,
      Link
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum WinUom
   {
      [DisplayString(ResourceKey = "DialogUnits_s")]
      Dlg = 1, //should be 1
      [DisplayString(ResourceKey = "Centimeters_s")]
      Mm,
      [DisplayString(ResourceKey = "Inches_s")]
      Inch,
      [DisplayString(ResourceKey = "Pixels_s")]
      Pix
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum ControlStyle
   {
      TwoD = 1, //should be 1 //CTRL_DIM_2D
      ThreeD,                 //CTRL_DIM_3D
      ThreeDSunken,           //CTRL_DIM_3D_SUNKEN
      Windows3d,              //CTRL_DIM_WINDOWS_3D
      Windows,                //CTRL_DIM_WINDOWS
      Emboss,                 //CTRL_DIM_EMBOSS
      NoBorder                //CTRL_DIM_NO_BORDER

   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum CtrlLineType
   {
      [DisplayString(ResourceKey = "RegularLine_s")]
      Normal = 1, //should be 1
      Dash,
      Dot,
      Dashdot,
      Dashdotdot
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum CtrlTextType
   {
      Default = 1, //should be 1
      Bullet,
      Number
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum CtrlLineDirection
   {
      Asc = 1, //should be 1
      Des
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum CtrlOleDisplayType
   {
      Icon = 1, //should be 1
      Content,
      Any
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum CtrlOleStoreType
   {
      Link = 1,
      Embeded,
      Any
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum CtrlButtonType
   {
      Submit = 1,
      Clear,
      Default
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum CtrlImageStyle
   {
      Tiled = 1, //should be 1
      Copied,
      ScaleFit,
      ScaleFill,
      Distorted
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum TabControlTabsWidth
   {
      FitToText = 1, //should be 1
      Fixed,
      FillToRight,
      FixedInLine,
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum CheckboxMainStyle
   {
      Box = 1, //should be 1
      Button,
      Switch
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum RbAppearance
   {
      Radio = 1, //should be 1
      Button
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum HelpCommand
   {
      Context = 1,
      Contents,
      Setcontents,
      Contextpopup,
      Key,
      Command,
      Forcefile,
      Helponhelp,
      Quit
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum FormExpandType
   {
      [DisplayString(ResourceKey = "No_s")]
      None = 1,
      OnePage,
      MultiPage
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum DitAttribute
   {
      Alpha = 1,
      Unicode,
      Numeric,
      Boolean,
      Date,
      Time,
      Memo,
      Blob
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum DspInterface
   {
      Text = 1,
      Gui,
      Html,
      Java,
      Frame,
      Merge,
      Webonline,
      Browser
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum PrgExecPlace
   {
      Before = 1,
      After,
      Prompt
   }


   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum SliderType
   {
      Vertical = 1,
      Horizontal
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum CtrlButtonTypeGui
   {
      Push = 1, //should be 1
      Image,
      Hypertext,
      TextOnImage
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum ImageEffects
   {
      Normal = 1,
      WipeDown,
      WipeUp,
      WipeRight,
      WipeLeft,
      Pixel,
      SmallBox,
      MediumBox,
      LargeBox,
      Hline,
      Vline,
      Vmiddle,
      Hmiddle,
      Hinterlace,
      Vinterlace,
      OutToIn,
      InToOut,
      OtiInterlace1,
      ItoInterlace2,
      SpiralIn3,
      SpiralOut4
   }


   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum CtrlHotspotType
   {
      Square = 1, //should be 1
      Circle
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum SubformType
   {
      Program = 1, //should be 1
      [DisplayString(ResourceKey = "SubTask_s")]
      Subtask,
      Form,
      None
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum DatabaseDefinitionType
   {
      String = 1, //should be 1
      Normal
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum DatabaseOperations
   {
      Insert = 1,
      Update,
      Delete,
      Where,
      None
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum DataTranslation
   {
      Ansi = 1, //should be 1
      Oem,
      Unicode
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum WindowPosition
   {
      Customized = 1,
      DefaultBounds,
      CenteredToParent,
      CenteredToMagic,
      CenteredToDesktop,
      DefaultLocation
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum FldStyle
   {
      None = 1,
      Activex,
      Ole,
      Vector,
      Dotnet
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum FieldComType
   {
      Obj = 1,
      Ref
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum ListboxSelectionMode
   {
      Single = 1,
      Multiple
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum SplitWindowType
   {
      None = 1,
      Vertical,
      Horizontal
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum SplitPrimaryDisplay
   {
      Default = 1,
      Left,
      Right,
      Top,
      Bottom
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum AutoFit
   {
      None = 1,
      AsControl,
      AsCalledForm
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum WindowType
   {
      Default = 1,
      Sdi,
      ChildWindow,
      SplitterChildWindow,
      Floating,
      Modal,
      ApplicationModal,
      Tool,
      FitToMdi,
      MdiChild,
      MdiFrame,

      LogonApplicationWindow = 97,
      TkDockChild = 98
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum StartupMode
   {
      Default = 1,
      Maximize,
      Minimize
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum ColumnUpdateStyle
   {
      Absolute = 1,
      Differential,
      AsTable
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum CallUdpConvention
   {
      C = 'C',
      Standard = 'S',
      Fast = 'F'
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum CallUDPType
   {
      Background = 'B',
      GUI = 'G'
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum VerifyMode
   {
      Error = 'E',
      Warning = 'W',
      Revert = 'R',
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum VerifyDisplay
   {
      Box = 'B',
      Status = 'S',
      None = 'N'
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum VerifyImage
   {
      [DisplayString(ResourceKey = "Exclamation_s")]
      Exclamation = 'E',//IMAGE_EXCLAMATION 
      [DisplayString(ResourceKey = "Critical_s")]
      Critical = 'C',//IMAGE_CRITICAL 
      [DisplayString(ResourceKey = "Question_s")]
      Question = 'Q',//IMAGE_QUESTION 
      [DisplayString(ResourceKey = "Information_s")]
      Information = 'I', //IMAGE_INFORMATION 
      [DisplayString(ResourceKey = "None_s")]
      None = 'N'// IMAGE_NONE 
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum VerifyButtons
   {
      [DisplayString(ResourceKey = "VerifyButtonOk_s")]
      Ok = 'O',//BUTTONS_OK 
      [DisplayString(ResourceKey = "OkCancel_s")]
      OkCancel = 'K', // BUTTONS_OK_CANCEL 
      [DisplayString(ResourceKey = "AbortRetryIgnore_s")]
      AbortRetryIgnore = 'A',//BUTTONS_ABORT_RETRY_IGNORE 
      [DisplayString(ResourceKey = "YesNoCancel_s")]
      YesNoCancel = 'Y', //BUTTONS_YES_NO_CANCEL 
      [DisplayString(ResourceKey = "YesNo_s")]
      YesNo = 'N',//BUTTONS_YES_NO 
      [DisplayString(ResourceKey = "RetryCancel_s")]
      RetryCancel = 'R'//BUTTONS_RETRY_CANCEL 
   } ;

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum CallComOption
   {
      Method = 1,
      GetProp,
      SetProp
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum CallWsStyle
   {
      Rpc = 1,
      Document
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum CallOsShow
   {
      Hide = 1,
      Normal,
      Maximize,
      Minimize
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum LogicUnit
   {
      Remark = 1,
      Task,
      Group,
      Record,
      Variable,
      Control,
      Event,
      Function,
      SeqFlow
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum LogicLevel
   {
      Prefix = 'P',
      Suffix = 'S',
      Verification = 'V',
      Change = 'C'
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum BrkScope
   {
      Task = 'T',       //EVENT_SCOPE_TASK
      [DisplayString(ResourceKey = "ScopeSubTree_s")]
      Subtree = 'S',    // EVENT_SCOPE_SUBTREE
      Global = 'G'      // EVENT_SCOPE_GLOBAL
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum LDir
   {
      Default = 'A',     //DB_DIR_ASCENDING     
      Reversed = 'D'    //DB_DIR_DESCENDING
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum Order
   {
      Ascending = 'A',     //DB_DIR_ASCENDING     
      Descending = 'D'    //DB_DIR_DESCENDING
   }


   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum LnkEval_Cond
   {
      Record = 'R',      //LINKOP_EVALCOND_RECORD
      Task = 'T'        //LINKOP_EVALCOND_TASK
   }

   //public enum DataviewEvalLink
   //{
   //   Task = 1,
   //   Record
   //}

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum Access
   {
      NoAccess = ' ',  //DB_NO_ACCESS
      Read = 'R',      //DB_ACCESS_READ
      Write = 'W'      //DB_ACCESS_WRITE
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum DbShare
   {
      NoShare = ' ',    //DB_NO_SHARE
      Write = 'W',  //DB_SHARE_WRITE
      Read = 'R',   //DB_SHARE_READ
      None = 'N'    //DB_SHARE_NONE
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum DbOpen
   {
      Normal = 'N',    //DB_OPEN_NORMAL
      Fast = 'F',      //DB_OPEN_FAST
      Damaged = 'D',   //DB_OPEN_DAMAGED
      Reindex = 'R'    //DB_OPEN_REINDEX
   }


   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum DbDelUpdMode       //DbDel_Upd_Mode known as 'IdentifyModifiedRow'
   {
      Position = 'P',                  //UPD_POS_CHECK = 'P'
      PositionAndSelectedFields = 'S', //UPD_ALL_CHECK = 'S'
      PositionAndUpdatedFields = 'U',  //UPD_CHANGED_CHECK = 'U'
      AsTable = 'T',              //UPD_AS_TABLE = 'T'
      None = 'N'                  //UPD_NONE = 'N'
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum RaiseAt
   {
      Container = 1,
      TaskInFocus
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum EngineDirect
   {
      None = ' ',
      [DisplayString(ResourceKey = "EngineDirectAbortTask_s")]//
      AbortTask = 'A',  // ENGINE_DIRECT_ABORT_TASK
      [DisplayString(ResourceKey = "EngineDirectRollbackAndRestart_s")]//
      Rollback = 'B',   // ENGINE_DIRECT_ROLLBACK
      [DisplayString(ResourceKey = "EngineDirectAutoRetry_s")]//
      AutoRetry = 'R',  // ENGINE_DIRECT_AUTO_RETRY
      [DisplayString(ResourceKey = "EngineDirectUserRetry_s")]//
      UserRetry = 'U',  // ENGINE_DIRECT_USER_RETRY
      [DisplayString(ResourceKey = "EngineDirectIgnore_s")]//
      Ignore = 'I',     // ENGINE_DIRECT_IGNORE
      [DisplayString(ResourceKey = "EngineDirectAsStrategy_s")]//
      AsStrategy = 'S', // ENGINE_DIRECT_AS_STRATEGY
      [DisplayString(ResourceKey = "EngineDirectContinue_s")]
      Continue = 'C'    // ENGINE_DIRECT_CONTINUE
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum FlowDirection     //Flw_Dir
   {
      [DisplayString(ResourceKey = "Forward_s")]
      Forward = 'F',  //FLW_DIR_FORWARD
      [DisplayString(ResourceKey = "Backward_s")]
      Backward = 'B', //FLW_DIR_BACKWARD
      [DisplayString(ResourceKey = "Combine_s")]
      Combined = 'C'  //FLW_DIR_COMBINED
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum FlwMode
   {
      [DisplayString(ResourceKey = "Fast_s")]
      Fast = 'F',                //FLW_MODE_FAST 
      [DisplayString(ResourceKey = "Step_s")]
      Step = 'S',                //FLW_MODE_STEP 
      [DisplayString(ResourceKey = "Combine_s")]
      Combine = 'B',             //FLW_MODE_BOTH 
      [DisplayString(ResourceKey = "Jump_s")]
      Before = 'J',              //FLW_MODE_JUMP 
      [DisplayString(ResourceKey = "Zoom_s")]
      After = 'Z'                // FLW_MODE_ZOOM 
   }


   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum RowHighlightType
   {
      None = 1,
      Frame,
      Background,
      BackgroundControls
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum SplitterStyle
   {
      TwoD = 1,
      ThreeD

   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum BottomPositionInterval
   {
      NoneRowHeight = 1,
      RowHeight

   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum TableColorBy
   {
      Column = 1,
      Table,
      Row
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum ExecOn
   {
      None = 0, // it is the default 
      Optimized = 1, // BRK_EXEC_ON_OPTIMIZED 
      Client = 2,    // BRK_EXEC_ON_CLIENT 
      Server = 3     // BRK_EXEC_ON_SERVER 
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum MultilineHorizontalScrollBar
   {
      Unknown = 0,
      Yes = 1,
      No,
      WordWrap
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum FramesetStyle
   {
      None = 0,
      Vertical,
      Horizontal
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum FrameType
   {
      FrameSet = 0,
      Subform,
      Form
   }


   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum Storage
   {
      AlphaString = 1,
      AlphaLstring = 2,
      AlphaZtring = 3,
      NumericSigned = 4,
      NumericUnsigned = 5,
      NumericFloat = 6,
      NumericFloatMs = 7,
      NumericFloatDec = 8,
      NumericPackedDec = 9,
      NumericNumeric = 10,
      NumericCharDec = 11,
      NumericString = 12,
      NumericMagic = 13,
      NumericCisam = 14,
      BooleanInteger = 15,        // boolean 
      BooleanDbase = 16,
      DateInteger = 17,        // date 
      DateInteger1901 = 18,
      DateString = 19,
      DateYymd = 20,
      DateMagic = 21,
      DateMagic1901 = 22,
      TimeInteger = 23,       // time 
      TimeString = 24,
      TimeHmsh = 25,
      TimeMagic = 26,
      MemoString = 27,
      MemoMagic = 28,
      Blob = 29,
      NumericExtFloat = 30,
      UnicodeString = 31,
      UnicodeZstring = 32,
      AnsiBlob = 33,
      UnicodeBlob = 34

   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum StorageAttributeType
   {
      Alpha = 'A',
      Numeric = 'N',
      [DisplayString(ResourceKey = "Logical_s")]
      Boolean = 'B',
      Date = 'D',
      Time = 'T',
      Blob = 'O',
      Unicode = 'U',
      String = 'S',
      [DisplayString(ResourceKey = "Ole_s")]
      BlobOle = 'L',
      [DisplayString(ResourceKey = "ActiveX_s")]
      BlobActiveX = 'X',
      [DisplayString(ResourceKey = "Vector_s")]
      BlobVector = 'V',
      [DisplayString(ResourceKey = "Dotnet_s")]
      BlobDotNet = 'E',
      [DisplayString(ResourceKey = "EmptyString")]
      None = ' '
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum BrkLevel
   {
      Task = 'T',           // HANDLER_LEVEL_TASK 
      Group = 'G',          // HANDLER_LEVEL_GROUP 
      All = 'A',            // HANDLER_LEVEL_ALL 
      Record = 'R',         // HANDLER_LEVEL_RECORD 
      Control = 'C',        // HANDLER_LEVEL_CONTROL
      [DisplayString(ResourceKey = "BrkLevelHandler_s")]
      Handler = 'H',        // HANDLER_LEVEL_HANDLER
      MainProgram = 'M',    // HANDLER_LEVEL_MAIN_PROG 
      Variable = 'V',       // HANDLER_LEVEL_VARIABLE 
      Function = 'F',       // HANDLER_LEVEL_FUNCTION 
      Remark = 'K',         // HANDLER_LEVEL_REMARK 
      RM_Compat = 'M',      // HANDLER_LEVEL_RM_COMPAT 
      SubForm = 'U',        // HANDLER_LEVEL_SUBFORM 
      [DisplayString(ResourceKey = "BrkLevelEvent_s")]
      Event = 'E',          // HANDLER_LEVEL_EVENT 
      OPStatOnChange = 'O'  // OPSTAT_ON_CHANGE      It can be also ... need to be check more...
   }

   /// <summary>
   ///[IDAN][IO_DEVICES]:additional enumerators for IO Devices
   /// <summary>
   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum MediaOrientation
   {
      Portrait = 'P',            //originally c++ value from const.dat
      Landscape = 'L',
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum MediaFormat
   {
      Page = 'P',            //originally c++ value IOFE_FORMAT_PAGE
      Line = 'L',            //originally c++ value IOFE_FORMAT_LINE
      None = 'N'             //originally c++ value IOFE_FORMAT_NONE
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum CharacterSet
   {
      Ansi = 0,                //originally c++ value IO_CHARSET_ANSI
      Oem = 1,                 //originally c++ value IO_CHARSET_OEM
      Unicode = 3,             //originally c++ value IO_CHARSET_UNICODE
      Utf8 = 4,                //originally c++ value IO_CHARSET_UTF8   

   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum MediaAccess
   {
      Read = 'R',                                              //originally c++ value FIO_ACCESS_READ
      Write = 'W',                                             //originally c++ value FIO_ACCESS_WRITE
      Append = 'A',                                            //originally c++ value FIO_ACCESS_APPEND
      Direct = 'D',                                            //originally c++ value FIO_ACCESS_DIRECT
      AppendFlush = 'F',                                       //originally c++ value FIO_ACCESS_APPEND_FLUSH
      Create = 'C',                                            //originally c++ value FIO_ACCESS_CREATE
   }
   //TODO: [IDAN][CODE_REVIEW]:when merged to enum paper size 
   // there were conflicts: PaperSizePdfDisabled- B4='B',
   //PaperSizePdfEnabled-B5 ='B',B4='4' 
   //so in the merged B5='W'
   //and converter added to cpp code

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum PaperSize
   {
      Default = 'D',
      Letter = 'L',
      A4 = 'A',
      Folio = 'F',
      Quarto = 'Q',
      [DisplayString(ResourceKey = "PaperSizeTabloid_s")]
      Tabloid = 'T',    //Tabloid 17 x 11 inch
      [DisplayString(ResourceKey = "PaperSizeLedger_s")]
      Ledger = 'R',    //LedgeR 17 x 11 inch
      [DisplayString(ResourceKey = "PaperSizeLegal_8_14_s")]
      Legal_8_14 = 'G',    //LeGal 8.5 x 14 inch
      [DisplayString(ResourceKey = "PaperSizeStatement_s")]
      Statement = 'S',    //Statement 5.5 x 8.5 inch    
      [DisplayString(ResourceKey = "PaperSizeExecutive_7_10_s")]
      Executive_7_10 = 'X',  //EXecutive 7.5 x 10.5 inch
      A3 = '3',
      A5 = '5',
      [DisplayString(ResourceKey = "PaperSizeNote_s")]
      Note = 'N',  //Note 8.5 x 11 inch 
      [DisplayString(ResourceKey = "PaperSizeEnvelope_3_8_s")]
      Envelope_3_8 = 'E', //Envelope #9 3.5 x 8.5 inch
      B4 = 'B',
      B5_v = 'V',
      UserDefined = 'U',
      B5 = 'W', // Added new value because there is conflict with b4 in pdf disabled paper size enum
      C5 = 'C',
      Legal = 'g',
      [DisplayString(ResourceKey = "PaperSizeMultipurpose_s")]
      Multipurpose = 'M',   //Multipurpose 8 X 11
      Executive = 'x',
      EnvelopeB4 = 'v',
      EnvelopeB5 = 'p',
      EnvelopeC6 = '6',
      EnvelopeDL = 'o',
      EnvelopeMonarch = 'h',
      Envelope9 = '9',
      Envelope10 = '0',
      Envelope11 = '1'


   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum PaperSizePdfDisabled
   {
      Default = 'D',
      Letter = 'L',
      A4 = 'A',
      Folio = 'F',
      Quarto = 'Q',
      [DisplayString(ResourceKey = "PaperSizeTabloid_s")]
      Tabloid = 'T',    //Tabloid 17 x 11 inch
      [DisplayString(ResourceKey = "PaperSizeLedger_s")]
      Ledger = 'R',    //LedgeR 17 x 11 inch
      [DisplayString(ResourceKey = "PaperSizeLegal_8_14_s")]
      Legal_8_14 = 'G',    //LeGal 8.5 x 14 inch
      [DisplayString(ResourceKey = "PaperSizeStatement_s")]
      Statement = 'S',    //Statement 5.5 x 8.5 inch    
      [DisplayString(ResourceKey = "PaperSizeExecutive_7_10_s")]
      Executive_7_10 = 'X',  //EXecutive 7.5 x 10.5 inch
      A3 = '3',
      A5 = '5',
      [DisplayString(ResourceKey = "PaperSizeNote_s")]
      Note = 'N',  //Note 8.5 x 11 inch 
      [DisplayString(ResourceKey = "PaperSizeEnvelope_3_8_s")]
      Envelope_3_8 = 'E', //Envelope #9 3.5 x 8.5 inch
      B4 = 'B',
      B5_v = 'V',
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum PaperSizePdfEnabled
   {
      Default = 'D',
      UserDefined = 'U',
      Letter = 'L',
      A4 = 'A',
      A3 = '3',
      Legal = 'g',
      B5 = 'B',
      C5 = 'C',
      [DisplayString(ResourceKey = "PaperSizeMultipurpose_s")]
      Multipurpose = 'M',   //Multipurpose 8 X 11
      B4 = '4',
      A5 = '5',
      Folio = 'F',
      Executive = 'x',
      EnvelopeB4 = 'v',
      EnvelopeB5 = 'p',
      EnvelopeC6 = '6',
      EnvelopeDL = 'o',
      EnvelopeMonarch = 'h',
      Envelope9 = '9',
      Envelope10 = '0',
      Envelope11 = '1'
   }

   /// <summary>
   ///[IDAN][Forms]:additional enumerators for Forms
   /// <summary>
   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum Area
   {
      Detail = 'N',
      Header = 'H',
      Footer = 'F',
      PageHeader = 'P',
      PageFooter = 'G'
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum DisplayTextType
   {
      Edit,
      Query
   }

   //BrkLevel
   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum LogicHeaderType
   {
      None = ' ',
      Remark = 'K',
      Task = 'T',
      Function = 'F',
      Handler = 'H',
      Record = 'R',
      Variable = 'V',
      Control = 'C',
      Group = 'G',
      All = 'A',
      MainProgram = 'P',      //need to be M
      RecordCompat = 'M',
      SubForm = 'U',
      Event = 'E'
   }

   //Operation Level
   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum LogicOperationType
   {
      None = 'x',
      Remark = ' ',
      Update = 'U',
      Call = 'C',
      Invoke = 'I',
      RaiseEvent = 'R',
      Evaluate = 'A',
      Block = 'B',
      Verify = 'E',
      Form = 'F',
      Variable = 'V'
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum Opr
   {
      Remark = 0,             // FLW_OP_REMARK
      SelFld = 1,             // FLW_OP_SEL_FLD
      Stop = 2,               // FLW_OP_STOP
      BeginLink = 3,          // FLW_OP_BEG_LNK
      EndLink = 4,            // FLW_OP_END_LNK
      BeginBlock = 5,         // FLW_OP_BEG_BLK
      EndBlock = 6,           // FLW_OP_END_BLK
      Call = 7,               // FLW_OP_CALL
      EvaluateExpression = 8, // FLW_OP_EVAL_EXP
      UpdateFld = 9,          // FLW_OP_UPD_FLD
      WriteFile = 10,         // FLW_OP_WRITE_FILE
      ReadFile = 11,          // FLW_OP_READ_FILE
      DataviewSrc = 12,       // FLW_OP_DATAVW_SRC
      UserExit = 13,          // FLW_OP_USR_EXIT
      RaiseEvent = 14         // FLW_OP_RAISE_EVNT
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum DataViewHeaderType
   {
      None = ' ',
      Remark = 'R',
      Declare = 'D',
      MainSource = 'M',
      DirectSQL = 'Q',
      LinkQuery = 'L',
      LinkWrite = 'W',
      LinkCreate = 'C',
      LinkIJoin = 'I',
      LinkOJoin = 'O',
      EndLink = 'E',
   }

   // Enum values should be similar to TASKRULE_* values (TaskContainer.h)
   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum DataViewOperationType
   {
      Remark = ' ',
      Column = 'C',
      Virtual = 'V',
      Parameter = 'P',
      LinkedColumn = 'L'
   }

   /// <summary>
   /// enum copy from c++
   /// </summary>
   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum LoadedValues
   {
      None = 0,
      HeaderOnly = 1,
      Failed = 2,
      Full = 3
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum YesNoValues
   {
      Yes = 1,
      No = 0

   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum TrueFalseValues
   {
      True = 1,
      False = 0

   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum HelpType
   {
      Internal = 'I',
      Prompt = 'P',
      Windows = 'W',
      Tooltip = 'T',
      URL = 'U'

   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum NullArithmetic
   {
      Nullify = 0,
      UseDefault = 1
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum ModelClass
   {
      Help = 'A',
      Field = 'B',
      Browser = 'C',
      [DisplayString(ResourceKey = "GUIDisplay_s")]
      GUI0 = 'D',
      [DisplayString(ResourceKey = "GUIOutPut_s")]
      GUI1 = 'E',
      TextBased = 'F',
      Frameset = 'G',
      Merge = 'H',
      RCDisplay = 'I',
      RCFrame = 'J',
      [DisplayString(ResourceKey = "GUIFrames_s")]
      GuiFrame = 'K'

   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum CompTypes
   {
      Magicxpa = 'U',
      [DisplayString(ResourceKey = "Dotnet_s")]
      DotNet = 'D'
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum TaskFlow
   {
      Undefined = 'U',
      Online = 'O',
      Batch = 'B',
      Browser = 'R',
      RichClient = 'C'
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum RemarkType
   {
      RegularOperation = 0x0000, // REGULAR_OPR_REMARK
      Dataviewheader = 0x0001,   // DATAVW_HDR_REMARK
      TaskLogic = 0x0002         // TASK_LOGIC_REMARK
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum VeeMode
   {
      None = ' ',      // TASKRULE_VEE_NONE
      Parameter = 'P', // TASKRULE_VEE_PARAMETER
      Virtual = 'V',   // TASKRULE_VEE_VIRTUAL
      Real = 'R',      // TASKRULE_VEE_REAL
      Column = 'C',    // TASKRULE_VEE_COLUMN
      LinkCol = 'L'    // TASKRULE_VEE_LINK_COL
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum VeeDiffUpdate
   {
      AsTable = 'T',      // DIFF_UPDATE_AS_TABLE
      Absolute = 'N',     // DIFF_UPDATE_ABSOLUTE
      Differential = 'Y', // DIFF_UPDATE_DIFFERENTIAL
      None = 0            // DIFF_UPDATE_NONE
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum VeePartOfDataview
   {
      Undefined = 'U' //VEE_PARTOFDATAVIEW_UNDEFINED
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum DataviewType
   {
      MainTable = 'M',  // DATA_VW_MAIN_TABLE
      DSQL = 'Q',       // DATA_VW_DSQL
      Declaration = 'D' // DATA_VW_DECLARATION
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum TabbingCycleType
   {
      RemainInCurrentRecord = 'R', // TABBING_CYCLE_REMAIN_IN_CURRENT_RECORD
      MoveToNextRecord = 'N',      // TABBING_CYCLE_MOVE_TO_NEXT_RECORD
      MoveToParentTask = 'P'       // TABBING_CYCLE_MOVE_TO_PARENT_TASK
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum LockingStrategy
   {
      [DisplayString(ResourceKey = "LockingStrategy_Immediate_s")]
      Immediate = 'I',    // LOCK_STRG_IMMEDIATE
      [DisplayString(ResourceKey = "LockingStrategy_OnModify_s")]
      OnModify = 'O',     // LOCK_STRG_ON_MODIFY
      [DisplayString(ResourceKey = "LockingStrategy_AfterModify_s")]
      AfterModify = 'A',  // OLD_LOCK_STRG_AFTER_MODIFY
      [DisplayString(ResourceKey = "LockingStrategy_BeforeUpdate_s")]
      BeforeUpdate = 'B', // LOCK_STRG_BEFORE_UPD
      [DisplayString(ResourceKey = "LockingStrategy_Minimum_s")]
      Minimum = 'M'       // LOCK_STRG_MINIMUM
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum TransBegin
   {
      [DisplayString(ResourceKey = "TransBegin_Update_s")]
      Update = 'U',     // FLW_TRANS_UPDATE
      [DisplayString(ResourceKey = "TransBegin_Prefix_s")]
      Prefix = 'P',     // FLW_TRANS_PREFIX
      [DisplayString(ResourceKey = "TransBegin_Suffix_s")]
      Suffix = 'S',     // FLW_TRANS_SUFFIX
      [DisplayString(ResourceKey = "TransBegin_OnLock_s")]
      OnLock = 'L',     // FLW_TRANS_ON_LOCK
      [DisplayString(ResourceKey = "TransBegin_None_s")]
      None = 'N',       // FLW_TRANS_NONE
      [DisplayString(ResourceKey = "TransBegin_BeforeTask_s")]
      BeforeTask = 'T', // FLW_TRANS_BEFORE_TASK
      [DisplayString(ResourceKey = "TransBegin_Group_s")]
      Group = 'G'       // FLW_TRANS_GROUP
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum ErrStrategy
   {
      [DisplayString(ResourceKey = "ErrStrategy_Recover_s")]
      Recover = 'R', // ERR_STRATEGY_RECOVER
      [DisplayString(ResourceKey = "ErrStrategy_Abort_s")]
      Abort = 'A'    // ERR_STRATEGY_ABORT
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum CacheStrategy
   {
      [DisplayString(ResourceKey = "CacheStrategy_Position_s")]
      Pos = 'P',     // CACHE_STRATEGY_POS
      [DisplayString(ResourceKey = "CacheStrategy_PosData_s")]
      PosData = 'D', // CACHE_STRATEGY_POS_DATA
      [DisplayString(ResourceKey = "CacheStrategy_None_s")]
      None = 'N',    // CACHE_STRATEGY_NONE
      [DisplayString(ResourceKey = "CacheStrategy_AsTable_s")]
      AsTable = 'T'  // CACHE_STRATEGY_AS_TABLE
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum ExeState
   {
      Prefix = 'P',        // EXESTAT_PREFIX
      Suffix = 'S',        // EXESTAT_SUFFIX
      Update = 'U',        // EXESTAT_UPDATE
      Main = 'M',          // EXESTAT_MAIN
      Before = 'B',        // EXESTAT_BEFORE
      AfterOnChange = 'O', // EXESTAT_AFTER_ON_CHANGE
      Verify = 'V',        // EXESTAT_VERIFY
      Change = 'C'         // EXESTAT_CHANGE
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum TransMode
   {
      [DisplayString(ResourceKey = "TransMode_Deferred_WithinActiveDeferred_s")]
      Deferred = 'D',          // TRANS_MODE_DEFERRED
      [DisplayString(ResourceKey = "TransMode_NestedNewDeffered_s")]
      NestedDeffered = 'N',    // TRANS_MODE_NESTED_DEFERRED
      [DisplayString(ResourceKey = "TransMode_Physical_s")]
      Physical = 'P',          // TRANS_MODE_PHYSICAL
      [DisplayString(ResourceKey = "TransMode_WithinActiveTrans_s")]
      WithinActiveTrans = 'W', // TRANS_MODE_WITHIN_ACTIVE_TRANS
      [DisplayString(ResourceKey = "TransMode_None_s")]
      None = 'O'               // TRANS_MODE_NONE
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum PositionUsage
   {
      [DisplayString(ResourceKey = "PositionUsage_RangeOn_s")]
      RangeOn = 'O',   // POSITION_RANGE_ON
      [DisplayString(ResourceKey = "PositionUsage_DisplayFrom_s")]
      RangeFrom = 'F', // POSITION_RANGE_FROM
      [DisplayString(ResourceKey = "PositionUsage_Locate_s")]
      Locate = 'L'     // POSITION_LOCATE
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum LnkMode
   {
      Query = 'R',  // LINK_QUERY
      Write = 'W',  // LINK_WRITE
      Create = 'A', // LINK_CREATE
      IJoin = 'J',  // LINK_I_JOIN
      OJoin = 'O'   // LINK_O_JOIN
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum InitialMode
   {
      Modify = 'M',
      Create = 'C',
      Delete = 'D',
      Query = 'E',
      AsParent = 'P',
      Locate = 'L',
      Range = 'R',
      Key = 'K',
      Sort = 'S',
      Files = 'O',
      Options = 'N',
      ByExp = 'B'
   }


   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum KeyMode
   {
      Normal = 'N', // KEY_MODE_NORMAL
      Insert = 'I', // KEY_MODE_INSERT
      Append = 'A'  // KEY_MODE_APPEND
   }


   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum BoxDir
   {
      Vertical = 'V',  // BOX_DIR_VERTICAL
      Horizontal = 'H' // BOX_DIR_HORIZONTAL
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum EndMode
   {
      [DisplayString(ResourceKey = "EndMode_BeforeEnteringRecord_s")]
      Before = 'B',   // End_MODE_BEFORE
      [DisplayString(ResourceKey = "EndMode_AfterUpdatingRecord_s")]
      After = 'A',    // End_MODE_After
      [DisplayString(ResourceKey = "EndMode_ImmediatelyWhenConditionIsChanged_s")]
      Immediate = 'I' // End_MODE_IMMEDIATE
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum UniqueTskSort
   {
      AccordingToIndex = 'A', // UNIQUE_TSK_SORT_ACCORDING_TO_INDEX
      Unique = 'U'            // UNIQUE_TSK_SORT_UNDEFINED
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum BrkType
   {
      Prefix = 'P',      // HANDLER_TYPE_PREFIX
      Suffix = 'S',      // HANDLER_TYPE_SUFFIX
      Main = 'M',        // HANDLER_TYPE_MAIN
      User = 'U',        // HANDLER_TYPE_USER
      Error = 'E',       // HANDLER_TYPE_ERROR
      [DisplayString(ResourceKey = "Verification_s")]
      Verify = 'V',      // HANDLER_TYPE_VERIFY
      [DisplayString(ResourceKey = "Change_s")]
      ChoiceChange = 'C' // HANDLER_TYPE_CHOICE_CNG
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum ErrorClassific
   {
      Any = 0,                // DB_ERR_ANY
      RecLocked = 1,          // DB_ERR_REC_LOCKED
      DupKey = 2,             // DB_ERR_DUP_KEY
      ConstrFail = 3,         // DB_ERR_CONSTR_FAIL
      TriggerFail = 4,        // DB_ERR_TRIGGER_FAIL
      RecUpdated = 5,         // DB_ERR_REC_UPDATED
      RowsAffected = 6,       // DB_ERR_NO_ROWS_AFFECTED
      UpdateFail = 7,         // DB_ERR_UPDATE_FAIL
      Unmapped = 8,           // DB_ERR_UNMAPPED
      ExecSql = 9,            // DB_ERR_EXEC_SQL
      BadSqlCmd = 10,         // DB_ERR_BAD_SQL_CMD
      BadIni = 11,            // DB_ERR_BADINI
      BaName = 12,            // DB_ERR_BADNAME
      Damaged = 13,           // DB_ERR_DAMAGED
      Unlocked = 14,          // DB_ERR_UNLOCKED
      BadOpen = 15,           // DB_ERR_BADOPEN
      BadClose = 16,          // DB_ERR_BADCLOSE
      RsrcLocked = 17,        // DB_ERR_RSRC_LOCKED
      RecLockedNoBuf = 18,    // DB_ERR_REC_LOCKED_NOBUF
      NoDef = 19,             // DB_ERR_NODEF
      RecLockedNow = 20,      // DB_ERR_REC_LOCKED_NOW
      WrnRetry = 21,          // DB_WRN_RETRY
      RecLockedMagic = 22,    // DB_ERR_REC_LOCKED_MAGIC
      ReadOnly = 23,          // DB_ERR_READONLY
      WrnCreated = 24,        // DB_WRN_CREATED
      Capacity = 25,          // DB_ERR_CAPACITY
      TransCommit = 26,       // DB_ERR_TRANS_COMMIT
      TransOpen = 27,         // DB_ERR_TRANS_OPEN
      TransAbort = 28,        // DB_ERR_TRANS_ABORT
      BadDef = 29,            // DB_ERR_BADDEF
      InvalidOwnr = 30,       // DB_ERR_INVALID_OWNR
      ClrOwnrFail = 31,       // DB_ERR_CLR_OWNR_FAIL
      AlterTbl = 32,          // DB_ERR_ALTER_TBL
      SortTbl = 33,           // DB_ERR_SORT_TBL
      CanotRemove = 34,       // DB_ERR_CANOT_REMOVE
      CanotRename = 35,       // DB_ERR_CANOT_RENAME
      WrnLogActive = 36,      // DB_WRN_LOG_ACTIVE
      TargetFileExist = 37,   // DB_ERR_TARGET_FILE_EXIST
      FileIsView = 38,        // DB_ERR_FILE_IS_VIEW
      CanotCopy = 39,         // DB_ERR_CANOT_COPY
      Stop = 40,              // DB_ERR_STOP
      StrBadName = 41,        // DB_ERR_STR_BAD_NAME
      InsertIntoAll = 42,     // DB_ERR_INSERT_INTO_ALL
      BadQry = 43,            // DB_ERR_BAD_QRY
      FilterAfterInsert = 44, // DB_ERR_FILTER_AFTER_INSERT
      GetUserPwdDst = 45,     // DB_GET_USER_PWD_DST
      WrnCacheTooBig = 46,    // DB_WRN_CACHE_TOO_BIG
      LostRec = 47,           // DB_ERR_LOSTREC
      FileLocked = 48,        // DB_ERR_FILE_LOCKED
      MaxConnEx = 49,         // DB_ERR_MAX_CONN_EX
      Deadlock = 50,          // DB_ERR_DEADLOCK
      BadCreate = 51,         // DB_ERR_BADCREATE
      FilNotExist = 52,       // DB_ERR_FIL_NOT_EXIST
      Unused = 53,            // DB_ERR_UNUSED
      IdxCreateFail = 54,     // DB_ERR_IDX_CREATE_FAIL
      ConnectFail = 55,       // DB_ERR_CONNECT_FAIL
      Fatal = 56,             // DB_ERR_FATAL
      InsertFail = 57,        // DB_ERR_INSERT_FAIL
      DeleteFail = 58,        // DB_ERR_DELETE_FAIL
      InErrorZone = 59,       // DB_ERR_IN_ERR_ZONE
      NoRec = 60,             // DB_ERR_NOREC
      NotExist = 61,          // DB_ERR_NOT_EXIST
      GetUserPwd = 62,        // DB_GET_USER_PWD
      WrnCancel = 63,         // DB_WRN_CANCEL
      NotSupportedFunc = 64,  // DB_ERR_NOTSUPPORT_FUNC
      ModifyWithinTrans = 65, // DB_ERR_MODIFY_WITHIN_TRANS
      LoginPwd = 66,          // DB_ERR_LOGIN_PWD
      None = 67               // DB_ERR_NONE
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum ComponentItemType
   {
      Models = 0,
      DataSources,
      Programs,
      Helps,
      Rights,
      Events,
      Functions
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum FieldComAlloc
   {
      Auto = 1, // FIELD_COM_ALLOC_AUTO 
      None      // FIELD_COM_ALLOC_NONE
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum BlobContent
   {
      Unknown = '0', // BLOB_CONTENT_UNKNOWN
      Ansi = '1',        // BLOB_CONTENT_ANSI
      Unicode = '2',     // BLOB_CONTENT_UNICODE
      Binary ='3'      // BLOB_CONTENT_BINARY
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum DBHRowIdentifier
   {
      RowId = 'R',    // IDENTIFIER_ROW_ID
      Default = 'D',  // IDENTIFIER_DEFAULT
      UniqueKey = 'U' // IDENTIFIER_UNIQUE_KEY
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum UseSQLCursor
   {
      Yes = 'Y', // DB_DBH_CURSOR_YES
      No = 'N', // DB_DBH_CURSOR_NO
      Default = 'D', // DB_DBH_CURSOR_DEFAULT
      //CheckExistsYes = 'Y', // DB_DBH_CHECK_EXIST_YES
      //CheckExistsNo = 'N', // DB_DBH_CHECK_EXIST_NO
      //CheckExistsDB = 'D' // DB_DBH_CHECK_EXIST_DB
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum Encr
   {
      None = 'N', // DB_NO_ENCR
      DB = 'E',   // DB_ENCR
      ROnly = 'R' // DB_ENCR_RONLY
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum DBHCache
   {
      Pos = 'P',     // CACHE_STRATEGY_POS
      PosData = 'D', // CACHE_STRATEGY_POS_DATA
      None = 'N',    // CACHE_STRATEGY_NONE
      AsTable = 'T'  // CACHE_STRATEGY_AS_TABLE
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum Resident
   {
      No = 'N',                 // DB_RESIDENT_NO
      Immediate = 'I',          // DB_RESIDENT_IMMEDIATE
      OnDemand = 'D',           // DB_RESIDENT_ON_DEMAND
      ImmediateAndClient = 'C', // DBRESIDENT_IMMEDIATE_AND_CLIENT
      ImmediateAndBrowser = 'B' // DBRESIDENT_IMMEDIATE_AND_BROWSER
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum CheckExist
   {
      //CursorYes = 'Y',     // DB_DBH_CURSOR_YES
      //CursorNo = 'N',      // DB_DBH_CURSOR_NO
      //CursorDefault = 'D', // DB_DBH_CURSOR_DEFAULT
      [DisplayString(ResourceKey = "Yes_s")]
      CheckYes = 'Y',      // DB_DBH_CHECK_EXIST_YES
      [DisplayString(ResourceKey = "No_s")]
      CheckNo = 'N',       // DB_DBH_CHECK_EXIST_NO
      [DisplayString(ResourceKey = "AsDatabase_s")]
      CheckDB = 'D'        // DB_DBH_CHECK_EXIST_DB
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum ValType
   {
      ZString = 1, // VAL_TYPE_Z_STRING
      MagicNum,    // VAL_TYPE_MAGIC_NUM
      Boolean,     // VAL_TYPE_BOOLEAN
      UString      // VAL_TYPE_U_STRING
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum FldStorage
   {
      // supported by all DB's!
      AlphaString = 1,        // STORAGE_ALPHA_STRING
      AlphaLString = 2,       // STORAGE_ALPHA_LSTRING
      AlphaZString = 3,       // STORAGE_ALPHA_ZSTRING
      // numeric
      NumericSigned = 4,      // STORAGE_NUMERIC_SIGNED
      NumericUnsigned = 5,    // STORAGE_NUMERIC_UNSIGNED
      NumericFloat = 6,       // STORAGE_NUMERIC_FLOAT
      NumericFloatMS = 7,     // STORAGE_NUMERIC_FLOAT_MS
      NumericFloatDec = 8,    // STORAGE_NUMERIC_FLOAT_DEC
      NumericPackedDec = 9,   // STORAGE_NUMERIC_PACKED_DEC
      NumericNumeric = 10,    // STORAGE_NUMERIC_NUMERIC
      NumericCharDec = 11,    // STORAGE_NUMERIC_CHAR_DEC
      NumericString = 12,     // STORAGE_NUMERIC_STRING
      NumericMagic = 13,      // STORAGE_NUMERIC_MAGIC
      NumericCisam = 14,      // STORAGE_NUMERIC_CISAM
      NumericExtFloat = 30,   // STORAGE_NUMERIC_EXT_FLOAT
      // boolean
      BooleanInteger = 15,    // STORAGE_BOOLEAN_INTEGER
      BooleanDBase = 16,      // STORAGE_BOOLEAN_DBASE
      // date
      DateInteger = 17,       // STORAGE_DATE_INTEGER
      DateInteger1901 = 18,   // STORAGE_DATE_INTEGER_1901
      DateString = 19,        // STORAGE_DATE_STRING
      DateYYMD = 20,          // STORAGE_DATE_YYMD
      DateMagic = 21,         // STORAGE_DATE_MAGIC
      DateMagic1901 = 22,     // STORAGE_DATE_MAGIC_1901
      // time
      TimeInteger = 23,       // STORAGE_TIME_INTEGER
      TimeString = 24,        // STORAGE_TIME_STRING
      TimeHMSH = 25,          // STORAGE_TIME_HMSH
      TimeMagic = 26,         // STORAGE_TIME_MAGIC

      MemoString = 27,        // STORAGE_MEMO_STRING
      MemoMagic = 28,         // STORAGE_MEMO_MAGIC

      Blob = 29,              // STORAGE_BLOB
      UnicodeString = 31,     // STORAGE_UNICODE_STRING
      UnicodeZString = 32,    // STORAGE_UNICODE_ZSTRING
      AnsiBlob = 33,          // STORAGE_ANSI_BLOB
      UnicodeBlob = 34        // STORAGE_UNICODE_BLOB
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum DriverDB
   {
      Btrv = 0,          // DRIVER_DB_BTRV
      Prevesive2000 = 1, // DRIVER_DB_PREVESIVE2000
      RMS = 2,           // DRIVER_DB_RMS
      MySQL = 3,         // DRIVER_DB_MYSQL
      DBase = 4,         // DRIVER_DB_DBASE
      Cache = 5,         // DRIVER_DB_CACHE
      DB2AS400 = 6,      // DRIVER_DB_DB2_AS400
      FoxBase = 7,       // DRIVER_DB_FOXBASE
      Clipper = 8,       // DRIVER_DB_CLIPPER
      SyBase = 9,        // DRIVER_DB_SYBASE
      // DRIVER_DB_MSQL           =10,
      // DRIVER_DB_PARADOX        =11,
      Cics = 12,         // DRIVER_DB_CICS
      Oracle = 13,       // DRIVER_DB_ORACLE
      Informix = 14,     // DRIVER_DB_INFORMIX
      Ingres = 15,       // DRIVER_DB_INGRES
      AS400 = 16,        // DRIVER_DB_AS400
      // DRIVER_DB_MFCOB          =17,
      DB2 = 18,          // DRIVER_DB_DB2
      Odbc = 19,         // DRIVER_DB_ODBC
      MS6 = 20,          // DRIVER_DB_MS6
      Memory = 21,       // DRIVER_DB_MEMORY
      RMCOB = 22         // DRIVER_DB_RMCOB
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum ExportType
   {
      EntireProject = 'A',
      Models = 'E',
      DataSources = 'F',
      Programs = 'P',
      Helps = 'H',
      Rights = 'R',
      Menus = 'M',
      CompositeResources = 'O',
      ApplicationProperties = 'C'
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum TriggerType
   {
      System = 'S',            // TRIGGER_TYPE_SYSTEM
      Timer = 'T',             // TRIGGER_TYPE_TIMER
      Expression = 'E',        // TRIGGER_TYPE_EXPRESSION
      Internal = 'I',          // TRIGGER_TYPE_INTERNAL
      None = 'N',              // TRIGGER_TYPE_NONE
      Component = 'C',         // TRIGGER_TYPE_COMPONENT
      User = 'U',              // TRIGGER_TYPE_USER
      Error = 'R',             // TRIGGER_TYPE_ERROR
      ComEvent = 'X',          // TRIGGER_TYPE_COM_EVENT
      DotNetEvent = 'D',       // TRIGGER_TYPE_DOTNET_EVENT
      PublicUserEvent = 'P',   // TRIGGER_TYPE_PUBLIC_USER_EVENT
      UserFunc = 'F'           // TRIGGER_TYPE_USER_FUNC
   }

   /// <summary>
   /// flags for operation. see defines in exp.h file
   /// </summary>
   [TypeConverter(typeof(LocalizationEnumConverter))]
   [Flags]
   public enum ItemMasks
   {
      Undefined = 0,                         // UNDEFINED
      ActiveInClient = 1,                    // ACTIVE_IN_CLIENT
      MagicSqlFunc = 1<<2,                   // MAGIC_SQL_FUNC
      CacheAlways = 1<<3,                    // CACHE_ALWAYS
      CacheSometimes = 1<<4,                 // CACHE_SOMETIMES
      RtSearchExecAllowed = 1<<5,            // RT_SEARCH_EXEC_ALLOWED
      ArgAttrAsResult = 1<<6,                // ARG_ATTR_AS_RESULT
      CalcResAttr = 1<<7,                    // CALC_RES_ATTR                     // Need to calculate result attr. before evaluating an expression
      PossibleReentrance = 1<<8,             // POSSIBLE_REENTRANCE               // need to set possiblereentrancy_ flag to true
      ForceClientExecBrowserClient = 1<<9,   // FORCE_CLIENT_EXEC_BROWSER_CLIENT
      ForceServerExecBrowserClient = 1<<10,  // FORCE_SERVER_EXEC_BROWSER_CLIENT 
      FuncNotSupportedBrowserClient = 1<<11, // FUNC_NOT_SUPPORTED_BROWSER_CLIENT
      ForceClientExecRichClient = 1<<12,     // FORCE_CLIENT_EXEC_RICH_CLIENT
      ForceServerExecRichClient = 1<<13,     // FORCE_SERVER_EXEC_RICH_CLIENT
      FuncNotSupportedRichClient = 1<<14,    // FuncNotSupportedRichClient
      FuncNotSupportedOnlineBatch = 1<<15,   // FUNC_NOT_SUPPORTED_ONLINE_BATCH
      ForceMixExecRichClient = 1<<16,        // FORCE_MIX_EXEC_RICH_CLIENT
      ForceUnknownExecRichClient = 1<<17     // FORCE_UNKNOWN_EXEC_RICH_CLIENT
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum UpdateMode
   {
      Incremental = 'I', // UPDATE_INCREMENTAL
      Normal = 'N' // v
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum BlockTypes
   {
      //TASKRULE_BLK = 'B',
      [DisplayString(ResourceKey = "BlockIf_s")]
      If = 'I',       // TASKRULE_BLK_IF
      [DisplayString(ResourceKey = "BlockElse_s")]
      Else = 'E',     // TASKRULE_BLK_ELSE
      [DisplayString(ResourceKey = "BlockEndBlock_s")]
      EndBlock = 'N', // TASKRULE_BLK_ENDBLK
      [DisplayString(ResourceKey = "BlockLoop_s")]
      Loop = 'L'      // TASKRULE_BLK_LOOP
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum FormOperationType
   {
      Input = 'I',       //TASKRULE_FORM_INPUT
      Output = 'O',      //TASKRULE_FORM_OUTPUT

   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum FormPage
   {
      Skip = 'S', //FORM_PAGE_SKIP
      Auto = 'A', //FORM_PAGE_AUTO
      Top = 'T'   //FORM_PAGE_TOP
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum FormDelimiter
   {
      Column = 'C', //FORM_DL_COLUMN
      Single = 'S', //FORM_DL_SINGLE
      Double = 'D'  //FORM_DL_DOUBLE
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum CallOperationMode
   {
      Program = 'P',    //Call_Program
      SubTask = 'T',    //Call_SubTask
      [DisplayString(ResourceKey = "CallByExp_s")]
      ByExp = 'E',       //Call_ByExp
      [DisplayString(ResourceKey = "CallByName_s")]
      ByName = 'B',      //Call_ByName
      Remote = 'R',      //Call_Remote

      Com = 'C',
      OsCommand = 'O',
      UDP = 'U',         // invoke UDP
      WebS = 'W',
      WebSLite = 'L',
      [DisplayString(ResourceKey = "Dotnet_s")]
      DotNet = '.'
   }


   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum RowType
   {
      Header = 1,
      Operation = 2
   }

   ///<summary>
   ///[DROR][USER_EVENTS]: additional enumerators for UserEvents
   ///<summary>
   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum ForceExit
   {
      None = 'N',       // FORCE_EXIT_NONE
      Control = 'C',    // FORCE_EXIT_CONTROL
      PreRecordUpdate = 'R',  // FORCE_EXIT_PRE_RECORD
      PostRecordUpdate = 'P', // FORCE_EXIT_POST_RECORD
      Ignore = 'I',     // FORCE_EXIT_IGNORE (like HANDLER_LEVEL_ALL)
      Editing = 'E'     // FORCE_EXIT_EDITING
   }

   ///<summary>
   ///[IDAN][IO_DEVICES]:additional enumerators for IO Devices
   ///</summary>
   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum MediaType
   {
      None = 'N', //added to ensure valid value
      GraphicalPrinter = 'G',
      Printer = 'P',
      Console = 'C',
      File = 'F',                //STORAGE_MEDIA_FILE
      Requester = 'R',
      XMLDirect = 'D',
      Variable = 'V'
   }

   ///<summary>
   ///Enumerators for Mobile Devices Operating System type
   ///</summary>
   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum OSType
   {
      Android = 'A',
      IOS = 'I'
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum APGMode
   {
      Execute = 'E',
      Generate = 'G',
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum APGOption
   {
      Browse = 'B',
      Export = 'E',
      Import = 'I',
      Print = 'P',
      Browser = 'R',
      RichClient = 'H',
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum APGDisplayMode
   {
      Line = 'L',
      Screen = 'S',
      None = 'N',
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum APGFormSize
   {
      AsModel = 'M',
      AsContent = 'C',
      AsContentWithinMDI = 'D',
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum APGType
   {
      Single = 'S',
      Multiple = 'M',
      Program = 'P',
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum APGInvokedFrom
   {
      TablesRepository = 1,
      ProgramsRepository = 2,
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum MgModelType
   {
      Model = 'M', //MG_MODEL_MODEL
      Var = 'V'    //MG_MODEL_VAR
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum CallbackType
   {
      ProgressBar = 0,
      Import = 1,
      FormEditor = 2,
      CollectionChanges = 3
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum Axis
   {
      X,
      Y
   }

   ///<summary>
   ///Model attribute
   ///</summary>
   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum ModelAttrHelp
   {
      Internal = 'A',
      Windows = 'B'
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum ModelAttrField
   {

      Alpha = 'A',
      Numeric = 'C',
      Unicode = 'B',
      Logical = 'D',
      Date = 'E',
      Time = 'F',
      Blob = 'G',
      [DisplayString(ResourceKey = "Ole_s")]
      OLE = 'H',
      [DisplayString(ResourceKey = "ActiveX_s")]
      ActiveX = 'I',
      Vector = 'J',
      [DisplayString(ResourceKey = "Dotnet_s")]
      DotNet = 'K'
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum ModelAttrGui0
   {

      Form = 'A',
      Edit = 'B',
      Static = 'C',
      Button = 'D',
      Check = 'E',
      Radio = 'F',
      Tab = 'G',
      List = 'H',
      Combo = 'I',
      Line = 'J',
      [DisplayString(ResourceKey = "CtrlGui0Slider_s")]
      Slider = 'K',
      Table = 'L',
      Column = 'M',
      Image = 'N',
      Ole = 'O',
      Redit = 'P',
      Tree = 'Q',
      [DisplayString(ResourceKey = "ActiveX_s")]
      Activex = 'R',
      Subform = 'S',
      Dotnet = 'T',
      Browser = 'O'
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum ModelAttrGui1
   {
      Form = 'A',
      Edit = 'B',
      Static = 'C',
      Line = 'D',
      Table = 'E',
      Column = 'F',
      Image = 'G',
      Redit = 'H'
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum ModelAttrText
   {
      Form = 'A',
      Edit = 'B',
      Static = 'C',
      Line = 'D',

   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum ModelAttrRichClient
   {

      Form = 'A',
      Edit = 'B',
      Label = 'C',
      Button = 'D',
      Check = 'E',
      Radio = 'F',
      Tab = 'G',
      List = 'H',
      Combo = 'I',
      Group = 'J',
      Table = 'K',
      Column = 'L',
      Image = 'M',
      Tree = 'P',
      Subform = 'N',
      Browser = 'O',
      Line = 'Q',
      RichEdit = 'S',
      RichText = 'T',
      Dotnet = 'U',
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum ModelAttrFramesetForm
   {
      Form = 'A',
      Frame = 'B'
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum ModelAttRichClientFrameSet
   {
      Form = 'A',
      Frame = 'B'
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum ModelAttrBrowser
   {
      Form = 'A',
      Edit = 'B',
      [DisplayString(ResourceKey = "Hypertext_s")]
      Static = 'C',
      Button = 'D',
      Check = 'E',
      Radio = 'F',
      List = 'G',
      Combo = 'H',
      Table = 'I',
      Image = 'J',
      Subform = 'K',
      [DisplayString(ResourceKey = "IFrame_s")]
      Iframe = 'L',
      Opaque = 'M',

   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum ModelAttGuiFrame
   {
      Form = 'A',
      Frame = 'B'
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum ModelAttMerge
   {
      Form = 'A',
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum DbhKeyMode
   {
      Unique = 'S',
      NonUnique = 'N'
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum DbhKeyDirection
   {
      OneWay = 'A',
      TwoWay = 'B'
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum DbhKeyRangeMode
   {
      Quick = 'Q',
      Full = 'F'
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum DbhKeyIndexType
   {
      Real = 'R',
      Virtual = 'V'
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum DbhSegmentDirection
   {
      Ascending = 'A',
      Descending = 'D'
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum ChoiceControlStyle
   {
      ListBox = 1, // CTRL_STYLE_LIST
      ComboBox,    // CTRL_STYLE_COMBO
      Tab,         // CTRL_STYLE_TAB
      RadioButton  // CTRL_STYLE_RB
   }

   public enum Recursion
   {
      None, First, Second, FirstOpen
   }

   public enum ViewSelectType
   {
      IncludeInView, ExcludeFromView
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum RangeMode
   {
      From = 'F',  // RANGE_FROM
      To = 'T',    // RANGE_TO
      Equal = 'E', // RANGE_EQUAL
   }

   public enum TableType
   {
      Table = 'T',
      View = 'V',
      Undefined = 'U'
   }

   public enum DatabaseDataType
   {
      XmlDataSource = 'X',       // DATA_SOURCE_IS_XML
      DatabaseDataSource = 'D'   // DATA_SOURCE_IS_DATABASE
   }

   public enum LogicHeaderAction
   {
      None,
      CreateVariableChangeParameters,
      DeleteVariableChangeParameters,
      ReplaceVariableChangeParameters,
      CreateEventParameters,
   }

   public enum DatabaseFilters
   {
      EnvironmentDatabaseAll = 'A',                      //ENV_DBS_ALL
      EnvironmentDatabaseSql = 'S',                      //ENV_DBS_SQL
      EnvironmentDatabaseCanLoadDefinition = 'D',        //ENV_DBS_DEF
      EnvironmentDatabaseIsam = 'I',                      // ENV_DBS_ISAM
      EnvironmentDatabaseXmlOnly = 'X',                  //ENV_DBS_XML_ONLY
      EnvironmentDatabaseOnly = 'O',                     //ENV_DBS_DBASE_ONLY
      EnvironmentDatabaseCanLoadDefinitionAndXml = 'G'   // Used for GetDefinitionWizard (combination of EnvironmentDatabaseCanLoadDefinition && EnvironmentDatabaseXmlOnly)
   }

   public enum FieldViewModelType
   {
      DataSource,                // Field from datas ource
      DataView,                  // Field from program's data view
      Logic                      // Field from program's logic tab e.g. event parameters
   }

   public enum SourceContextType
   {
      CurrentContext = 'C',
      MainContext = 'M'
   }

   public enum MagicSystemColor
   {
      ScrollBar = 1,                //COLOR_SCROLLBAR 
      Background = 2,               //COLOR_BACKGROUND
      ActiveCaption = 3,            //COLOR_ACTIVECAPTION
      InactiveCaption = 4,          //COLOR_INACTIVECAPTION
      Menu = 5,                     //COLOR_MENU
      Window = 6,                   //COLOR_WINDOW
      WindowFrame = 7,              //COLOR_WINDOWFRAME
      MenuText = 8,                 //COLOR_MENUTEXT
      WindowText = 9,               //COLOR_WINDOWTEXT
      CaptionText = 10,             //COLOR_CAPTIONTEXT
      ActiveBorder = 11,            //COLOR_ACTIVEBORDER
      InActiveBorder = 12,          //COLOR_INACTIVEBORDER
      AppWorkSpace = 13,            //COLOR_APPWORKSPACE
      Highlight = 14,               //COLOR_HIGHLIGHT
      HighlightText = 15,           //COLOR_HIGHLIGHTTEXT
      BtnFace = 16,                 //COLOR_BTNFACE
      BtnShadow = 17,               //COLOR_BTNSHADOW
      GrayText = 18,                //COLOR_GRAYTEXT
      BtnText = 19,                 //COLOR_BTNTEXT
      InActiveCaptionText = 20,     //COLOR_INACTIVECAPTIONTEXT
      BtnHighlight = 21,            //COLOR_BTNHIGHLIGHT
      ThreeDDarkShadow = 22,        //COLOR_3DDKSHADOW
      ThreeDLight = 23,             //COLOR_3DLIGHT
      InfoText = 24,                //COLOR_INFOTEXT
      Info = 25,                    //COLOR_INFOBK
      Undefined = 26                //UNDEFINED
   }

   public enum ViewRefreshMode
   {
      None = 0,
      CurrentLocation = 1,
      UseTaskLocate = 2,
      FirstRecord = 3
   }

   public enum LineManipulationType
   {
      None = 0,
      RepeatEntries = 1,
      MoveEntries  = 2,
      OverwriteCurrent = 3
   }

   /// <summary>
   /// Output Type for DataView contents.
   /// </summary>
   public enum DataViewOutputType
   {
      Xml = 'X',
      ClientFile = 'C'
   }

   /// <summary>
   /// Action type for undo redo operations
   /// </summary>
   public enum UndoRedoAction
   {
      Undo = 0,
      Redo = 1
   }

   /// <summary>
   /// 
   /// </summary>
   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum OrientationLock
   {
      No = 1,
      Portrait,
      Landscape
   }

   /// <summary>
   /// 
   /// </summary>
   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum EnterAnimation
   {
     Default = 1,
     Left,
     Right,
     Top,
     Bottom,
     Flip,
     Fade,
     None 
   }

   /// <summary>
   /// 
   /// </summary>
   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum ExitAnimation
   {
      Default = 1,
      Left,
      Right,
      Top,
      Bottom,
      Flip,
      Fade,
      None
   }

   /// <summary>
   /// 
   /// </summary>
   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum KeyboardTypes
   {
      Default = 1,
      Numeric,
      URL,
      NumberPad,
      PhonePad,
      NamePhonePad,
      Email
   }

   /// <summary>
   /// 
   /// </summary>
   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum KeyboardReturnKeys
   {
     Default = 1,
     Go,
     Next,
     Previous,
     Search,
     Done
   }

   /// <summary>
   /// 
   /// </summary>
   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum OpenEditDialog
   {
      Default = 1,
      Yes,
      No
   }

   public enum FrameLayoutTypes
   {
      TwoFramesHorizontal  = 7901,    // IDM_2HRC
      TwoFramesVertical    = 7902,    //IDM_2VRC
      ThreeFramesBottom    = 7903,    //IDM_3BRC
      ThreeFramesRight     = 7904,    //IDM_3RRC
      ThreeFramesTop       = 7905,    //IDM_3TRC
      ThreeFramesLeft      = 7906,    //IDM_3LRC
   }

   /// <summary>
   /// Enum which denotes the kind of Line
   /// </summary>
   public enum LineDirection
   {
      Horizontal = 0,
      Vertical = 1,
      NESW = 2,
      NWSE = 3,
   }

   [TypeConverter(typeof(LocalizationEnumConverter))]
   public enum ScrollBarThumbType
   {
      Incremental = 1,      
      Absolute
   }
}
