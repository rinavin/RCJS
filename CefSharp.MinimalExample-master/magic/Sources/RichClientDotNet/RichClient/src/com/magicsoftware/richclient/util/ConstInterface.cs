using System;
using com.magicsoftware.unipaas.util;
using util.com.magicsoftware.util;

namespace com.magicsoftware.richclient.util
{
   /// <summary> This interface is used to define some global constants. 
   /// Rules: 
   /// 1. Any class that need one or more of these constants must implement this interface 
   ///    or access the constant directly using the interface name as its prefix.
   /// 2. Please define the constants using UPPERCASE letters to make them stand out clearly.</summary>
   internal class ConstInterface
   {
      internal const String EXECUTION_PROPERTIES_FILE_NAME = "execution.properties";
      internal const string FORMS_USER_STATE_FILE_NAME = "FormsUserStateRIA";
      
      internal const String NAMES_SEPARATOR = "#+%";

      /// <summary> Tags and Attributes of the XML protocol</summary>
      internal const String MG_TAG_CONTEXT = "context";
      internal const String MG_TAG_PROTOCOL = "protocol";
      internal const String MG_TAG_ENV = "env";
      internal const String MG_TAG_COMMAND = "command";
      internal const String MG_TAG_LANGUAGE = "language";
      internal const String MG_TAG_LANGUAGE_END = "/language";
      internal const String MG_TAG_CONSTMESSAGES = "ConstMessages";
      internal const String MG_TAG_CONSTMESSAGES_END = "/ConstMessages";
      internal const String MG_TAG_CONSTMESSAGESCONTENT = "ConstLangFileContent";
      internal const String MG_TAG_CONSTMESSAGESCONTENT_END = "/ConstLangFileContent";
      internal const String MG_TAG_MLS_FILE_URL = "MlsFileUrl";
      internal const String MG_TAG_MLS_FILE_URL_END = "/MlsFileUrl";
      internal const String MG_TAG_MLS_CONTENT = "MlsContent";
      internal const String MG_TAG_MLS_CONTENT_END = "/MlsContent";      
      internal const String MG_TAG_COLORTABLE_URL = "ColorTblurl";
      internal const String MG_TAG_FONTTABLE = "fonttable";
      internal const String MG_TAG_FONTTABLE_URL = "fontTblurl";
      internal const String MG_TAG_COMPMAINPRG = "compmainprg";
      internal const String MG_TAG_STARTUP_PROGRAM = "StartupProgram";
      internal const String MG_TAG_CONTEXT_ID = "ContextID";
      internal const String MG_TAG_HTTP_COMMUNICATION_TIMEOUT = "HttpTimeout"; // communication-level timeout (i.e. the access to the web server, NOT the entire request/response round-trip), in seconds.
      internal const String MG_TAG_UNSYNCRONIZED_METADATA = "UnsynchronizedMetadata";

      // task definition ids
      //---------------------------------------------------------------
      internal const String MG_TAG_TASKDEFINITION_IDS_URL = "taskDefinitionIdsUrl";
      internal const String MG_TAG_OFFLINE_SNIPPETS_URL = "offlineSnippetsUrl";
      internal const String MG_TAG_DEFAULT_TAG_LIST = "defaultTagList";
      internal const String MG_TAG_TASK_INFO = "taskinfo";
      internal const String MG_TAG_EXECUTION_RIGHT = "executionRightIdx";
      
      //---------------------------------------------------------------
            
      internal const String MG_TAG_ASSEMBLIES = "assemblies";
      internal const String MG_TAG_ASSEMBLY = "assembly";
      internal const String MG_ATTR_ISSPECIFIC = "isSpecific";
      internal const String MG_ATTR_IS_GUI_THREAD_EXECUTION = "isGuiThreadExecution";
      internal const String MG_ATTR_ASSEMBLY_PATH = "path";
      internal const String MG_ATTR_ASSEMBLY_CONTENT = "Content";
      internal const String MG_ATTR_FULLNAME = "fullname";
      internal const String MG_ATTR_PUBLIC_NAME = "publicName";
      internal const String MG_TAG_TASKURL = "taskURL";
      internal const String MG_TAG_TASK_IS_OFFLINE = "IsOffline";      
      internal const String MG_TAG_EXPTABLE = "exptable";
      internal const String MG_TAG_EXP = "exp";
      internal const String MG_TAG_DATAVIEW = "dataview";
      internal const String MG_TAG_COMPLETE_DV = "CompleteDataView";
      internal const String MG_TAG_REC = "rec";      
      internal const String MG_TAG_FLD_END = "/fld";
      internal const String MG_TAG_HANDLER = "handler";
      internal const String MG_TAG_EVENTHANDLERS = "eventhandlers";
      internal const String MG_TAG_EVENT = "event";
      internal const String MG_TAG_OPER = "oper";
      internal const String MG_TAG_KBDMAP_URL = "kbdmapurl";
      internal const String MG_TAG_KBDMAP = "kbdmap";
      internal const String MG_TAG_KBDITM = "kbditm";
      internal const String MG_TAG_USER_EVENTS = "userevents";
      internal const String MG_TAG_USER_EVENTS_END = "/userevents";
      internal const String MG_TAG_EVENTS_QUEUE = "eventsqueue";
      internal const String MG_TAG_DC_VALS = "dc_vals";
      internal const String MG_TAG_FLWMTR_CONFIG = "flwmtr_config";
      internal const String MG_TAG_FLWMTR_MSG = "flwmtr_msg";
      internal const String MG_TAG_FLWMTR_ACT = "act";
      internal const String MG_TAG_DEL_LIST = "cacheDelList";
      internal const String MG_TAG_LINKS = "links";
      internal const String MG_TAG_LINKS_END = "/links";
      internal const String MG_TAG_LINK = "link";
      internal const String MG_TAG_TASK_TABLES = "taskTables";
      internal const String MG_TAG_TASK_TABLES_END = "/taskTables";
      internal const String MG_TAG_TASK_TABLE = "taskTable";
      internal const String MG_TAG_COLUMN = "column";
      internal const String MG_TAG_SORT = "sort";
      internal const String MG_TAG_SORTS = "sorts";
      internal const String MG_TAG_SORTS_END = "/sorts";
      internal const String MG_TAG_CACHED_TABLE = "cachedTable";
      internal const String MG_TAG_CACHED_TABLE_END = "/cachedTable";
      internal const String MG_TAG_RECORDS = "records";
      internal const String MG_TAG_RECORDS_END = "/records";
      internal const String MG_TAG_TASK_XML = "taskxml";
      internal const String MG_TAG_EXEC_STACK_ENTRY = "execstackentry";
      internal const String MG_TAG_USER_RIGHTS = "userRights";
      internal const String MG_TAG_DBH_REAL_IDXS = "dbhRealIdxs";
      internal const String MG_TAG_USER_DETAILS = "userDetails";
      internal const String MG_SSL_STORE_ARG_PREFIX = "STORE:";      

