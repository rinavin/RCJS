﻿using System;
using System.Text;
using util.com.magicsoftware.util;

namespace com.magicsoftware.httpclient
{
   /// <summary>Provides methods for encoding and decoding URLs when processing Web requests. 
   ///   This class cannot be inherited.
   ///   Taken from http://iron9light.wordpress.com/2008/05/24/systemwebhttputility-for-net-compact-or-micro-framework/
   /// </summary>
   public class HttpUtility
   {
      public static Boolean EncodingEnabled = true;

      /// <summary>Decodes all the bytes in the specified byte array into a string.</summary>
      /// <remarks>Replace the method "System.Text.Encoding.ASCII.GetString(byte[] bytes);" in .Net Framework.</remarks>
      /// <param name = "bytes">The byte array containing the sequence of bytes to decode.</param>
      /// <returns>A String containing the results of decoding the specified sequence of bytes.</returns>
      private static string ASCIIGetString(byte[] bytes)
      {
         if (bytes == null)
            throw new ArgumentNullException("bytes");
         return Encoding.ASCII.GetString(bytes, 0, bytes.Length);
      }

      private static int HexToInt(char h)
      {
         if (h >= '0' && h <= '9')
         {
            return (h - '0');
         }
         if (h >= 'a' && h <= 'f')
         {
            return (h - 'a' + 10);
         }
         if (h >= 'A' && h <= 'F')
         {
            return (h - 'A' + 10);
         }
         return -1;
      }

      internal static char IntToHex(int n)
      {
         if (n <= 9)
         {
            return (char)(n + 0X30);
         }
         return (char)((n - 10) + 0X61);
      }

      private static bool IsNonAsciiByte(byte b)
      {
         if (b < 0X7f)
         {
            return (b < 0X20);
         }
         return true;
      }

      internal static bool IsSafe(char ch)
      {
         if ((ch >= 'a' && ch <= 'z') ||
             (ch >= 'A' && ch <= 'Z') ||
             (ch >= '0' && ch <= '9'))
         {
            return true;
         }
         switch (ch)
         {
            case '\'':
            case '(':
            case ')':
            case '*':
            case '-':
            case '.':
            case '_':
            case '!':
               return true;
         }
         return false;
      }

      /// <summary>Converts a URL-encoded string into a decoded string, using the specified encoding object.</summary>
      /// <returns>A decoded string.</returns>
      /// <param name = "e">The <see cref = "T:System.Text.Encoding"></see> that specifies the decoding scheme. </param>
      /// <param name = "str">The string to decode. </param>
      public static string UrlDecode(string str, Encoding e)
      {
         string decodedStr = null;

         if (str != null)
         {
            decodedStr = UrlDecodeStringFromStringInternal(str, e);
            Logger.Instance.WriteServerMessagesToLog(String.Format("\nString: {0}\nUrlDecoded by: {1}\nTo: {2}", str, e, decodedStr));
         }

         return decodedStr;
      }

      /// <summary>Converts a URL-encoded byte array into a decoded string using the specified encoding object, starting at the specified position in the array, and continuing for the specified number of bytes.</summary>
      /// <returns>A decoded string.</returns>
      /// <param name = "offset">The position in the byte to begin decoding. </param>
      /// <param name = "count">The number of bytes to decode. </param>
      /// <param name = "e">The <see cref = "T:System.Text.Encoding"></see> object that specifies the decoding scheme. </param>
      /// <param name = "bytes">The array of bytes to decode. </param>
      public static string UrlDecode(byte[] bytes, int offset, int count, Encoding e)
      {
         if ((bytes == null) && (count == 0))
         {
            return null;
         }
         if (bytes == null)
         {
            throw new ArgumentNullException("bytes");
         }
         if ((offset < 0) || (offset > bytes.Length))
         {
            throw new ArgumentOutOfRangeException("offset");
         }
         if ((count < 0) || ((offset + count) > bytes.Length))
         {
            throw new ArgumentOutOfRangeException("count");
         }
         return UrlDecodeStringFromBytesInternal(bytes, offset, count, e);
      }

