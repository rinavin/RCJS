using System;
namespace com.magicsoftware.richclient.util
{

    internal class FlowMonitorInterface
    {
        internal const int FLWMTR_START = 0;
        internal const int FLWMTR_END = 1;
        internal const int FLWMTR_PROPAGATE = 2;
        internal const int FLWMTR_USE_COMP = 3;
        /* Task level messages */
        //internal final static int FLWMTR_TSKOPEN       = 1;
        //internal final static int FLWMTR_TSKCLOS       = 2;
        //internal final static int FLWMTR_OPENDB        = 3;
        //internal final static int FLWMTR_CLOSEDB       = 4;
        //internal final static int FLWMTR_OPENIO        = 5;
        //internal final static int FLWMTR_CLOSEIO       = 6;
        internal const int FLWMTR_EVENT = 7;
        internal const int FLWMTR_CHNG_MODE = 8;
        /* Flw level messages */
        //internal final static int FLWMTR_TSK_PREFIX   = 11;
        //internal final static int FLWMTR_TSK_SUFFIX   = 12;
        //internal final static int FLWMTR_BRK_PREFIX   = 13;
        //internal final static int FLWMTR_BRK_SUFFIX   = 14;
        internal const int FLWMTR_PREFIX = 15;
        internal const int FLWMTR_SUFFIX = 16;
        //internal final static int FLWMTR_REC_MAIN     = 17;
        internal const int FLWMTR_CTRL_PREFIX = 18;
        internal const int FLWMTR_CTRL_SUFFIX = 19;
        internal const int FLWMTR_TSK_HANDLER = 20;
        internal const int FLWMTR_CTRL_VERIFY = 21;
        /* View level messages */
        internal const int FLWMTR_SORT = 22;
        internal const int FLWMTR_LOCATE = 23;
        internal const int FLWMTR_RANGE = 24;
        internal const int FLWMTR_KEY_CHANGE = 25;
        internal const int FLWMTR_TRANS = 26;
        internal const int FLWMTR_LOADREC = 27;
        internal const int FLWMTR_UPDATE = 28;
        internal const int FLWMTR_DELETE = 29;
        internal const int FLWMTR_INSERT = 30;
        /* Recompute messages */
        internal const int FLWMTR_RECOMP = 31;
        /* Operations messages */
        internal const int FLWMTR_DATA_OPER = 41;
        /* LOG messages */
        internal const int FLWMTR_LOG_MSG = 51;
        /* free messages (not filtered) */
        internal const int FLWMTR_USER_MSG = 96;
        internal const int FLWMTR_ERROR_MSG = 97;
        internal const int FLWMTR_FREE_MSG = 98;
        internal const int FLWMTR_DEBUG_MSG = 99;
        internal const int FLWMTR_WARN_MSG = 100;
        internal const int FLWMTR_VARCHG_VALUE = 119;
        internal const int FLWMTR_VARCHG_REASON = 120;
        internal const int FLWMTR_EVENT_KBD = 1;
        internal const int FLWMTR_EVENT_ACT = 2;
        internal const int FLWMTR_EVENT_TIME = 4;
        internal const int FLWMTR_EVENT_EXP = 8;
    }
}