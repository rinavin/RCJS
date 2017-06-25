// In order to convert some functionality to Visual C#, the Java Language Conversion Assistant
// creates "support classes" that duplicate the original functionality.  
//
// Support classes replicate the functionality of the original code, but in some cases they are 
// substantially different architecturally. Although every effort is made to preserve the 
// original architecture of the application in the converted project, the user should be aware that 
// the primary goal of these support classes is to replicate functionality, and that at times 
// the architecture of the resulting solution may differ somewhat.

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace com.magicsoftware.util
{
   /// <summary>
   /// Contains conversion support elements such as classes, interfaces and static methods.
   /// </summary>
   public class Misc
   {
      /// <summary>
      /// Writes the exception stack trace to the received stream
      /// </summary>
      /// <param name="throwable">Exception to obtain information from</param>
      /// <param name="stream">Output sream used to write to</param>
      public static void WriteStackTrace(Exception throwable, TextWriter stream)
      {
         stream.Write(throwable);
         stream.Flush();
      }

      /*******************************/

      /// <summary>
      /// Converts the specified collection to its string representation.
      /// </summary>
      /// <param name="c">The collection to convert to string.</param>
      /// <returns>A string representation of the specified collection.</returns>
      // TODO: since this method is used only for debugging purposes, we can make it much more simple -
      // something like forach (object o in c) o.ToString()
      public static String CollectionToString(ICollection c)
      {
         var s = new StringBuilder();

         if (c != null)
         {
            var l = new ArrayList(c); // Can't use List<T> - uses ICollection

            bool isDictionary = (c is BitArray || c is Hashtable || c is IDictionary || c is NameValueCollection ||
                                 (l.Count > 0 && l[0] is DictionaryEntry));
            for (int index = 0; index < l.Count; index++)
            {
               if (l[index] == null)
                  s.Append("null");
               else if (!isDictionary)
                  s.Append(l[index]);
               else
               {
                  isDictionary = true;
                  if (c is NameValueCollection)
                     s.Append(((NameValueCollection) c).GetKey(index));
                  else
                     s.Append(((DictionaryEntry) l[index]).Key);
                  s.Append("=");
                  if (c is NameValueCollection)
                     s.Append(((NameValueCollection) c).GetValues(index)[0]);
                  else
                     s.Append(((DictionaryEntry) l[index]).Value);
               }
               if (index < l.Count - 1)
                  s.Append(", ");
            }

            if (isDictionary)
            {
               if (c is ArrayList)
                  isDictionary = false;
            }
            if (isDictionary)
            {
               s.Insert(0, "{");
               s.Append("}");
            }
            else
            {
               s.Insert(0, "[");
               s.Append("]");
            }
         }
         else
            s.Insert(0, "null");
         return s.ToString();
      }

      /*******************************/

      /// <summary>
      /// Receives a byte array and returns it transformed in an byte array
      /// </summary>
      /// <param name="byteArray">Byte array to process</param>
      /// <returns>The transformed array</returns>
      // TODO:  There are more efficient methods to copy one array to another array of different sign but same unit size like http://bytes.com/forum/thread605517.html. 
      public static sbyte[] ToSByteArray(byte[] byteArray)
      {
         sbyte[] sbyteArray = null;
         if (byteArray != null)
         {
            sbyteArray = new sbyte[byteArray.Length];
            for (int index = 0; index < byteArray.Length; index++)
               sbyteArray[index] = (sbyte) byteArray[index];
         }
         return sbyteArray;
      }

      /*******************************/

      /// <summary>
      /// Receives sbyte array and returns it transformed in a byte array
      /// </summary>
      /// <param name="sbyteArray">sbyte array to process</param>
      /// <returns>The transformed array</returns>
      public static byte[] ToByteArray(sbyte[] sbyteArray)
      {
         byte[] byteArray = null;
         if (sbyteArray != null)
         {
            byteArray = new byte[sbyteArray.Length];
            for (int index = 0; index < sbyteArray.Length; index++)
               byteArray[index] = (byte) sbyteArray[index];
         }
         return byteArray;
      }


      /// <summary> Compares number of bytes in two byte arrays</summary>
      /// <param name="source"></param>
      /// <param name="destination"></param>
      /// <param name="numberOfBytes"></param>
      /// <returns> boolen true if equal</returns>
      public static bool CompareByteArray(byte[] source, byte[] destination, int numberOfBytes)
      {
         if (source.Length >= numberOfBytes && destination.Length >= numberOfBytes)
         {
            for (int len = 0; len < numberOfBytes; len++)
            {
               if (source[len] != destination[len])
                  return false;
            }
         }
         else
            return false;

         return true;
      }

      /*******************************/

      /// <summary>
      /// Performs an unsigned bitwise right shift with the specified number
      /// </summary>
      /// <param name="number">Number to operate on</param>
      /// <param name="bits">Ammount of bits to shift</param>
      /// <returns>The resulting number from the shift operation</returns>
      // TODO: instead of calling URShift(number, bits), we can use((uint)number) >> bits.
      public static int URShift(int number, int bits)
      {
         if (number >= 0)
            return number >> bits;
         else
            return (number >> bits) + (2 << ~bits);
      }

      /// <summary>Checks if the path is web-URL (http:// or https:// or /alias/..)</summary>
      /// <param name="path"></param>
      /// <param name="forwardSlashUsage">instruct how to refer a forward slash - either as a relative web url, or as a file in the file system.</param>
      /// <returns>true iff:
      /// 1. Path is prefixed with "http".
      /// 2. Path is prefixed with a forward slash and forward slash is to be considered as relative URL.
      /// </returns>
      public static Boolean isWebURL(String path, String forwardSlashUsage)
      {
         return (path.StartsWith("http", StringComparison.CurrentCultureIgnoreCase) 
                 || path.StartsWith("/") && forwardSlashUsage == Constants.ForwardSlashWebUsage
                 || path.StartsWith("?"));
      }

      /// <summary>create a URI from a given URL (in case the protocol is missing in the URL, 'http://' will be used as a default </summary>
      /// <param name="url"> input url string </param>
      /// <returns>Uri</returns>
      public static Uri createURI(string url)
      {
         Debug.Assert(!String.IsNullOrEmpty(url));

         Uri uri = null;

         try
         {
            uri = new Uri(url);
         }
         catch (UriFormatException)
         {
            // Don't Prepend http:// if url is physical path
            if (url.IndexOf(':') == -1 && !url.StartsWith(@"\"))
               url = "http://" + url;

            try
            {
               uri = new Uri(url);
            }
            catch (Exception ex)
            {
               throw ex;
            }
         }
         return uri;
      }

      /// <summary>
      /// get system's time in milliseconds
      /// </summary>
      /// <returns></returns>
      public static long getSystemMilliseconds()
      {
         return ((DateTime.Now.Ticks - 621355968000000000)/10000);
      }

      /// <summary>
      /// resize the buffer to the new size
      /// </summary>
      /// <param name="buffer"></param>
      /// <param name="newSize"></param>
      public static void arrayResize(ref byte[] buffer, int newSize)
      {
#if !PocketPC || RCMobile_CF35
         Array.Resize(ref buffer, newSize);
#else
         var newBuffer = new byte[newSize];
         Array.Copy(buffer, newBuffer, Math.Min(buffer.Length, newSize));
         buffer = newBuffer;
#endif
      }

      /// <summary>
      /// Compares 2 int arrays
      /// </summary>
      /// <param name="arrayOne"></param>
      /// <param name="arrayTwo"></param>
      /// <returns>true if arrays are equal else false</returns>
      public static bool CompareIntArrays(int[] arrayOne, int[] arrayTwo)
      {
         bool areEqual = false;

         if (arrayOne == arrayTwo)
            areEqual = true;
         else
         {
            if (arrayOne != null && arrayTwo != null)
            {
               if (arrayOne.Length == arrayTwo.Length)
               {
                  for (int i = 0; i < arrayOne.Length; i++)
                  {
                     if (arrayOne[i] != arrayTwo[i])
                        break;
                     else
                        areEqual = true;
                  }
               }
            }
         }

         return areEqual;
      }

      /// <summary>
      /// Returns the comma separated string for the values passed in int array.
      /// </summary>
      /// <param name="values">Integer array</param>
      /// <returns>comma separated string</returns>
      public static string GetCommaSeperatedString(int[] intArray)
      {
         StringBuilder temp = new StringBuilder();

         foreach (var val in intArray)
         {
            if (temp.Length > 0)
               temp.Append(",");

            temp.Append(val);
         }

         return temp.ToString();
      }

      /// <summary>
      /// Returns int array out of comma separated string
      /// </summary>
      /// <param name="value">comma separated string</param>
      /// <returns>Integer array</returns>
      public static int[] GetIntArray(string commaSeparatedValue)
      {
         int[] intArray = new int[0];

         if (!string.IsNullOrEmpty(commaSeparatedValue))
         {
            string[] vals = commaSeparatedValue.Split(',');
            intArray = new int[vals.Length];

            for (int iCtr = 0; iCtr < vals.Length; iCtr++)
               Int32.TryParse(vals[iCtr], out intArray[iCtr]);
         }

         return intArray;
      }

      /// <returns>true if the calling thread is the main/gui thread</returns>
      public static void MarkGuiThread()
      {
         Thread.CurrentThread.Name = Constants.MG_GUI_THREAD;
      }

      public static bool IsGuiThread()
      {
         return (Thread.CurrentThread.Name != null && Thread.CurrentThread.Name == Constants.MG_GUI_THREAD);
      }

      /// <returns>true if the calling thread is a work thread</returns>
      public static void MarkWorkThread()
      {
         Thread.CurrentThread.Name = Constants.MG_WORK_THREAD;
      }

      public static bool IsWorkThread()
      {
         return (Thread.CurrentThread.Name != null && Thread.CurrentThread.Name == Constants.MG_WORK_THREAD);
      }

      /// <returns>true if the calling thread is activated by a timer</returns>
      public static void MarkTimerThread()
      {
         Thread.CurrentThread.Name = Constants.MG_TIMER_THREAD;
      }

      public static bool IsTimerThread()
      {
         return (Thread.CurrentThread.Name != null && Thread.CurrentThread.Name == Constants.MG_TIMER_THREAD);
      }
   }
}  