      private static byte[] UrlDecodeBytesFromBytesInternal(byte[] buf, int offset, int count)
      {
         int length = 0;
         var sourceArray = new byte[count];
         for (int i = 0; i < count; i++)
         {
            int index = offset + i;
            byte num4 = buf[index];
            if (num4 == 0X2b)
            {
               num4 = 0X20;
            }
            else if ((num4 == 0X25) && (i < (count - 2)))
            {
               int num5 = HexToInt((char)buf[index + 1]);
               int num6 = HexToInt((char)buf[index + 2]);
               if ((num5 >= 0) && (num6 >= 0))
               {
                  num4 = (byte)((num5 << 4) | num6);
                  i += 2;
               }
            }
            sourceArray[length++] = num4;
         }
         if (length < sourceArray.Length)
         {
            var destinationArray = new byte[length];
            Array.Copy(sourceArray, destinationArray, length);
            sourceArray = destinationArray;
         }
         return sourceArray;
      }

      private static string UrlDecodeStringFromBytesInternal(byte[] buf, int offset, int count, Encoding e)
      {
         var decoder = new UrlDecoder(count, e);
         for (int i = 0; i < count; i++)
         {
            int index = offset + i;
            byte b = buf[index];
            if (b == 0X2b)
            {
               b = 0X20;
            }
            else if ((b == 0X25) && (i < (count - 2)))
            {
               if ((buf[index + 1] == 0X75) && (i < (count - 5)))
               {
                  int num4 = HexToInt((char)buf[index + 2]);
                  int num5 = HexToInt((char)buf[index + 3]);
                  int num6 = HexToInt((char)buf[index + 4]);
                  int num7 = HexToInt((char)buf[index + 5]);
                  if (((num4 < 0) || (num5 < 0)) || ((num6 < 0) || (num7 < 0)))
                  {
                     goto Label_00DA;
                  }
                  var ch = (char)((((num4 << 12) | (num5 << 8)) | (num6 << 4)) | num7);
                  i += 5;
                  decoder.AddChar(ch);
                  continue;
               }
               int num8 = HexToInt((char)buf[index + 1]);
               int num9 = HexToInt((char)buf[index + 2]);
               if ((num8 >= 0) && (num9 >= 0))
               {
                  b = (byte)((num8 << 4) | num9);
                  i += 2;
               }
            }
         Label_00DA:
            decoder.AddByte(b);
         }
         return decoder.GetString();
      }

      private static string UrlDecodeStringFromStringInternal(string s, Encoding e)
      {
         int length = s.Length;
         var decoder = new UrlDecoder(length, e);
         for (int i = 0; i < length; i++)
         {
            char ch = s[i];
            if (ch == '+')
            {
               ch = ' ';
            }
            else if (ch == '%')
            {
               if (s[i + 1] == 'u')
               {
                  int num3 = HexToInt(s[i + 2]);
                  int num4 = HexToInt(s[i + 3]);
                  int num5 = HexToInt(s[i + 4]);
                  int num6 = HexToInt(s[i + 5]);
                  if (((num3 < 0) || (num4 < 0)) || ((num5 < 0) || (num6 < 0)))
                  {
                     goto Label_0106;
                  }
                  ch = (char)((((num3 << 12) | (num4 << 8)) | (num5 << 4)) | num6);
                  i += 5;
                  decoder.AddChar(ch);
                  continue;
               }
               int num7 = HexToInt(s[i + 1]);
               int num8 = HexToInt(s[i + 2]);
               if ((num7 >= 0) && (num8 >= 0))
               {
                  var b = (byte)((num7 << 4) | num8);
                  i += 2;
                  decoder.AddByte(b);
                  continue;
               }
            }
         Label_0106:
            if ((ch & 0xff80) == 0)
            {
               decoder.AddByte((byte)ch);
            }
            else
            {
               decoder.AddChar(ch);
            }
         }
         return decoder.GetString();
      }

      /// <summary>Converts a URL-encoded array of bytes into a decoded array of bytes.</summary>
      /// <returns>A decoded array of bytes.</returns>
      /// <param name = "bytes">The array of bytes to decode. </param>
      internal static byte[] UrlDecodeToBytes(byte[] bytes)
      {
         if (bytes == null)
         {
            return null;
         }
         return UrlDecodeToBytes(bytes, 0, bytes.Length);
      }

      /// <summary>Converts a URL-encoded string into a decoded array of bytes.</summary>
      /// <returns>A decoded array of bytes.</returns>
      /// <param name = "str">The string to decode. </param>
      internal static byte[] UrlDecodeToBytes(string str)
      {
         if (str == null)
         {
            return null;
         }
         return UrlDecodeToBytes(str, Encoding.UTF8);
      }

