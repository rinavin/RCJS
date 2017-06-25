using System;
using System.Text;
using System.Reflection;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.unipaas.dotnet;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.unipaas.management.exp;

namespace com.magicsoftware.unipaas.dotnet
{
   /// <summary>
   /// This static class handles all conversion between magic types and dotnet,
   /// and also take care of casting dotnet variables between different types.
   /// </summary>
   public static class DNConvert
   {
      /// <summary>
      /// converts magic value into a corrosponding dotnet object, as defined by the table below
      /// 
      /// 1. NUMERIC to - SByte, Byte, Int16, Uint16, Int32, UInt32, Int64, UInt64, IntPtr, UIntPtr, Char, Decimal, Single, Double, Float
      /// 2. ALPHA, UNICODE to - Char, Char[], String, StringBuilder
      /// 3. DATE to - DateTime
      /// 4. TIME to - DateTime, TimeSpan
      /// 5. LOGICAL to - Boolean
      /// 6. BLOB to - Byte, Byte[], Char, Char[], String, StringBuilder
      /// 7. VECTOR to - jagged Array
      /// 8. DOTNET to - Object
      /// 
      /// </summary>
      /// <param name="magicVal">the string value in magic type</param>
      /// <param name="magicType">Type of magicVal</param>
      /// <param name="dotNetType">DotNet type</param>
      /// <returns></returns>
      public static Object convertMagicToDotNet(String magicVal, StorageAttribute magicType, Type dotNetType)
      {
         Object retObject = null;

         // if the dotnettype to convert is System.Object, we should get convert magicVal to default
         // dotnet type based on 'magicVal'.
         if (dotNetType == typeof(Object))
            dotNetType = getDefaultDotNetTypeForMagicType(magicVal, magicType);

         try
         {
            switch (magicType)
            {
               case StorageAttribute.NUMERIC:
                  // NUMERIC Magic type conversion            
                  retObject = convertNumericToDotNet(magicVal, dotNetType);
                  break;

               case StorageAttribute.ALPHA:
               case StorageAttribute.UNICODE:
                  // ALPHA UNICODE Magic type conversion
                  retObject = convertAlphaUnicodeToDotNet(magicVal, dotNetType);
                  break;

               case StorageAttribute.DATE:
                  // DATE Magic type conversion
                  retObject = convertDateToDotNet(magicVal, dotNetType);
                  break;

               case StorageAttribute.TIME:
                  // TIME Magic type conversion
                  retObject = convertTimeToDotNet(magicVal, dotNetType);
                  break;

               case StorageAttribute.BOOLEAN:
                  // LOGICAL BOOLEAN Magic type conversion
                  retObject = convertLogicalBooleanToDotNet(magicVal, dotNetType);
                  break;

               case StorageAttribute.BLOB:
                  // BLOB Magic type conversion
                  retObject = convertBlobToDotNet(magicVal, dotNetType);
                  break;

               case StorageAttribute.BLOB_VECTOR:
                  // VECTOR Magic type conversion          
                  retObject = convertVectorToDotNet(magicVal, dotNetType);
                  break;

               case StorageAttribute.DOTNET:
                  // DOTNET Magic type conversion
                  retObject = convertDotNetMagicToDotNet(magicVal, dotNetType);
                  break;

               default:
                  break;
            }
         }
         catch (Exception exception)
         {
            DNException dnException = Manager.GetCurrentRuntimeContext().DNException;
            dnException.set(exception);
            throw dnException;
         }

         return retObject;
      }

      /// <summary>
      /// Converts Magic 'Numeric' Type to 'dotNetType'
      /// </summary>
      /// <param name="magicVal"></param>
      /// <param name="dotNetType"></param>
      /// <returns></returns>
      private static Object convertNumericToDotNet(String magicVal, Type dotNetType)
      {
         Object retObject = null;
         NUM_TYPE numType = new NUM_TYPE(magicVal);
         double doubleVal = 0.0;

         if (dotNetType == typeof(SByte) || dotNetType == typeof(Byte) || dotNetType == typeof(Int16)
            || dotNetType == typeof(UInt16) || dotNetType == typeof(Int32) || dotNetType == typeof(UInt32)
            || dotNetType == typeof(Int64) || dotNetType == typeof(UInt64) || dotNetType == typeof(float)
            || dotNetType == typeof(Decimal) || dotNetType == typeof(Single) || dotNetType == typeof(Double))
         {
            doubleVal = numType.to_double();
            retObject = ReflectionServices.DynCast(doubleVal, dotNetType);
         }
         else
         {
            doubleVal = numType.to_double();

            if (dotNetType == typeof(Char))
            {
               retObject = ReflectionServices.DynCast((Int64)doubleVal, dotNetType);
            }
            else if (dotNetType == typeof(IntPtr))
            {
               retObject = new IntPtr((long)doubleVal);
            }
            else if (dotNetType == typeof(UIntPtr))
            {
               retObject = new UIntPtr((ulong)doubleVal);
            }
         }

         return retObject;
      }

