using System;
using System.Text;

namespace com.magicsoftware.util
{
   public static class Base64
   {
      private static readonly byte[] _base64EncMap;
      private static readonly byte[] _base64DecMap;

      /// <summary> This method encodes the given string using the base64-encoding
      /// specified in RFC-2045 (Section 6.8). It's used for example in the
      /// "Basic" authorization scheme.
      /// </summary>
      /// <param name="str">the string </param>
      /// <param name="encoding"> Environment.Encoding </param>
      /// <returns> the base64-encoded str </returns>
      public static String encode(String str, Encoding encoding)
      {
         return encode(str, false, encoding);
      }

      /// <summary> Encodes string using the base64-encoding.
      /// If isUseEnvCharset is true, use the specific charset when converting
      /// string to byte array. (DBCS support)
      /// </summary>
      /// <param name="str">the string </param>
      /// <param name="isUseEnvCharset"> </param>
      /// <param name="encoding"> Environment.Encoding </param>
      /// <returns> the base64-encoded str </returns>
      public static String encode(String str, bool isUseEnvCharset, Encoding encoding)
      {
         if (str == null)
            return null;
         if (str.Equals(""))
            return str;

         try
         {
            Encoding base64Encoding = ISO_8859_1_Encoding.getInstance();
            Encoding srcEncoding = isUseEnvCharset ? encoding : base64Encoding;
            byte[] ba = encode(srcEncoding.GetBytes(str));
            return base64Encoding.GetString(ba, 0, ba.Length);
         }
         catch (Exception uee)
         {
            throw new ApplicationException(uee.Message);
         }
      }

      /// <summary> This method encodes the given byte[] using the base64-encoding
      /// specified in RFC-2045 (Section 6.8).
      /// </summary>
      /// <param name="data">the data </param>
      /// <returns> the base64-encoded data </returns>
      public static byte[] encode(byte[] data)
      {
         if (data == null)
            return null;

         int sidx, didx;
         byte[] dest = new byte[((data.Length + 2) / 3) * 4];

         // 3-byte to 4-byte conversion + 0-63 to ASCII printable conversion
         for (sidx = 0, didx = 0; sidx < data.Length - 2; sidx += 3)
         {
            dest[didx++] = _base64EncMap[(Misc.URShift(data[sidx], 2)) & 63];
            dest[didx++] = _base64EncMap[(Misc.URShift(data[sidx + 1], 4)) & 15 | (data[sidx] << 4) & 63];
            dest[didx++] = _base64EncMap[(Misc.URShift(data[sidx + 2], 6)) & 3 | (data[sidx + 1] << 2) & 63];
            dest[didx++] = _base64EncMap[data[sidx + 2] & 63];
         }
         if (sidx < data.Length)
         {
            dest[didx++] = _base64EncMap[(Misc.URShift(data[sidx], 2)) & 63];
            if (sidx < data.Length - 1)
            {
               dest[didx++] = _base64EncMap[(Misc.URShift(data[sidx + 1], 4)) & 15 | (data[sidx] << 4) & 63];
               dest[didx++] = _base64EncMap[(data[sidx + 1] << 2) & 63];
            }
            else
               dest[didx++] = _base64EncMap[(data[sidx] << 4) & 63];
         }

         // add padding
         for (; didx < dest.Length; didx++)
            dest[didx] = (byte)'=';

         return dest;
      }

      /// <summary> This method decodes the given string using the base64-encoding
      /// specified in RFC-2045 (Section 6.8).
      /// </summary>
      /// <param name="str">the base64-encoded string. </param>
      /// <returns> the decoded str.</returns>
      public static String decode(String str)
      {
         return decode(str, null);
      }

      /// <summary> This method decodes the given string using the base64-encoding
      /// specified in RFC-2045 (Section 6.8).
      /// </summary>
      /// <param name="str">the base64-encoded string. </param>
      /// <param name="encoding">Environment.Encoding or null.</param>
      /// <returns> the decoded str.</returns>
      public static String decode(String str, Encoding encoding)
      {
         if (str == null)
            return null;

         if (str.Equals(""))
            return str;
         try
         {
            Encoding base64Encoding = ISO_8859_1_Encoding.getInstance();
            byte[] ba = decode(base64Encoding.GetBytes(str));
            Encoding destEncoding = (encoding != null) ? encoding : base64Encoding;
            return destEncoding.GetString(ba, 0, ba.Length);
         }
         catch (Exception uee)
         {
            throw new ApplicationException(uee.Message);
         }
      }