      /// <summary>Converts a URL-encoded string into a decoded array of bytes using the specified decoding object.</summary>
      /// <returns>A decoded array of bytes.</returns>
      /// <param name = "e">The <see cref = "T:System.Text.Encoding"></see> object that specifies the decoding scheme. </param>
      /// <param name = "str">The string to decode. </param>
      internal static byte[] UrlDecodeToBytes(string str, Encoding e)
      {
         if (str == null)
         {
            return null;
         }
         return UrlDecodeToBytes(e.GetBytes(str));
      }

      /// <summary>Converts a URL-encoded array of bytes into a decoded array of bytes, starting at the specified position in the array and continuing for the specified number of bytes.</summary>
      /// <returns>A decoded array of bytes.</returns>
      /// <param name = "offset">The position in the byte array at which to begin decoding. </param>
      /// <param name = "count">The number of bytes to decode. </param>
      /// <param name = "bytes">The array of bytes to decode. </param>
      internal static byte[] UrlDecodeToBytes(byte[] bytes, int offset, int count)
      {
         if ((bytes == null) && (count == 0))
         {
            return null;
         }
         if (bytes == null)
         {
            throw new ArgumentNullException("bytes");
         }
         if ((offset < 0) || (offset > bytes.Length))
         {
            throw new ArgumentOutOfRangeException("offset");
         }
         if ((count < 0) || ((offset + count) > bytes.Length))
         {
            throw new ArgumentOutOfRangeException("count");
         }
         return UrlDecodeBytesFromBytesInternal(bytes, offset, count);
      }

      /// <summary>Converts a byte array into an encoded URL string.</summary>
      /// <returns>An encoded string.</returns>
      /// <param name = "bytes">The array of bytes to encode. </param>
      public static string UrlEncode(byte[] bytes)
      {
         if (bytes == null)
         {
            return null;
         }
         // return Encoding.ASCII.GetString(UrlEncodeToBytes(bytes));
         return ASCIIGetString(UrlEncodeToBytes(bytes));
      }

      /// <summary>Encodes a URL string.</summary>
      /// <returns>An encoded string.</returns>
      /// <param name = "str">The text to encode. </param>
      public static string UrlEncode(string str)
      {
         return UrlEncode(str, Encoding.UTF8);
      }

      /// <summary>Encodes a URL string using the specified encoding object.</summary>
      /// <returns>An encoded string.</returns>
      /// <param name = "e">The <see cref = "T:System.Text.Encoding"></see> object that specifies the encoding scheme. </param>
      /// <param name = "str">The text to encode. </param>
      public static string UrlEncode(string str, Encoding e)
      {
         string encodedStr = null;

         if (str != null)
         {
            encodedStr = ASCIIGetString(UrlEncodeToBytes(str, e));

            Logger.Instance.WriteServerMessagesToLog(String.Format("\nString: {0}\nUrlEncoded by: {1}\nTo: {2}", str, e, encodedStr));
         }

         return encodedStr;
      }

      /// <summary>Converts a byte array into a URL-encoded string, starting at the specified position in the array and continuing for the specified number of bytes.</summary>
      /// <returns>An encoded string.</returns>
      /// <param name = "offset">The position in the byte array at which to begin encoding. </param>
      /// <param name = "count">The number of bytes to encode. </param>
      /// <param name = "bytes">The array of bytes to encode. </param>
      public static string UrlEncode(byte[] bytes, int offset, int count)
      {
         if (bytes == null)
         {
            return null;
         }
         // return Encoding.ASCII.GetString(UrlEncodeToBytes(bytes, offset, count));
         return ASCIIGetString(UrlEncodeToBytes(bytes, offset, count));
      }

