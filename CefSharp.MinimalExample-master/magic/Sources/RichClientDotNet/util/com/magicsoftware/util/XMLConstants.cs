using System;
using System.Collections.Generic;
using System.Text;

namespace util.com.magicsoftware.util
{
   public class XMLConstants
   {
      public const String MG_TAG_XML = "xml";
      public const String MG_TAG_XML_END = "/xml";
      public const String MG_TAG_XML_END_TAGGED = XMLConstants.END_TAG + MG_TAG_XML + XMLConstants.TAG_CLOSE;
      public const String MG_TAG_OPEN = XMLConstants.TAG_OPEN + MG_TAG_XML + " " + XMLConstants.MG_ATTR_ID + "=\"MGDATA\">";
      public const String MG_TAG_TASK = "task";
      public const String MG_TAG_TASK_END = "/task";
      public const String MG_TAG_RECOMPUTE = "recompute";
      public const String MG_TAG_DCVALUES = "dcvalues";

      //TODO: this class contains the constants which are used for XML parsing.
      //Check if we can move it to Util.dll
      public const String MG_TAG_FONT_ENTRY = "font";
      public const String MG_TAG_FONTTABLE = "fonttable";
      public const String MG_ATTR_ID = "id";
      public const String MG_ATTR_VB_VIEW_ROWIDX = "db_view_rowidx";
      public const String MG_ATTR_CONTROL_ISN = "controlIsn";
      public const String MG_HOR_ALIGMENT_IS_INHERITED = "horAligmentIsInherited";
      public const String MG_ATTR_HEIGHT = "height";
      public const String MG_TYPE_FACE = "typeFace";
      public const String MG_ATTR_STYLE = "style";
      public const String MG_ATTR_CHAR_SET = "charSet";
      
      public const String MG_ATTR_ORIENTATION = "orientation";

      // MainHeaders
      public const int MAX_PATH = 260;
      public const int FILE_NAME_SIZE = (MAX_PATH + 1);

      public const String CDATA_START = "<![CDATA[";
      public const String CDATA_END = "]]>";
      
      public const String TAG_TERM = "/>";
      public const String TAG_OPEN = "<";
      public const String TAG_CLOSE = ">";
      public const String XML_ATTR_DELIM = "\"";
      public const String START_TAG = "\n   <";
      public const String END_TAG = "</";
      public const String XML_TAB = "   ";

