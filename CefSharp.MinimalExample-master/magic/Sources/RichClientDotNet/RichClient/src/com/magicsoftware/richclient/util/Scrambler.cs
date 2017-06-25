using System;
using System.Collections.Generic;
using System.Text;

namespace com.magicsoftware.richclient.util
{
   class Scrambler
   {
      internal static Boolean ScramblingEnabled = true; // Disable scrambling
      private const int XML_MIN_RANDOM = -48;
      private const int XML_MAX_RANDOM = 47;
      private const int XML_ILLEGAL_RANDOM = '<' - 81;
      private static Random _randomizer = new Random();

      /// <summary>scramble string</summary>
      /// <param name="inVal">string to scramble</param>
      /// <returns> scrambled string</returns>
      internal static String Scramble(String inVal)
      {
         if (!ScramblingEnabled)
            return inVal;

         int curr = 0;
         char currChr;
         int length = inVal.Length;
         int random = RandomScramble(length);
         int key = (int)Math.Sqrt(length) + random;
         var outVal = new StringBuilder(length + 1);

         // The first char in the scrambled string is a random modifier to the default key
         outVal.Append((char)(random + 81));

         for (int i = 0; i < key; i++)
         {
            curr = i;

            while (curr < length)
            {
               currChr = inVal[curr];
               outVal.Append(currChr);
               curr += key;
            }
         }

         // The last char in the scrambled string is a padding readable char.
         outVal.Append('_');
         return outVal.ToString();
      }

      /// <summary>delete first character of scrambled text and makes left trim to the string</summary>
      private static int LocateScramble(String inVal, int from)
      {
         int i;
         for (i = from; i < inVal.Length && Char.IsWhiteSpace(inVal[i]); i++)
         {
         }

         return ++i;
      }

      /// <summary> Choose a random modifier to the key on which we base the scrambling process.
      /// The random factor cannot be just any number we choose. Since the scramble key
      /// determines the amount of 'jumps' we perform on the text to be scrambled, then   
      /// the random number we add to it must not be too big, nor too small. As a rule,   
      /// the random modifier can range from SQRT(len)/2' to '-SQRT(len)/2', and since we 
      /// pass the selected number as a character within the XML, the whole range cannot  
      /// exceed the number of the printable characters (95), thus the range is limited   
      /// between (-48) to (47), so we cap the allowed range according to these limits.   
      /// Last, the value XML_ILLEGAL_RANDOM, since it will result in adding the char '<' 
      /// to the beginning of the XML. This will make the client confused, since it will  
      /// think the XML is not scrambled.                               
      /// </summary>
      /// <param name="len">of outgoing String</param>
      /// <returns> random modifier</returns>
      private static int RandomScramble(int len)
      {
         int delta;
         double sqrt = Math.Sqrt(len);
         int low = XML_MIN_RANDOM;
         int high = XML_MAX_RANDOM;

         if (low < (int)(((-1) * sqrt) / 2))
            low = (int)(((-1) * sqrt) / 2);

         if (high > (int)(sqrt / 2))
            high = (int)(sqrt / 2);

         delta = (int)(_randomizer.NextDouble() * (high - low)) + low;
         if (delta == XML_ILLEGAL_RANDOM)
            delta++;

         return delta;
      }

      /// <summary>make unscrambling to string inside upper and down border</summary>
      /// <param name="inVal">scrambled string to build unscrambled string from it</param>
      /// <param name="beginOffSet">offset where the scrambled string begins</param>
      /// <param name="endOffSet">offset of the last char in the scrambled string.</param>
      /// <returns> unscrambled string</returns>
      internal static String UnScramble(String inVal, int beginOffSet, int endOffSet)
      {
         if (!ScramblingEnabled)
         {
            String outVal = inVal.Substring(beginOffSet, endOffSet - beginOffSet + 1);
            return outVal;
         }
         else
         {
            int currOut, currIn, i;
            int length;
            int key;
            char[] outVal;
            int currBlk;
            int blockSize;
            int reminder;
            int start;
            char randomChr;

            // ignore the last char in the input, it's just a padding character.
            endOffSet--;

            // skip over the first char in the input, it only contains a random modifier to the key.
            // it is not part of the data.
            start = LocateScramble(inVal, beginOffSet);
            randomChr = inVal[start - 1];

            length = endOffSet - start + 1;
            key = (randomChr - 81) + (int)Math.Sqrt(length);
            outVal = new char[length];
            blockSize = length / key;
            reminder = length % key;

            for (i = currOut = 0; currOut < length; i++)
            {
               currIn = i;
               currBlk = 1;

               while (currIn < length && currOut < length)
               {
                  outVal[currOut] = inVal[currIn + start];
                  currIn += blockSize;
                  if (currBlk <= reminder)
                     currIn++;
                  currOut++;
                  currBlk++;
               }
            }

            return new String(outVal);
         }
      }
   }
}