      private static byte[] UrlEncodeBytesToBytesInternal(byte[] bytes, int offset, int count,
                                                          bool alwaysCreateReturnValue)
      {
         if (!EncodingEnabled)
         {
            return bytes;
         }
         int num = 0;
         int num2 = 0;
         for (int i = 0; i < count; i++)
         {
            var ch = (char)bytes[offset + i];
            if (ch == ' ')
            {
               num++;
            }
            else if (!IsSafe(ch))
            {
               num2++;
            }
         }
         if ((!alwaysCreateReturnValue && (num == 0)) && (num2 == 0))
         {
            return bytes;
         }
         var buffer = new byte[count + (num2 * 2)];
         int num4 = 0;
         for (int j = 0; j < count; j++)
         {
            byte num6 = bytes[offset + j];
            var ch2 = (char)num6;
            if (IsSafe(ch2))
            {
               buffer[num4++] = num6;
            }
            else if (ch2 == ' ')
            {
               buffer[num4++] = 0X2b;
            }
            else
            {
               buffer[num4++] = 0X25;
               buffer[num4++] = (byte)IntToHex((num6 >> 4) & 15);
               buffer[num4++] = (byte)IntToHex(num6 & 15);
            }
         }
         return buffer;
      }

      private static byte[] UrlEncodeBytesToBytesInternalNonAscii(byte[] bytes, int offset, int count,
                                                                  bool alwaysCreateReturnValue)
      {
         int num = 0;
         for (int i = 0; i < count; i++)
         {
            if (IsNonAsciiByte(bytes[offset + i]))
            {
               num++;
            }
         }
         if (!alwaysCreateReturnValue && (num == 0))
         {
            return bytes;
         }
         var buffer = new byte[count + (num * 2)];
         int num3 = 0;
         for (int j = 0; j < count; j++)
         {
            byte b = bytes[offset + j];
            if (IsNonAsciiByte(b))
            {
               buffer[num3++] = 0X25;
               buffer[num3++] = (byte)IntToHex((b >> 4) & 15);
               buffer[num3++] = (byte)IntToHex(b & 15);
            }
            else
            {
               buffer[num3++] = b;
            }
         }
         return buffer;
      }

      internal static string UrlEncodeNonAscii(string str, Encoding e)
      {
         if (string.IsNullOrEmpty(str))
         {
            return str;
         }
         if (e == null)
         {
            e = Encoding.UTF8;
         }
         byte[] bytes = e.GetBytes(str);
         bytes = UrlEncodeBytesToBytesInternalNonAscii(bytes, 0, bytes.Length, false);
         // return Encoding.ASCII.GetString(bytes);
         return ASCIIGetString(bytes);
      }

      public static string UrlEncodeSpaces(string str)
      {
         if (str != null && str.IndexOf(' ') >= 0)
         {
            str = str.Replace(" ", "%20");
         }
         return str;
      }

      /// <summary>
      /// </summary>
      /// <param name="str"></param>
      /// <returns></returns>
      public static string UrlDecodeSpaces(string str)
      {
         if (str != null)
            str = str.Replace("%20", " ");
         return str;
      }

      /// <summary>Converts a string into a URL-encoded array of bytes.</summary>
      /// <returns>An encoded array of bytes.</returns>
      /// <param name = "str">The string to encode. </param>
      internal static byte[] UrlEncodeToBytes(string str)
      {
         if (str == null)
         {
            return null;
         }
         return UrlEncodeToBytes(str, Encoding.UTF8);
      }

      /// <summary>Converts an array of bytes into a URL-encoded array of bytes.</summary>
      /// <returns>An encoded array of bytes.</returns>
      /// <param name = "bytes">The array of bytes to encode. </param>
      internal static byte[] UrlEncodeToBytes(byte[] bytes)
      {
         if (bytes == null)
         {
            return null;
         }
         return UrlEncodeToBytes(bytes, 0, bytes.Length);
      }

      /// <summary>Converts a string into a URL-encoded array of bytes using the specified encoding object.</summary>
      /// <returns>An encoded array of bytes.</returns>
      /// <param name = "e">The <see cref = "T:System.Text.Encoding"></see> that specifies the encoding scheme. </param>
      /// <param name = "str">The string to encode </param>
      internal static byte[] UrlEncodeToBytes(string str, Encoding e)
      {
         if (str == null)
         {
            return null;
         }
         byte[] bytes = e.GetBytes(str);
         return UrlEncodeBytesToBytesInternal(bytes, 0, bytes.Length, false);
      }