      internal const String MG_ATTR_VALUE_TAGGED = XMLConstants.TAG_OPEN + XMLConstants.MG_ATTR_VALUE + XMLConstants.TAG_CLOSE;
      internal const String MG_ATTR_VALUE_END_TAGGED = XMLConstants.END_TAG + XMLConstants.MG_ATTR_VALUE + XMLConstants.TAG_CLOSE;            
      internal const String MG_ATTR_HANDLER = "handler";
      internal const String MG_ATTR_OBJECT = "object";
      internal const String MG_ATTR_RECOMPUTE = "recompute";
      internal const String MG_ATTR_MESSAGE = "message";
      internal const String MG_ATTR_HTML = "html";
      internal const String MG_ATTR_ARGLIST = "arglist";      
      internal const String MG_ATTR_EXTRA = "extra";
      internal const String MG_ATTR_FIRSTPOS = "firstpos";
      internal const String MG_ATTR_LASTPOS = "lastpos";
      internal const String MG_ATTR_VIRTUAL = "virtual";
      internal const String MG_ATTR_SUBFORM_TASK = "subform_task";
      internal const String MG_ATTR_PARAM = "param";
      internal const String MG_ATTR_PARAMS = "params";
      internal const String MG_ATTR_PAR_ATTRS = "attr";
      internal const String MG_ATTR_PAR_ATTRS_TAGGED = XMLConstants.TAG_OPEN + MG_ATTR_PAR_ATTRS + XMLConstants.TAG_CLOSE;
      internal const String MG_ATTR_PAR_ATTRS_END_TAGGED = XMLConstants.END_TAG + MG_ATTR_PAR_ATTRS + XMLConstants.TAG_CLOSE;
      internal const String MG_ATTR_VIR_AS_REAL = "vir_as_real";
      internal const String MG_ATTR_LNK_CREATE = "link_create";
      internal const String MG_ATTR_LENGTH = "length";
      internal const String MG_ATTR_RANGE = "range";
      internal const String MG_ATTR_NULLVALUE = "nullvalue";
      internal const String MG_ATTR_INIT = "init";
      internal const String MG_ATTR_CURRPOS = "currpos";
      internal const String MG_ATTR_POS = "pos";
      internal const String MG_ATTR_OLDPOS = "oldpos";
      internal const String MG_ATTR_MODE = "mode";
      internal const String MG_ATTR_RECOMPUTEBY = "recompute_by";
      internal const String MG_ATTR_NAME_TAGGED = XMLConstants.TAG_OPEN + XMLConstants.MG_ATTR_NAME + XMLConstants.TAG_CLOSE;
      internal const String MG_ATTR_NAME_END_TAGGED = XMLConstants.END_TAG + XMLConstants.MG_ATTR_NAME + XMLConstants.TAG_CLOSE;
      internal const String MG_ATTR_TRANS = "trans";
      internal const String MG_ATTR_HANDLEDBY = "handledby";
      internal const String MG_ATTR_LEVEL = "level";
      internal const String MG_ATTR_GENERATION = "generation";
      internal const String MG_ATTR_TASKVARLIST = "taskVarList";
      internal const String MG_ATTR_OUTPUTTYPE = "outputType";
      internal const String MG_ATTR_LIST_OF_INDEXES_OF_SELECTED_DESTINATION_FIELDS = "listOfIndexesOfSelectedDestinationFields";
      internal const String MG_ATTR_DESTINATION_DATASOURCE = "destDataSource";
      internal const String MG_ATTR_DESTINATION_DATASOURCE_NAME = "destDataSourceName";
      internal const String MG_ATTR_DESTINATION_COLUMNLIST = "destColumnList";
      internal const String MG_ATTR_SOURCE_DATAVIEW = "sourceDataView";
      internal const String MG_ATTR_SECONDS = "seconds";
      internal const String MG_ATTR_SCOPE = "scope";
      internal const String MG_ATTR_RETURN = "return";
      internal const String MG_ATTR_PROPAGATE = "propagate";
      internal const String MG_ATTR_ENABLED = "enabled";
      internal const String MG_ATTR_FLD = "fld";
      internal const String MG_ATTR_FIELDID = "fieldid";
      internal const String MG_ATTR_IGNORE_SUBFORM_RECOMPUTE = "ignoreSubformRecompute";
      internal const String MG_ATTR_MAGICEVENT = "magicevent";
      internal const String MG_ATTR_EXIT_BY_MENU = "exitByMenu";
      internal const String MG_ATTR_FOCUSLIST = "focuslist";
      internal const String MG_ATTR_FOCUSTASK = "currFocusedTask";
      internal const String MG_ATTR_TEXT = "text";
      internal const String MG_ATTR_TITLE = "title";
      internal const String MG_ATTR_TITLE_EXP = "titleExp";
      internal const String MG_ATTR_IMAGE = "image";
      internal const String MG_ATTR_BUTTONS = "buttons";
      internal const String MG_ATTR_DEFAULT_BUTTON = "defaultButton";
      internal const String MG_ATTR_DEFAULT_BUTTON_EXP = "defaultButtonExp";
      internal const String MG_ATTR_RETURN_VAL = "returnVal";
      internal const String MG_ATTR_ERR_LOG_APPEND = "errorLogAppend";
      internal const String MG_ATTR_EVENTVALUE = "eventvalue";
      internal const String MG_ATTR_EVENTTYPE = "eventtype";
      internal const String MG_ATTR_DOTNET_NAME = "dn_name";
      internal const String MG_ATTR_DOTNET_VEE = "vee";
      internal const String MG_ATTR_HOW = "how";
      internal const String MG_ATTR_UNDO = "undo";
      internal const String MG_ATTR_CND = "cnd";
      internal const String MG_ATTR_ACTIONID = "actionid";
      internal const String MG_ATTR_KEYCODE = "keycode";
      internal const String MG_ATTR_MODIFIER = "modifier";
      internal const String MG_ATTR_STATES = "states";
      internal const String MG_ATTR_CURR_REC = "curr_rec";
      internal const String MG_ATTR_LAST_REC_ID = "lastRecIdx";
      internal const String MG_ATTR_OLD_FIRST_REC_ID = "oldFirstRecIdx";
      internal const String MG_ATTR_CURR_REC_POS_IN_FORM = "currRecPosInForm";
      internal const String MG_ATTR_REPEATABLE = "repeatable";
      internal const String MG_ATTR_FIELD = "field";
      internal const String MG_ATTR_IDX = "idx";
      internal const String MG_ATTR_URL = "url";
      internal const String MG_ATTR_DATEMODE = "datemode";
      internal const String MG_ATTR_THOUSANDS = "thousands";
      internal const String MG_ATTR_DECIMAL_SEPARATOR = "decimalseparator";
      internal const String MG_ATTR_DATE = "date";
      internal const String MG_ATTR_TIME = "time";
      internal const String MG_ATTR_LANGUAGE = "language";
      internal const String MG_ATTR_MODAL = "modal";
      internal const String MG_ATTR_TASK = "task";
      internal const String MG_ATTR_INTERNALEVENT = "internalevent";
      internal const String MG_ATTR_VARIABLE = "variable";
      internal const String MG_ATTR_HANDLERID = "handlerid";
      internal const String MG_ATTR_ROLLBACK_TYPE = "rollbackType";
      
