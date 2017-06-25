using com.magicsoftware.util;
using com.magicsoftware.gatewaytypes.data;
using com.magicsoftware.gatewaytypes;


namespace MgSqlite.src
{
   /// <summary>
   ///  
   /// </summary>
   public class Sql3Field
   {
      internal Sql3SqlVar SqlVar;

      internal DataSourceDefinition DataSourceDefinition;

      internal string Name;
      internal object Buf;

      internal FldStorage Storage;

      internal GatewayAdapterCursor GatewayAdapterCursor;
      internal int FieldLen; // changed from short 
      internal int NullIndicator;
      internal int PartOfDateTime; // '-1' - Normal Field, '-2' - Time part of Datetime, 'N' - idx of Time part for Date part of Datetime 
      internal int Whole;
      internal int Dec;
      internal DBField Fld;
      internal bool AllowNull;

      internal bool IsBlob()
      {
         if (Storage == FldStorage.Blob || Storage == FldStorage.AnsiBlob||
             Storage == FldStorage.UnicodeBlob)
            return true;
         else
            return false;
      }

   }
}