      /// <summary> This method decodes the given byte[] using the base64-encoding
      /// specified in RFC-2045 (Section 6.8).
      /// </summary>
      /// <param name="data">the base64-encoded data.</param>
      /// <returns> the decoded <var>data</var>. </returns>
      public static byte[] decode(byte[] data)
      {
         if (data == null)
            return null;

         int tail = data.Length;
         while (data[tail - 1] == '=')
            tail--;

         byte[] dest = new byte[tail - data.Length / 4];

         // ASCII printable to 0-63 conversion
         for (int idx = 0; idx < data.Length; idx++)
            data[idx] = _base64DecMap[data[idx]];

         // 4-byte to 3-byte conversion
         int sidx, didx;
         for (sidx = 0, didx = 0; didx < dest.Length - 2; sidx += 4, didx += 3)
         {
            dest[didx] = (byte)(((data[sidx] << 2) & 255) | ((Misc.URShift(data[sidx + 1], 4)) & 3));
            dest[didx + 1] = (byte)(((data[sidx + 1] << 4) & 255) | ((Misc.URShift(data[sidx + 2], 2)) & 15));
            dest[didx + 2] = (byte)(((data[sidx + 2] << 6) & 255) | (data[sidx + 3] & 63));
         }
         if (didx < dest.Length)
            dest[didx] = (byte)(((data[sidx] << 2) & 255) | ((Misc.URShift(data[sidx + 1], 4)) & 3));
         if (++didx < dest.Length)
            dest[didx] = (byte)(((data[sidx + 1] << 4) & 255) | ((Misc.URShift(data[sidx + 2], 2)) & 15));

         return dest;
      }

      /// <summary> decoded and return an hex representation of the data</summary>
      public static String decodeToHex(String str)
      {
         if (str == null)
            return null;

         if (str.Equals(""))
            return str;

         return StrUtil.stringToHexaDump(decode(str), 2);
      }

      /// <summary> decodes a string to byte array</summary>
      public static byte[] decodeToByte(String str)
      {
         if (str == null)
            return null;

         // QCR 740918 if we have and empty expression it is sent from the server as empty string
         // and changed locally to a string with one blank either way they are not valid base64 encoded
         // string and should not be decoded.
         if (str.Equals("") || str.Equals(" "))
            return new byte[0];

         try
         {
            Encoding base64Encoding = ISO_8859_1_Encoding.getInstance();
            return decode(base64Encoding.GetBytes(str));
         }
         catch (Exception uee)
         {
            throw new ApplicationException(uee.Message);
         }
      }

      /// <summary>
      /// 
      /// </summary>
      static Base64()
      {
         // Class Initializer
         {
            // rfc-2045: Base64 Alphabet
            byte[] map = new byte[] { (byte)'A', (byte)'B', (byte)'C', (byte)'D', (byte)'E', (byte)'F', (byte)'G', (byte)'H', (byte)'I', (byte)'J', (byte)'K', (byte)'L', (byte)'M', (byte)'N', (byte)'O', (byte)'P', (byte)'Q', (byte)'R', (byte)'S', (byte)'T', (byte)'U', (byte)'V', (byte)'W', (byte)'X', (byte)'Y', (byte)'Z', (byte)'a', (byte)'b', (byte)'c', (byte)'d', (byte)'e', (byte)'f', (byte)'g', (byte)'h', (byte)'i', (byte)'j', (byte)'k', (byte)'l', (byte)'m', (byte)'n', (byte)'o', (byte)'p', (byte)'q', (byte)'r', (byte)'s', (byte)'t', (byte)'u', (byte)'v', (byte)'w', (byte)'x', (byte)'y', (byte)'z', (byte)'0', (byte)'1', (byte)'2', (byte)'3', (byte)'4', (byte)'5', (byte)'6', (byte)'7', (byte)'8', (byte)'9', (byte)'+', (byte)'/' };
            _base64EncMap = map;
            _base64DecMap = new byte[128];
            for (int idx = 0; idx < _base64EncMap.Length; idx++)
               _base64DecMap[_base64EncMap[idx]] = (byte)idx;
         }
      }
   }
}