      internal const String MG_ATTR_CENTURY = "century";
      internal const String MG_ATTR_DATATYPE = "datatype";
      internal const String MG_ATTR_INCLUDE_FIRST = "include_first";
      internal const String MG_ATTR_INCLUDE_LAST = "include_last";
      internal const String MG_ATTR_IS_ONEWAY_KEY = "isonewaykey";
      internal const String MG_ATTR_INSERT_AT = "insert_at";
      internal const String MG_ATTR_DBVIEWSIZE = "dbviewsize";
      internal const String MG_ATTR_FRAME_EXP = "frameexp";
      internal const String MG_ATTR_PICLEN = "piclen";
      internal const String MG_ATTR_SRCTASK = "srctask";
      internal const String MG_ATTR_INVALIDATE = "invalidate";
      internal const String MG_ATTR_RMPOS = "rmpos";
      internal const String MG_ATTR_DISPLAY = "display";
      internal const String MG_ATTR_SUBTYPE = "subtype";
      internal const String MG_ATTR_ADD_AFTER = "add_after";
      internal const String MG_ATTR_TOP_REC_ID = "top_rec_id";
      internal const String MG_ATTR_LOW_ID = "low_id";
      internal const String MG_ATTR_OWNER = "owner";
      internal const String MG_ATTR_UPD_IN_QUERY = "updateInQuery";
      internal const String MG_ATTR_UPD_IN_QUERY_LOWER = "updateinquery";
      internal const String MG_ATTR_CRE_IN_MODIFY = "createInModify";
      internal const String MG_ATTR_CRE_IN_MODIFY_LOWER = "createinmodify";
      internal const String MG_ATTR_IDLETIME = "idletime";
      internal const String MG_ATTR_VER = "ver";
      internal const String MG_ATTR_SUBVER = "subver";
      internal const String MG_ATTR_WAIT = "wait";
      internal const String MG_ATTR_RETAIN_FOCUS = "retainFocus";
      internal const String MG_ATTR_SYNC_DATA = "syncData";
      internal const String MG_ATTR_SHOW = "show";
      internal const String MG_ATTR_COMPONENT = "component";
      internal const String MG_ATTR_PATH_PARENT_TASK = "path_parent_task";
      internal const String MG_ATTR_USER = "user";
      internal const String MG_ATTR_FORCE_EXIT = "force_exit";
      internal const String MG_ATTR_PUBLIC = "public_name";
      internal const String MG_ATTR_PRG_DESCRIPTION = "prgDescription";
      //internal const String MG_ATTR_ALLOW_EVENTS = "AllowEvents";
      internal const String MG_ATTR_MENU_CONTENTEND = "MenusContentEnd";
      internal const String MG_ATTR_OBJECT_TYPE = "object_type";
      internal const String MG_ATTR_TABLE_NAME = "table_name";
      internal const String MG_ATTR_INDEX_IN_TABLE = "index_in_table";
      internal const String MG_ATTR_CALLINGTASK = "callingtask";
      internal const String MG_ATTR_FLAGS = "flags";
      internal const String MG_ATTR_LNKEXP = "link_exp";
      internal const String MG_ATTR_MODIFIED = "modi";
      internal const String MG_ATTR_OPER = "oper";
      internal const String MG_ATTR_REFRESHON = "refreshon";
      internal const String MG_ATTR_TASKMODE = "taskmode";
      internal const String MG_ATTR_EMPTY_DATAVIEW = "EmptyDataview";
      internal const String MG_ATTR_TASKLEVEL = "tasklevel";
      internal const String MG_ATTR_TRANS_CLEARED = "trans_cleared";
      internal const String MG_ATTR_TRANS_STAT = "trans_stat";
      internal const String MG_ATTR_SUBFORM_VISIBLE = "visible";
      internal const String MG_ATTR_ACK = "ack";
      internal const String MG_ATTR_COMPUTE_BY = "compute_by";
      internal const String MG_ATTR_CHUNK_SIZE = "chunk_size";
      internal const String MG_ATTR_CHUNK_SIZE_EXPRESSION = "chunk_size_expression";
      internal const String MG_ATTR_HAS_MAIN_TBL = "has_main_tbl";
      internal const String MG_ATTR_SERVER_ID = "serverid";
      internal const String MG_ATTR_SUBFORM_CTRL = "subformCtrl";
      internal const String MG_ATTR_CHECK_BY_SERVER = "checkByServer";
      internal const String MG_ATTR_OPER_IDX = "operidx";
      internal const String MG_ATTR_EXP_IDX = "expidx";
      internal const String MG_ATTR_EXP_TYPE = "exptype";
      internal const String MG_ATTR_NULL = "null";
      internal const String MG_ATTR_NULLS = "nulls";            
      internal const String MG_ATTR_DC_REFS = "dc_refs";
      internal const String MG_ATTR_LINKED = "linked";
      internal const String MG_ATTR_DISP = "disp";
      internal const String MG_ATTR_REMOVE = "remove";
      internal const String MG_ATTR_DVPOS = "dvpos";
      internal const String MG_ATTR_SIGNIFICANT_NUM_SIZE = "significant_num_size";
      internal const String MG_ATTR_DEBUG_MODE = "debug";
      internal const String MG_ATTR_POINT_TRANSLATION = "changePoint";
      internal const String MG_ATTR_SPECIAL_EXITCTRL = "specialExitCtrl";
      internal const String MG_ATTR_USE_WINDOWS_XP_THEAMS = "UseWindowsXPThemes";
      internal const String MG_ATTR_DEFAULT_COLOR = "defaultColor";
      internal const String MG_ATTR_DEFAULT_FOCUS_COLOR = "defaultFocusColor";
      internal const String MG_ATTR_CONTEXT_INACTIVITY_TIMEOUT = "ContextInactivityTimeout";
      internal const String MG_ATTR_CONTEXT_INACTIVITY_TIMEOUT_LOWER = "contextinactivitytimeout";
      internal const String MG_ATTR_CONTEXT_UNLOAD_TIMEOUT = "ContextUnloadTimeout";
      internal const String MG_ATTR_CONTEXT_UNLOAD_TIMEOUT_LOWER = "contextunloadtimeout";
      internal const String MG_ATTR_TOOLTIP_TIMEOUT = "TooltipTimeout";
      internal const String MG_ATTR_LOWHIGH = "lowhigh";
      internal const String MG_ATTR_ACCESS_TEST = "AccessTest";
      internal const String MG_ATTR_SPECIAL_TEXT_SIZE_FACTORING = "SpecialTextSizeFactoring";
      internal const String MG_ATTR_SPECIAL_FLAT_EDIT_ON_CLASSIC_THEME = "SpecialFlatEditOnClassicTheme";

