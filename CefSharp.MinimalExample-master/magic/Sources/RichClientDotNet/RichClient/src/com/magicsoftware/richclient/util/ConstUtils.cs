using System;
namespace com.magicsoftware.richclient.util
{

   internal class ConstUtils
   {
      /// <summary> Return the selected option string from a comma separated options string
      /// 
      /// </summary>
      /// <param name="strOptions">the strings of the options separated by comma, e.g.: "Error,Warning"
      /// </param>
      /// <param name="options">the value of the options, e.g.: "XY"
      /// </param>
      /// <param name="selectedOption">The option, e.g.: 'Y'
      /// </param>
      /// <returns> the selected option from strOptions
      /// </returns>
      internal static String getStringOfOption(String strOptions, String options, char selectedOption)
      {
         String strOption = "";
         int indexOpt = -1;

         indexOpt = options.IndexOf((Char)selectedOption);

         /**
         * return the string from the strOptions
         */
         if (indexOpt > -1)
         {
            String[] tokens = strOptions.Split(',');

            if (indexOpt < tokens.Length)
               strOption = tokens[indexOpt];
         }

         return strOption;
      }
   }
}