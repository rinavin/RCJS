using com.magicsoftware.util;
using com.magicsoftware.gatewaytypes.data;

namespace MgSqlite.src
{
   /// <summary>
   ///  This class represents a single cursor operation read/insert/range.
   /// </summary>
   public class SQL3Dbd
   {
      public string TableName,
                    DatabaseName,
                    FullName;

      public bool   IsView,        
                    IsUnique,
                    IsPhysicalLock;

      public Access   access;
      public DbOpen   mode;
      public DbShare  share;

      public int arrayBufferSize,
                    posLen,
                    flds;

      public DataSourceDefinition DataSourceDefinition;
      public DBField identityFld, // the field idx of an identity column if exists, else null.
              magicFld;
     

      public SQL3Dbd()
      {
      }
      public void SetFullName ()
      {
         FullName = string.Concat(FullName, TableName);
      }
      public bool IsLoaclTempTable()
      {
         return TableName[0] == '#' ||
            (TableName.Length > 1 && TableName[1] != '#');
      }
   }
}