      /// <summary>
      /// Converts Magic 'Alpha' 'Unicode' Type to 'dotNetType'
      /// </summary>
      /// <param name="magicVal"></param>
      /// <param name="dotNetType"></param>
      /// <returns></returns>
      private static object convertAlphaUnicodeToDotNet(string magicVal, Type dotNetType)
      {
         Object retObject = null;

         if (magicVal != null)
         {
            // remove trailing white-space chars
            magicVal = magicVal.TrimEnd(' ');

            if (dotNetType == typeof(Char))
            {
               retObject = ReflectionServices.DynCast(magicVal, dotNetType);
            }
            else if (dotNetType == typeof(String))
            {
               retObject = magicVal;
            }
            else if (dotNetType == typeof(Char[]))
            {
               retObject = magicVal.ToCharArray();
            }
            else if (dotNetType == typeof(StringBuilder))
            {
               retObject = new StringBuilder(magicVal);
            }
         }
         return retObject;
      }

      /// <summary>
      /// Converts Magic 'Date' Type to 'dotNetType'
      /// </summary>
      /// <param name="magicVal"></param>
      /// <param name="dotNetType"></param>
      /// <returns></returns>
      private static object convertDateToDotNet(string magicVal, Type dotNetType)
      {
         if (dotNetType == typeof(DateTime))
         {
            NUM_TYPE numType = new NUM_TYPE(magicVal);
            DisplayConvertor displayConvertor = DisplayConvertor.Instance;

            int date = numType.NUM_2_LONG();
            if (date == 0)
            {
               magicVal = FieldDef.getMagicDefaultValue(StorageAttribute.DATE);
               numType = new NUM_TYPE(magicVal);
               date = numType.NUM_2_LONG();
            }

            // Break date into its components - year month date
            DisplayConvertor.DateBreakParams breakParams = displayConvertor.getNewDateBreakParams();
            displayConvertor.date_break_datemode(breakParams, date, true, -1);
            int year = breakParams.year;
            int month = breakParams.month;
            int day = breakParams.day;

            return new DateTime(year, month, day);
         }
         else
            return null;
      }

      /// <summary>
      /// Converts Magic 'Time' Type to 'dotNetType'
      /// </summary>
      /// <param name="magicVal"></param>
      /// <param name="dotNetType"></param>
      /// <returns></returns>
      private static object convertTimeToDotNet(string magicVal, Type dotNetType)
      {
         Object retObject = null;

         if (dotNetType == typeof(DateTime) || dotNetType == typeof(TimeSpan))
         {
            NUM_TYPE numType = new NUM_TYPE(magicVal);
            DisplayConvertor displayConvertor = DisplayConvertor.Instance;

            int time = numType.NUM_2_ULONG();

            // Break time into its components - hour min sec
            DisplayConvertor.TimeBreakParams breakParams = displayConvertor.getNewTimeBreakParams();
            DisplayConvertor.time_break(breakParams, time);
            int hour = breakParams.hour;
            int minute = breakParams.minute;
            int second = breakParams.second;

            if (dotNetType == typeof(DateTime))
            {
               DateTime dateTimeObj = new DateTime();

               dateTimeObj = dateTimeObj.AddHours(hour);
               dateTimeObj = dateTimeObj.AddMinutes(minute);
               dateTimeObj = dateTimeObj.AddSeconds(second);

               retObject = dateTimeObj;
            }
            else if (dotNetType == typeof(TimeSpan))
               retObject = new TimeSpan(hour, minute, second);
         }

