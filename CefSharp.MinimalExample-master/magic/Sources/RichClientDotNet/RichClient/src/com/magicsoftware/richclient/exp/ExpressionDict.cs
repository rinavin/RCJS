using System;
namespace com.magicsoftware.richclient.exp
{

    internal sealed class ExpressionDict
    {
        /*----------------------------------------------------------------------- */
        /* expression description class to hold expression details               */
        /*----------------------------------------------------------------------- */
        internal sealed class ExpDesc
        {
            internal char Attr_;
            internal short Prec_;
            internal short ArgCount_;
            internal short ArgEvalCount_;
            internal String ArgAttr_;
            internal bool RtfAsAlpha_;

            /// <summary> expression description class</summary>
            /// <param name="Attr">opcode attribute - A/B/N/...
            /// </param>
            /// <param name="Prec">opcode precedence
            /// </param>
            /// <param name="ArgCount">operand count
            /// </param>
            /// <param name="ArgEvalCount">operand pre-evaulation count
            /// </param>
            internal ExpDesc(char Attr, short Prec, short ArgCount, short ArgEvalCount, String ArgAttr, bool RtfAsAlpha)
            {
                Attr_ = Attr;
                Prec_ = Prec;
                ArgCount_ = ArgCount;
                ArgEvalCount_ = ArgEvalCount;
                ArgAttr_ = ArgAttr;
                RtfAsAlpha_ = RtfAsAlpha;
            }
        }

