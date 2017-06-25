using System;
using System.Text;

namespace com.magicsoftware.util
{
   public class ChoiceUtils
   {
      /// <summary>
      ///   init the display Value from string
      /// </summary>
      /// <param name = "choiceDispStr">the all substring separated with comma.
      ///   The behavior:
      ///   a. when have "\" before char a-z need to ignore the \ put the a-z char
      ///   b. when "\," -> ","
      ///   c. when "\-" -> "-"
      ///   d. when "\\" -> "\"
      ///   e. when "\\\\" -> "\\"
      ///   the display can be all string. and we don't need to check validation according to the dataType(as we do in Link
      /// </param>
      public static String[] GetDisplayListFromString(String choiceDispStr, bool removeAccelerators, bool shouldMakePrintable, bool shouldTrimOptions)
      {
         var fromHelp = new[] { "\\\\", "\\-", "\\," };
         var toHelp = new[] { "XX", "XX", "XX" };
         var trimChar = new[] { ' ' };

         choiceDispStr = choiceDispStr.TrimEnd(trimChar);

         String helpStrDisp = StrUtil.searchAndReplace(choiceDispStr, fromHelp, toHelp);
         String[] sTok = StrUtil.tokenize(helpStrDisp, ",");
         int size = (helpStrDisp != ""
                        ? sTok.Length
                        : 0);
         StringBuilder tokenBuffer;

         String helpTokenDisp, token;
         int currPosDisp = 0, nextPosDisp = 0, tokenPosDisp, i;
         var choiceDisp = new String[size];

         for (i = 0, currPosDisp = 0, nextPosDisp = 0; i < size; i++)
         {
            nextPosDisp = currPosDisp;
            nextPosDisp = helpStrDisp.IndexOf(',', nextPosDisp);

            if (nextPosDisp == currPosDisp)
               token = helpTokenDisp = "";
            else if (nextPosDisp == -1)
            {
               token = choiceDispStr.Substring(currPosDisp);
               helpTokenDisp = helpStrDisp.Substring(currPosDisp);
            }
            else
            {
               token = choiceDispStr.Substring(currPosDisp, (nextPosDisp) - (currPosDisp));
               helpTokenDisp = helpStrDisp.Substring(currPosDisp, (nextPosDisp) - (currPosDisp));
            }
            currPosDisp = nextPosDisp + 1;

            if (token != null)
            {
               token = StrUtil.ltrim(token);
               if (removeAccelerators)
                  token = RemoveAcclCharFromOptions(new StringBuilder(token));
               //the same we need to do for helpTokenDisp
               helpTokenDisp = StrUtil.ltrim(helpTokenDisp);
               if (removeAccelerators)
                  helpTokenDisp = RemoveAcclCharFromOptions(new StringBuilder(helpTokenDisp));
            }

            if (helpTokenDisp.IndexOf('\\') >= 0)
            {
               tokenBuffer = new StringBuilder();
               for (tokenPosDisp = 0; tokenPosDisp < helpTokenDisp.Length; tokenPosDisp++)
                  if (helpTokenDisp[tokenPosDisp] != '\\')
                     tokenBuffer.Append(token[tokenPosDisp]);
                  else if (tokenPosDisp == helpTokenDisp.Length - 1)
                     tokenBuffer.Append(' ');

               token = tokenBuffer.ToString();
            }

            if (shouldMakePrintable)
            {
               token = StrUtil.makePrintableTokens(token, StrUtil.SEQ_2_STR);
               if (shouldTrimOptions)
               {
                  string temp = token.TrimEnd(trimChar);
                  if (temp.Length == 0)
                     choiceDisp[i] = " ";
                  else
                     choiceDisp[i] = token.TrimEnd(trimChar);
               }
               else
                  choiceDisp[i] = token;
            }
            else
               choiceDisp[i] = token;
         }

         return choiceDisp;
      }

      /// <summary>
      ///   Remove the & if needed : Choicinp.cpp method RemoveAcclCharFromOptions_U() 
      ///   For tab control remove one & only if we have duplicate &, we need to send at lest one & to the items list.
      ///   This method isn't call form RADIO because we don't support the accelerator for radio.
      /// </summary>
      public static String RemoveAcclCharFromOptions(StringBuilder OptionStr)
      {
         int i = 0;

         if (OptionStr != null)
         {
            for (i = 0; i < OptionStr.Length;)
            {
               if (OptionStr[i] == '&')
               {
                  // Allow the display of the '&',  character - as an accelerator key: */
                  if (i < OptionStr.Length - 1 && OptionStr[i + 1] == ('&'))
                     i++;
                  //for tab control we need to remove only the duplicate, we must have at lest one &.
                  OptionStr = OptionStr.Remove(i, 1);
               }
               else
                  i++;
            }
         }
         return (OptionStr != null
                    ? OptionStr.ToString()
                    : null);
      }
   }
}