      internal const String MG_ATTR_ENCODING = "encoding";
      internal const String MG_ATTR_SYSTEM = "system";
      internal const String MG_ATTR_RECOVERY = "recovery";
      internal const String MG_ATTR_REVERSIBLE = "reversible";
      internal const String MG_ATTR_OLDID = "oldid";
      internal const String MG_ATTR_NEWID = "newid";
      internal const String MG_ATTR_MPRG_SOURCE = "mprg_source";
      internal const String MG_ATTR_INVOKER_ID = "invokerid";
      internal const String MG_ATTR_TRANS_LEVEL = "level";
      internal const String MG_ATTR_TOP = "top";
      internal const String MG_ATTR_LEFT = "left";
      internal const String MG_ATTR_HEIGHT = "height";
      internal const String MG_ATTR_WIDTH = "width";
      internal const String MG_ATTR_LEN = "len";
      internal const String MG_ATTR_SCAN = "scan";
      internal const String MG_ATTR_ENCRYPT = "encrypt";
      internal const String MG_ATTR_STANDALONE = "standalone";
      internal const String MG_ATTR_DESC = "desc"; // description
      internal const String MG_ATTR_MNUUID = "mnuuid";
      internal const String MG_ATTR_MNUCOMP = "mnucomp";
      internal const String MG_ATTR_MNUPATH = "mnupath";
      internal const String MG_ATTR_CLOSE_SUBFORM_ONLY = "closeSubformOnly";
      internal const String MG_ATTR_PATH = "treePath";
      internal const String MG_ATTR_VALUES = "treeValues";
      internal const String MG_ATTR_TREE_IS_NULLS = "treeIsNulls";
      internal const String MG_ATTR_CHECK_ONLY = "checkOnly";
      internal const String MG_ATTR_EXEC_ON_SERVER = "execOnServer";
      internal const String MG_ATTR_USER_SORT = "userSort";
      internal const String MG_TAG_USERNAME = "userName";
      internal const String MG_TAG_PASSWORD = "password";
      internal const String MG_ATTR_USERID = "userId";
      internal const String MG_ATTR_USERINFO = "userInfo";
      internal const String MG_ATTR_CPY_GLB_PRMS = "copy_global_params";
      internal const String MG_TAG_GLOBALPARAMS = "globalparams";
      internal const String MG_TAG_GLOBALPARAMSCHANGES = "globalParamChanges";
      internal const String MG_TAG_CACHED_FILE = "cachedFile";
      internal const String MG_ATTR_FILE_PATH = "filepath";
      internal const String MG_ATTR_CACHE_URL = "cacheUrl";
      internal const String MG_TAG_GLOBALPARAMSCHANGES_TAGGED = XMLConstants.TAG_OPEN + MG_TAG_GLOBALPARAMSCHANGES + XMLConstants.TAG_CLOSE;
      internal const String MG_TAG_GLOBALPARAMSCHANGES_END_TAGGED = XMLConstants.END_TAG + MG_TAG_GLOBALPARAMSCHANGES + XMLConstants.TAG_CLOSE;
      internal const String MG_TAG_PARAM = "param";
      internal const String MG_TAG_PARAM_TAGGED = XMLConstants.TAG_OPEN + MG_TAG_PARAM + XMLConstants.TAG_CLOSE;
      internal const String MG_TAG_PARAM_END_TAGGED = XMLConstants.END_TAG + MG_TAG_PARAM + XMLConstants.TAG_CLOSE;
      internal const String MG_ATTR_IME_AUTO_OFF = "imeAutoOff";
      internal const String MG_ATTR_LOCAL_AS400SET = "local_as400set";
      internal const String MG_ATTR_LOCAL_EXTRA_GENGO = "local_extraGengo";
      internal const String MG_ATTR_LOCAL_FLAGS = "local_flags";
      internal const String MG_ATTR_SPEACIAL_ANSI_EXP = "SpecialAnsiExpression";
      internal const String MG_ATTR_SPECIAL_SHOW_STATUSBAR_PANES = "SpecialShowStatusBarPanes";
      internal const String MG_ATTR_SPECIAL_SPECIAL_EDIT_LEFT_ALIGN = "SpecialEditLeftAlign"; 
      internal const String MG_ATTR_SPECIAL_IGNORE_BUTTON_FORMAT = "SpecialIgnoreButtonFormat";
      internal const String MG_ATTR_SPEACIAL_SWF_CONTROL_NAME = "SpecialSwfControlNameProperty";
      internal const String MG_ATTR_FRAMESET_STYLE = "frameSetStyle";
      internal const String MG_ATTR_OPER_DIRECTION = "operDir";
      internal const String MG_ATTR_OPER_FLOWMODE = "operFlowMode";
      internal const String MG_ATTR_OPER_CALLMODE = "operCallMode";
      internal const String MG_ATTR_TASK_FLOW_DIRECTION = "taskFlowDir";
      internal const String MG_ATTR_TASK_MAINLEVEL = "mainlevel";
      internal const String MG_ATTR_OPER_METHODNAME = "method_name";
      internal const String MG_ATTR_OPER_ASMURL = "asm_url";
      internal const String MG_ATTR_OPER_ASMFILE = "asm_file";
      internal const String MG_ATTR_OPER_ASMNAME = "asm_name";
      internal const String MG_ATTR_OPER_ASMCONTENT = "asm_content";
      internal const String MG_ATTR_GUID = "GUID";
      internal const String MG_ATTR_CONTROLS_PERSISTENCY_PATH = "ControlsPersistencyPath";
      internal const String MG_ATTR_PROJDIR = "projdir";
      internal const String MG_ATTR_TERMINAL = "terminal";
      internal const String MG_TAG_ENV_PARAM_URL = "envparamurl";
      internal const String MG_TAG_ENV_PARAM = "environment";      
      internal const String MG_ATTR_CLOSE_TASKS_ON_PARENT_ACTIVATE = "closeTasksOnParentActivate";
      internal const String MG_ATTR_HANDLER_ONFORM = "HandlerOnForm";
      internal const String MG_ATTR_DROP_USERFORMATS = "dropuserformats";
      internal const String MG_ATTR_CACHED_FILES = "cachedFiles"; // seems unused. can be removed?
      internal const String MG_ATTR_IS_ACCESSING_SERVER_USING_HTTPS = "isAccessingServerUsingHTTPS";
      internal const String MG_ATTR_SHOULD_PRE_PROCESS = "shouldPreProcess";
      internal const String MG_TAG_CACHE_FILE_INFO = "cacheFileInfo";
      internal const String MG_ATTR_SERVER_PATH = "serverPath";
      internal const String MG_ATTR_TIMESTAMP = "timeStamp";
      internal const String MG_TAG_CACHED_FILES = "cachedFiles";
      internal const String MG_TAG_CACHED_FILES_END = "/cachedFiles";
      internal const String MG_ATTR_CONTROL_NAME = "ControlName";
      internal const String MG_TAG_HIDDEN_CONTOLS = "HiddenControls";
      internal const String MG_ATTR_ISNS = "Isns";
      
      // for env var table
      internal const String MG_ATTR_TEMP_DATABASE = "tempdatabase";
      internal const String MG_ATTR_TIME_SEPARATOR = "timeseparator";
      internal const String MG_ATTR_IOTIMING = "iotiming";
      internal const String MG_ATTR_HTTP_TIMEOUT = "httptimeout";
      internal const String MG_ATTR_USE_SIGNED_BROWSER_CLIENT = "usesignedbrowserclient";
      internal const String MG_ATTR_CLOSE_PRINTED_TABLES_IN_SUBTASKS = "closeprintedtablesinsubtasks";
      internal const String MG_ATTR_GENERIC_TEXT_PRINTING = "generictextprinting";
      internal const String MG_ATTR_HIGH_RESOLUTION_PRINT = "highresolutionprint";
      internal const String MG_ATTR_MERGE_TRIM = "mergetrim";
      internal const String MG_ATTR_ORIGINAL_IMAGE_LOAD = "originalimageload";
      internal const String MG_ATTR_PRINT_DATA_TRIM = "printdatatrim";
      internal const String MG_ATTR_PSCRIPT_PRINT_NT = "pscriptprintnt";
      internal const String MG_ATTR_THOUSAND_SEPARATOR = "thousandseparator";
      internal const String MG_ATTR_RTF_BUFFER_SIZE = "rtfbuffersize";
      internal const String MG_ATTR_SPECIAL_CONV_ADD_SLASH = "specialconvaddslash";
      internal const String MG_ATTR_SPECIAL_FULL_EXPAND_PRINT = "specialfullexpandprint";
      internal const String MG_ATTR_SPECIAL_FULL_TEXT = "specialfulltext";
      internal const String MG_ATTR_SPECIAL_LAST_LINE_PRINT = "speciallastlineprint";
      internal const String MG_ATTR_SPECIAL_PRINTER_OEM = "specialprinteroem";
      internal const String MG_ATTR_RANGE_POP_TIME = "rangepoptime";
      internal const String MG_ATTR_TEMP_POP_TIME = "temppoptime";
      internal const String MG_ATTR_EMBED_FONTS = "embedfonts";
      internal const String MG_ATTR_BATCH_PAINT_TIME = "batchpainttime";
      internal const String MG_ATTR_ISAM_TRANSACTION = "isamtransaction";
      internal const String MG_ATTR_CENTER_SCREEN_IN_ONLINE = "centerscreeninonline";
      internal const String MG_ATTR_REPOSITION_AFTER_MODIFY = "repositionaftermodify";
      // for user event
      internal const String MG_ATTR_OVERLAY = "overlayid";
      internal const String MG_ATTR_TASKFLW = "taskflw";
      internal const String MG_ATTR_DATAVIEW = "dataview";
      internal const String MG_ATTR_RECOMP = "recomp";
      internal const String MG_ATTR_FLWOP = "flwop";
      internal const String MG_ATTR_START = "start";
      internal const String MG_ATTR_END = "end";
      internal const String MG_ATTR_INFO = "info";           
      
