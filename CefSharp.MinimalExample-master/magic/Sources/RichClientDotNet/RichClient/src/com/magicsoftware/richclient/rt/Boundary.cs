using System;
using com.magicsoftware.richclient.exp;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.unipaas.management.exp;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.management.gui;

namespace com.magicsoftware.richclient.rt
{
   /// <summary>
   ///   this class represent a range or locate sections - i.e. min/max expressions
   /// </summary>
   internal class Boundary
   {
      private readonly int _cacheTableFldId;
      private readonly Expression _max;
      private readonly Expression _min;
      private readonly StorageAttribute _retType;
      private readonly int _size;
      private GuiExpressionEvaluator.ExpVal _maxExpVal;
      private GuiExpressionEvaluator.ExpVal _minExpVal;
      internal bool DiscardMin { get; private set; }
      internal bool DiscardMax { get; private set; }

      internal bool MaxEqualsMin { get; private set; }

      internal GuiExpressionEvaluator.ExpVal MaxExpVal
      {
         get { return _maxExpVal; }
      }

      internal GuiExpressionEvaluator.ExpVal MinExpVal
      {
         get { return _minExpVal; }
      }

      /// <summary>
      /// Instantiates a new Boundary object 
      /// </summary>
      /// <param name="task"></param>
      /// <param name="minIdx"></param>
      /// <param name="maxIdx"></param>
      /// <param name="returnType"></param>
      /// <param name="size"></param>
      internal Boundary(Task task, int minIdx, int maxIdx, StorageAttribute returnType, int size):
         this(task, minIdx, maxIdx, returnType, size, -1)
      {}

      internal Boundary(Task task, int minIdx, int maxIdx, StorageAttribute returnType, int size, int cacheTableId)
      {
         _retType = returnType;
         _size = size;
         _cacheTableFldId = cacheTableId;
         if (minIdx != -1)
            _min = task.getExpById(minIdx);

         if (maxIdx != -1)
            _max = task.getExpById(maxIdx);
      }

      //sometimes the min or max does not have an expression
      protected internal bool hasMinExp()
      {
         return _min != null;
      }

      //sometimes the min or max does not have an expression
      protected internal bool hasMaxExp()
      {
         return _max != null;
      }


      /// <summary>
      ///   returns the min/max expressions return type - it actually the type of the field they are Associated with
      /// </summary>
      protected internal StorageAttribute getExpType()
      {
         return _retType;
      }

      /// <summary>
      ///   return the id of the cached table field this range corresponds to
      /// </summary>
      protected internal int getCacheTableFldId()
      {
         return _cacheTableFldId;
      }

      /// <summary>
      ///   computes the min and max expression to check their values
      /// </summary>
      protected internal void compute(bool padValueWithMinMaxCharacters)
      {
         if (hasMinExp())
         {
            //evaluate the min expression
            //         String minVal = min.evaluate(getExpType(),size, true, DATA_TYPE_SKIP);
            String minVal = _min.evaluate(getExpType(), _size);

            if (minVal != null)
            {
               minVal = minVal.TrimEnd();
            }

            _minExpVal = new GuiExpressionEvaluator.ExpVal(getExpType(), (minVal == null), minVal);

            // check and set the MaxEqualsMin before the wild chars are replaced on the string result
            MaxEqualsMin = IsMaxEqualsMin();
            
            if (!_minExpVal.IsNull && (_minExpVal.Attr == StorageAttribute.ALPHA || _minExpVal.Attr == StorageAttribute.UNICODE))
            {
               if (padValueWithMinMaxCharacters)
               {
                  _minExpVal.StrVal = DisplayConvertor.StringValueToMgValue(_minExpVal.StrVal, _minExpVal.Attr, Char.MinValue, _size).ToString();
                  MaxEqualsMin = false;
               }

               _minExpVal.StrVal = StrUtil.SearchAndReplaceWildChars(_minExpVal.StrVal, _size, Char.MinValue);
            }
            
            DiscardMin = _min.DiscardCndRangeResult();
         }

         if (hasMaxExp())
         {
            //evaluate the max expression
            //         String maxVal = max.evaluate(getExpType(),size, true, DATA_TYPE_SKIP);
            String maxVal = _max.evaluate(getExpType(), _size);
            _maxExpVal = new GuiExpressionEvaluator.ExpVal(getExpType(), (maxVal == null), maxVal);

            if (!_maxExpVal.IsNull && (_maxExpVal.Attr == StorageAttribute.ALPHA || _maxExpVal.Attr == StorageAttribute.UNICODE))
            {
               if (padValueWithMinMaxCharacters)
               {
                  _maxExpVal.StrVal = DisplayConvertor.StringValueToMgValue(_maxExpVal.StrVal, _maxExpVal.Attr, Char.MaxValue, _size).ToString();
               }

               _maxExpVal.StrVal = StrUtil.SearchAndReplaceWildChars(_maxExpVal.StrVal, _size, Char.MaxValue);

            }
            
            DiscardMax = _max.DiscardCndRangeResult();
         }
      }

