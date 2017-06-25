using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.util;

namespace com.magicsoftware.unipaas.management.data
{
   public class DcValues : IReferencedObject
   {
      public const int EMPTY_DCREF = -2;
      internal const int NOT_FOUND = GuiConstants.DEFAULT_VALUE_INT;

      private int _id = 0;
      private StorageAttribute _type; // Alpha | Numeric | Date | Time | Logical
      private int _refCount; // references counter
      private bool[] _nullFlags; // linked null value flags
      private bool _isNumericType; // true if the type is one of: numeric, date, time
      private NUM_TYPE[] _numVals; // numeric linked values
      private String[] _linkVals; // linked values
      private String[] _dispVals; // display values

      /// <summary>
      /// 
      /// </summary>
      /// <param name="empty"></param>
      /// <param name="isVector"></param>
      public DcValues(bool empty, bool isVector)
      {
         if (empty)
            _id = EMPTY_DCREF;
         _isNumericType = false;
      }

      /// <summary>
      /// Returns the attr of DcValues
      /// </summary>
      public StorageAttribute GetAttr()
      {
         return _type;
      }

      internal void setType(StorageAttribute type)
      {
         _type = type;
         if (_type == StorageAttribute.MEMO)
            _type = StorageAttribute.ALPHA;
         if (_type == StorageAttribute.NUMERIC || _type == StorageAttribute.DATE ||
             _type == StorageAttribute.TIME)
            _isNumericType = true;

      }

      internal void SetID(int newId)
      {
         _id = newId;
      }

      internal void SetDisplayValues(string[] displayValues)
      {
         if (displayValues != null)
            _dispVals = (string[])displayValues.Clone();
         else
            _dispVals = null;
      }

      internal void SetLinkValues(string[] linkValues)
      {
         if (linkValues != null)
            _linkVals = (string[])linkValues.Clone();
         else
            _linkVals = null;
         setNumericVals();
      }

      internal void setNullFlags (bool[] nullFlags)
      {
         if (nullFlags != null)
            _nullFlags = (bool[])nullFlags.Clone();
         else
            _nullFlags = null;
      }

      private void setNumericVals()
      {
         if (_isNumericType && _linkVals != null)
         {
            _numVals = new NUM_TYPE[_linkVals.Length];
            for (int j = 0; j < _linkVals.Length; j++)
               _numVals[j] = new NUM_TYPE(_linkVals[j]);
         }
         else
            _numVals = null;
      }

      /// <summary>
      ///   returns the id of this dcVals object
      /// </summary>
      public int getId()
      {
         return _id;
      }

      /// <summary>
      ///   return the display values
      /// </summary>
      public String[] getDispVals()
      {
         return _dispVals;
      }

      /// <summary>
      /// return Link field values.
      /// </summary>
      /// <returns></returns>
      public String[] GetLinkVals()
      {
         return _linkVals;
      }