      internal const String MG_ATTR_KEY = "key";
      internal const String MG_ATTR_KEY_EXP = "key_exp"; 
      internal const String MG_ATTR_NULL_FLAGS = "nullFlags";
      internal const String MG_ATTR_NEXTOPERIDX = "nextOperIdx";
      internal const String MG_ATTR_DVPOS_VALUE = "dvPosValue";
      internal const String MG_ATTR_DVPOS_DEC = "DvPosDec";
      internal const String MG_ATTR_TRANS_ID = "transId";
      internal const String MG_ATTR_REALREFRESH = "realRefresh";
      internal const String MG_ATTR_SUB_FORM_RCMP = "sub_form_recomp";
      internal const String MG_ATTR_HAS_LINK_RECOMPUTES = "has_link_recomputes";
      internal const String MG_ATTR_LOCATE_FIRST_ID = "locateFirstRec";
      internal const String MG_ATTR_OFFSET = "offset";
      internal const String MG_ATTR_HAS_LOCATE = "hasLocate";
      internal const String MG_ATTR_AS_PARENT = "AsParent";
      internal const String MG_ATTR_TASK_UNIQUE_SORT = "TaskUniqueSort";
      internal const String MG_ATTR_TRANS_OWNER = "newTransOwner";
      internal const String MG_ATTR_CLOSE = "close";
      internal const String MG_ATTR_TASK_COUNTER = "task_counter";
      internal const String MG_ATTR_LOOP_COUNTER = "loop_counter";
      internal const String MG_ATTR_CHACHED_FLD_ID = "cachedFldId";
      internal const String MG_ATTR_LOCATE = "locate";
      internal const String MG_ATTR_LINK = "link";
      internal const String MG_ATTR_IS_LINK_FIELD = "isLinkFld";
      internal const String MG_ATTR_CACHED_TABLE = "cached_table";
      internal const String MG_ATTR_TABLE_INDEX = "tableIndex";
      internal const String MG_ATTR_LINK_EVAL_CONDITION = "evalCondition";
      internal const String MG_ATTR_LINK_MODE = "linkmode";
      internal const String MG_ATTR_LINK_START = "linkBeginAfterField";
      internal const String MG_ATTR_IDENT = "tid";
      internal const String MG_ATTR_DIR = "dir";
      internal const String MG_ATTR_COND = "cndExp";
      internal const String MG_ATTR_COND_RES = "tskCndRes";
      internal const String MG_ATTR_RET_VAL = "retVal";
      internal const String MG_ATTR_DBPOS = "db_pos";      
      internal const String MG_TAG_USR_DEF_FUC_RET_EXP_ID = "returnexp";
      internal const String MG_TAG_USR_DEF_FUC_RET_EXP_ATTR = "returnexpattr";
      internal const String MG_TAG_PROPERTIES = "properties";
      internal const String MG_TAG_PROPERTY = "property";
      internal const String MG_ATTR_IS_VECTOR = "is_vector";
      internal const String MG_ATTR_DIRECTION = "direction";
      internal const String MG_ATTR_KEEP_USER_SORT = "keepUserSort";
      internal const String MG_ATTR_SEARCH_STR = "searchStr";
      internal const String MG_ATTR_RESET_SEARCH = "resetSearch";
      // for DBH
      internal const String MG_TAG_DBHS = "DBHS";
      internal const String MG_TAG_DBHS_END = "/DBHS";
      internal const String MG_TAG_DBH_DATA_IDS_URL = "dbhDataIdsUrl";
      internal const String MG_TAG_DBH_DATA_ID = "dbhDataId";
      internal const String MG_TAG_DBH = "DBH";
      internal const String MG_TAG_DBH_END = "/DBH";
      internal const String MG_TAG_FLDS = "FLDS";
      internal const String MG_TAG_FLDS_END = "/FLDS";
      internal const String MG_TAG_FLD = "FLD";
      internal const String MG_TAG_KEYS = "KEYS";
      internal const String MG_TAG_KEYS_END = "/KEYS";
      internal const String MG_TAG_KEY = "KEY";
      internal const String MG_TAG_KEY_END= "/KEY";
      internal const String MG_TAG_SEGS = "SEGS";
      internal const String MG_TAG_SEGS_END = "/SEGS";
      internal const String MG_TAG_SEG = "SEG";
      internal const String MG_ATTR_ISN="isn";
      internal const String MG_ATTR_ATTR="Attr";
      internal const String MG_ATTR_ALLOW_NULL="AllowNull";
      internal const String MG_ATTR_DEFAULT_NULL="DefaultNull";
      internal const String MG_ATTR_STORAGE="Storage";
      internal const String MG_ATTR_DATASOURCE_DEFINITION="DataSourceDefinition";
      internal const String MG_ATTR_DIFF_UPDATE="DiffUpdate";
      internal const String MG_ATTR_DEC="Dec";
      internal const String MG_ATTR_WHOLE="Whole";
      internal const String MG_ATTR_PART_OF_DATETIME="PartOfDateTime";
      internal const String MG_ATTR_DEFAULT_STORAGE="DefaultStorage";
      internal const String MG_ATTR_CONTENT="Content";
      internal const String MG_ATTR_PICTURE="Picture";
      internal const String MG_ATTR_DB_DEFAULT_VALUE="DbDefaultValue";
      internal const String MG_ATTR_FLD_DB_INFO="DbInfo";
      internal const String MG_ATTR_DB_NAME="DbName";
      internal const String MG_ATTR_DB_TYPE="DbType";
      internal const String MG_ATTR_USER_TYPE="UserType";
      internal const String MG_ATTR_NULL_DISPLAY = "NullDisplay";
      internal const String MG_ATTR_FIELD_NAME = "Name";
      internal const String MG_ATTR_FLD_ISN= "fld_isn";
      internal const String MG_TAG_KEY_SEGMENTS = "KeySegs";
      internal const String MG_TAG_SEGMENT = "segment";
      internal const String MG_ATTR_KEY_DB_NAME = "KeyDBName";
      internal const String MG_ATTR_NAME = "name";
      internal const String MG_ATTR_DBASE_NAME = "dbase_name";
      internal const String MG_ATTR_POSITION_ISN = "position_isn";
      internal const String MG_ATTR_ARRAY_SIZE = "array_size";
      internal const String MG_ATTR_ROW_IDENTIFIER = "row_identifier";
      internal const String MG_ATTR_CHECK_EXIST = "check_exist";
      internal const String MG_ATTR_DEL_UPD_MODE = "del_upd_mode";
      internal const String MG_ATTR_DBH_DATA_URL = "dbhDataURL";
      internal const String MG_ATTR_FIELD_INDEX = "fieldindex";
      internal const String MG_ATTR_MIN_VALUE = "min";
      internal const String MG_ATTR_MAX_VALUE = "max";
      internal const String MG_ATTR_NULL_MIN_VALUE = "null_min";
      internal const String MG_ATTR_NULL_MAX_VALUE = "null_max";

      //for task table
      internal const String MG_ATTR_TASK_TABLE_NAME_EXP = "tableNameExp";
      internal const String MG_ATTR_TASK_TABLE_ACCESS = "access";
      internal const String MG_ATTR_TASK_TABLE_IDENTIFIER = "dataSourceIdentifier";

      // for databases
      internal const String MG_TAG_DATABASES_HEADER = "Databases";
      internal const String MG_TAG_DATABASES_END = "/Databases";
      internal const String MG_TAG_DATABASE_INFO = "database";
      internal const String MG_TAG_DATABASE_URL = "databaseUrl";
      internal const String MG_ATTR_DATABASE_NAME = "name";
      internal const String MG_ATTR_DATABASE_LOCATION = "location";
      internal const String MG_ATTR_DATABASE_TYPE = "dbtype";
      internal const String MG_ATTR_DATABASE_USER_PASSWORD = "userPassword";