      /// <summary>Converts an array of bytes into a URL-encoded array of bytes, starting at the specified position in the array and continuing for the specified number of bytes.</summary>
      /// <returns>An encoded array of bytes.</returns>
      /// <param name = "offset">The position in the byte array at which to begin encoding. </param>
      /// <param name = "count">The number of bytes to encode. </param>
      /// <param name = "bytes">The array of bytes to encode. </param>
      internal static byte[] UrlEncodeToBytes(byte[] bytes, int offset, int count)
      {
         if ((bytes == null) && (count == 0))
         {
            return null;
         }
         if (bytes == null)
         {
            throw new ArgumentNullException("bytes");
         }
         if ((offset < 0) || (offset > bytes.Length))
         {
            throw new ArgumentOutOfRangeException("offset");
         }
         if ((count < 0) || ((offset + count) > bytes.Length))
         {
            throw new ArgumentOutOfRangeException("count");
         }
         return UrlEncodeBytesToBytesInternal(bytes, offset, count, true);
      }

      /// <summary>Converts a string into a Unicode string.</summary>
      /// <returns>A Unicode string in %UnicodeValue notation.</returns>
      /// <param name = "str">The string to convert. </param>
      internal static string UrlEncodeUnicode(string str)
      {
         if (str == null)
         {
            return null;
         }
         return UrlEncodeUnicodeStringToStringInternal(str, false);
      }

      private static string UrlEncodeUnicodeStringToStringInternal(string s, bool ignoreAscii)
      {
         int length = s.Length;
         var builder = new StringBuilder(length);
         for (int i = 0; i < length; i++)
         {
            char ch = s[i];
            if ((ch & 0xff80) == 0)
            {
               if (ignoreAscii || IsSafe(ch))
               {
                  builder.Append(ch);
               }
               else if (ch == ' ')
               {
                  builder.Append('+');
               }
               else
               {
                  builder.Append('%');
                  builder.Append(IntToHex((ch >> 4) & '\x000f'));
                  builder.Append(IntToHex(ch & '\x000f'));
               }
            }
            else
            {
               builder.Append("%u");
               builder.Append(IntToHex((ch >> 12) & '\x000f'));
               builder.Append(IntToHex(ch & '\x000f'));
               builder.Append(IntToHex((ch >> 4) & '\x000f'));
               builder.Append(IntToHex(ch & '\x000f'));
            }
         }
         return builder.ToString();
      }

      /// <summary>Converts a Unicode string into an array of bytes.</summary>
      /// <returns>A byte array.</returns>
      /// <param name = "str">The string to convert. </param>
      internal static byte[] UrlEncodeUnicodeToBytes(string str)
      {
         if (str == null)
         {
            return null;
         }
         return Encoding.ASCII.GetBytes(UrlEncodeUnicode(str));
      }

      /// <summary>Encodes the path portion of a URL string for reliable HTTP transmission from the Web server to a client.</summary>
      /// <returns>The URL-encoded text.</returns>
      /// <param name = "str">The text to URL-encode. </param>
      internal static string UrlPathEncode(string str)
      {
         if (str == null)
         {
            return null;
         }
         int index = str.IndexOf('?');
         if (index >= 0)
            return (UrlPathEncode(str.Substring(0, index)) + str.Substring(index));
         return UrlEncodeSpaces(UrlEncodeNonAscii(str, Encoding.UTF8));
      }

      #region Nested type: UrlDecoder

      private class UrlDecoder
      {
         private readonly int _bufferSize;
         private readonly char[] _charBuffer;
         private readonly Encoding _encoding;
         private byte[] _byteBuffer;
         private int _numBytes;
         private int _numChars;

         internal UrlDecoder(int bufferSize, Encoding encoding)
         {
            _bufferSize = bufferSize;
            _encoding = encoding;
            _charBuffer = new char[bufferSize];
         }

         internal void AddByte(byte b)
         {
            if (_byteBuffer == null)
            {
               _byteBuffer = new byte[_bufferSize];
            }
            _byteBuffer[_numBytes++] = b;
         }

         internal void AddChar(char ch)
         {
            if (_numBytes > 0)
            {
               FlushBytes();
            }
            _charBuffer[_numChars++] = ch;
         }

         private void FlushBytes()
         {
            if (_numBytes > 0)
            {
               _numChars += _encoding.GetChars(_byteBuffer, 0, _numBytes, _charBuffer, _numChars);
               _numBytes = 0;
            }
         }

         internal string GetString()
         {
            if (_numBytes > 0)
            {
               FlushBytes();
            }
            if (_numChars > 0)
            {
               return new string(_charBuffer, 0, _numChars);
            }
            return string.Empty;
         }
      }

      #endregion
   }
}