      public const String MG_ATTR_VALUE = "val";
      public const String MG_ATTR_STUDIO_VALUE = "studioValue";
      public const String MG_ATTR_NAME = "name";
      public const String MG_ATTR_TYPE = "type";
      public const String MG_ATTR_SIZE = "size";
      public const String MG_ATTR_VAR_NAME = "var_name";
      public const String MG_ATTR_VAR_DISP_NAME = "var_disp_name";
      public const String MG_ATTR_VEC_CELLS_SIZE = "vecCellsSize";
      public const String MG_ATTR_VEC_CELLS_ATTR = "vec_cells_attr";
      public const String MG_ATTR_VEC_CELLS_CONTENT = "CellContentType";
      public const String MG_ATTR_NULLVALUE = "nullvalue";
      public const String MG_ATTR_NULLDISPLAY = "nulldisplay";
      public const String MG_ATTR_NULLDEFAULT = "nulldefault";
      public const String MG_ATTR_DB_MODIFIABLE = "db_modifiable";
      public const String MG_ATTR_DEFAULTVALUE = "defaultvalue";
      public const String MG_ATTR_NULLALLOWED = "nullallowed";
      public const String MG_ATTR_BLOB_CONTENT = "ContentType";
      public const String MG_ATTR_PART_OF_DATAVIEW = "partOfDataview";
      public const String MG_ATTR_DATA_TYPE = "dataType";      
      public const String MG_ATTR_DATA_CTRL = "data_ctrl";
      public const String MG_ATTR_LINKED_PARENT = "linked_parent";
      public const String MG_ATTR_CONTAINER = "container";
      public const String MG_ATTR_IS_FRAMESET = "isFrameSet";
      public const String MG_ATTR_IS_LIGAL_RC_FORM = "isLegalRcForm";      
      public const String MG_ATTR_USERSTATE_ID = "userStateId";
      public const String MG_ATTR_PB_IMAGES_NUMBER = "PBImagesNumber";
      public const String MG_ATTR_FORM_ISN = "formIsn";
      public const String MG_ATTR_TASKID = "taskid";
      public const String MG_ATTR_TASK_IS_SERIALIZATION_PARTIAL = "isSerializationPartial";
      public const String MG_ATTR_XML_TRUE = "1";
      public const String MG_ATTR_XML_FALSE = "0";
      public const String MG_ATTR_TOOLKIT_PARENT_TASK = "toolkit_parent_task";
      public const String MG_ATTR_CTL_IDX = "ctl_idx";
      public const String MG_ATTR_MAINPRG = "mainprg";
      public const String MG_ATTR_NULL_ARITHMETIC = "nullArithmetic";
      public const String MG_ATTR_INTERACTIVE = "interactive";
      public const String MG_ATTR_IS_OFFLINE = "IsOffline";
      public const String MG_ATTR_RETURN_VALUE_EXP = "returnValueExp";
      public const String MG_ATTR_PARALLEL = "ParallelExecution";
      public const String MG_ATTR_OPEN_WIN = "OpenWin";
      public const String MG_ATTR_ALLOW_EVENTS = "AllowEvents";      
      public const String MG_ATTR_ISPRG = "isPrg";
      public const String MG_ATTR_APPL_GUID = "applicationGuid";
      public const String MG_ATTR_PROGRAM_ISN = "programIsn";
      public const String MG_ATTR_TASK_ISN = "taskIsn";
      public const String MG_ATTR_EXPAND = "expand";
      public const String MG_ATTR_EXP = "exp";
      public const String MG_ATTR_IS_GENERIC = "isGeneric";
      public const String MG_ATTR_HASHCODE = "hashCode";
      public const String MG_ATTR_PICTURE = "picture";
      public const String MG_ATTR_STORAGE = "storage";
      public const String MG_ATTR_DITIDX = "ditidx";
      public const String MG_ATTR_ICON_FILE_NAME = "iconFileName";
      public const String MG_ATTR_SYS_CONTEXT_MENU = "systemContextMenu";
      public const String MG_TAG_FLDH = "fldh";
      public const String MG_TAG_DVHEADER = "dvheader";
      public const String MG_ATTR_MENUS_FILE_NAME = "menusFileName";
      public const String MG_ATTR_MENU_CONTENT = "MenusContent";
      public const String MG_ATTR_SORT_BY_RECENTLY_USED = "SortWindowListByRecentlyUsed";

      public const String MG_ATTR_CONTROL_Z_ORDER = "zorder";

      public const String MG_TAG_PROP = "prop";
      public const String MG_TAG_CONTROL = "control";
      public const String MG_TAG_FORM = "form";
      public const String MG_TAG_FORM_PROPERTIES = "propertiesForm";
      public const String MG_TAG_FORMS = "forms";
      public const String MG_TAG_TREE = "tree";
      public const String MG_TAG_NODE = "node";
      public const String MG_TAG_FLD = "fld";
      public const String MG_ATTR_CHILDREN_RETRIEVED = "children_retrieved";

      public const String MG_TAG_COLORTABLE = "colortable";
      public const String MG_TAG_COLORTABLE_ID = "colortableId";
      public const String MG_TAG_COLOR_ENTRY = "color";      

      public const String MG_TAG_HELPTABLE = "helptable";
      public const String MG_TAG_HELPITEM = "helpitem";

      public const String MG_TAG_ASSEMBLIES = "assemblies";
      public const String MG_TAG_ASSEMBLY = "assembly";
      public const String MG_ATTR_ASSEMBLY_PATH = "path";
      public const String MG_ATTR_ASSEMBLY_CONTENT = "Content";
      public const String MG_ATTR_FULLNAME = "fullname";
      public const String MG_ATTR_ISSPECIFIC = "isSpecific";
      public const String MG_ATTR_IS_GUI_THREAD_EXECUTION = "isGuiThreadExecution";

      public const String MG_ATTR_DOTNET_TYPE = "dn_type";
      public const String MG_ATTR_DOTNET_ASSEMBLY_ID = "assembly_id";

      public const String MG_TAG_TASKDEFINITIONID_ENTRY = "taskDefinitionId";
      public const String MG_TAG_OBJECT_REFERENCE = "objectRef";

      //Help types
      public const String MG_ATTR_HLP_TYP_TOOLTIP = "T";
      public const String MG_ATTR_HLP_TYP_PROMPT = "P";
      public const String MG_ATTR_HLP_TYP_URL = "U";
      public const String MG_ATTR_HLP_TYP_INTERNAL = "I";
      public const String MG_ATTR_HLP_TYP_WINDOWS = "W";