         return retObject;
      }

      /// <summary>
      /// Converts Magic 'Logical' 'Boolean' Type to 'dotNetType'
      /// </summary>
      /// <param name="magicVal"></param>
      /// <param name="dotNetType"></param>
      /// <returns></returns>
      private static object convertLogicalBooleanToDotNet(string magicVal, Type dotNetType)
      {
         if (dotNetType == typeof(Boolean))
            return magicVal.Equals("1");
         else
            return null;
      }

      /// <summary>
      /// Converts Magic 'Blob' Type to 'dotNetType'
      /// </summary>
      /// <param name="magicVal"></param>
      /// <param name="dotNetType"></param>
      /// <returns></returns>
      private static object convertBlobToDotNet(string magicVal, Type dotNetType)
      {
         Object retObject = null;

         if (dotNetType == typeof(Byte))
         {
            retObject = BlobType.getBytes(magicVal)[0];
         }
         else if (dotNetType == typeof(Byte[]))
         {
            retObject = BlobType.getBytes(magicVal);
         }
         else if (dotNetType == typeof(Char))
         {
            retObject = ReflectionServices.DynCast(BlobType.getString(magicVal), dotNetType);
         }
         else if (dotNetType == typeof(Char[]))
         {
            retObject = BlobType.getString(magicVal).ToCharArray();
         }
         else if (dotNetType == typeof(String))
         {
            retObject = BlobType.getString(magicVal);
         }
         else if (dotNetType == typeof(StringBuilder))
         {
            retObject = new StringBuilder(BlobType.getString(magicVal));
         }

         return retObject;
      }

      /// <summary>
      /// gets the default dotnet type for vector
      /// </summary>
      /// <param name="vector"></param>
      /// <returns></returns>
      private static Type getDefaultDotNetTypeForVector(VectorType vector)
      {
         StorageAttribute vecCellAttr = vector.getCellsAttr();
         Type dotNetType = null;

         switch (vecCellAttr)
         {
            case StorageAttribute.NUMERIC:
               dotNetType = typeof(Double);
               break;

            case StorageAttribute.ALPHA:
            case StorageAttribute.UNICODE:
               dotNetType = typeof(String);
               break;

            case StorageAttribute.DATE:
               dotNetType = typeof(DateTime);
               break;

            case StorageAttribute.TIME:
               dotNetType = typeof(TimeSpan);
               break;

            case StorageAttribute.BOOLEAN:
               dotNetType = typeof(Boolean);
               break;

            case StorageAttribute.BLOB:
               if (vector.getVecSize() > 0 && BlobType.getContentType(vector.getVecCell(1)) == BlobType.CONTENT_TYPE_BINARY)
                  dotNetType = typeof(Byte[]);
               else
                  dotNetType = typeof(String);
               break;

            case StorageAttribute.BLOB_VECTOR:
               dotNetType = typeof(Array);
               break;

            case StorageAttribute.DOTNET:
               dotNetType = typeof(Object);
               break;
         }

         return dotNetType;
      }

      /// <summary>
      /// Converts Magic 'Vector' Type to 'dotNetType'
      /// </summary>
      /// <param name="magicVal"></param>
      /// <param name="dotNetType"></param>
      /// <returns></returns>
      private static object convertVectorToDotNet(string magicVal, Type dotNetType)
      {
         Object retObject = null;
         VectorType vector = new VectorType(magicVal);
         StorageAttribute vecCellAttr = VectorType.getCellsAttr(magicVal);
         int vecSize = (int)VectorType.getVecSize(magicVal);
         Type arrayType = null;
         String vecCellMagicFormat;
         Object vecCellDotNetFormat;

         // get the default conversion Type
         arrayType = getDefaultDotNetTypeForVector(vector);

         if (arrayType != null)
         {
            // construct the arrayObject
            Array arrayObj = ReflectionServices.CreateArrayInstance(arrayType, new int[] { vecSize });

            for (int idx = 0; idx < vecSize; idx++)
            {
               vecCellMagicFormat = vector.getVecCell(idx + 1);
               vecCellDotNetFormat = convertMagicToDotNet(vecCellMagicFormat, vecCellAttr, arrayType);
               arrayObj.SetValue(vecCellDotNetFormat, idx);
            }

            // perform cast into 'dotNetType'
            retObject = doCast(arrayObj, dotNetType);
         }