      internal const int BYTES_IN_CHAR = 2; // ilana
      
      /// <summary> magic operation codes</summary>
      internal const int MG_OPER_VERIFY = 2;
      internal const int MG_OPER_BLOCK = 5;
      internal const int MG_OPER_LOOP = 97;
      internal const int MG_OPER_ELSE = 98;
      internal const int MG_OPER_ENDBLOCK = 6;
      internal const int MG_OPER_CALL = 7;
      internal const int MG_OPER_EVALUATE = 8;
      internal const int MG_OPER_UPDATE = 9;
      internal const int MG_OPER_USR_EXIT = 13;
      internal const int MG_OPER_RAISE_EVENT = 14;
      internal const int MG_OPER_SERVER = 99;
          
      internal const char TRANS_BEGIN = 'B';
      internal const char TRANS_COMMIT = 'C';
      internal const char TRANS_ABORT = 'A';
      
      /// <summary> frame style</summary>
      internal const char FRAME_SET_STYLE_HOR = 'H';
      internal const char FRAME_SET_STYLE_VER = 'V';

      /// <summary> event types</summary>
      internal const char EVENT_TYPE_SYSTEM = 'S';
      internal const char EVENT_TYPE_INTERNAL = 'I';
      internal const char EVENT_TYPE_TIMER = 'T';
      internal const char EVENT_TYPE_EXPRESSION = 'E';
      internal const char EVENT_TYPE_USER = 'U';
      internal const char EVENT_TYPE_PUBLIC = 'P';
      internal const char EVENT_TYPE_DOTNET = 'D';
      internal const char EVENT_TYPE_NONE = 'N';
      internal const char EVENT_TYPE_NOTINITED = 'X'; // internal
      internal const char EVENT_TYPE_MENU_PROGRAM = 'M';
      internal const char EVENT_TYPE_MENU_OS = 'O';

      // temporary type
      internal const char EVENT_TYPE_USER_FUNC = 'F';

      /// <summary> break levels - used for display purposes</summary>
      internal const String BRK_LEVEL_TASK_PREFIX = "TP";
      internal const String BRK_LEVEL_TASK_SUFFIX = "TS";
      internal const String BRK_LEVEL_REC_PREFIX = "RP";
      internal const String BRK_LEVEL_REC_MAIN = "RM";
      internal const String BRK_LEVEL_REC_SUFFIX = "RS";
      internal const String BRK_LEVEL_CTRL_PREFIX = "CP";
      internal const String BRK_LEVEL_CTRL_SUFFIX = "CS";
      internal const String BRK_LEVEL_CTRL_VERIFICATION = "CV";
      internal const String BRK_LEVEL_HANDLER_INTERNAL = "HI";
      internal const String BRK_LEVEL_HANDLER_SYSTEM = "HS";
      internal const String BRK_LEVEL_HANDLER_TIMER = "HT";
      internal const String BRK_LEVEL_HANDLER_EXPRESSION = "HE";
      internal const String BRK_LEVEL_HANDLER_ERROR = "HR";
      internal const String BRK_LEVEL_HANDLER_USER = "HU";
      internal const String BRK_LEVEL_HANDLER_DOTNET = "HD";
      internal const String BRK_LEVEL_USER_FUNCTION = "UF";
      internal const String BRK_LEVEL_VARIABLE = "VC";
      internal const String BRK_LEVEL_MAIN_PROG = "MP";
      internal const String BRK_LEVEL_SUBFORM = "SUBFORM";
      internal const String BRK_LEVEL_FRAME = "FRAME";

      /// <summary> break levels - used for flow monitor purposes</summary>
      internal const String BRK_LEVEL_TASK_PREFIX_FM = "Task Prefix ";
      internal const String BRK_LEVEL_TASK_SUFFIX_FM = "Task Suffix ";
      internal const String BRK_LEVEL_REC_PREFIX_FM = "Record Prefix ";
      internal const String BRK_LEVEL_REC_SUFFIX_FM = "Record Suffix ";
      internal const String BRK_LEVEL_CTRL_PREFIX_FM = "Control Prefix ";
      internal const String BRK_LEVEL_CTRL_SUFFIX_FM = "Control Suffix ";
      internal const String BRK_LEVEL_VARIABLE_FM = "Variable Change ";
      internal const String BRK_LEVEL_CTRL_VERIFICATION_FM = "Control Verification ";
      internal const String BRK_LEVEL_HANDLER_INTERNAL_FM = "Internal: ";
      internal const String BRK_LEVEL_HANDLER_SYSTEM_FM = "System: ";
      internal const String BRK_LEVEL_HANDLER_TIMER_FM = "Timer:";
      internal const String BRK_LEVEL_HANDLER_EXPRESSION_FM = "Expression: ";
      internal const String BRK_LEVEL_HANDLER_ERROR_FM = "Handler Error ";
      internal const String BRK_LEVEL_HANDLER_USER_FM = "User";
      internal const String BRK_LEVEL_HANDLER_DOTNET_FM = "Dotnet: ";
      internal const String BRK_LEVEL_USER_FUNCTION_FM = "User Defined Function";
      internal const String HANDLER_LEVEL_VARIABLE = "V";

      /// <summary> transactions</summary>
      internal const char TRANS_NONE = 'O';
      internal const char TRANS_RECORD_PREFIX = 'P';
      internal const char TRANS_TASK_PREFIX = 'T';
      internal const char TRANS_IGNORE = 'I';
      internal const char TRANS_FORCE_OPEN= 'F';

      // recovery type (accepted from the server)
      internal const char RECOVERY_NONE = (char)(0);
      internal const char RECOVERY_ROLLBACK = 'R';
      internal const char RECOVERY_RETRY = 'T';

      /// <summary> Verify Operation Constants</summary>
      internal const char FLW_VERIFY_MODE_ERROR = 'E';
      internal const char FLW_VERIFY_MODE_WARNING = 'W';
      internal const char FLW_VERIFY_MODE_REVERT = 'R';
      internal const char IMAGE_EXCLAMATION = 'E';
      internal const char IMAGE_CRITICAL = 'C';
      internal const char IMAGE_QUESTION = 'Q';
      internal const char IMAGE_INFORMATION = 'I';
      internal const char IMAGE_NONE = 'N';
      internal const char BUTTONS_OK = 'O';
      internal const char BUTTONS_OK_CANCEL = 'K';
      internal const char BUTTONS_ABORT_RETRY_IGNORE = 'A';
      internal const char BUTTONS_YES_NO_CANCEL = 'Y';
      internal const char BUTTONS_YES_NO = 'N';
      internal const char BUTTONS_RETRY_CANCEL = 'R';
      internal const char DISPLAY_BOX = 'B';
      internal const char DISPLAY_STATUS = 'S';

      /// <summary> END TASK EVALUATION</summary>
      internal const String END_COND_EVAL_BEFORE = "B";
      internal const String END_COND_EVAL_AFTER = "A";
      internal const String END_COND_EVAL_IMMIDIATE = "I";

      /// <summary> GUI MANAGER OPEN WINDOW PROCESS STATUSES</summary>
      internal const char OPENWIN_STATE_NONE = 'N';
      internal const char OPENWIN_STATE_INPROCESS = 'I';
      internal const char OPENWIN_STATE_OVER = 'O';

      /// <summary> argument constants</summary>
      internal const char ARG_TYPE_FIELD = 'F';
      internal const char ARG_TYPE_EXP = 'E';
      internal const char ARG_TYPE_VALUE = 'V';
      internal const char ARG_TYPE_SKIP = 'X';

      /// <summary> general use consts</summary>
      internal const int TOP_LEVEL_CONTAINER = 0;

