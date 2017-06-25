using System;
using System.Collections.Generic;
using System.Text;

namespace com.magicsoftware.util
{
   public enum StorageAttribute
   {
      NONE = ' ',
      ALPHA = 'A',
      NUMERIC = 'N',
      DATE = 'D',
      TIME = 'T',
      BOOLEAN = 'B',
      MEMO = 'M',
      BLOB = 'O',
      BLOB_VECTOR = 'V',
      UNICODE = 'U',
      SKIP = '0',
      DOTNET = 'E'
   }

   /// <summary>
   /// type checking for enum 'StorageAttribute'
   /// </summary>
   public static class StorageAttributeCheck
   {
      /// <summary>
      ///   is the both types belong to the same inner data types
      /// </summary>
      /// <param name = "type1">data type</param>
      /// <param name = "type2">data type</param>
      public static bool isTheSameType(StorageAttribute type1, StorageAttribute type2)
      {
         if (type1 == type2 || (isTypeNumeric(type1) && isTypeNumeric(type2)) ||
             (isTypeLogical(type1) && isTypeLogical(type2)) ||
             (IsTypeAlphaOrUnicode(type1) && IsTypeAlphaOrUnicode(type2)) ||
             (isTypeBlob(type1) && isTypeBlob(type2)) || (isTypeDotNet(type1) && isTypeDotNet(type2)))
            return true;
         return false;
      }

      public static bool isTypeBlob(StorageAttribute type)
      {
         return (type == StorageAttribute.BLOB || type == StorageAttribute.BLOB_VECTOR);
      }

      public static bool isTypeAlpha(StorageAttribute type)
      {
         return (type == StorageAttribute.ALPHA || type == StorageAttribute.MEMO);
      }

      /// <summary>
      ///   is the both types belong to the NUMERIC inner type
      /// </summary>
      public static bool isTypeNumeric(StorageAttribute type)
      {
         return (type == StorageAttribute.DATE || type == StorageAttribute.TIME ||
                 type == StorageAttribute.NUMERIC);
      }

      /// <summary>
      ///   is the both types belong to the LOGICAL inner type
      /// </summary>
      public static bool isTypeLogical(StorageAttribute type)
      {
         return (type == StorageAttribute.BOOLEAN);
      }

      /// <summary>
      ///   is the type is DOTNET
      /// </summary>
      public static bool isTypeDotNet(StorageAttribute type)
      {
         return (type == StorageAttribute.DOTNET);
      }

      /// <summary>
      ///   is the type ALPHA or UNICODE
      /// </summary>
      /// <param name = "type">data type</param>
      public static bool IsTypeAlphaOrUnicode(StorageAttribute type)
      {
         return (type == StorageAttribute.ALPHA || type == StorageAttribute.UNICODE);
      }

      /// <summary>
      ///   is the inner type ALPHA or UNICODE
      /// </summary>
      /// <param name = "type1">data type</param>
      /// <param name = "type2">data type</param>
      public static bool StorageFldAlphaOrUnicode(StorageAttribute type1, StorageAttribute type2)
      {
         return (IsTypeAlphaOrUnicode(type1) && IsTypeAlphaOrUnicode(type2));
      }

      public static bool StorageFldAlphaUnicodeOrBlob(StorageAttribute type1, StorageAttribute type2)
      {
         bool type1AlphaOrUnicode = IsTypeAlphaOrUnicode(type1);
         bool type2AlphaOrUnicode = IsTypeAlphaOrUnicode(type2);

         if (type1AlphaOrUnicode && type2AlphaOrUnicode)
            return true;

         bool type1Blob = (type1 == StorageAttribute.BLOB);
         bool type2Blob = (type2 == StorageAttribute.BLOB);

         return ((type1AlphaOrUnicode && type2Blob) || (type2AlphaOrUnicode && type1Blob));
      }

      /// <summary>
      /// Check if types are compatible or not.
      /// </summary>
      /// <param name="sourceAttribute"></param>
      /// <param name="destinationAttribute"></param>
      /// <returns></returns>
      public static bool IsTypeCompatibile(StorageAttribute sourceAttribute, StorageAttribute destinationAttribute)
      {
         bool isTypeCompatible = false;
         switch (sourceAttribute)
         {
            case StorageAttribute.ALPHA:
            case StorageAttribute.UNICODE:
               if (destinationAttribute == StorageAttribute.ALPHA || destinationAttribute == StorageAttribute.UNICODE ||
                   destinationAttribute == StorageAttribute.DATE || destinationAttribute == StorageAttribute.TIME ||
                   destinationAttribute == StorageAttribute.NUMERIC)
               {
                  isTypeCompatible = true;
               }
               break;
            case StorageAttribute.NUMERIC:
               if (destinationAttribute == StorageAttribute.NUMERIC || destinationAttribute == StorageAttribute.ALPHA ||
                   destinationAttribute == StorageAttribute.UNICODE || destinationAttribute == StorageAttribute.BOOLEAN ||
                   destinationAttribute == StorageAttribute.DATE || destinationAttribute == StorageAttribute.TIME)
               {
                  isTypeCompatible = true;
               }
               break;
            case StorageAttribute.BOOLEAN:
               if (destinationAttribute == StorageAttribute.BOOLEAN || destinationAttribute == StorageAttribute.ALPHA ||
                   destinationAttribute == StorageAttribute.UNICODE || destinationAttribute == StorageAttribute.NUMERIC)
               {
                  isTypeCompatible = true;
               }
               break;
            case StorageAttribute.DATE:
            case StorageAttribute.TIME:
               if (destinationAttribute == StorageAttribute.ALPHA || destinationAttribute == StorageAttribute.UNICODE ||
                   destinationAttribute == StorageAttribute.NUMERIC || destinationAttribute == StorageAttribute.BOOLEAN ||
                   destinationAttribute == StorageAttribute.DATE || destinationAttribute == StorageAttribute.TIME)
               {
                  isTypeCompatible = true;
               }
               break;
            case StorageAttribute.BLOB:
               if (destinationAttribute == StorageAttribute.BLOB)
               {
                  isTypeCompatible = true;
               }
               break;
         }

         return isTypeCompatible;
      }

   }
}