         return retObject;
      }

      /// <summary>
      /// Converts Magic 'DotNet' Type to 'dotNetType'
      /// </summary>
      /// <param name="magicVal"></param>
      /// <param name="dotNetType"></param>
      /// <returns></returns>
      private static object convertDotNetMagicToDotNet(string magicVal, Type dotNetType)
      {
         if (dotNetType == typeof(Object))
         {
            // From blobstr, get the key. Then get object from DNObjectsCollection, clone it and return it.
            int key = BlobType.getKey(magicVal);
            Object obj = DNManager.getInstance().DNObjectsCollection.GetDNObj(key);

            return obj;
         }
         else
            return null;
      }

      /// <summary>
      /// Handles the conversion of Objects (both MagicTypes and DotNet) into the specified type.
      /// It works only for an ExpVal object (for MagicType) or a DotNet object.
      /// </summary>
      /// <param name="obj"></param>
      /// <param name="dotNetType"></param>
      /// <returns></returns>
      public static Object doCast(Object obj, Type dotNetType)
      {
         Object objToCast;
         Object castedObj = null;

         try
         {
            if (GuiExpressionEvaluator.isExpVal(obj))
               objToCast = GuiExpressionEvaluator.convertExpValToDotNet(obj, dotNetType);
            else
               objToCast = obj;

            castedObj = ReflectionServices.DynCast(objToCast, dotNetType);
         }
         catch (Exception exception)
         {
            DNException dnException = Manager.GetCurrentRuntimeContext().DNException;
            dnException.set(exception);
            throw dnException;
         }

         return castedObj;
      }

      /// <summary>
      /// Ref to coversion table in ConvertMagicToDotNet, checks if 'magicType' can be converted into 'dotNetType'
      /// </summary>
      /// <param name="magicType"></param>
      /// <param name="dotNetType"></param>
      /// <returns></returns>
      public static bool canConvert(StorageAttribute magicType, Type dotNetType)
      {
         bool canConvert = false;

         switch (magicType)
         {
            case StorageAttribute.NUMERIC:
               if (dotNetType == typeof(SByte) || dotNetType == typeof(Byte) || dotNetType == typeof(Int16)
                  || dotNetType == typeof(UInt16) || dotNetType == typeof(Int32) || dotNetType == typeof(UInt32)
                  || dotNetType == typeof(Int64) || dotNetType == typeof(UInt64) || dotNetType == typeof(float)
                  || dotNetType == typeof(Decimal) || dotNetType == typeof(Single) || dotNetType == typeof(Double)
                  || dotNetType == typeof(Char) || dotNetType == typeof(IntPtr) || dotNetType == typeof(UIntPtr))
                  canConvert = true;
               break;

            case StorageAttribute.ALPHA:
            case StorageAttribute.UNICODE:
               if (dotNetType == typeof(Char) || dotNetType == typeof(String) || dotNetType == typeof(Char[])
                  || dotNetType == typeof(StringBuilder))
                  canConvert = true;
               break;

            case StorageAttribute.DATE:
               if (dotNetType == typeof(DateTime))
                  canConvert = true;
               break;

            case StorageAttribute.TIME:
               if (dotNetType == typeof(TimeSpan))
                  canConvert = true;
               break;

            case StorageAttribute.BOOLEAN:
               if (dotNetType == typeof(Boolean))
                  canConvert = true;
               break;

            case StorageAttribute.BLOB:
               if (dotNetType == typeof(Byte) || dotNetType == typeof(Byte[]) || dotNetType == typeof(Char)
                  || dotNetType == typeof(Char[]) || dotNetType == typeof(String) || dotNetType == typeof(StringBuilder))
                  canConvert = true;
               break;

            case StorageAttribute.BLOB_VECTOR:
               if (dotNetType == typeof(Array))
                  canConvert = true;
               break;

            case StorageAttribute.DOTNET:
               if (dotNetType == typeof(Object))
                  canConvert = true;
               break;
         }

         return canConvert;
      }