        /*----------------------------------------------------------------------- */
        /* The expression definition table                                       */
        /*----------------------------------------------------------------------- */
        internal static readonly ExpDesc[] expDesc = new ExpDesc[]
      {
         new ExpDesc(' ', (short)0, (short)0, (short)0,  "", false), /* EXP_OP_NONE */
         new ExpDesc(' ', (short)0, (short)0, (short)0,  "", false), /* EXP_OP_V    */
         new ExpDesc('N', (short)0, (short)0, (short)0,  "", false), /* EXP_OP_VAR  */
         new ExpDesc('A', (short)0, (short)0, (short)0,  "", false), /* EXP_OP_A    */
         new ExpDesc('A', (short)0, (short)0, (short)0,  "", false), /* EXP_OP_H    */
         new ExpDesc('N', (short)0, (short)0, (short)0,  "", false), 
         new ExpDesc('D', (short)0, (short)0, (short)0,  "", false), 
         new ExpDesc('T', (short)0, (short)0, (short)0,  "", false), 
         new ExpDesc('B', (short)0, (short)0, (short)0,  "", false), 
         new ExpDesc('N', (short)0, (short)0, (short)0,  "", false), //10 
         new ExpDesc('N', (short)0, (short)0, (short)0,  "", false), 
         new ExpDesc('N', (short)0, (short)0, (short)0,  "", false), 
         new ExpDesc('A', (short)0, (short)0, (short)0,  "", false), /* EXP_OP_M */ 
         new ExpDesc('A', (short)0, (short)0, (short)0,  "", false), /* EXP_OP_ACT */ 
         new ExpDesc('A', (short)0, (short)0, (short)0,  "", false), /* EXP_OP_KBD */
         
         new ExpDesc('N', (short)4, (short)2, (short)2, "NN", false),   //EXP_OP_ADD   Num+Num :  Num
         new ExpDesc('N', (short)4, (short)2, (short)2, "NN", false),   //EXP_OP_SUB   Num-Num :  Num
         new ExpDesc('N', (short)5, (short)2, (short)2, "NN", false),   //EXP_OP_MUL   Num*Num :  Num
         new ExpDesc('N', (short)5, (short)2, (short)2, "NN", false),   //EXP_OP_DIV   Num/Num :  Num
         new ExpDesc('N', (short)5, (short)2, (short)2, "NN", false),   //EXP_OP_MOD   Num MOD Num        :  Num //20
         new ExpDesc('N', (short)4, (short)1, (short)1, "N", false),    //EXP_OP_NEG   -Num    :  Num
         new ExpDesc('N', (short)0, (short)3, (short)3, "NNN", false),  //EXP_OP_FIX   FIX  (Num,Num,Num) :  Num
         new ExpDesc('N', (short)0, (short)3, (short)3, "NNN", false),  //EXP_OP_ROUND ROUND(Num,Num,Num) :  Num
         
         new ExpDesc('B', (short)3, (short)2, (short)2, "  ", false),   //EXP_OP_EQ    X   =   Y  :  Log
         new ExpDesc('B', (short)3, (short)2, (short)2, "  ", false),   //EXP_OP_NE    X   <>  Y  :  Log
         new ExpDesc('B', (short)3, (short)2, (short)2, "  ", false),   //EXP_OP_LE    X   <=  Y  :  Log
         new ExpDesc('B', (short)3, (short)2, (short)2, "  ", false),   //EXP_OP_LT    X   <   Y  :  Log
         new ExpDesc('B', (short)3, (short)2, (short)2, "  ", false),   //EXP_OP_GE    X   >=  Y  :  Log
         new ExpDesc('B', (short)3, (short)2, (short)2, "  ", false),   //EXP_OP_GT    X   >   Y  :  Log
         new ExpDesc('B', (short)2, (short)1, (short)1, "B", false),    //EXP_OP_NOT   NOT Log    :  Log //30
         new ExpDesc('B', (short)1, (short)2, (short)1, "BB", false),   //EXP_OP_OR    Log OR Log :  Log
         new ExpDesc('B', (short)2, (short)2, (short)1, "BB", false),   //EXP_OP_AND   Log AND Log:  Log
         new ExpDesc(' ', (short)0, (short)3, (short)1, "B  ", false),  //EXP_OP_IF    IF(Log,X,Y):  X or Y
         
         new ExpDesc('N', (short)0, (short)1, (short)1, "U", true),     //EXP_OP_LEN   LEN(Str)  :  Num
         new ExpDesc('A', (short)4, (short)2, (short)2, "UU", true),    //EXP_OP_CON   Str & Str :  Str
         new ExpDesc('A', (short)0, (short)3, (short)3, "UNN", true),   //EXP_OP_MID   MID(Str,start,length)   :  Str
         new ExpDesc('A', (short)0, (short)2, (short)2, "UN", true),    //EXP_OP_LEFT  LEFT(Str,length)        :  Str
         new ExpDesc('A', (short)0, (short)2, (short)2, "UN", true),    //EXP_OP_RIGTH RIGTH(Str,length)       :  Str
         new ExpDesc('A', (short)0, (short)2, (short)2, "UN", true),    //EXP_OP_FILL  FILL(Str,times)         :  Str
         new ExpDesc('N', (short)0, (short)2, (short)2, "UU", true),    //EXP_OP_INSTR INSTR(Str,subStr)       :  Num //40
         new ExpDesc('A', (short)0, (short)1, (short)1, "U", true),     //EXP_OP_TRIM  TRIM(Str)               :  Str
         new ExpDesc('A', (short)0, (short)1, (short)1, "U", true),     //EXP_OP_LTRIM LTRIM(Str)              :  Str
         new ExpDesc('A', (short)0, (short)1, (short)1, "U", true),     //EXP_OP_RTRIM RTRIM(Str)              :  Str
         
         new ExpDesc('A', (short)0, (short)2, (short)2, "NU", true),    //EXP_OP_STR   STR(Num,Str)            :  Str
         new ExpDesc('N', (short)0, (short)2, (short)2, "UU", true),    //EXP_OP_VAL   VAL(Str,Str)            :  Num
         
         new ExpDesc('B', (short)0, (short)2, (short)2, "NA", true),    //EXP_OP_STAT     STAT(Str)            :  Str
         new ExpDesc('A', (short)0, (short)1, (short)1, "N", false),    //EXP_OP_LEVEL   LEVEL(Str)            :  Str
         new ExpDesc('N', (short)0, (short)1, (short)1, "N", false),    //EXP_OP_COUNTER COUNTER(Str)          :  Str

         new ExpDesc('*', (short)0, (short)1, (short)1, "V", false),    //EXP_OP_VARPREV
         new ExpDesc('*', (short)0, (short)1, (short)1, "V", false),    //EXP_OP_VARCURR   //50
         new ExpDesc('B', (short)0, (short)1, (short)1, "V", false),    //EXP_OP_VARMOD
         new ExpDesc('N', (short)0, (short)1, (short)1, "N", false),    //EXP_OP_VARINP
         new ExpDesc('A', (short)0, (short)1, (short)1, "V", false),    //EXP_OP_VARNAME
         new ExpDesc('B', (short)0, (short)1, (short)1, "N", false),    //EXP_OP_VIEWMOD
         new ExpDesc('H', (short)0, (short)1, (short)1, "A", true),     //EXP_OP_ENV
         new ExpDesc('A', (short)0, (short)1, (short)1, "A", true),     //EXP_OP_INIGET
         new ExpDesc('B', (short)0, (short)2, (short)2, "AB", true),    //EXP_OP_INIPUT
         new ExpDesc('H', (short)0, (short)1, (short)1, "N", false),    //EXP_OP_USER
         null,    //EXP_OP_GROUP
         new ExpDesc('N', (short)0, (short)0, (short)0, "", false),     //EXP_OP_TERM
         null,    //EXP_OP_MDATE,not needed in browser
         new ExpDesc('D', (short)0, (short)0, (short)0, "", false),    //EXP_OP_DATE
         new ExpDesc('T', (short)0, (short)0, (short)0, "", false),    //EXP_OP_TIME
         new ExpDesc('H', (short)0, (short)0, (short)0, "", false),    //EXP_OP_SYS
         null,    //EXP_OP_PREF
         new ExpDesc('U', (short)0, (short)0, (short)0, "", false),    //EXP_OP_MENU
         new ExpDesc('H', (short)0, (short)0, (short)0, "", false),    //EXP_OP_PROG
         null,    //EXP_OP_PPD
         
         // math functions 
         new ExpDesc('N', (short)6, (short)2, (short)2, "NN", false),  //EXP_OP_PWR  Num ^ Num : Num                        
         new ExpDesc('N', (short)0, (short)1, (short)1, "N", false),   //EXP_OP_LOG  LOG(Num)  : Num                         //70
         new ExpDesc('N', (short)0, (short)1, (short)1, "N", false),   //EXP_OP_EXP  EXP(Num)  : Num                         
         new ExpDesc('N', (short)0, (short)1, (short)1, "N", false),   //EXP_OP_ABS  ABS(Num)  : Num                         
         new ExpDesc('N', (short)0, (short)1, (short)1, "N", false),   //EXP_OP_SIN  SIN(Num)  : Num                         
         new ExpDesc('N', (short)0, (short)1, (short)1, "N", false),   //EXP_OP_COS  COS(Num)  : Num                         
         new ExpDesc('N', (short)0, (short)1, (short)1, "N", false),   //EXP_OP_TAN  TAN(Num)  : Num                         
         new ExpDesc('N', (short)0, (short)1, (short)1, "N", false),   //EXP_OP_ASIN    ASIN(Num) : Num                        
         new ExpDesc('N', (short)0, (short)1, (short)1, "N", false),   //EXP_OP_ACOS    ACOS(Num) : Num                        
         new ExpDesc('N', (short)0, (short)1, (short)1, "N", false),   //EXP_OP_ATAN    ATAN(Num) : Num                        
         new ExpDesc('N', (short)0, (short)1, (short)1, "N", false),   //EXP_OP_RAND    RAND(Num) : Num                        
         
         // comparison functions
         new ExpDesc(' ', (short)0, (short)-2, (short)-2, "  ", false),  //EXP_OP_MIN    MIN(X, Y) : Z                        //80
         new ExpDesc(' ', (short)0, (short)-2, (short)-2, "  ", false),  //EXP_OP_MAX    MAX(X, Y) : Z                        
         new ExpDesc('B', (short)0, (short)3, (short)3, "   ", false),   //EXP_OP_RANGE  RANGE(X, Y, Z) : Log                   

         // string functions 
         new ExpDesc('A', (short)0, (short)4, (short)4, "UUNN", true),  //EXP_OP_REP     REP(Str, Str, Num, Num) : Str          
         new ExpDesc('A', (short)0, (short)4, (short)4, "UUNN", true),  //EXP_OP_INS     INS(Str, Str, Num, Num) : Str          
         new ExpDesc('A', (short)0, (short)3, (short)3, "UNN", true),   //EXP_OP_DEL     DEL(Str, Num, Num) : Str               
         new ExpDesc('A', (short)0, (short)1, (short)1, "U", true),     //EXP_OP_FLIP    FLIP(Str)  : Str                        
         new ExpDesc('A', (short)0, (short)1, (short)1, "U", true),     //EXP_OP_UPPER   UPPER(Str) : Str                       
         new ExpDesc('A', (short)0, (short)1, (short)1, "U", true),     //EXP_OP_LOWER   LOWER(Str) : Str                       
         new ExpDesc('A', (short)0, (short)2, (short)2, "AN", true),    //EXP_OP_CRC     CRC(Str, Num)    : Str                    
         new ExpDesc('N', (short)0, (short)2, (short)2, "AN", true),    //EXP_OP_CHKDGT  CHKDGT(Str, Num) : Num                //90
         new ExpDesc('A', (short)0, (short)1, (short)1, "A", true),     //EXP_OP_SOUNDX  SOUNDX(Str)      : Str(4)                   
         
         //conversion functions
         new ExpDesc('A', (short)0, (short)1, (short)1, "N", false),    //EXP_OP_HSTR    HSTR(Num)       :  Str                        
         new ExpDesc('N', (short)0, (short)1, (short)1, "A", true),     //EXP_OP_HVAL    HVAL(Str)       :  Num                    
         new ExpDesc('A', (short)0, (short)1, (short)1, "N", false),    //EXP_OP_CHR     CHR(Num)        :  Str                  
         new ExpDesc('N', (short)0, (short)1, (short)1, "A", true),     //EXP_OP_ASC     ASC(Str)        :  Num                    
         null,       //EXP_OP_ISTR
         null,       //EXP_OP_IVAL
         null,       //EXP_OP_LSTR
         null,       //EXP_OP_LVAL
         null,       //EXP_OP_RSTR                                   //100
         null,       //EXP_OP_RVAL
         null,       //EXP_OP_ESTR
         null,       //EXP_OP_EVAL
         new ExpDesc('A', (short)0, (short)2, (short)2, "NN", false),   //EXP_OP_MSTR    MSTR(Num,Num)   :  Str
         new ExpDesc('N', (short)0, (short)1, (short)1, "A", true) ,    //EXP_OP_MVAL    MVAL(Str)       :  Num 
         
         // date functions  
         new ExpDesc('A', (short)0, (short)2, (short)2, "DU", true) ,   //EXP_OP_DSTR
         new ExpDesc('D', (short)0, (short)2, (short)2, "UU", true) ,   //EXP_OP_DVAL
         new ExpDesc('A', (short)0, (short)2, (short)2, "TU", true) ,   //EXP_OP_TSTR
         new ExpDesc('T', (short)0, (short)2, (short)2, "UU", true) ,   //EXP_OP_TVAL
         new ExpDesc('N', (short)0, (short)1, (short)1, "D", false) ,   //EXP_OP_DAY  //110
         new ExpDesc('N', (short)0, (short)1, (short)1, "D", false) ,   //EXP_OP_MONTH
         new ExpDesc('N', (short)0, (short)1, (short)1, "D", false) ,   //EXP_OP_YEAR
         new ExpDesc('N', (short)0, (short)1, (short)1, "D", false) ,   //EXP_OP_DOW
         new ExpDesc('A', (short)0, (short)1, (short)1, "D", false) ,   //EXP_OP_CDOW
         new ExpDesc('A', (short)0, (short)1, (short)1, "D", false) ,   //EXP_OP_CMONTH
         new ExpDesc('A', (short)0, (short)1, (short)1, "N", false) ,   //EXP_OP_NDOW
         new ExpDesc('A', (short)0, (short)1, (short)1, "N", false) ,   //EXP_OP_NMONTH
         new ExpDesc('N', (short)0, (short)1, (short)1, "T", false) ,   //EXP_OP_SECOND
         new ExpDesc('N', (short)0, (short)1, (short)1, "T", false) ,   //EXP_OP_MINUTE
         new ExpDesc('N', (short)0, (short)1, (short)1, "T", false) ,   //EXP_OP_HOUR //120
         new ExpDesc('B', (short)0, (short)1, (short)1, "N", false) ,   //EXP_OP_DELAY
         new ExpDesc('N', (short)0, (short)0, (short)0, "", false) ,    //EXP_OP_IDLE
         
         //IOFILE functions
         null, //EOF
         null, //EOP
         null, //PAGE
         null, //LINE
         
         // FM functions 
         null, //IOEXIST
         null, //IOSIZE
         null, //IODEL
         null, //IOREN                 //130
         null, //IOCOPY
         null, //DBEXIST
         null, //DBSIZE
         null, //DBRECS
         null, //DBDEL
         
         //action functions
         null, //KBPUT
         null, //KBGET 
         new ExpDesc('B', (short)0, (short)1, (short)1, "A", false) , //FLOW
         
         //Japanese functions (obsolete, but should not be deleted)
         new ExpDesc('A', (short)0, (short)2, (short)2, "AB", false), /* 138 - EXP_OP_LOGICAL */
         new ExpDesc('A', (short)0, (short)2, (short)2, "AB", false), /* 139 - EXP_OP_VISUAL */
         null,
         null,
         null,
         null,
         null,
         null,
         null,
         
         //Version 3.xx old functions 
         null, //STR3
         null, //VAL3 
         null, //DSTR3
         null, //DVAL3
         null, //TSTR3
         null, //TVAL3 
         null, //RANGE3
         null, //FLOW3
         
         // FM functions - continued 
         null, //DBNAME, Server
         
         //date functions - continued
         new ExpDesc('D', (short)0, (short)4, (short)4, "DNNN",  false) ,   //EXP_OP_ADDDATE
         new ExpDesc('T', (short)0, (short)4, (short)4, "TNNN",  false) ,   //EXP_OP_ADDTIME
         
         //environment functions - continued
         new ExpDesc('H', (short)0, (short)0, (short)0, "", false) ,    //EXP_OP_OWNER
         
         //kernel functions - continued
         new ExpDesc('A', (short)0, (short)1, (short)1, "V", false) ,   //EXP_OP_VARATTR
         
         //date functions - continued
         new ExpDesc('D', (short)0, (short)1, (short)1, "D", false) ,   //EXP_OP_BOM
         new ExpDesc('D', (short)0, (short)1, (short)1, "D", false) ,   //EXP_OP_BOY
         new ExpDesc('D', (short)0, (short)1, (short)1, "D", false) ,   //EXP_OP_EOM
         new ExpDesc('D', (short)0, (short)1, (short)1, "D", false) ,   //EXP_OP_EOY

         //User defined function 
         null ,   //EXP_OP_UDF server
         
         // kernel functions - continued 
         new ExpDesc('N', 0, 0, 0, "", false),  //EXP_OP_RIGHT_LITERAL
         new ExpDesc('B', 0, 1, 1, "N", false), //EXP_OP_RIGHTS

         new ExpDesc('B', 0, 2, 2, "BN", false), //EXP_OP_ROLLBACK

         new ExpDesc('B', (short)0, (short)2, (short)2, "V ", false),   //VARSET
         null, //ZENS
         null, //DBERR
         null, //""
         null, //""
         
         //Null functions
         new ExpDesc('*', (short)0, (short)0, (short)0, "", false) ,  //EXP_OP_NULL     173
         new ExpDesc('A', (short)0, (short)0, (short)0, "", false) ,  //EXP_OP_NULL_A   174
         new ExpDesc('N', (short)0, (short)0, (short)0, "", false) ,  //EXP_OP_NULL_N   175
         new ExpDesc('B', (short)0, (short)0, (short)0, "", false) ,  //EXP_OP_NULL_B   176
         new ExpDesc('D', (short)0, (short)0, (short)0, "", false) ,  //EXP_OP_NULL_D   177
         new ExpDesc('T', (short)0, (short)0, (short)0, "", false) ,  //EXP_OP_NULL_T   178
         new ExpDesc('B', (short)0, (short)1, (short)1, " ", false),  //EXP_OP_ISNULL   179
         
         new ExpDesc('N', (short)0, (short)0, (short)0, "", false),   //EXP_OP_TDEPTH   180
         
         null, //EXP_OP_USERADD  181 /*SERVER */
         null, //EXP_OP_RIGHTADD 182 /*SERVER */
         null, //EXP_OP_GROUPADD 183 /*SERVER */
         null, //EXP_OP_DBCACHE  184 /*SERVER */
         
         //DDE functions
         null, //EXP_OP_DDEGET  185 /*SERVER */
         null, //EXP_OP_DDEXEC  186 /*SERVER */
         null, //EXP_OP_DDEPOKE 187 /*SERVER */
         null, //EXP_OP_DDERR   188 /*SERVER */
         
         //MOUSE last click pos
         new ExpDesc('N', (short)0, (short)0, (short)0, "", false) ,  //EXP_OP_CLICKWX
         new ExpDesc('N', (short)0, (short)0, (short)0, "", false) ,  //EXP_OP_CLICKWY
         new ExpDesc('N', (short)0, (short)0, (short)0, "", false) ,  //EXP_OP_CLICKCX
         new ExpDesc('N', (short)0, (short)0, (short)0, "", false) ,  //EXP_OP_CLICKCY
         
         //Magic Resizing functions: 
         new ExpDesc('B', (short)0, (short)0, (short)0, "", false) ,  //EXP_OP_MINMAGIC make magic window minimum 
         new ExpDesc('B', (short)0, (short)0, (short)0, "", false) ,  //EXP_OP_MAXMAGIC make magic window maximum 
         new ExpDesc('B', (short)0, (short)0, (short)0, "", false) ,  //EXP_OP_RESMAGIC make magic window normal size 
         null,   //EXP_OP_FILEDLG 196-open windows file dialog 
         null,    //EXP_OP_HITZORDR not supported 197-control id in ctrl hit
         
         // Current IO
         null,    //IOCURR not supported 
         
         // Multi lingual support 
         new ExpDesc('A', (short)0, (short)1, (short)1, "U", false),  //EXP_OP_MLS_TRANS  
         
         // control information 
         new ExpDesc('A', (short)0, (short)0, (short)0, "", true),    //CTRLNAME /*200-control name in ctrl hit */
         new ExpDesc('N', (short)0, (short)2, (short)2, "NA", true),  //WINBOX
         new ExpDesc('N', (short)0, (short)1, (short)1, "N", false),  /* 202 - EXP_OP_WIN_HWND */ 
         
         null,    //DBRELOAD /*SERVER */
         null,    //TEXT not supported
         null,    //SETLANG /*SERVER */
         new ExpDesc('A', (short)0, (short)0, (short)0, "", false) ,  //GETLANG
         
         // Menu manipulation functions
         new ExpDesc('B', (short)0, (short)2, (short)2, "AB", false), //MNUCHECK
         new ExpDesc('B', (short)0, (short)2, (short)2, "AB", false), //MNUENABL 
         new ExpDesc('B', (short)0, (short)2, (short)2, "AB", false), //MNUSHOW
         
         new ExpDesc('O', (short)0, (short)0, (short)0, "", false),   //NULL/*210 - NULL function Expected Blob attribute */
         null, //DISCSRVR  /*211 - Disconnect from Server */
         new ExpDesc('*', (short)0, (short)1, (short)1, "A",  false), // 212 - EXP_OP_GETPARAM
         new ExpDesc('B', (short)0, (short)2, (short)2, "A ", false), // 213 - EXP_OP_SETPARAM
         null, //FILE2REQ  /*214 - send file to requester */
         null, //LOGON     /*215 - LogOn a New user       */
         null, //FILE2BLB 
         null, //BLB2FILE
         null, //INTRANS   /* 218 - indicates whether a transaction is active */
         new ExpDesc('*', (short)0, (short)2, (short)2, "B*", false), // 219 - EXP_OP_CND_RANGE

         null, //RQRTS     /*220 number of registered RT            */
         null, //RQRTINF   /*221 information about a RT             */
         null, //RQRTAPPS  /*222 number of supported applications   */
         null, //RQRTAPP   /*223 information about an application   */
         null, //RQQUELST  /*224 list of requests in queue          */
         null, //RQREQLST  /*225 list of requests                   */ 
         null, //RQREQINF  /*226 information about a request        */
         null, //RQQUEPRI  /*227 reset priority of a request        */
         null, //RQQUEDEL  /*228 clear a request from the queue     */
         null, //RQRTTRM   /*229 terminate a registered RT          */
         null, //RQLOAD    /*230 average waiting time for a service & hits distribution for each request status     */
         
         null, //RQEXE     /*231 request the MRD to load an executable */
         null, //DDEBEGIN  /*232 dde begin transaction                 */
         null, //DDEEND    /*233 dde end transaction                   */
         null, //FILE2OLE  /*234 convert file to OLE                   */
         new ExpDesc('B', (short)0, (short)1, (short)1, "V", false) ,  //ISDEFAULT /*235 checks if field's content equal to its default */
         new ExpDesc('N', (short)0, (short)0, (short)0,  "", false), /* 236 EXP_OP_FORM*/
         new ExpDesc('A', (short)0, (short)3, (short)3, "UNU", true),  //STRTOKEN 
         null, //RQSTAT /* 238 - Request status     */
         null, //DBCOPY /* 239 - copying a data file */
         null, //FLWMTR /*240 */
         new ExpDesc('A', (short)0, (short)2, (short)2, "AN", true), // INIGETLN, 241
         new ExpDesc('A', (short)0, (short)1, (short)1, "N", true), //EXPCALC /* 242 - getting an index for other expression */
         new ExpDesc('N', (short)0, (short)0, (short)0, "", true), // EXP_OP_E 
         
         // Current edited control in the screen positions
         new ExpDesc('N', (short)0, (short)2, (short)2, "AN", false) ,  //CLEFT     /* 244 - EXP_OP_CTRL_LEFT    */
         new ExpDesc('N', (short)0, (short)2, (short)2, "AN", false) ,  //CTOP      /* 245 - EXP_OP_CTRL_TOP     */
         null,  //CLEFTMDI  /* 246 - EXP_OP_CTRL_LEFT_MDI */
         null,  //CTOPMDI   /* 247 - EXP_OP_CTRL_TOP_MDI */
         new ExpDesc('N', (short)0, (short)2, (short)2, "AN", false) ,  //CWIDTH    /* 248 - EXP_OP_CTRL_WIDTH   */
         new ExpDesc('N', (short)0, (short)2, (short)2, "AN", false) ,  //CHEIGHT   /* 249 - EXP_OP_CTRL_HEIGHT  */
         null,   //EUROCNV    /* 250 - EXP_OP_EUROCNV      */
         null,   //EUROUPD    /* 251 - EXP_OP_EUROUPD      */
         null,   //EUROSET    /* 252 - EXP_OP_EUROSET      */
         null,   //EUROGET    /* 253 - EXP_OP_EUROGET      */
         null,   //EURODEL    /* 254 - EXP_OP_EURODEL      */
         null,   //DIRFIRST  /* 255 - EXP_OP_DIR_FIRST    */
         null,   //DIRNEXT /* 256 - EXP_OP_DIR_NEXT     */
         null,   //DBDISCNT  server only /* 257 - EXP_OF_DBDISCNT     */
         null,
         null,
         null,    //CLRCACHE  server only  /* 260 - EXP_OP_CLEAR_CACHE */
         new ExpDesc('B', (short)0, (short)1, (short)1, "N", false),   //SETCRSR
         
         // User defined function
         null,    //CALLDLL
         new ExpDesc('N', (short)0, (short)1, (short)1, "A", false),   /* 263 - EXP_OP_CTRLHWND */ 
         new ExpDesc('A', (short)0, (short)1, (short)1, "N", true),    //LASTPARK/* 264 - EXP_OP_LAST_CTRL_PARK */
         
         null,    //CALLDLLS
         null,    //CALLDLLF
         new ExpDesc('A', (short)0, (short)1, (short)1, "AAAA", true),   //WEBREF
         null,    //LMCHKOUT
         null,    //LMCHKIN
         null,    //PROGIDX 270
         
         //Modifications for MAIN_PROGRAM
         null,    //RUNMODE
         
         new ExpDesc('N', (short)0, (short)1, (short)1, "N", true),          //CURROW
         new ExpDesc(' ', (short)0, (short)-4, (short)-4, "    ", false),    //CASE 273
         null,
         new ExpDesc('B', (short)2, (short)2, (short)2, "AA", true),         //275 LIKE
         null,
         null,
         null,
         null,
         null,      /* 280 - EXP_OP_CURR_POSITION */
         null,      /* 281 - EXP_OP_ERR_POSITION  */
         null,      /* 282 - EXP_OP_TRANSMODE     */
         null,      /* 283 - CALLJS     */
         null,      /* 284 - CALLOBJ     */
         null,    /* 285 - EXP_OP_ERRTABLENAME     */
         null,    /* 286 - EXP_OP_ ERRDATABASENAME    */
         null,    /* 287 - EXP_OP_ERRDBMSCODE     */
         null,    /* 288 - EXP_OP_ERRDBMSMESSAGE     */
         null,    /* 289 - EXP_OP_ERRMAGICNAME     */
         new ExpDesc('N', (short)0, (short)0, (short)0, "", false),       //THIS 
         null,    /* 291 - EXP_OP_ERR */ 
         null,    /* 292 - MMSTOP */ 
         null,    /* 293 - EXP_OP_LOCK */ 
         null,    /* 294 - EXP_OP_UNLOCK */ 
         null,    /* 295 - CTXNUM */ 
         null,    /* 296 - CTXSTAT */ 
         null,    /* 297 - CTXLSTUSE */ 
         null,    /* 298 - CTXPROG */ 
         null,    /* 299 - CTXSIZE */ 
         null,    /* 300 - CTXKILL */ 
         null,    /* 301 - LMVSTR */ 
         null,    /* 302 - LMUVSTR */
         new ExpDesc('A', (short)0, (short)3, (short)3, "UUU", false),    /* 303 - REPSTR */
         new ExpDesc('*', (short)0, (short)0, (short)0, "", false),       /* 304 - EDITGET */
         new ExpDesc('B', (short)0, (short)1, (short)1, " ", false),      /* 305 - EDITSET */
         new ExpDesc('N', (short)0, (short)2, (short)2, "NN", false),     /* 306 - DBROUND*/
         new ExpDesc('A', (short)0, (short)2, (short)2, "NN", false),     /* 307 - EXP_OP_VARPIC */
         null,    /* 308 */
         new ExpDesc('N', (short)0, (short)2, (short)2, "UU", false),     /* 309 - StrTokenCnt*/
         new ExpDesc('*', (short)0, (short)1, (short)1, "A", false),      /* 310 - EXP_OP_VARCURRN */
         new ExpDesc('N', (short)0, (short)1, (short)1, "A", false),      /* 311 - EXP_OP_VARINDEX */
         new ExpDesc('A', (short)0, (short)0, (short)0, "", false),       /* 312 - HANDLEDCTRL, EXP_OP_HAND_CTRL */ 
         null,    /* 313 */
         null,    /* 314 */
         null,    /* 315 */
         null,    /* 316 */
         null,    /* 317 */
         null,    /* 318 */
         null,    /* 319 */
         null,    /* 320 */
         null,    /* 321 */
         null,    /* 322 */
         null,    /* 323 */
         null,    /* 324 */
         null,    /* 325 */
         null,    /* 326 */
         null,    /* 327 */
         null,    /* 328 */
         null,    /* 329 */
         null,    /* 330 */
         null,    /* 331 */
         null,    /* 332 */
         null,    /* 333 */
         null,    /* 334 */
         null,    /* 335 */
         null,    /* 336 */
         null,    /* 337 */
         new ExpDesc('B', (short)0, (short)-2, (short)-2, " U", true),   /* 338 EXP_OP_CLIPADD*/
         new ExpDesc('B', (short)0, (short)0, (short)0,   "", false),    /* 339 EXP_OP_CLIPWRITE*/
         new ExpDesc('A', (short)0, (short)0, (short)0,   "", false),    /* 340 EXP_OP_CLIPREAD*/
         null,    /* 341 */
         null,    /* 342 */

         // Reserve place for local extended functions -->
         new ExpDesc('A', (short)0, (short)1, (short)1, "A", false),   /* 343 - EXP_OP_JCDOW */
         new ExpDesc('A', (short)0, (short)1, (short)1, "A", false),   /* 344 - EXP_OP_JMONTH */
         new ExpDesc('A', (short)0, (short)1, (short)1, "A", false),   /* 345 - EXP_OP_JNDOW */
         new ExpDesc('N', (short)0, (short)1, (short)1, "A", false),   /* 346 - EXP_OP_JYEAR */
         new ExpDesc('A', (short)0, (short)2, (short)2, "AA", false),  /* 347 - EXP_OP_JGENGO */
         null,    /* 348 */
         null,    /* 349 */
         new ExpDesc('A', (short)0, (short)1, (short)1, "A", true),    /* 350 - EXP_OP_HAN */
         new ExpDesc('A', (short)0, (short)1, (short)1, "A", true),    /* 351 - EXP_OP_ZEN */
         new ExpDesc('A', (short)0, (short)2, (short)2, "AA", true),   /* 352 - EXP_OP_ZENS */
         new ExpDesc('A', (short)0, (short)1, (short)1, "N", false),   /* 353 - EXP_OP_ZIMEREAD */
         new ExpDesc('A', (short)0, (short)2, (short)2, "AA", true),   /* 354 - EXP_OP_ZKANA */
         null,    /* 355 */
         null,    /* 356 */
         null,    /* 357 */
         null,    /* 358 */
         null,    /* 359 */
         null,    /* 360 */
         null,    /* 361 */
         null,    /* 362 */
         // <-- Reserve place for local extended functions

         new ExpDesc('B', (short)0, (short)2, (short)2, "AU", false),     /* 363 - EXP_OP_MENU_NAME*/
         null,    /* 364 */
         null,    /* 365 */
         null,    /* 366 */
         null,    /* 367 */
         new ExpDesc('B', (short)0, (short)3, (short)3, "ANN", false),    /* 368 - EXP_OP_CTRL_GOTO GOTOCTRL(Str, Num, Num) : Log */ 
         null,     /* 369 */
         null,     /* 370 */
         null,     /* 371 */
         null,     /* 372 */
         new ExpDesc('A', (short)0, (short)1, (short)1, "A", false),      /* 373 EXP_OP_TRANSLATE */      
         new ExpDesc('U', (short)0, (short)2, (short)2,"UU", false),      /* 374 */
         null,     /* 375 */
         new ExpDesc('B', (short)0, (short)2, (short)2, "AA", false),     /* 376 - EXP_OP_CALLURL */
         new ExpDesc('B', (short)0, (short)-3, (short)-3, "AAA", false)   /* 377 - EXP_OP_CALLPROGURL */,
         new ExpDesc('N', (short)0, (short)0, (short)0, "" , false),      /* 378 - EXP_OP_TREELEVEL*/
         new ExpDesc('*', (short)0, (short)1, (short)1, "N", false),      /* 379  - EXP_OP_TREEVALUE*/
         new ExpDesc('B', (short)0, (short)-2, (short)-2, "*NA", false),  /* 380  - EXP_OP_DRAG_SET_DATA */
         new ExpDesc('B', (short)0, (short)2, (short)2, "NA", false),     /* 381  - EXP_OP_DRAG_SET_CURSOR */
         new ExpDesc('B', (short)0, (short)-1, (short)-1, "*NA", false),  /* 382  - EXP_OP_DROP_FORMAT */
         new ExpDesc('*', (short)0, (short)-1, (short)-1, "*NA", false),  /* 383  - EXP_OP_GET_DROP_DATA */
         null,     /* 384 */
         null,     /* 385 */
         null,     /* 386 */
         null,     /* 387 */
         null,     /* 388 */
         null,     /* 389 */
         null,     /* 390 */
         null,     /* 391 */
         null,     /* 392 */
         null,     /* 393 */
         null,     /* 394 */
         null,     /* 395 */
         null,     /* 396 */
         null,     /* 397 */
         null,     /* 398 */
         null,     /* 399 */
         null,     /* 400 */
         new ExpDesc('N', (short)0, (short)0, (short)0, "", false)    /* 401 - EXP_OP_LOOPCOUNTER */,
         null,     /* 402 */
         null,     /* 403 */
         null,     /* 404 */
         null,     /* 405 */
         null,     /* 406 */
         null,     /* 407 */
         null,     /* 408 */
         null,     /* 409 */
         null,     /* 410 */
         null,     /* 411 */
         null,     /* 412 */
         new ExpDesc ('N', (short)0, (short)0, (short)0,"",false),  /* 413 - EXP_OP_DROP_GET_X */
         new ExpDesc ('N', (short)0, (short)0, (short)0,"",false),  /* 414 - EXP_OP_DROP_GET_Y */
         null,     /* 415 */
         null,     /* 416 */
         new ExpDesc('*', (short)0 , (short)2, (short)2, "ON", false),    /* 417  VecGet */
         new ExpDesc('B', (short)0 , (short)3, (short)3, "VN ", false),   /* 418  VecSet */
         new ExpDesc('N', (short)0 , (short)1, (short)1, "O", false),     /* 419  VecSize*/
         new ExpDesc('A', (short)0 , (short)1, (short)1, "O", false),     /* 420  VecCellAttr*/
         null,     /* 421 */
         null,     /* 422 */
         null,     /* 423 */
         null,     /* 424 */
         null,     /* 425 */
         null,     /* 426 */
         null,     /* 427 */
         null,     /* 428 */
         null,     /* 429 */
         null,     /* 430 */
         null,     /* 431 */
         null,     /* 432 */
         null,     /* 433 */
         null,     /* 434 */
         null,     /* 435 */
         null,     /* 436 */
         null,     /* 437 */
         null,     /* 438 */
         null,     /* 439 */
         null,     /* 440 */
         null,     /* 441 */
         new ExpDesc('N', (short)0 , (short)1, (short)1, "O", false),     /* 442  BlobSize*/
         null,     /* 443 */
         null,     /* 444 */
         null,     /* 445 */
         null,     /* 446 */
         null,     /* 447 */
         new ExpDesc('N', (short)0 , (short)3, (short)3, "UUU", false),     /* 448 - StrTokenIdx */
         new ExpDesc('N', (short)0, (short)0, (short)0, "", false) ,        //EXP_OP_MTIME     /* 449 */
         new ExpDesc('T', (short)0, (short)2, (short)2, "UU", true) ,       //EXP_OP_MTVAL     /* 450 */
         new ExpDesc('A', (short)0, (short)2, (short)2, "NU", true),        //EXP_OP_MTSTR     /* 451 */
         null,     /* 452 */
         null,     /* 453 */
         null,     /* 454 */
         null,     /* 455 */
         null,     /* 456 */
         new ExpDesc('B', (short)0,  (short)1, (short)1, " ", false) ,     /* EXP_OP_TREENODEGOTO 457 */
         null,     /* 458 */
         null,     /* 459 */
         null,     /* 460 */
         null,     /* 461 */
         new ExpDesc('B', (short)0, (short)-2, (short)-2, "  ", true),     //EXP_OP_IN     /* 462 */
         null,     /* 463 */
         null,     /* 464 */
         new ExpDesc('B', (short)0, (short)0, (short)0, "", false),     /* 465 */
         null,     /* 466 */
         null,     /* 467 */
         null,     /* 468 */
         null,     /* 469 */
         null,     /* 470 */
         null,     //EXP_OP_DIRDLG
         new ExpDesc('N', (short)0, (short)2, (short)2, "NN", true),    //EXP_OP_MARKTEXT
         new ExpDesc('B', (short)0, (short)1, (short)1, "U", true),     //EXP_OP_MARKTEXTSET
         new ExpDesc('A', (short)0, (short)0, (short)0, "", true),      //EXP_OP_MARKTEXTGET
         new ExpDesc('N', (short)0, (short)0, (short)0, "", true),      //EXP_OP_CARETPOSGET
         null,     /* 476 */
         null,     /* 477 */
         null,     /* 478 */
         null,     /* 479 */
         null,     /* 480 */
         null,     /* 481 */
         null,     /* 482 */
         null,     /* 483 */
         null,     /* 484 */
         null,     /* 485 */
         null,     /* 486 */
         null,     /* 487 */
         null,     /* 488 */
         null,     /* 489 */
         null,     /* 490 */
         new ExpDesc('U', (short)0, (short)0, (short)0, "", false),     //EXP_OP_NULL_U      491
         new ExpDesc('B', (short)0, (short)2, (short)2, "NA", false),   //EXP_OP_MNUADD      492
         new ExpDesc('B', (short)0, (short)-1, (short)-1, "NA", false), //EXP_OP_MNUREMOVE   493
         new ExpDesc('B', (short)0, (short)0, (short)0, "", false),     //EXP_OP_MNURESET    494
         new ExpDesc('N', (short)0, (short)0, (short)0,  "", false), /* EXP_OP_MNU  */
         new ExpDesc(' ', (short)0, (short)-16, (short)-16, "               ", false) , //EXP_OP_USER_DEFINED_FUNC   496
         new ExpDesc('N', (short)0, (short)1, (short)1, "N", false),    //EXP_OP_SUBFORM_EXEC_MODE    497
         null,     /* 498 */
         null,     /* 499 */
         null,     /* 500 */
         null,     /* 501 */
         null,     /* 502 */
         null,     /* 503 */
         null,     /* 504 */
         new ExpDesc('A', (short)0, (short)1, (short)1, "N", false) ,   //EXP_OP_UNICODECHR   505
         new ExpDesc('N', (short)0, (short)1, (short)1, "U", false) ,   //EXP_OP_UNICODEASC   506
         new ExpDesc('U', (short)0, (short)2, (short)2, "AN", false) ,  //EXP_OP_UNICODEFROMANSI   507
         new ExpDesc('A', (short)0, (short)2, (short)2, "UN", false) ,  //EXP_OP_UNICODETOANSI   508
         new ExpDesc('B', (short)0, (short)8, (short)8, "VVNNNNNN", false) ,  //EXP_OP_ADDDT 509 
         new ExpDesc('B', (short)0, (short)6, (short)6, "DTDTVV", false),    /* EXP_OP_DIFDT 510 */
         new ExpDesc('B', (short)0, (short)1, (short)1, "N", false),    /*EXP_OP_ISFIRSTRECORDCYCLE*/
         new ExpDesc('A', (short)0, (short)1, (short)1, "N", false),    /* EXP_OP_MAINLEVEL    */
         null,     /* 513 */
         null,     /* 514 */
         new ExpDesc('N', (short)0, (short)1, (short)1, "N", false),    /* EXP_OP_MAINDISPLAY */
         null,     /* 516 */
         null,     /* 517 */
         new ExpDesc('B', (short)0, (short)1, (short)1, "U", false),    /* EXP_OP_SETWINDOW_FOCUS */
         new ExpDesc('N', (short)0, (short)2, (short)2, "AB", false),   /* EXP_OP_MENU_IDX 519 */
         null,     /* 520 */
         null,     /* 521 */
         null,     /* 522 */
         null,     /* 523 */
         null,     /* 524 */
         null,     /* 525 */
         null,     /* 526 */
         null,     /* 527 */
         null,     /* 528 */
         null,     /* 529 */
         null,     /* 530 */
         null,     /* 531 */
         null,     /* 532 */
         null,     /* 533 */
         null,     /* 534 */
         null,     /* 535 */
         null,     /* 536 */
         null,     /* 537 */
         null,     /* 538 */
         new ExpDesc('N', (short)0, (short)1, (short)1, "N", false),    /* EXP_OP_DBVIEWSIZE    */
         new ExpDesc('N', (short)0, (short)1, (short)1, "N", false),    /* EXP_OP_DBVIEWROWIDX  */
         new ExpDesc('S', (short)0, (short)0, (short)0, "", false),    /* EXP_OP_PROJECTDIR  */
         new ExpDesc('B', (short)0, (short)1, (short)1, "A", false),    /* EXP_OP_FORMSTATECLEAR   */
         new ExpDesc('A', (short)0, (short)1, (short)1, "N", false),    /* EXP_OP_PUBLICNAME    */
         new ExpDesc('A', (short)0, (short)1, (short)1, "N", false),    /* EXP_OP_TASKID        */
         null,     /* 545 */
         null,     /* 546 */
         null,     /* 547 */
         null,     /* 548 */
         new ExpDesc('A', (short)0, (short)-2, (short)-2, "UU", false),   /* EXP_OP_STR_BUILD */
         null,     /* 550 */
         
         new ExpDesc('O', (short)0, (short)1, (short)1, "A", false),      /* 551 EXP_OP_CLIENT_FILE2BLOB*/
         new ExpDesc('B', (short)0, (short)2, (short)2, "OA", false),     /* 552 EXP_OP_CLIENT_BLOB2FILE*/
         new ExpDesc('B', (short)0, (short)2, (short)2, "AA", false),     /* 553 EXP_OP_CLIENT_FILECOPY*/
         new ExpDesc('B', (short)0, (short)1, (short)1, "A", false),      /* 554 EXP_OP_CLIENT_FILEDEL*/
         new ExpDesc('B', (short)0, (short)1, (short)1, "A", false),      /* 555 EXP_OP_CLIENT_FILEEXIST*/
         new ExpDesc('O', (short)0, (short)3, (short)3, "AAB", false),    /* 556 EXP_OP_CLIENT_FILE_LIST_GET*/
         new ExpDesc('B', (short)0, (short)2, (short)2, "AA", false),     /* 557 EXP_OP_CLIENT_FILEREN*/
         new ExpDesc('N', (short)0, (short)1, (short)1, "A", false),      /* 558 EXP_OP_CLIENT_FILESIZE*/
         new ExpDesc('A', (short)0, (short)1, (short)1, "A", false),      /* 559 EXP_OP_CLIENT_OS_ENV_GET*/
         new ExpDesc('B', (short)0, (short)2, (short)2, "AA", false),     /* 560 EXP_OP_CLIENT_OS_ENV_SET*/
         new ExpDesc('B', (short)0, (short)1, (short)1, "N", false),      /* 561 EXP_OP_EMPTY_DATA_VIEW*/
         new ExpDesc('B', (short)0, (short)2, (short)2, "AA", false),     /* EXP_OP_BROWSER_SET_CONTENT 562   */
         new ExpDesc('A', (short)0, (short)1, (short)1, "A", false),      /* EXP_OP_BROWSER_GET_CONTENT 563   */
         new ExpDesc('B', (short)0, (short)-3, (short)-3, "AABU", false),    /* EXP_OP_BROWSER_SCRIPT_EXECUTE 564*/      
         new ExpDesc('A', (short)0, (short)0, (short)0, "",    false),    /* EXP_OP_CLIENT_GET_UNIQUE_MC_ID 565*/
         new ExpDesc('N', (short)0, (short)2, (short)2, "AN", false) ,    /* EXP_OP_CTRL_CLIENT_CX 566   */
         new ExpDesc('N', (short)0, (short)2, (short)2, "AN", false) ,    /* EXP_OP_CTRL_CLIENT_CX 567     */
         new ExpDesc('A', (short)0, (short)1, (short)1, "U", false) ,    /* EXP_OP_STATUSBARSETTEXT 568     */
         null,     /* 569 */
         null,     /* 570 */
         new ExpDesc('A', (short)0, (short)2, (short)2, "AN", false) ,    /* EXP_OP_CLIENT_FILEINFO 571    */
         new ExpDesc('A', (short)0, (short)5, (short)5, "AAABB", true),   /* EXP_OP_CLIENT_FILEOPENDLG 572 */ 
         new ExpDesc('A', (short)0, (short)5, (short)5, "AAAAB", true),   /* EXP_OP_CLIENT_FILESAVEDLG 573 */ 
         new ExpDesc('A', (short)0, (short)3, (short)3, "AAB", true),     /* EXP_OP_CLIENT_DIRDLG 574*/ 
         null,     /* 575 */
         null,     /* 576 */
         new ExpDesc('B', (short)0, (short)4, (short)4, "UUUB", true),   /*EXP_OP_CLIENT_REDIRECT 577*/
         null,     /* 578 */
         null,     /* 579 */
         null,     /* 580 */
         null,     /* 581 */
         null,     /* 582 */
         null,     /* 583 */
         null,     /* 584 */
         null,     /* 585 */
         null,     /* 586 */
         null,     /* 587 */
         null,     /* 588 */
         null,     /* 589 */
         null,     /* 590 */
         null,     /* 591 */
         null,     /* 592 */
         new ExpDesc('B', (short)0, (short)0, (short)0, "", true),   /*EXP_OP_IS_MOBILE_CLIENT 593*/
         new ExpDesc('A', (short)0, (short)0, (short)0, "", true),   /*EXP_OP_CLIENT_SESSION_STATISTICS_GET 594*/
         new ExpDesc(' ', (short)0, (short)3, (short)3, "ENA", false),     /* 595 - EXP_OP_DN_MEMBER */
         new ExpDesc(' ', (short)0, (short)3, (short)3, "ENA", false),     /* 596 - EXP_OP_DN_STATIC_MEMBER */
         new ExpDesc(' ', (short)0, (short)-3, (short)-3, "ENA", false),   /* 597 - EXP_OP_DN_METHOD */
         new ExpDesc(' ', (short)0, (short)-3, (short)-3, "ENA", false),   /* 598 - EXP_OP_DN_STATIC_METHOD */
         new ExpDesc(' ', (short)0, (short)-2, (short)-2, "EN", false),    /* 599 - EXP_OP_DN_CTOR */
         new ExpDesc(' ', (short)0, (short)-2, (short)-2, "EN", false),    /* 600 - EXP_OP_DN_ARRAY_CTOR */
         new ExpDesc(' ', (short)0, (short)-2, (short)-2, "EN", false),    /* 601 - EXP_OP_DN_ARRAY_ELEMENT */
         new ExpDesc(' ', (short)0, (short)-3, (short)-3, "NE ", false),   /* 602 - EXP_OP_DN_INDEXER */
         new ExpDesc(' ', (short)0, (short)3, (short)3, "ENA", false),     /* 603 - EXP_OP_DN_PROP_GET */
         new ExpDesc(' ', (short)0, (short)3, (short)3, "ENA", false),     /* 604 - EXP_OP_DN_STATIC_PROP_GET */
         new ExpDesc(' ', (short)0, (short)2, (short)2, "EA", false),      /* 605 - EXP_OP_DN_ENUM */
         new ExpDesc(' ', (short)0, (short)2, (short)2, "EE", false),      /* 606 - EXP_OP_DN_CAST */
         new ExpDesc(' ', (short)0, (short)1, (short)1, " ", false),       /* 607 - EXP_OP_DN_REF */
         new ExpDesc(' ', (short)0, (short)2, (short)2, "  ", false),      /* 608 - EXP_OP_DN_SET */
         new ExpDesc(' ', (short)0, (short)2, (short)2, "NA", false),      /* 609 - EXP_OP_DNTYPE */
         new ExpDesc(' ', (short)0, (short)0, (short)0, "", false),        /* 610 - EXP_OP_DN_EXCEPTION */
         new ExpDesc(' ', (short)0, (short)0, (short)0, "", false),        /* 611 - EXP_OP_DN_EXCEPTION_OCCURED */ 
         new ExpDesc('A', (short)0, (short)1, (short)1, "N", false),       /* 612 - EXP_OP_TASKTYPE */
         null,                                                             /* 613 - EXP_OP_DOTNET */
         new ExpDesc('A', (short)0, (short)1, (short)1, "A", false),       /* 614 - EXP_OP_SERVER_FILE_TO_CLIENT */
         new ExpDesc('N', (short)0, (short)2, (short)2, "AA", false),      /* 615 - EXP_OP_CLIENT_FILE_TO_SERVER */
         new ExpDesc('B', (short)0, (short)-2, (short)-2, "V  ", false),   /* 616 - EXP_OP_RANGE_ADD */
         new ExpDesc('B', (short)0, (short)1, (short)1, "N", false),       /* 617 - EXP_OP_RANGE_RESET */
         new ExpDesc('B', (short)0, (short)-2, (short)-2, "V  ", false),   /* 618 - EXP_OP_LOCATE_ADD */
         new ExpDesc('B', (short)0, (short)1, (short)1, "N", false),       /* 619 - EXP_OP_LOCATE_RESET */
         new ExpDesc('B', (short)0, (short)2, (short)2, "VB", false),      /* 620 - EXP_OP_SORT_ADD */
         new ExpDesc('B', (short)0, (short)1, (short)1, "N", false),       /* 621 - EXP_OP_SORT_RESET */
         new ExpDesc('N', (short)0, (short)1, (short)1, "N", false),       /* 622 - EXP_OP_TSK_INSTANCE */
         null,                                                             /* 623 - EXP_OP_RQHTTPSTATUSCODE */
         null,                                                             /* 624 - EXP_OP_HTTPCALL */
         new ExpDesc(' ', (short)0, (short)3, (short)3, "NAA", false),     /* 625 - EXP_OP_DATAVIEW_TO_DN_DATATABLE */
         null,                                                             /* 626 - EXP_OP_GET_PARAM_NAMES */
         null,                                                             /* 627 - EXP_OP_SHARED_VAL_GET_NAMES */
         null,                                                             /* 628 - EXP_OP_GET_PARAM_ATTR */
         null,                                                             /* 629 - EXP_OP_SHARED_VAL_GET_ATTR */      
         new ExpDesc('N', (short)0, (short)0, (short)0, "", false),        /* 630 - EXP_OP_SERVER_LAST_ACCESS_STATUS */
         null,
         new ExpDesc('B', (short)0, (short)2, (short)2, "A ", false),      /* 632 - EXP_OP_CLIENTSESSION_SET */
         new ExpDesc('D', (short)0, (short)0, (short)0, "", false),        /* 633 - EXP_OP_DATE */
         new ExpDesc('T', (short)0, (short)0, (short)0, "", false),        /* 634 - EXP_OP_TIME */
         new ExpDesc('N', (short)0, (short)0, (short)0, "", false),        /* 635 - EXP_OP_MTIME */
         new ExpDesc('B', (short)0, (short)1, (short)1, "A", false),       /* 636 - EXP_OP_CLIENT_DB_DISCONNECT */
         new ExpDesc('B', (short)0, (short)5, (short)5, "NANAA", false),   /* 637 - EXP_OP_DATAVIEW_TO_DATASOURCE */
         new ExpDesc('B', (short)0, (short)2, (short)2, "NA", false),       /* 638 - EXP_OP_CLIENT_DB_DEL */
         null,
         null,
         null,
         null,
         null,
         null,
         new ExpDesc('*', (short)0, (short)-2, (short)-2, "AA", false),   /* 645 - EXP_OP_CLIENT_NATIVE_CODE_EXECUTION */
         null,
         new ExpDesc('U', (short)0, (short)1, (short)1, "V", false),       /* 647 - EXP_OP_VARDISPLAYNAME */
         new ExpDesc('B', (short)0, (short)2, (short)2, "NB", false),      // 648 EXP_OP_CONTROLS_PERSISTENCY_CLEAR
         new ExpDesc('B', (short)0, (short)2, (short)2, "AN", false),     /* 649 - EXP_OP_CONTROL_ITEMS_REFRESH */
         new ExpDesc('N', (short)0, (short)1, (short)1, "V", false),     /* 650 - EXP_OP_VARCONTROLID */
         new ExpDesc('U', (short)0, (short)1, (short)1, "N", false),     /* 651 - EXP_OP_CONTROLITEMSLIST */
         new ExpDesc('U', (short)0, (short)1, (short)1, "N", false),     /* 652 - EXP_OP_CONTROLDISPLAYLIST */
         new ExpDesc('B', (short)0, (short)-2, (short)-2, "AU", false),     /* 653 - EXP_OP_CLIENT_SQL_EXECUTE */
         new ExpDesc('N', (short)0, (short)2, (short)2, "NB", false),   /* 654 - EXP_OP_PIXELSTOFROMUNITS  */  
         new ExpDesc('N', (short)0, (short)2, (short)2, "NB", false),   /* 655 - EXP_OP_FORMUNITSTOPIXELS  */  
         new ExpDesc('N', (short)0, (short)1, (short)1, "N", false),    /* 656 - EXP_OP_CONTROL_SELECT_PROGRAM */  
         null,
         null,
         new ExpDesc('B', (short)0, (short)3, (short)3, "NAA", false), /* 659 - EXP_OP_COLOR_SET */
         new ExpDesc('B', (short)0, (short)9, (short)9, "NANNNBBBB", false), /* 660 - EXP_OP_FONT_SET */
         null,
         null,
         null,
         null,
         new ExpDesc('S', (short)0, (short)5, (short)5, "NNNNB", false), /* 665 - EXP_OP_CLIENT_IMAGE_CAPTURE */
         new ExpDesc('B', (short)0, (short)2, (short)2, "UU", false), /* 666 - EXP_OP_PROGRESS_DIALOG_SET */
      };
    }
}