      // eye catcher for Marshaling arguments passed on a hyper link
      internal const String MG_HYPER_ARGS = "MG_HYPER_ARGS";

      /// <summary> Requester parameters tags</summary>
      internal const String REQ_ARG_ALPHA = "-A";
      internal const String REQ_ARG_UNICODE = "-C";
      internal const String REQ_ARG_NUMERIC = "-N";
      internal const String REQ_ARG_DOUBLE = "-D";
      internal const String REQ_ARG_LOGICAL = "-L";
      internal const String REQ_ARG_NULL = "-U";
      internal const String REQ_ARG_SEPARATOR = "&";
      internal const String REQ_ARG_START = "?";
      internal const String REQ_ARG_COMMA = ",";
      internal const String REQ_APP_NAME = "appname";
      internal const String REQ_PRG_NAME = "prgname";
      internal const String REQ_PRG_DESCRIPTION = "prgdescription";
      internal const String REQ_ARGS = "arguments";

      // if true, the RC accepting this property should not display an authentication dialog, 
      // even if required according to other conditions (InputPassword/SystemLogin),
      // and perform a silent authentication using the USERNAME & PASSWORD execution properties.
      internal const String REQ_SKIP_AUTHENTICATION = "SkipAuthentication";
      internal const String RC_INDICATION_INITIAL = "RICHCLIENT=INITIAL&"; //first request of the handshake
      internal const String RC_INDICATION_SCRAMBLED = "SCRAMBLED&";
      internal const String RC_INDICATION = "RICHCLIENT=Y&";
      internal const String UTF8TRANS = "UTF8TRANS=Y&";
      internal const String DEBUG_CLIENT_NETWORK_RECOVERY = "&DEBUGCLIENTNETWORKRECOVERY=Y";
      internal const String RC_TOKEN_CACHED_FILE = "CACHE=";        //retrieve a cached file from the runtime-engine (uniPaaS 1.8, topic #16: "RC - Cache security enhancements")
      internal const String RC_TOKEN_NON_ENCRYPTED = "ENCRYPTED=N"; //the server informs that the cached file isn't encrypted.
      internal const String RC_TOKEN_CTX_ID = "CTX=";
      internal const String RC_TOKEN_CTX_GROUP = "CTXGROUP=";
      internal const String RC_TOKEN_SESSION_COUNT = "SESSION=";
      internal const String RC_TOKEN_DATA = "DATA=";
      internal const String RC_TOKEN_TARGET_FILE = "TARGETFILE=";   // path of the file on the server after uploading
      internal const String CACHED_ASSEMBLY_EXTENSION = ".dll";
      internal const String OFFLINE_RC_INITIAL_REQUEST = "OFFLINERCINITIALREQUEST=Y";
      internal const String RC_AUTHENTICATION_REQUEST = "RCAUTHENTICATIONREQUEST=Y";
      internal const String V24_RIA_ERROR_PREFIX = "<RIA_ERROR_RESPONSE>";

      // session count -1 serves as an indication from the client to close the context.
      internal const long SESSION_COUNTER_CLOSE_CTX_INDICATION = -1;

      // execution properties
      internal const String SERVER = "server";
      internal const String REQUESTER = "requester";
      internal const String LOCALID = "localid";
      internal const String DEBUGCLIENT = "debugclient";
      internal const String ENVVARS = "envvars";
      internal const String INTERNAL_LOG_LEVEL = "InternalLogLevel";
      internal const String INTERNAL_LOG_FILE = "InternalLogFile";
      internal const String INTERNAL_LOG_SYNC = "InternalLogSync";
      internal const String LOG_CLIENTSEQUENCE_FOR_ACTIVITY_MONITOR = "LogClientSequenceForActivityMonitor";
      internal const String DISPLAY_STATISTIC_INFORMATION = "DisplayStatisticInformation";
      internal const String CTX_GROUP = "CtxGroup";
      internal const String XP_THEMES = "UseWindowsXPThemes";
      internal const String DPI_AWARE = "DPIAware";
      internal const String PARALLEL_EXECUTION = "ParallelExecution";  // Current process spawned for parallel execution
      internal const String DISPLAY_GENERIC_ERROR = "DisplayGenericError";
      internal const String FIRST_HTTP_REQUEST_TIMEOUT = "FirstHTTPRequestTimeout";
      internal const String CONNECT_ON_STARTUP = "ConnectOnStartup";
      internal const String CLIENT_CACHE_PATH = "ClientCachePath";   // Optional property from execution properties
      internal const String WRITE_CLIENT_CACHE_MAX_RETRY_TIME = "WriteClientCacheMaxRetryTime"; //Optional property from execution properties. 
                                                                                                //Defines max time in seconds for retrying to access cache file 


      // execution properties for customizing change password screen (not documented)
      internal const String MG_TAG_CHNG_PASS_CAPTION = "LogonChangePasswordCaption";
      internal const String MG_TAG_CHNG_PASS_WIN_TITLE = "ChangePasswordWindowTitle";
      internal const String MG_TAG_CHNG_PASS_OLD_PASS_CAPTION = "ChangePasswordOldPasswordCaption";
      internal const String MG_TAG_CHNG_PASS_NEW_PASS_CAPTION = "ChangePasswordNewPasswordCaption";
      internal const String MG_TAG_CHNG_PASS_CONFIRM_PASS_CAPTION = "ChangePasswordConfirmPasswordCaption";
      internal const String MG_TAG_CHNG_PASS_MISMATCH_MSG = "ChangePasswordMismatchMessage";
      
      /// <summary>ClickOnce</summary>
      internal const string DEPLOYMENT_MANIFEST = "APPLICATION";
      internal const string PUBLISH_HTML = "publish.html";
      internal const string PROPS_START_TAG = "<properties>";
      internal const string PROPS_END_TAG = "</properties>";
      
      /// <summary>Authentication dialog</summary>
      internal const string APPL_LOGON_SUB_CAPTION = "Logon Parameters";
      
      /// <summary>Authentication Process</summary>
      internal const String DEFAULT_ERROR_LOGON_FAILED = "Error during authentication process";
      internal const String DEFAULT_ERROR_WRONG_PASSWORD = "Incorrect password";
      internal const String ERROR_STRING = "Error";
      internal const String INFORMATION_STRING = "Information";

      /// <summary>Password change process</summary>
      internal const String DEFAULT_MSG_PASSWORD_CHANGE = "Password was changed successfully";
      internal const String PASSWORD_MISMATCH = "New password and confirm password not same";

      // setData() keys: all keys moved to TagData class
      //          keys for table\tree
      internal const String ROW_CONTROLS_KEY = "ROW_CONTROLS_KEY";
      internal const String ROW_INVALIDATED_KEY = "ROW_INVALIDATED_KEY";
      internal const String IS_EDITOR_KEY = "IS_EDITOR_KEY";
      internal const String TMP_EDITOR = "TMP_EDITOR";

      //          keys saved control
      internal const String LAST_FOCUSED_WIDGET = "LAST_FOCUSED_WIDGET";
      internal const String LAST_CURSOR = "LAST_CURSOR";

      //need to be check if we need those keys
      internal const String KEYDOWN_IGNORE_KEY = "KEYDOWN_IGNORE_key";// need to be check if needed
      internal const String STATUS_BAR_WIDGET_KEY = "STATUS_BAR_WIDGET_KEY";
      internal const String MGBROWSER_KEY = "MGBROWSER_Key";
      internal const String POPUP_WAS_OPENED = "COMBO_BOX_WAS_OPENED";
      internal const String POPUP_WAS_CLOSED_COUNT = "COMBO_BOX_WAS_CLOSED_COUNT";
      internal const String IGNORE_SELECTION_KEY = "IGNORE_SELECTION_KEY";