      /// <summary>
      /// Ref to coversion table in ConvertMagicToDotNet, gets the default DotNetType for 'magicType'
      /// </summary>
      /// <param name="magicType"></param>
      /// <returns></returns>
      public static Type getDefaultDotNetTypeForMagicType(String magicVal, StorageAttribute magicType)
      {
         Type dotNetType = null;

         switch (magicType)
         {
            case StorageAttribute.NUMERIC:
               NUM_TYPE numType = new NUM_TYPE(magicVal);

               if (numType.NUM_IS_LONG())
                  dotNetType = typeof(long);
               else
                  dotNetType = typeof(Double);
               break;

            case StorageAttribute.ALPHA:
            case StorageAttribute.UNICODE:
               dotNetType = typeof(String);
               break;

            case StorageAttribute.DATE:
               dotNetType = typeof(DateTime);
               break;

            case StorageAttribute.TIME:
               dotNetType = typeof(TimeSpan);
               break;

            case StorageAttribute.BOOLEAN:
               dotNetType = typeof(Boolean);
               break;

            case StorageAttribute.BLOB:
               dotNetType = typeof(String);
               break;

            case StorageAttribute.BLOB_VECTOR:
               dotNetType = typeof(Array);
               break;

            case StorageAttribute.DOTNET:
               dotNetType = typeof(Object);
               break;

            default:
               break;
         }

         return dotNetType;
      }

      /// <summary>
      /// Ref to coversion table in ConvertMagicToDotNet, gets the default MagicType for 'dotnetType'
      /// </summary>
      /// <param name="dotnetType"></param>
      /// <returns></returns>
      public static StorageAttribute getDefaultMagicTypeForDotNetType(Type dotnetType)
      {
         StorageAttribute defMagicType = StorageAttribute.NONE;

         // System.String is always unicode, so we should check with Unicode not alpha
         if (canConvert(StorageAttribute.UNICODE, dotnetType))
            defMagicType = StorageAttribute.UNICODE;
         else if (canConvert(StorageAttribute.NUMERIC, dotnetType))
            defMagicType = StorageAttribute.NUMERIC;
         else if (canConvert(StorageAttribute.DATE, dotnetType))
            defMagicType = StorageAttribute.DATE;
         else if (canConvert(StorageAttribute.TIME, dotnetType))
            defMagicType = StorageAttribute.TIME;
         else if (canConvert(StorageAttribute.BOOLEAN, dotnetType))
            defMagicType = StorageAttribute.BOOLEAN;
         else if (canConvert(StorageAttribute.BLOB, dotnetType))
            defMagicType = StorageAttribute.BLOB;
         else if (canConvert(StorageAttribute.BLOB_VECTOR, dotnetType))
            defMagicType = StorageAttribute.BLOB_VECTOR;

         return defMagicType;
      }

      /// <summary>
      /// converts dotnet object into a corrosponding magic value.
      /// </summary>
      /// <param name="dotNetObj"></param>
      /// <param name="magicType"></param>
      /// <returns></returns>
      public static String convertDotNetToMagic(Object dotNetObj, StorageAttribute magicType)
      {
         String retStr = "";

         if (dotNetObj == null || magicType == StorageAttribute.NONE)
            return retStr;

         try
         {
            switch (magicType)
            {
               case StorageAttribute.NUMERIC:
                  // conversion to NUMERIC Magic type
                  retStr = convertDotNetToNumeric(dotNetObj);
                  break;

               case StorageAttribute.ALPHA:
               case StorageAttribute.UNICODE:
                  // conversion to ALPHA UNICODE Magic type
                  retStr = convertDotNetToAlphaUnicode(dotNetObj);
                  break;

               case StorageAttribute.DATE:
                  // conversion to DATE Magic type
                  retStr = convertDotNetToDate(dotNetObj);
                  break;

               case StorageAttribute.TIME:
                  // conversion to TIME Magic type
                  retStr = convertDotNetToTime(dotNetObj);
                  break;

               case StorageAttribute.BOOLEAN:
                  // conversion to LOGICAL BOOLEAN Magic type
                  retStr = convertDotNetToLogicalBoolean(dotNetObj);
                  break;

               case StorageAttribute.BLOB:
                  // conversion to BLOB Magic type
                  retStr = convertDotNetToBlob(dotNetObj);
                  break;

               case StorageAttribute.BLOB_VECTOR:
                  // conversion to VECTOR Magic type
                  retStr = convertDotNetToVector(dotNetObj);
                  break;

               case StorageAttribute.DOTNET:
                  // conversion to DOTNET Magic type - not allowed. This should be done only in ExpValToMgVal.
                  break;

               default:
                  break;
            }
         }
         catch (Exception exception)
         {
            DNException dnException = Manager.GetCurrentRuntimeContext().DNException;
            dnException.set(exception);
            throw dnException;
         }

         return retStr;
      }