      /// <summary>
      ///   this function gets a value and checks whether it satisfies the range section
      /// </summary>
      /// <param name = "val">- the value to be compared    
      /// </param>
      protected internal bool checkRange(String val, bool IsNull)
      {
         bool res = true;

         var cmpVal = new GuiExpressionEvaluator.ExpVal(getExpType(), IsNull, val);

         if (cmpVal.IsNull && ((hasMinExp() && _minExpVal.IsNull) || (hasMaxExp() && _maxExpVal.IsNull)))
         {
            res = true;
         }
         else
         {
            //check min expression Compliance 
            if (hasMinExp())
            {
               //if both of the compared values are not null
               if (!_minExpVal.IsNull && !cmpVal.IsNull)
               {
                  try
                  {
                     //the compared value must be equal or greater to the min value
                     if (ExpressionEvaluator.val_cmp_any(cmpVal, _minExpVal, true) < 0)
                        res = false;
                  }
                  catch (ExpressionEvaluator.NullValueException)
                  {
                     res = false;
                  }
               }
               else if (cmpVal.IsNull != _minExpVal.IsNull)
                  res = false;
            }

            //check max expression Compliance 
            if (hasMaxExp() && res)
            {
               //if both of the compared values are not null
               if (!_maxExpVal.IsNull && !cmpVal.IsNull)
               {
                  try
                  {
                     //the compared value must be equal or greater to the min value
                     if (ExpressionEvaluator.val_cmp_any(cmpVal, _maxExpVal, true) > 0)
                        res = false;
                  }
                  catch (ExpressionEvaluator.NullValueException)
                  {
                     res = false;
                  }
               }
               // if one of them is null and null is the greatest value there is we must check that maxExpVal is not null
               else if (cmpVal.IsNull != _maxExpVal.IsNull)
                  res = false;
            }
         }

         return res;
      }

      /// <summary>
      /// returns true if min expression equal to max expression and no wild chars (*, ?) exist in the range
      /// </summary>
      /// <returns></returns>
      internal bool IsMaxEqualsMin()
      {
         bool result = false;
         if (hasMaxExp() && hasMinExp())
         {
            if (_min.getId() == _max.getId())
            {
               if (_retType == StorageAttribute.ALPHA || _retType == StorageAttribute.UNICODE)
                  result = !WildCharExist();
               else
                  result = true;
            }
         }
         return result;
      }

      /// <summary>
      /// checks if wild char exist in tne value
      /// </summary>
      /// <returns></returns>
      private bool WildCharExist()
      {
         string[] wildChars = { "*", "?" };
         bool result = false;

         //check trim
         if (!_minExpVal.IsNull)
         {
            String stringValue = _minExpVal.StrVal;
            foreach (var item in wildChars)
            {
               if (stringValue.EndsWith(item))
               {
                  result = true;
                  break;
               }
            }
         }
         return result;
      }

      public override string ToString()
      {
         return String.Format("{{Boundary: {0}-{1}, {2}, {3}}}", this._max, this._max, this._retType, this._size);
      }
   }
}