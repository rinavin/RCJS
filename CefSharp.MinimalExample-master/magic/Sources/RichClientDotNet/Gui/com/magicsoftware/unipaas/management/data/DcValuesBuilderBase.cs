using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.util;

namespace com.magicsoftware.unipaas.management.data
{
   /// <summary>
   /// Base class for DcValues builder. The class is abstract and, thus, must be inherited.
   /// The inheriting class, which will usually be in another package, will not be able to modify
   /// a DcValues object directly, since all its setter methods are 'internal'. This class will
   /// provide the setter methods for the inheriting builder.
   /// <para>Here's a usage example:
   /// <example>
   /// class MyConcreteBuilder : DcValuesBuilderBase
   /// {
   ///   public override DcValues Build()
   ///   {
   ///      var dcValues = base.CreateDcValues(false);
   ///      SetId(newId);
   ///      SetType(theType);
   ///      SetDisplayValues(theDisplayValues);
   ///      ...
   ///      return dcValues;
   ///   }      
   /// }
   /// </example>
   /// </summary>
   public abstract class DcValuesBuilderBase
   {
      public abstract DcValues Build();

      protected DcValues CreateDcValues(bool isVector)
      {
         return new DcValues(false, isVector);
      }

      protected void SetId(DcValues dcValues, int newId)
      {
         dcValues.SetID(newId);
      }

      protected void SetType(DcValues dcValues, StorageAttribute type)
      {
         dcValues.setType(type);
      }

      protected void SetDisplayValues(DcValues dcValues, string[] displayValues)
      {
         dcValues.SetDisplayValues(displayValues);
      }

      protected void SetLinkValues(DcValues dcValues, string[] linkValues)
      {
         dcValues.SetLinkValues(linkValues);
      }

      protected void SetNullFlags(DcValues dcValues, string nullFlagsString)
      {
         bool[] nullFlags = ParseNullFlags(nullFlagsString);
         dcValues.setNullFlags(nullFlags);
      }

      protected void SetNullFlags(DcValues dcValues, bool[] nullFlags)
      {
         dcValues.setNullFlags(nullFlags);
      }

      /// <summary>
      /// Parse a serialized values string as an array of values.
      /// </summary>
      /// <param name="valueStr">This is string of the concatenated values, with each value preceded with a hex number denoting its length.</param>
      /// <param name="dataType">The data type of the values.</param>
      /// <param name="useHex"></param>
      protected String[] ParseValues(String valueStr, StorageAttribute dataType, bool useHex)
      {
         String snumber;
         String tmpValue = null;
         List<String> buffer = new List<String>();
         String[] array;
         int len;
         int nextIdx;
         int endIdx;
         StringBuilder suffixBuf = null;

         switch (dataType)
         {
            case StorageAttribute.NUMERIC:
            case StorageAttribute.DATE:
            case StorageAttribute.TIME:
               if (useHex)
                  len = Manager.Environment.GetSignificantNumSize() * 2;
               else
                  len = (Manager.Environment.GetSignificantNumSize() + 2) / 3 * 4;
               break;

            case StorageAttribute.BOOLEAN:
               len = 1;
               break;

            default:
               len = 0;
               break;
         }

         nextIdx = 0;
         while (nextIdx < valueStr.Length)
         {
            // compute the length of an alpha data
            if (dataType == StorageAttribute.ALPHA || dataType == StorageAttribute.UNICODE ||
                dataType == StorageAttribute.BLOB || dataType == StorageAttribute.BLOB_VECTOR)
            {
               snumber = valueStr.Substring(nextIdx, 4);
               len = Convert.ToInt32(snumber, 16);
               nextIdx += 4;
            }

            // check if the data is spanned
            if (len >= 0x8000)
            {
               suffixBuf = new StringBuilder();
               len -= 0x8000;
               // next segment may also be spanned
               nextIdx += RecordUtils.getSpannedField(valueStr, len, nextIdx, dataType, suffixBuf, useHex);
               buffer.Add(suffixBuf.ToString());
            }
            else
            {
               endIdx = nextIdx + len;
               if (useHex)
                  tmpValue = valueStr.Substring(nextIdx, (endIdx) - (nextIdx));
               else
                  tmpValue = Base64.decodeToHex(valueStr.Substring(nextIdx, (endIdx) - (nextIdx)));
               // the following two lines are redundant since the makePrintable() is called
               // in the guiManager when building the options
               /*if (dataType == DATA_TYPE_ALPHA)
                    tmpValue = guiManager.makePrintable(tmpValue);*/
               buffer.Add(tmpValue);
               nextIdx = endIdx;
            }
         }

         // vector to array
         array = new String[buffer.Count];
         buffer.CopyTo(array);
         return array;
      }

      /// <summary>
      /// Parses a string of 0 and 1 into an array of booleans.
      /// </summary>
      /// <param name="nullFlagsString">The string boolean flags. For example: "000100001"</param>
      /// <returns>An array of boolean items, with one item for each character in the string, set to false if the corresponding
      /// character is set to 0 and true otherwise.</returns>
      protected bool[] ParseNullFlags(string nullFlagsString)
      {
         bool[] nullFlags = new bool[nullFlagsString.Length];
         for (int i = 0; i < nullFlagsString.Length; i++)
            nullFlags[i] = (nullFlagsString[i] != '0');
         return nullFlags;
      }
   }
}