      /// <summary>
      ///   returns the array of indice for an item in the list by comparing the mgVal to the linked value or (-1) if none was
      ///   found
      /// </summary>
      /// <param name = "mgVal">the internal value to look for </param>
      /// <param name = "isVectorValue">Denotes whether the value in mgVal should be treated as a vector (true) or not (false).</param>
      /// <param name = "isNull">true if the value to look for is null </param>
      /// <param name = "extraVals">Additional values, prepended to the searched values.</param>
      /// <param name="extraNums">Additional numeric values to be prepended to the searched values.</param>
      /// <param name = "splitCommaSeperatedVals">to split the val on comma or not </param>
      public int[] getIndexOf(string mgVal, bool isVectorValue, bool isNull, string[] extraVals, NUM_TYPE[] extraNums, bool splitCommaSeperatedVals)
      {
         int result = NOT_FOUND;
         String tmpMgVal;
         NUM_TYPE ctrlNumVal;
         String trimmedVal;
         String[] vals = null;
         NUM_TYPE[] nums = null;
         int offset = 0;
         int firstFitMatchIdx = NOT_FOUND;
         int firstFitMatchLength = -1;
         int minLength = -1;
         String compStr = "";
         int[] indice = null;
         string[] values = null;

         if (isNull)
         {
            int i = 0;
            indice = new int[1] { NOT_FOUND };
            for (i = 0; _nullFlags != null && i < _nullFlags.Length; i++)
            {
               if (_nullFlags[i])
               {
                  indice[0] = i;
                  break;
               }
            }

            return indice;
         }

         if (!isVectorValue)
         {
            //split the comma separated values;
            if (splitCommaSeperatedVals)
               values = mgVal.Split(new char[] { ',' });
            else
               values = new string[] { mgVal };
         }
         else
         {
            VectorType vector = new VectorType(mgVal);
            values = vector.GetCellValues();
         }

         indice = new int[values.Length];

         for(int iCtr = 0; iCtr < values.Length; iCtr++)
         {
            //Initialize result.
            result = NOT_FOUND;
            firstFitMatchIdx = NOT_FOUND;

            tmpMgVal = values[iCtr];

            if (_isNumericType || extraNums != null)
            {
               try
               {
                  ctrlNumVal = new NUM_TYPE(tmpMgVal);
               }
               catch (IndexOutOfRangeException)
               {
                  indice = new int[1];

                  indice[0] = NOT_FOUND;
                  return indice;
               }
               trimmedVal = tmpMgVal;
            }
            else
            {
               ctrlNumVal = null;
               trimmedVal = StrUtil.rtrim(tmpMgVal);
            }

           
            // Run two passes. First one on extra values, second one on values belonging to this object
            for (int i = 0; i < 2 && result == NOT_FOUND; i++)
            {
               switch (i)
               {
                  case 0:
                     vals = extraVals;
                     nums = extraNums;
                     offset = 0;
                     break;

                  case 1:
                  default:
                     if (_isNumericType || nums != null)
                        offset = (nums == null ? 0 : nums.Length);
                     else
                        offset = (vals == null ? 0 : vals.Length);
                     vals = _linkVals;
                     nums = _numVals;
                     break;
               }

               if (vals != null)
               {
                  for (int j = 0; j < vals.Length && result == NOT_FOUND; j++)
                  {
                     if (_isNumericType || nums != null)
                     {
                        if (NUM_TYPE.num_cmp(ctrlNumVal, nums[j]) == 0 || (Object)tmpMgVal == (Object)vals[j] ||
                            tmpMgVal.Equals(vals[j]))
                        {
                           // the numeric type is found exactly
                           result = j + offset;
                           break;
                        }
                     }
                     else
                     {
                        if (vals[j].Equals(tmpMgVal) || trimmedVal.Length > 0 && StrUtil.rtrim(vals[j]).Equals(trimmedVal))
                        {
                           result = j + offset;
                           break;
                        }

                        //If Magic sent us a blank value, and such a value exists in the "non DC range" select it.
                        //QCR # 751037 - search for blank values even in the linked values array of the data control
                        if (trimmedVal.Length == 0 && StrUtil.rtrim(vals[j]).Length == 0)
                        {
                           result = j + offset;
                           break;
                        }

                        // save the first fitting result
                        // fixed bug#: 935015, the comparison will be according to the min length of the data & the options
                        if (result == NOT_FOUND && trimmedVal.Length > 0)
                        {
                           minLength = Math.Min(trimmedVal.Length, vals[j].Length);
                           compStr = trimmedVal.Substring(0, minLength);
                           if (compStr.Length > 0 && vals[j].StartsWith(compStr))
                           {
                              // if there is a min length match, check if it is the first fit match.
                              // eg: if list has a,aaa,aaaaa and field value is 'aa' then first fit match is 'aaa'.
                              // if list is a,aaaaa,aaa and field value is 'aa' then first fit match is 'aaaaa'.
                              // if first fit not found, then closest match is used (eg- if field value is
                              // 'aaaaaaaaa' in both the above list, closest match would be 'aaaaa').
                              if (minLength > firstFitMatchLength)
                              {
                                 firstFitMatchIdx = j + offset;
                                 firstFitMatchLength = minLength;
                              }
                           }
                        }
                     }
                  }
               }
            }
          
            if (result == NOT_FOUND)
               result = firstFitMatchIdx;

            //store indice found in integer array.
            indice[iCtr] = result;
         }
        
         return indice;
      }

      /// <summary>
      ///   gets the value of a specific link item
      /// </summary>
      /// <param name = "idx">the index of the link value (0 = first item) </param>
      /// <returns> Magic value of the item </returns>
      public String getLinkValue(int idx)
      {
         string lnkVal = null;

         if (_linkVals != null)
            lnkVal =  _linkVals[idx];

         return lnkVal;
      }

      /// <summary>
      ///   returns true, if value at index idx is null
      /// </summary>
      /// <param name = "the">index of an item in the list </param>
      public bool isNull(int idx)
      {
         if (_nullFlags == null)
            //happens for non-DC choice controls
            return false;

         return _nullFlags[idx];
      }

      #region IDcValues Members

      /// <summary>
      ///   increase the references count by 1
      /// </summary>
      public void AddReference()
      {
         _refCount++;
      }

      /// <summary>
      ///   decrease the references count by 1
      /// </summary>
      public void RemoveReference()
      {
         _refCount--;
         if (_refCount < 0)
            throw new ApplicationException("in dcVals.decrease() references count is less than zero");
      }

      /// <summary>
      ///   returns true if the references count is zero
      /// </summary>
      public bool HasReferences
      {
         get
         {
            return _refCount > 0;
         }
      }

      #endregion

      public override string  ToString()
      {
         return String.Format("{{DCValues 0x{0:X8}, {1} refs}}", _id, _refCount);
      }
   }

}
