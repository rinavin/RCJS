using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace MgSqlite.src
{
   class SQLite3DLLImports
   {
      public const String SQLite3DLL = "sqlite3.dll";

      public static SQLite3DLLImports Instance { get; private set; }

      public static void Initialize(SQLite3DLLImports newInstance)
      {
         if (Instance == null)
            Instance = newInstance;
      }

      public int sqlite3_open(string connectionString, ref IntPtr connDb)
      {
         return Imports.sqlite3_open(connectionString, ref connDb);
      }

      public int sqlite3_open16(string connectionString, ref IntPtr connDb)
      {
         return Imports.sqlite3_open16(connectionString, ref connDb);
      }

      public int sqlite3_key(IntPtr db, string connectionPassword, int nKey)
      {
         return Imports.sqlite3_key(db, connectionPassword, nKey);
      }

      public int sqlite3_close(IntPtr connDb)
      {
         return Imports.sqlite3_close(connDb);
      }

      public int sqlite3_prepare(IntPtr db,
                                 string zSql,
                                 ref IntPtr ppStmt)
      {
         IntPtr pzTail = new IntPtr(0);
         return Imports.sqlite3_prepare(db, zSql, -1, ref ppStmt, ref pzTail);
      }

      public int sqlite3_prepare16_v2(IntPtr db,
                                      string zSql,                                      
                                      ref IntPtr ppStmt)
      {
         IntPtr pzTail = new IntPtr(0);
         return Imports.sqlite3_prepare16_v2(db, zSql, -1, ref ppStmt, ref pzTail);
      }

      public int sqlite3_step(IntPtr sqlite3_stmt)
      {
         return Imports.sqlite3_step(sqlite3_stmt);
      }

      public int sqlite3_finalize(IntPtr pStmt)
      {
         return Imports.sqlite3_finalize(pStmt);
      }

      public int sqlite3_errcode(IntPtr db)
      {
         return Imports.sqlite3_errcode(db);
      }

      public int sqlite3_extended_errcode(IntPtr db)
      {
         return Imports.sqlite3_extended_errcode(db);
      }

      public string sqlite3_errmsg(IntPtr db)
      {
         return Imports.sqlite3_errmsg(db);
      }

      public int sqlite3_exec(IntPtr db, string zSql, ref IntPtr errmsg)
      {
         IntPtr arg = new IntPtr(0);
         return Imports.sqlite3_exec(db, zSql, 0, ref arg, ref errmsg);
      }

      public int sqlite3_reset(IntPtr pStmt)
      {
         return Imports.sqlite3_reset(pStmt);
      }

      public int sqlite3_bind_blob(IntPtr pStmt, int colIndex, object data, int dataLength)
      {         
         IntPtr destructor = new IntPtr(0);
         IntPtr dataPtr = Marshal.AllocHGlobal(dataLength);
         Marshal.Copy((byte[])data, 0, dataPtr, dataLength);
         return Imports.sqlite3_bind_blob(pStmt, colIndex, dataPtr, dataLength, destructor);
      }

      public int sqlite3_bind_double(IntPtr pStmt, int colIndex, double data)
      {
         return Imports.sqlite3_bind_double(pStmt, colIndex, data);
      }


      public int sqlite3_bind_int(IntPtr pStmt, int colIndex, int data)
      {
         return Imports.sqlite3_bind_int(pStmt, colIndex, data);
      }


      public int sqlite3_bind_int64(IntPtr pStmt, int colIndex, Int64 data)
      {
         return Imports.sqlite3_bind_int64(pStmt, colIndex, data);
      }


      public int sqlite3_bind_null(IntPtr pStmt, int colIndex)
      {
         return Imports.sqlite3_bind_null(pStmt, colIndex);
      }


      public int sqlite3_bind_text(IntPtr pStmt, int colIndex, string data)
      {
         IntPtr destructor = new IntPtr(0);
         return Imports.sqlite3_bind_text(pStmt, colIndex, data, -1, destructor);
      }


      public int sqlite3_bind_text16(IntPtr pStmt, int colIndex, string data)
      {
         IntPtr destructor = new IntPtr(-1);
         return Imports.sqlite3_bind_text16(pStmt, colIndex, data, -1, destructor);
      }


      public int sqlite3_column_count(IntPtr pStmt)
      {
         return Imports.sqlite3_column_count(pStmt);
      }


      public int sqlite3_column_type(IntPtr pStmt, int iCol)
      {
         return Imports.sqlite3_column_type(pStmt, iCol);
      }


      public IntPtr sqlite3_column_blob(IntPtr pStmt, int iCol)
      {
         return Imports.sqlite3_column_blob(pStmt, iCol);
      }


      public int sqlite3_column_bytes(IntPtr pStmt, int iCol)
      {
         return Imports.sqlite3_column_bytes(pStmt, iCol);
      }


      public int sqlite3_column_bytes16(IntPtr pStmt, int iCol)
      {
         return Imports.sqlite3_column_bytes16(pStmt, iCol);
      }


      public double sqlite3_column_double(IntPtr pStmt, int iCol)
      {
         return Imports.sqlite3_column_double(pStmt, iCol);
      }


      public int sqlite3_column_int(IntPtr pStmt, int iCol)
      {
         return Imports.sqlite3_column_int(pStmt, iCol);
      }


      public Int64 sqlite3_column_int64(IntPtr pStmt, int iCol)
      {
         return Imports.sqlite3_column_int64(pStmt, iCol);
      }


      public string sqlite3_column_text(IntPtr pStmt, int iCol)
      {
         return Marshal.PtrToStringAnsi(Imports.sqlite3_column_text(pStmt, iCol));
      }


      public string sqlite3_column_text16(IntPtr pStmt, int iCol)
      {         
         return Marshal.PtrToStringUni(Imports.sqlite3_column_text16(pStmt, iCol));
      }

      public int sqlite3_changes(IntPtr db)
      {
         return Imports.sqlite3_changes(db);
      }


      public int sqlite3_total_changes(IntPtr db)
      {
         return Imports.sqlite3_total_changes(db);
      }


      public Int64 sqlite3_last_insert_rowid(IntPtr db)
      {
         return Imports.sqlite3_last_insert_rowid(db);
      }


      public string sqlite3_errmsg16(IntPtr db)
      {
         return Imports.sqlite3_errmsg16(db);
      }


      class Imports
      {
         #region Connection
         [DllImport(SQLite3DLL, EntryPoint = "sqlite3_open", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
         public static extern int sqlite3_open([MarshalAs(UnmanagedType.LPStr)] string connectionString, ref IntPtr connDb);

         [DllImport(SQLite3DLL, EntryPoint = "sqlite3_open16", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
         public static extern int sqlite3_open16([MarshalAs(UnmanagedType.LPWStr)] string connectionString, ref IntPtr connDb);

         [DllImport(SQLite3DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
         public static extern int sqlite3_key(IntPtr db, [MarshalAs(UnmanagedType.LPStr)] string connectionPassword, int nKey);

         [DllImport(SQLite3DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
         public static extern int sqlite3_close(IntPtr connDb);
         #endregion

         
         #region Statement Execution
         [DllImport(SQLite3DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
         public static extern int sqlite3_prepare(IntPtr db,
                                                   [MarshalAs(UnmanagedType.LPStr)] string zSql,
                                                   int nByte,
                                                   ref IntPtr ppStmt,
                                                   ref IntPtr pzTail);

         [DllImport(SQLite3DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
         public static extern int sqlite3_prepare16_v2(IntPtr db,
                                                       string zSql,
                                                       int nByte,
                                                       ref IntPtr ppStmt,
                                                       ref IntPtr pzTail);

         [DllImport(SQLite3DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
         public static extern int sqlite3_step(IntPtr sqlite3_stmt);

         [DllImport(SQLite3DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
         public static extern int sqlite3_finalize(IntPtr pStmt);

         [DllImport(SQLite3DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
         public static extern int sqlite3_exec(IntPtr db, [MarshalAs(UnmanagedType.LPStr)] string zSql, int callback, ref IntPtr arg, ref IntPtr errmsg);
         #endregion


         #region Error Handling 
         [DllImport(SQLite3DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
         public static extern int sqlite3_errcode(IntPtr db);

         [DllImport(SQLite3DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
         public static extern int sqlite3_extended_errcode(IntPtr db);

         [DllImport(SQLite3DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
         public static extern string sqlite3_errmsg(IntPtr db);

         [DllImport(SQLite3DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
         public static extern string sqlite3_errmsg16(IntPtr db);
         #endregion


         #region Binding
         [DllImport(SQLite3DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
         public static extern int sqlite3_reset(IntPtr pStmt);

         [DllImport(SQLite3DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
         public static extern int sqlite3_bind_blob(IntPtr pStmt, int colIndex, IntPtr data, int n, IntPtr destructor);

         [DllImport(SQLite3DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
         public static extern int sqlite3_bind_double(IntPtr pStmt, int colIndex, double data);

         [DllImport(SQLite3DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
         public static extern int sqlite3_bind_int(IntPtr pStmt, int colIndex, int data);

         [DllImport(SQLite3DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
         public static extern int sqlite3_bind_int64(IntPtr pStmt, int colIndex, Int64 data);

         [DllImport(SQLite3DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
         public static extern int sqlite3_bind_null(IntPtr pStmt, int colIndex);

         [DllImport(SQLite3DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
         public static extern int sqlite3_bind_text(IntPtr pStmt, int colIndex, [MarshalAs(UnmanagedType.LPStr)] string data, int dataLen, IntPtr destructor);

         [DllImport(SQLite3DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
         public static extern int sqlite3_bind_text16(IntPtr pStmt, int colIndex, [MarshalAs(UnmanagedType.LPWStr)] string data, int dataLen, IntPtr destructor);
         #endregion
         

         #region Data Fetching
         [DllImport(SQLite3DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
         public static extern int sqlite3_column_count(IntPtr pStmt);

         [DllImport(SQLite3DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
         public static extern int sqlite3_column_type(IntPtr pStmt, int iCol);

         [DllImport(SQLite3DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
         public static extern IntPtr sqlite3_column_blob(IntPtr pStmt, int iCol);

         [DllImport(SQLite3DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
         public static extern int sqlite3_column_bytes(IntPtr pStmt, int iCol);

         [DllImport(SQLite3DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
         public static extern int sqlite3_column_bytes16(IntPtr pStmt, int iCol);

         [DllImport(SQLite3DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
         public static extern double sqlite3_column_double(IntPtr pStmt, int iCol);

         [DllImport(SQLite3DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
         public static extern int sqlite3_column_int(IntPtr pStmt, int iCol);

         [DllImport(SQLite3DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
         public static extern Int64 sqlite3_column_int64(IntPtr pStmt, int iCol);

         [DllImport(SQLite3DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
         public static extern IntPtr sqlite3_column_text(IntPtr pStmt, int iCol);

         [DllImport(SQLite3DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
         public static extern IntPtr sqlite3_column_text16(IntPtr pStmt, int iCol);
         #endregion


         #region Database Operation
         [DllImport(SQLite3DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
         public static extern int sqlite3_changes(IntPtr db);

         [DllImport(SQLite3DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
         public static extern int sqlite3_total_changes(IntPtr db);

         [DllImport(SQLite3DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
         public static extern Int64 sqlite3_last_insert_rowid(IntPtr db);
         #endregion
      }
   }
}