      /// <summary>
      /// Converts 'dotNetObj' to 'Numeric' Magic type
      /// </summary>
      /// <param name="dotNetObj"></param>
      /// <returns></returns>
      private static string convertDotNetToNumeric(object dotNetObj)
      {
         Double doubleVal = 0.0;
         Type dotNetType = dotNetObj.GetType();
         NUM_TYPE numType;

         if (dotNetType.IsEnum)
            dotNetType = Enum.GetUnderlyingType(dotNetType);


         if (dotNetType == typeof(SByte) || dotNetType == typeof(Byte) || dotNetType == typeof(Int16)
            || dotNetType == typeof(UInt16) || dotNetType == typeof(Int32) || dotNetType == typeof(Char))
         {
            int intVal = (int)ReflectionServices.DynCast(dotNetObj, typeof(int));

            numType = new NUM_TYPE();
            numType.NUM_4_LONG(intVal);
         }
         else if (dotNetType == typeof(UInt32)
            || dotNetType == typeof(Int64) || dotNetType == typeof(UInt64) || dotNetType == typeof(float)
            || dotNetType == typeof(Decimal) || dotNetType == typeof(Single) || dotNetType == typeof(Double))
         {
            doubleVal = (Double)ReflectionServices.DynCast(dotNetObj, typeof(Double));

            numType = NUM_TYPE.from_double(doubleVal);
         }
         else
         {
            if (dotNetType == typeof(IntPtr))
            {
               doubleVal = (Double)((IntPtr)dotNetObj).ToInt64();
            }
            else if (dotNetType == typeof(UIntPtr))
            {
               doubleVal = (Double)((UIntPtr)dotNetObj).ToUInt64();
            }
            
            numType = NUM_TYPE.from_double(doubleVal);
         }

         return numType.toXMLrecord();
      }

      /// <summary>
      /// Converts 'dotNetObj' to 'Alpha' 'Unicode' Magic type
      /// </summary>
      /// <param name="dotNetObj"></param>
      /// <returns></returns>
      private static string convertDotNetToAlphaUnicode(object dotNetObj)
      {
         String retObject = "";
         Type dotNetType = dotNetObj.GetType();

         if (dotNetType == typeof(Char))
         {
            retObject = new String((Char)dotNetObj, 1);
         }
         else if (dotNetType == typeof(String))
         {
            retObject = (String)dotNetObj;
         }
         else if (dotNetType == typeof(Char[]))
         {
            retObject = new String((Char[])dotNetObj);
         }
         else if (dotNetType == typeof(StringBuilder))
         {
            retObject = ((StringBuilder)dotNetObj).ToString();
         }
         else
         {
            retObject = dotNetObj.ToString();
         }

         return retObject;

      }

      /// <summary>
      /// Converts 'dotNetObj' to 'Date' Magic type
      /// </summary>
      /// <param name="dotNetObj"></param>
      /// <returns></returns>
      private static string convertDotNetToDate(object dotNetObj)
      {
         String retObject = "";
         Type dotNetType = dotNetObj.GetType();

         if (dotNetType == typeof(DateTime))
         {
            DateTime date = (DateTime)dotNetObj;

            int magicDate = DisplayConvertor.Instance.date_4_calender(date.Year, date.Month, date.Day, 0, false);
            NUM_TYPE numtypeMagicDate = new NUM_TYPE();
            numtypeMagicDate.NUM_4_LONG(magicDate);

            retObject = numtypeMagicDate.toXMLrecord();
         }

         return retObject;
      }