      //Internal help attributes.
      public const String MG_ATTR_INTERNAL_HELP_TYPE = "type";
      public const String MG_ATTR_INTERNAL_HELP_NAME = "name";

      public const String MG_ATTR_INTERNAL_HELP_FRAMEX = "framex";
      public const String MG_ATTR_INTERNAL_HELP_FRAMEY = "framey";

      public const String MG_ATTR_INTERNAL_HELP_FRAMEDX = "famedx";
      public const String MG_ATTR_INTERNAL_HELP_FRAMEDY = "framedy";

      public const String MG_ATTR_INTERNAL_HELP_SIZEDX = "sizedx";
      public const String MG_ATTR_INTERNAL_HELP_SIZEDY = "sizedy";

      public const String MG_ATTR_INTERNAL_HELP_FACTORX = "factorx";
      public const String MG_ATTR_INTERNAL_HELP_FACTORY = "factory";

      public const String MG_ATTR_INTERNAL_HELP_BORDERSTYLE = "borderstyle";
      public const String MG_ATTR_INTERNAL_TITLE_BAR = "titlebar";
      public const String MG_ATTR_INTERNAL_HELP_SYSTEM_MENU = "sysmenu";

      public const String MG_ATTR_INTERNAL_HELP_FONT_TABLE_INDEX = "fonttableindex";

      //Windows help attributes.
      public const String MG_ATTR_WINDOWS_HELP_FILE      = "file";
      public const String MG_ATTR_WINDOWS_HELP_COMMAND   = "command";
      public const String MG_ATTR_WINDOWS_HELP_KEY       = "key";

      //Print data attributes.
      public const String MG_TAG_PRINT_DATA              = "Print_data";
      public const String MG_TAG_PRINT_DATA_END          = "/Print_data";
      public const String MG_TAG_RECORD                  = "Record";
      public const String MG_TAG_RECORD_END              = "/Record";

      public const String MG_TAG_RANGES = "ranges";
      public const String MG_TAG_BOUNDARY = "boundary";
      public const string MG_ATTR_MIN = "min";
      public const string MG_ATTR_MAX = "max";
      public const string MG_ATTR_FLD = "fld";

      // date/time formats for DateTimeUtils
      //TODO: isolate to a different file?
      public const String ERROR_LOG_TIME_FORMAT = "HH:mm:ss.f";
      public const String ERROR_LOG_DATE_FORMAT = "dd/MM/yyyy";
      public const String HTTP_ERROR_TIME_FORMAT = "HH:mm:ss";

      public const String CACHED_DATE_TIME_FORMAT = "dd/MM/yyyy HH:mm:ss";

      //webs constants
      public const String MG_TAG_WS_READ_REQUEST = "Read";
      public const String MG_TAG_WS_CREATE_REQUEST = "Create";
      public const String MG_TAG_WS_CREATE_REQUEST_END = "/Create";
      public const String MG_TAG_WS_UPDATE_REQUEST = "Update";
      public const String MG_TAG_WS_UPDATE_REQUEST_END = "/Update";
      public const String MG_TAG_WS_DELETE_REQUEST = "Delete";
      public const String MG_TAG_WS_DELETE_REQUEST_END = "/Delete";
      public const String MG_TAG_WS_MANIPULATE_REQUEST = "Manipulate";
      public const String MG_TAG_WS_MANIPULATE_REQUEST_END = "/Manipulate";

      public const String MG_TAG_WS_DATABASE = "Database";
      public const String MG_TAG_WS_DATASOURCE = "Datasource";
      public const String MG_TAG_WS_COLUMNS = "Columns";
      public const String MG_TAG_WS_COLUMN = "string";
      public const String MG_TAG_WS_RANGES = "Ranges";
      public const String MG_TAG_WS_RANGES_END = "/Ranges";
      public const String MG_TAG_WS_MIN = "Min";
      public const String MG_TAG_WS_MAX = "Max";
      public const String MG_TAG_WS_RECORD = "Row";
      public const String MG_TAG_WS_RECORD_END = "/Row";
      public const String MG_TAG_WS_WHERE = "Where";
      public const String MG_TAG_WS_WHERE_END = "/Where";

      public const String MG_TAG_WS_RESPONSE = "Response";
      public const String MG_TAG_WS_ERROR = "Error";
      public const String MG_TAG_WS_ERROR_CODE = "errorCode";
      public const String MG_TAG_WS_DESCRIPTION = "description";

      //      public const String MG_TAG_BOUNDARY = "boundary";

   }
}