      // MainHeaders
      internal const String CP_ISO_8859_ANSI = "8859_1";
      internal const String CP_UNICODE = "UTF-16LE";
      // order, during regular rcmp.
      // private Shell shell = null; // the window created by this form
      
      internal const int INCREMENTAL_LOCATE_TIMEOUT = 500;

      internal const String USER_RANGES = "ranges";
      internal const String USER_LOCATES = "locates";
      internal const String SORT = "srt";
      internal const String MIN_RNG = "min";
      internal const String MAX_RNG = "max";
      internal const String NULL_MIN_RNG = "isNullMin";
      internal const String NULL_MAX_RNG = "isNullMax";
      internal const String CLEAR_RANGE = "rangeReset";
      internal const String CLEAR_LOCATES = "locateReset";
      internal const String CLEAR_SORTS = "sortReset";
      internal const String USER_RNG = "rng";

      public const string TASK_MODE_QUERY_STR = "Query";
      public const string TASK_MODE_MODIFY_STR = "Modify";
      public const string TASK_MODE_CREATE_STR = "Create";
      public const string TASK_MODE_DELETE_STR = "Delete";

      internal const String MG_ZOOM = "ZOOM";
      internal const String MG_WIDE = "WIDE";
      internal const String MG_NORMAL = "NORMAL";
      internal const String MG_INSERT = "INS";
      internal const String MG_OVERWRITE = "OVR";

      // environment mirroring
      internal const String MG_TAG_ENV_CHANGES = "changes";
      internal const String MG_ATTR_ENV_VALUE = "value";
      internal const String MG_ATTR_ENV_WRITEINI = "writeToINI";
      internal const String MG_ATTR_ENV_RESERVED = "reserved";
      internal const String MG_ATTR_ENV_REMOVED = "removed";
      // .INI environment sections
      internal const String INI_SECTION_MAGIC_ENV_BRACKETS = "[MAGIC_ENV]";
      internal const String INI_SECTION_LOGICAL_NAMES_BRACKETS = "[MAGIC_LOGICAL_NAMES]";
      internal const String INI_SECTION_MAGIC_ENV = "MAGIC_ENV";
      internal const String INI_SECTION_MAGIC_SYSTEMS = "MAGIC_SYSTEMS";
      internal const String INI_SECTION_MAGIC_DBMS = "MAGIC_DBMS";
      internal const String INI_SECTION_MAGIC_SERVERS = "MAGIC_SERVERS";
      internal const String INI_SECTION_MAGIC_COMMS = "MAGIC_COMMS";
      internal const String INI_SECTION_MAGIC_PRINTERS = "MAGIC_PRINTERS";
      internal const String INI_SECTION_MAGIC_SYSTEM_MENU = "MAGIC_SYSTEM_MENU";
      internal const String INI_SECTION_MAGIC_DATABASES = "MAGIC_DATABASES";
      internal const String INI_SECTION_MAGIC_LANGUAGE = "MAGIC_LANGUAGE";
      internal const String INI_SECTION_MAGIC_SERVICES = "MAGIC_SERVICES";
      internal const String INI_SECTION_TOOLS_MENU = "TOOLS_MENU";

      internal const char MENU_PROGRAM_FLOW_ONLINE  = 'O';
      internal const char MENU_PROGRAM_FLOW_BATCH   = 'B';
      internal const char MENU_PROGRAM_FLOW_BROWSER = 'R';
      internal const char MENU_PROGRAM_FLOW_RC      = 'C';

      //RC specific pane constants.
      internal const int SB_IMG_PANE_LAYER = 1;
      //Width of the pane (in pixels) shown in status bar.
      internal const int SB_IMG_PANE_WIDTH = 33;

      //Offline specific execution prop tag
      internal const String SECURE_MESSAGES           = "SecureMessages";
      internal const String DISABLE_ENCRYPTION        = "DisableEncryption";
      internal const String ENCRYPTION_KEY            = "EncryptionKey";
      internal const String LAST_OFFLINE              = "LastOffline";
      internal const String INITIAL_RESPONSE          = "InitialResponse";
      internal const String LAST_OFFLINE_FILE_VERSION = "FileVersion";
      internal const int    LastOfflineFileVersion    = 1;

#if PocketPC
      internal const String RESPAWN_ON_EXIT = "respawnOnExit";
      internal const String AUTO_START = "AutoStart";
      internal const String STARTUP_IMAGE = "MobileStartupImage";
      internal const String SHOW_PROGRSSBAR = "MobileShowProgressBar";
#endif
#if !PocketPC
      internal const int GUI_COMMAND_EXEC_WAIT_TIME = 100;
      internal const String STARTUP_IMAGE_FILENAME = @"Resources\startup.png";
#endif
      internal const String V2L_DLL = "v2l32.dll";
      internal const String SQLITE_DLL_NAME = "MgRIASqliteGateway.dll";

      internal const uint INITIAL_OFFLINE_TASK_TAG = 1000000000;
      internal const String ENABLE_COMMUNICATION_DIALOGS = "EnableCommunicationDialogs";


      internal const String MG_ATTR_VAL_ABORT = "abort";
      internal const String MG_ATTR_VAL_ENHANCED_VERIFY = "enhancedverify";

      internal const String MG_ATTR_VAL_OPENURL = "openurl";
      internal const String MG_ATTR_VAL_RESULT = "result";
      internal const String MG_ATTR_VAL_VERIFY = "verify";
      internal const String MG_ATTR_VAL_ADD_RANGE = "addrange";
      internal const String MG_ATTR_VAL_ADD_LOCATE = "addlocate";
      internal const String MG_ATTR_VAL_ADD_SORT = "addsort";
      internal const String MG_ATTR_VAL_RESET_RANGE = "resetrange";
      internal const String MG_ATTR_VAL_RESET_LOCATE = "resetlocate";
      internal const String MG_ATTR_VAL_RESET_SORT   = "resetsort";
      internal const String MG_ATTR_VAL_CLIENT_REFRESH = "clientrefresh";
      internal const String MG_ATTR_VAL_EVAL = "evaluate";
      internal const String MG_ATTR_VAL_EVENT = "event";
      internal const String MG_ATTR_VAL_EXEC_OPER = "execoper";
      internal const String MG_ATTR_VAL_EXPAND = "expand";
      internal const String MG_ATTR_VAL_HIBERNATE = "hibernate";
      internal const String MG_ATTR_VAL_MENU = "execmenu";
      internal const String MG_ATTR_VAL_QUERY = "query";
      internal const String MG_ATTR_VAL_QUERY_CACHED_FILE = "cachedFile";
      internal const String MG_ATTR_VAL_QUERY_GLOBAL_PARAMS = "globalparams";
      internal const String MG_ATTR_VAL_QUERY_TYPE = "query_type";
      internal const String MG_ATTR_VAL_RECOMP = "recompute";
      internal const String MG_ATTR_VAL_RESUME = "resume";
      internal const String MG_ATTR_VAL_TRANS = "trans";
      internal const String MG_ATTR_VAL_UNLOAD = "unload";
      internal const String MG_ATTR_VAL_INIPUT_FORCE_WRITE = "iniputForceWrite";
      internal const String MG_ATTR_VAL_INIPUT_PARAM = "iniputParam";
      internal const String MG_ATTR_VAL_VERIFY_CACHE = "verifyCache";
      internal const String MG_ATTR_VAL_ABORT_NON_OFFLINE_TASKS = "abortNonOfflineTasks";
      internal const String MG_ATTR_ERROR_MESSAGE = "errorMessage";
      internal const String MG_ATTR_VAL_TOTAL_RECORDS_COUNT = "totalRecordsCount";
      internal const String MG_ATTR_VAL_RECORDS_BEFORE_CURRENT_VIEW = "recordsBeforeCurrentView";
   }
}
