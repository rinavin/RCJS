namespace com.magicsoftware.unipaas.management.exp
{
    public class ExpressionInterface
    {
        /*-----------------------------------------------------------------------*/
        /* General constants definitions                                         */
        /*-----------------------------------------------------------------------*/
        public const int EXP_OPER_LEN = 2;
        public const int EXP_OPER_FUNC_PTR_LEN = 4;

        /*-----------------------------------------------------------------------*/
        /* expression operators                                                  */
        /*-----------------------------------------------------------------------*/
        public const int EXP_OP_NONE = 0; /* variable                              */
        public const int EXP_OP_V = 1; /* variable                              */
        public const int EXP_OP_VAR = 2; /* 'variable'VAR                         */
        public const int EXP_OP_A = 3; /* 'alpha string'                        */
        public const int EXP_OP_H = 4; /* 'hebrew string'HEB                    */
        public const int EXP_OP_N = 5; /* number                                */
        public const int EXP_OP_D = 6; /* 'date'DATE                            */
        public const int EXP_OP_T = 7; /* 'time'TIME                            */
        public const int EXP_OP_L = 8; /* 'logical'LOG                          */
        public const int EXP_OP_F = 9; /* 'file'FILE                            */
        public const int EXP_OP_K = 10; /* 'key'KEY                              */
        public const int EXP_OP_P = 11; /* 'program'PROG                         */
        public const int EXP_OP_M = 12; /* 'mode'MODE                            */
        public const int EXP_OP_ACT = 13; /* 'action'ACT                           */
        public const int EXP_OP_KBD = 14; /* 'kbd'KBD                              */
        public const int EXP_OP_ADD = 15; /* Num + Num : Num                        */
        public const int EXP_OP_SUB = 16; /* Num - Num : Num                        */
        public const int EXP_OP_MUL = 17; /* Num * Num : Num                        */
        public const int EXP_OP_DIV = 18; /* Num / Num : Num                        */
        public const int EXP_OP_MOD = 19; /* Num MOD Num : Num                      */
        public const int EXP_OP_NEG = 20; /* -Num : Num                             */
        public const int EXP_OP_FIX = 21; /* FIX(Num, Num, Num) : Num               */
        public const int EXP_OP_ROUND = 22; /* ROUND(Num, Num, Num) : Num             */
        public const int EXP_OP_EQ = 23; /* X             = Y              : Log               */
        public const int EXP_OP_NE = 24; /* X <> Y             : Log               */
        public const int EXP_OP_LE = 25; /* X <            = Y             : Log               */
        public const int EXP_OP_LT = 26; /* X < Y              : Log               */
        public const int EXP_OP_GE = 27; /* X >            = Y             : Log               */
        public const int EXP_OP_GT = 28; /* X > Y              : Log               */
        public const int EXP_OP_NOT = 29; /* NOT Log            : Log               */
        public const int EXP_OP_OR = 30; /* Log OR Log         : Log               */
        public const int EXP_OP_AND = 31; /* Log AND Log        : Log               */
        public const int EXP_OP_IF = 32; /* IF(Log, X, Y)      : X OR Y            */
        public const int EXP_OP_LEN = 33; /* LEN(Str)           : Num               */
        public const int EXP_OP_CON = 34; /* Str & Str          : Str               */
        public const int EXP_OP_MID = 35; /* MID(Str,Num,Num)   : Str               */
        public const int EXP_OP_LEFT = 36; /* LEFT(Str,Num)      : Str               */
        public const int EXP_OP_RIGHT = 37; /* RIGHT(Str,Num)     : Str               */
        public const int EXP_OP_FILL = 38; /* FILL(Str,Num)      : Str               */
        public const int EXP_OP_INSTR = 39; /* INSTR(Str,Str)     : Num               */
        public const int EXP_OP_TRIM = 40; /* TRIM(Str)          : Str               */
        public const int EXP_OP_LTRIM = 41; /* LTRIM(Str)         : Str               */
        public const int EXP_OP_RTRIM = 42; /* RTRIM(Str)         : Str               */
        public const int EXP_OP_STR = 43; /* RTRIM(Str)         : Str               */
        public const int EXP_OP_VAL = 44; /* RTRIM(Str)         : Str               */
        public const int EXP_OP_STAT = 45; /* PHASE II                                */
        public const int EXP_OP_LEVEL = 46; /* PHASE II                                */
        public const int EXP_OP_COUNTER = 47; /* counter(num)                          */
        public const int EXP_OP_VARPREV = 48; /*              */
        public const int EXP_OP_VARCURR = 49; /*              */
        public const int EXP_OP_VARMOD = 50; /*              */
        public const int EXP_OP_VARINP = 51; /*              */
        public const int EXP_OP_VARNAME = 52; /*              */
        public const int EXP_OP_VIEWMOD = 53; /*              */
        public const int EXP_OP_ENV = 54; /*????????????              */
        public const int EXP_OP_INIGET = 55; /* INIGET(Str) : Str                      */
        public const int EXP_OP_INIPUT = 56; /* INIPUT(Str) : Log                      */
        public const int EXP_OP_USER = 57; /* User(Num) */
        public const int EXP_OP_TERM = 59; /*Term*/
        public const int EXP_OP_DATE = 61; /* DATE()        */
        public const int EXP_OP_TIME = 62; /* TIME()        */
        public const int EXP_OP_SYS = 63; /* AppName() */
        public const int EXP_OP_MENU = 65; /* MENU()        */
        public const int EXP_OP_PROG = 66; /*              */
        // math functions 
        public const int EXP_OP_PWR = 68; /* Num ^ Num : Num                        */
        public const int EXP_OP_LOG = 69; /* LOG(Num)  : Num                         */
        public const int EXP_OP_EXP = 70; /* EXP(Num)  : Num                         */
        public const int EXP_OP_ABS = 71; /* ABS(Num)  : Num                         */
        public const int EXP_OP_SIN = 72; /* SIN(Num)  : Num                         */
        public const int EXP_OP_COS = 73; /* COS(Num)  : Num                         */
        public const int EXP_OP_TAN = 74; /* TAN(Num)  : Num                         */
        public const int EXP_OP_ASIN = 75; /* ASIN(Num) : Num                        */
        public const int EXP_OP_ACOS = 76; /* ACOS(Num) : Num                        */
        public const int EXP_OP_ATAN = 77; /* ATAN(Num) : Num                        */
        public const int EXP_OP_RAND = 78; /* RAND(Num) : Num                        */
        // comparison functions 
        public const int EXP_OP_MIN = 79; /* MIN(X, Y) : Z                          */
        public const int EXP_OP_MAX = 80; /* MAX(X, Y) : Z                          */
        public const int EXP_OP_RANGE = 81; /* RANGE(X, Y, Z) : Log                   */
        // string functions 
        public const int EXP_OP_REP = 82; /* REP(Str, Str, Num, Num) : Str          */
        public const int EXP_OP_INS = 83; /* INS(Str, Str, Num, Num) : Str          */
        public const int EXP_OP_DEL = 84; /* DEL(Str, Num, Num) : Str               */
        public const int EXP_OP_FLIP = 85; /* FLIP(Str)  : Str                       */
        public const int EXP_OP_UPPER = 86; /* UPPER(Str) : Str                       */
        public const int EXP_OP_LOWER = 87; /* LOWER(Str) : Str                       */
        public const int EXP_OP_CRC = 88; /* CRC(Str, Num)    : Str                 */
        public const int EXP_OP_CHKDGT = 89; /* CHKDGT(Str, Num) : Num                 */
        public const int EXP_OP_SOUNDX = 90; /* SOUNDX(Str)      : Str(4)              */
        // conversion functions 
        public const int EXP_OP_HSTR = 91; /* HSTR(Num)       :  Str                */
        public const int EXP_OP_HVAL = 92; /* HVAL(Str)       :  Num                */
        public const int EXP_OP_CHR = 93; /* CHR(Num)        :  Str                */
        public const int EXP_OP_ASC = 94; /* ASC(Str)        :  Num                */
        public const int EXP_OP_MSTR = 103; /* MSTR(Num,Num)   :  Str              */
        public const int EXP_OP_MVAL = 104; /* MVAL(Str)       :  Num              */
        // date functions  
        public const int EXP_OP_DSTR = 105;
        public const int EXP_OP_DVAL = 106;
        public const int EXP_OP_TSTR = 107;
        public const int EXP_OP_TVAL = 108;
        public const int EXP_OP_DAY = 109;
        public const int EXP_OP_MONTH = 110;
        public const int EXP_OP_YEAR = 111;
        public const int EXP_OP_DOW = 112;
        public const int EXP_OP_CDOW = 113;
        public const int EXP_OP_CMONTH = 114;
        public const int EXP_OP_NDOW = 115;
        public const int EXP_OP_NMONTH = 116;
        public const int EXP_OP_SECOND = 117;
        public const int EXP_OP_MINUTE = 118;
        public const int EXP_OP_HOUR = 119;
        public const int EXP_OP_DELAY = 120;
        public const int EXP_OP_IDLE = 121;
        //Action functions
        public const int EXP_OP_KBPUT = 135; /* Phase II */
        public const int EXP_OP_KBGET = 136; /* Phase II */
        public const int EXP_OP_FLOW = 137;
        //Date functions continued

        public const int EXP_OP_LOGICAL = 138;
        public const int EXP_OP_VISUAL = 139;

        public const int EXP_OP_ADDDATE = 156; /* */
        public const int EXP_OP_ADDTIME = 157; /*  */
        //Environment functions continued
        public const int EXP_OP_OWNER = 158; /* */
        //Kernel functions continued 
        public const int EXP_OP_VARATTR = 159; /* */
        //Date functions continued 
        public const int EXP_OP_BOM = 160; /* */
        public const int EXP_OP_BOY = 161; /* */
        public const int EXP_OP_EOM = 162; /* */
        public const int EXP_OP_EOY = 163; /* */
        public const int EXP_OP_RIGHT_LITERAL = 165; /* */
        public const int EXP_OP_RIGHTS = 166; /* */
        public const int EXP_OP_ROLLBACK = 167; /*rollback function */
        public const int EXP_OP_VARSET = 168; /* VARSET(Var:Num, Value) : Log */
        public const int EXP_OP_EVALX = 171;
        public const int EXP_OP_IGNORE = 172;
        // NULL Functions: 
        public const int EXP_OP_NULL = 173; /*  */
        public const int EXP_OP_NULL_A = 174; /*  */
        public const int EXP_OP_NULL_N = 175; /*  */
        public const int EXP_OP_NULL_B = 176; /*  */
        public const int EXP_OP_NULL_D = 177; /*  */
        public const int EXP_OP_NULL_T = 178; /*  */
        public const int EXP_OP_ISNULL = 179; /*  */
        public const int EXP_OP_TDEPTH = 180; /*Phase II */
        //Mouse last click pos
        public const int EXP_OP_CLICKWX = 189; /*Phase II  */
        public const int EXP_OP_CLICKWY = 190; /*Phase II  */
        public const int EXP_OP_CLICKCX = 191; /*Phase II  */
        public const int EXP_OP_CLICKCY = 192; /*Phase II  */
        //Magic Resizing functions:
        public const int EXP_OP_MINMAGIC = 193; //make magic window minimum
        public const int EXP_OP_MAXMAGIC = 194; //make magic window maximum 
        public const int EXP_OP_RESMAGIC = 195; //make magic window normal size 
        public const int EXP_OP_FILEDLG = 196;
        public const int EXP_OP_MLS_TRANS = 199;
        // control information 
        public const int EXP_OP_CTRL_NAME = 200; //200-control name in ctrl hit
        public const int EXP_OP_WIN_BOX = 201; // 
        public const int EXP_OP_WIN_HWND = 202; // 
        public const int EXP_OP_GETLANG = 206; //
        public const int EXP_OP_CHECK_MENU = 207;
        public const int EXP_OP_ENABLE_MENU = 208;
        public const int EXP_OP_SHOW_MENU = 209;
        public const int EXP_OP_NULL_O = 210;
        public const int EXP_OP_GETPARAM = 212;
        public const int EXP_OP_SETPARAM = 213;
        public const int EXP_OP_CND_RANGE = 219; /* Cndrange */
        public const int EXP_OP_ISDEFAULT = 235; /*235 checks if field's content equal to its default*/
        public const int EXP_OP_FORM = 236; // 'X'FORM
        public const int EXP_OP_STRTOKEN = 237;
        public const int EXP_OP_INIGETLN = 241; /* Get line  # from section in magic.ini */
        public const int EXP_OP_EXPCALC = 242; /* 242 -  ExpCalc     */
        public const int EXP_OP_E = 243; /* 242 -  EXP     */
        // Current edited control in the screen positions
        public const int EXP_OP_CTRL_LEFT = 244; /* 244 -       */
        public const int EXP_OP_CTRL_TOP = 245; /* 245 -       */
        public const int EXP_OP_CTRL_LEFT_MDI = 246; /* 246 -       */
        public const int EXP_OP_CTRL_TOP_MDI = 247; /* 247 -       */
        public const int EXP_OP_CTRL_WIDTH = 248; /* 248 -       */
        public const int EXP_OP_CTRL_HEIGHT = 249; /* 249 -       */
        public const int EXP_OP_SETCRSR = 261;
        public const int EXP_OP_CTRLHWND = 263;
        public const int EXP_OP_LAST_CTRL_PARK = 264; /* 264 - EXP_OP_LAST_CTRL_PARK */
        public const int EXP_OP_WEB_REFERENCE = 267;
        public const int EXP_OP_CURRROW = 272;
        public const int EXP_OP_CASE = 273;
        public const int EXP_OP_LIKE = 275; /* Like LIKE in SQL */
        public const int EXP_OP_CALLJS = 283;
        public const int EXP_OP_CALLOBJ = 284;
        public const int EXP_OP_THIS = 290;
        public const int EXP_OP_REPSTR = 303; /* Get Edited Value */
        public const int EXP_OP_EDITGET = 304; /* Get Edited Value */
        public const int EXP_OP_EDITSET = 305; /* Set Edited Value */
        public const int EXP_OP_DBROUND = 306;
        public const int EXP_OP_VARPIC = 307;
        public const int EXP_OP_STRTOK_CNT = 309; /* STRTOKENCNT */
        public const int EXP_OP_VARCURRN = 310;
        public const int EXP_OP_VARINDEX = 311;
        public const int EXP_OP_HAND_CTRL = 312; /* Control parked on when handler invoked */
        public const int EXP_OP_CLIPADD = 338;
        public const int EXP_OP_CLIPREAD = 340;
        public const int EXP_OP_CLIPWRITE = 339;
        public const int EXP_OP_JCDOW = 343;
        public const int EXP_OP_JMONTH = 344;
        public const int EXP_OP_JNDOW = 345;
        public const int EXP_OP_JYEAR = 346;
        public const int EXP_OP_JGENGO = 347;
        public const int EXP_OP_HAN = 350;
        public const int EXP_OP_ZEN = 351;
        public const int EXP_OP_ZENS = 352;
        public const int EXP_OP_ZIMEREAD = 353;
        public const int EXP_OP_ZKANA = 354;
        public const int EXP_OP_MENU_NAME = 363;
        public const int EXP_OP_GOTO_CTRL = 368; /* Goto Control     */
        public const int EXP_OP_TRANSLATE = 373;
        public const int EXP_OP_ASTR = 374;
        public const int EXP_OP_CALLURL = 376;
        public const int EXP_OP_CALLPROGURL = 377;
        public const int EXP_OP_TREELEVEL = 378;
        public const int EXP_OP_TREEVALUE = 379;
        public const int EXP_OP_DRAG_SET_DATA = 380;
        public const int EXP_OP_DRAG_SET_CURSOR = 381;
        public const int EXP_OP_DROP_FORMAT = 382;
        public const int EXP_OP_GET_DROP_DATA = 383;
        public const int EXP_OP_LOOPCOUNTER = 401;
        public const int EXP_OP_DROP_GET_X = 413;
        public const int EXP_OP_DROP_GET_Y = 414;
        public const int EXP_OP_VECGET = 417;
        public const int EXP_OP_VECSET = 418;
        public const int EXP_OP_VECSIZE = 419;
        public const int EXP_OP_VECCELLATTR = 420;
        public const int EXP_OP_BLOBSIZE = 442;
        public const int EXP_OP_STRTOKEN_IDX = 448;
        public const int EXP_OP_MTIME = 449;
        public const int EXP_OP_MTVAL = 450;
        public const int EXP_OP_MTSTR = 451;
        public const int EXP_OP_TREENODEGOTO = 457;
        public const int EXP_OP_WINHELP = 460;
        public const int EXP_OP_IN = 462;
        public const int EXP_OP_ISCOMPONENT = 465;
        public const int EXP_OP_RQRTTRMTIME = 468;
        public const int EXP_OP_DIRDLG = 471;
        public const int EXP_OP_EXT_A = 485; /* 'alpha string'                        */
        // marked text functions
        public const int EXP_OP_MARKTEXT = 472;
        public const int EXP_OP_MARKEDTEXTSET = 473;
        public const int EXP_OP_MARKEDTEXTGET = 474;
        public const int EXP_OP_CARETPOSGET = 475;
        // NULL Functions (added for Unicode): 
        public const int EXP_OP_NULL_U = 491;
        public const int EXP_OP_MNUADD = 492;
        public const int EXP_OP_MNUREMOVE = 493;
        public const int EXP_OP_MNURESET = 494;
        public const int EXP_OP_MNU = 495;
        public const int EXP_OP_USER_DEFINED_FUNC = 496;
        public const int EXP_OP_SUBFORM_EXEC_MODE = 497;
        public const int EXP_OP_UNICODEASC = 506;
        public const int EXP_OP_ADDDT = 509;
        public const int EXP_OP_DIFDT = 510;
        public const int EXP_OP_ISFIRSTRECORDCYCLE = 511;
        public const int EXP_OP_MAINLEVEL = 512;
        public const int EXP_OP_MAINDISPLAY = 515;
        public const int EXP_OP_SETWINDOW_FOCUS = 518;
        public const int EXP_OP_MENU_IDX = 519;
        public const int EXP_OP_DBVIEWSIZE = 539;
        public const int EXP_OP_DBVIEWROWIDX = 540;
        public const int EXP_OP_PROJECTDIR = 541;
        public const int EXP_OP_FORMSTATECLEAR = 542;
        public const int EXP_OP_PUBLICNAME = 543;
        public const int EXP_OP_TASKID = 544;
        public const int EXP_OP_STR_BUILD = 549;
        public const int EXP_OP_CLIENT_FILE2BLOB = 551;
        public const int EXP_OP_CLIENT_BLOB2FILE = 552;
        public const int EXP_OP_CLIENT_FILECOPY = 553;
        public const int EXP_OP_CLIENT_FILEDEL = 554;
        public const int EXP_OP_CLIENT_FILEEXIST = 555;
        public const int EXP_OP_CLIENT_FILE_LIST_GET = 556;
        public const int EXP_OP_CLIENT_FILEREN = 557;
        public const int EXP_OP_CLIENT_FILESIZE = 558;
        public const int EXP_OP_CLIENT_OS_ENV_GET = 559;
        public const int EXP_OP_CLIENT_OS_ENV_SET = 560;
        public const int EXP_OP_EMPTY_DATA_VIEW = 561;
        public const int EXP_OP_BROWSER_SET_CONTENT = 562;
        public const int EXP_OP_BROWSER_GET_CONTENT = 563;
        public const int EXP_OP_BROWSER_SCRIPT_EXECUTE = 564;
        public const int EXP_OP_CLIENT_GET_UNIQUE_MC_ID = 565;
        public const int EXP_OP_CTRL_CLIENT_CX = 566;
        public const int EXP_OP_CTRL_CLIENT_CY = 567;
        public const int EXP_OP_STATUSBARSETTEXT = 568;
        public const int EXP_OP_CLIENT_FILEINFO = 571;
        public const int EXP_OP_CLIENT_FILEOPEN_DLG = 572;
        public const int EXP_OP_CLIENT_FILESAVE_DLG = 573;
        public const int EXP_OP_CLIENT_DIRDLG = 574;
        public const int EXP_OP_CLIENT_REDIRECT = 577;
        public const int EXP_OP_IS_MOBILE_CLIENT = 593;
        public const int EXP_OP_CLIENT_SESSION_STATISTICS_GET = 594;
        public const int EXP_OP_DN_MEMBER = 595;
        public const int EXP_OP_DN_STATIC_MEMBER = 596;
        public const int EXP_OP_DN_METHOD = 597;
        public const int EXP_OP_DN_STATIC_METHOD = 598;
        public const int EXP_OP_DN_CTOR = 599;
        public const int EXP_OP_DN_ARRAY_CTOR = 600;
        public const int EXP_OP_DN_ARRAY_ELEMENT = 601;
        public const int EXP_OP_DN_INDEXER = 602;
        public const int EXP_OP_DN_PROP_GET = 603;
        public const int EXP_OP_DN_STATIC_PROP_GET = 604;
        public const int EXP_OP_DN_ENUM = 605;
        public const int EXP_OP_DN_CAST = 606;
        public const int EXP_OP_DN_REF = 607;
        public const int EXP_OP_DN_SET = 608;
        public const int EXP_OP_DNTYPE = 609;
        public const int EXP_OP_DN_EXCEPTION = 610;
        public const int EXP_OP_DN_EXCEPTION_OCCURED = 611;
        public const int EXP_OP_TASKTYPE = 612;
        public const int EXP_OP_SERVER_FILE_TO_CLIENT = 614;
        public const int EXP_OP_CLIENT_FILE_TO_SERVER = 615;
        public const int EXP_OP_RANGE_ADD = 616;
        public const int EXP_OP_RANGE_RESET = 617;
        public const int EXP_OP_LOCATE_ADD = 618;
        public const int EXP_OP_LOCATE_RESET = 619;
        public const int EXP_OP_SORT_ADD = 620;
        public const int EXP_OP_SORT_RESET = 621;
        public const int EXP_OP_TSK_INSTANCE = 622;
        public const int EXP_OP_DATAVIEW_TO_DN_DATATABLE = 625;
        //Offline Support
        public const int EXP_OP_SERVER_LAST_ACCESS_STATUS = 630;
        public const int EXP_OP_CLIENTSESSION_SET = 632;

        public const int EXP_OP_UTCDATE = 633;
        public const int EXP_OP_UTCTIME = 634;
        public const int EXP_OP_UTCMTIME = 635;
        public const int EXP_OP_CLIENT_DB_DISCONNECT = 636;
        public const int EXP_OP_DATAVIEW_TO_DATASOURCE = 637;
        public const int EXP_OP_CLIENT_DB_DEL = 638;
        public const int EXP_OP_CLIENT_NATIVE_CODE_EXECUTION = 645;
        public const int EXP_OP_VARDISPLAYNAME = 647;
        public const int EXP_OP_CONTROLS_PERSISTENCY_CLEAR = 648;
        public const int EXP_OP_CONTROL_ITEMS_REFRESH = 649;
        public const int EXP_OP_VARCONTROLID = 650;
        public const int EXP_OP_CONTROLITEMSLIST = 651;
        public const int EXP_OP_CONTROLDISPLAYLIST = 652;
        public const int EXP_OP_CLIENT_SQL_EXECUTE = 653;

        public const int EXP_OP_PIXELSTOFROMUNITS = 654;
        public const int EXP_OP_FORMUNITSTOPIXELS = 655;
        public const int EXP_OP_CONTROL_SELECT_PROGRAM = 656;

        public const int EXP_OP_COLOR_SET = 659;
        public const int EXP_OP_FONT_SET = 660;
    }
}
