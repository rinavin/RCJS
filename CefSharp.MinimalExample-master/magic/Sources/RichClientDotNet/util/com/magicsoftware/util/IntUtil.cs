using System;
namespace com.magicsoftware.util
{
	
	public class IntUtil
	{
		/// <summary> Returns the value obtained by rotating the two's complement binary representation of the specified
		/// int value left by the specified number of bits.
		/// 
		/// </summary>
		/// <param name="value">value to be rotated
		/// </param>
		/// <param name="shiftBits">number of bits to be shifed
		/// </param>
		/// <returns>
		/// </returns>
		public static int rotateLeft(int val, int shiftBits)
		{
			return (val << shiftBits) | (com.magicsoftware.util.Misc.URShift(val, - shiftBits));
		}

      //
      // Summary:
      //     Converts the string representation of a number to its 32-bit signed integer
      //     equivalent. A return value indicates whether the operation succeeded.
      //
      // Parameters:
      //   s:
      //     A string containing a number to convert.
      //
      //   result:
      //     When this method returns, contains the 32-bit signed integer value equivalent
      //     to the number contained in s, if the conversion succeeded, or zero if the
      //     conversion failed. The conversion fails if the s parameter is null, is not
      //     of the correct format, or represents a number less than System.Int32.MinValue
      //     or greater than System.Int32.MaxValue. This parameter is passed uninitialized.
      //
      // Returns:
      //     true if s was converted successfully; otherwise, false.
      public static bool TryParse(string s, out int result)
      {
         bool converted;

#if !PocketPC
         converted = Int32.TryParse(s, out result);
#else
         result = 0;
         converted = false;
         if (s != null)
         {
            try
            {
               result = Int32.Parse(s);
               converted = true;
            }
            catch (Exception)
            {
            }
         }
#endif
         return converted;
      }
   }
}