      /// <summary>
      /// Converts 'dotNetObj' to 'Time' Magic type
      /// </summary>
      /// <param name="dotNetObj"></param>
      /// <returns></returns>
      private static string convertDotNetToTime(object dotNetObj)
      {
         String retObject = "";
         Type dotNetType = dotNetObj.GetType();

         if (dotNetType == typeof(DateTime))
         {
            DateTime date = (DateTime)dotNetObj;

            int magicTime = DisplayConvertor.Instance.time_2_int(date.Hour, date.Minute, date.Second);
            NUM_TYPE numtypeMagicTime = new NUM_TYPE();
            numtypeMagicTime.NUM_4_LONG(magicTime);

            retObject = numtypeMagicTime.toXMLrecord();
         }
         else if (dotNetType == typeof(TimeSpan))
         {
            TimeSpan time = (TimeSpan)dotNetObj;

            int magicTime = DisplayConvertor.Instance.time_2_int(time.Hours, time.Minutes, time.Seconds);
            NUM_TYPE numtypeMagicTime = new NUM_TYPE();
            numtypeMagicTime.NUM_4_LONG(magicTime);

            retObject = numtypeMagicTime.toXMLrecord();
         }

         return retObject;
      }

      /// <summary>
      /// Converts 'dotNetObj' to 'Logical' 'Boolean' Magic type
      /// </summary>
      /// <param name="dotNetObj"></param>
      /// <returns></returns>
      private static string convertDotNetToLogicalBoolean(object dotNetObj)
      {
         String retObject = "";
         Type dotNetType = dotNetObj.GetType();

         if (dotNetType == typeof(Boolean))
         {
            Boolean boolVal = (Boolean)dotNetObj;
            if (boolVal == true)
               retObject = "1";
            else
               retObject = "0";
         }

         return retObject;
      }

      /// <summary>
      /// Converts 'dotNetObj' to 'Blob' Magic type
      /// </summary>
      /// <param name="dotNetObj"></param>
      /// <returns></returns>
      private static string convertDotNetToBlob(object dotNetObj)
      {
         String blob = BlobType.createFromString("", BlobType.CONTENT_TYPE_UNKNOWN);
         Type dotNetType = dotNetObj.GetType();

         if (dotNetType == typeof(Byte))
         {
            Byte[] bytes = new Byte[] { (Byte)dotNetObj };
            blob = BlobType.createFromBytes(bytes, BlobType.CONTENT_TYPE_BINARY);
         }
         else if (dotNetType == typeof(Byte[]))
         {
            Byte[] bytes = (Byte[])dotNetObj;
            blob = BlobType.createFromBytes(bytes, BlobType.CONTENT_TYPE_BINARY);
         }
         else if (dotNetType == typeof(Char))
         {
            String blobStr = new String((Char)dotNetObj, 1);
            blob = BlobType.createFromString(blobStr, BlobType.CONTENT_TYPE_UNICODE);
         }
         else if (dotNetType == typeof(Char[]))
         {
            String blobStr = new String((Char[])dotNetObj);
            blob = BlobType.createFromString(blobStr, BlobType.CONTENT_TYPE_UNICODE);
         }
         else if (dotNetType == typeof(String))
         {
            blob = BlobType.createFromString((String)dotNetObj, BlobType.CONTENT_TYPE_UNICODE);
         }
         else if (dotNetType == typeof(StringBuilder))
         {
            String blobStr = ((StringBuilder)dotNetObj).ToString();
            blob = BlobType.createFromString(blobStr, BlobType.CONTENT_TYPE_UNICODE);
         }

         return blob;
      }

      /// <summary>
      /// Converts 'dotNetObj' to 'Vector' Magic type
      /// </summary>
      /// <param name="dotNetObj"></param>
      /// <returns></returns>
      private static string convertDotNetToVector(object dotNetObj)
      {
         String retObject = "";
         Type dotNetType = dotNetObj.GetType();

         if (dotNetType.IsArray)
         {
            Array arrObj = (Array)dotNetObj;
            Type arrCellType = arrObj.GetType().GetElementType();
            StorageAttribute vecCellType = getDefaultVecCellTypeForDotNet(arrCellType);

            if (vecCellType != StorageAttribute.NONE)
            {
               VectorType vector;
               String[] magicVals = new String[arrObj.Length];
               long maxLength = 0;

               // convert each element of array into magic equivalent and store it in temp string array.
               for (int idx = 0; idx < arrObj.Length; idx++)
               {
                  magicVals[idx] = convertDotNetToMagic(arrObj.GetValue(idx), vecCellType);

                  // evaluate max length
                  if (magicVals[idx].Length > maxLength)
                     maxLength = magicVals[idx].Length;
               }

               if (vecCellType == StorageAttribute.BLOB)
                  maxLength = 28; // length for a blob is always 28

               // find cellContentType and create a vector
               char cellContentType = BlobType.CONTENT_TYPE_UNKNOWN;
               if (vecCellType == StorageAttribute.BLOB && magicVals.Length > 0)
                  cellContentType = BlobType.getContentType(magicVals[0]);
               vector = new VectorType(vecCellType, cellContentType, "", true, true, maxLength);

               // set the temp string array into vector
               for (int idx = 0; idx < arrObj.Length; idx++)
                  vector.setVecCell(idx + 1, magicVals[idx], false);

               // get the flattened string
               retObject = vector.ToString();
            }
         }

         return retObject;
      }

      /// <summary>
      /// get the default Vec cell type for DotNet type
      /// </summary>
      /// <param name="type"></param>
      /// <returns></returns>
      private static StorageAttribute getDefaultVecCellTypeForDotNet(Type type)
      {
         StorageAttribute vecCellAttr = StorageAttribute.NONE;

         if (type == typeof(SByte) || type == typeof(Byte) || type == typeof(Int16)
            || type == typeof(UInt16) || type == typeof(Int32) || type == typeof(UInt32)
            || type == typeof(Int64) || type == typeof(UInt64) || type == typeof(float)
            || type == typeof(Decimal) || type == typeof(Single) || type == typeof(Double)
            || type == typeof(IntPtr) || type == typeof(UIntPtr))
            vecCellAttr = StorageAttribute.NUMERIC;

         else if (type == typeof(Char) || type == typeof(String) || type == typeof(StringBuilder))
            vecCellAttr = StorageAttribute.UNICODE;

         else if (type == typeof(DateTime))
            vecCellAttr = StorageAttribute.DATE;

         else if (type == typeof(TimeSpan))
            vecCellAttr = StorageAttribute.TIME;

         else if (type == typeof(Boolean))
            vecCellAttr = StorageAttribute.BOOLEAN;

         /// TODO : need to decide the conversion for char[]. Currently converted into Array of Unicode
         /// else if (type == typeof(Char[]))

         else if (type == typeof(Byte[]))
            vecCellAttr = StorageAttribute.BLOB;

         else if (type == typeof(Array))
            vecCellAttr = StorageAttribute.BLOB_VECTOR;

         else if (type == typeof(Object))
            vecCellAttr = StorageAttribute.DOTNET;

         return vecCellAttr;
      }
      
      /// <summary>
      /// returns a shallow copy of the object
      /// </summary>
      /// <param name="obj"></param>
      /// <returns></returns>
      public static Object doShallowCopy(Object obj)
      {
         return getShallowCopy(obj);
      }

      /// <summary>
      /// public function that performs shallow copy
      /// </summary>
      /// <param name="ObjFrm"></param>
      /// <returns></returns>
      private static Object getShallowCopy(Object ObjFrm)
      {
         FieldInfo[] fieldInfos;
         PropertyInfo[] propertyInfos;
         Object valueToSet;
         Object ObjTo;

         ObjTo = ReflectionServices.CreateInstance(ObjFrm.GetType(), null, null);

         if (ObjFrm == null && ObjTo == null && ObjFrm.GetType() != ObjTo.GetType())
            return null;

         // get all the fields
         fieldInfos = ObjFrm.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

         foreach (FieldInfo fieldInfo in fieldInfos)
         {
            try
            {
               valueToSet = fieldInfo.GetValue(ObjFrm);
               if (valueToSet != null)
                  fieldInfo.SetValue(ObjTo, valueToSet);
            }
            catch (Exception)
            { }
         }

         // get all the properties
         propertyInfos = ObjFrm.GetType().GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

         foreach (PropertyInfo propertyInfo in propertyInfos)
         {
            try
            {
               if (propertyInfo.GetIndexParameters().Length == 0)
               {
                  valueToSet = propertyInfo.GetValue(ObjFrm, null);
                  if (valueToSet != null)
                     propertyInfo.SetValue(ObjTo, valueToSet, null);
               }
            }
            catch (Exception)
            { }
         }

         return ObjTo;
      }
   }

}
