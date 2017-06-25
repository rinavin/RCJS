using System;
using System.Text;
using com.magicsoftware.util;
using com.magicsoftware.unipaas;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.unipaas.management.data;

namespace com.magicsoftware.richclient.exp
{
   /// <summary>
   ///   JPN: Class for Japanese extended functions
   /// </summary>
   /// <author>  Toshiro Nakayoshi (MSJ)
   /// </author>
   internal class ExpressionLocalJpn
   {
      /// <summary>
      ///   mapping table of Japanese characters #1
      ///   convert from zenkaku (0x309b ~ 0x30f6) to hankaku
      /// 
      ///   row index: zenkaku(full-width) code -(minus) 0x309b
      ///   column #1: hankaku(half-width) code
      ///   column #2: dakuon / han-dakuon or 0x0000
      /// </summary>
      internal static readonly char[][] tableZen2Han = new[]
                                                          {
                                                             new[] {(char) (0xff9e), (char) (0x0000)},
                                                             new[] {(char) (0xff9f), (char) (0x0000)},
                                                             new[] {(char) (0x309d), (char) (0x0000)},
                                                             new[] {(char) (0x309e), (char) (0x0000)},
                                                             new[] {(char) (0x309f), (char) (0x0000)},
                                                             new[] {(char) (0x30a0), (char) (0x0000)},
                                                             new[] {(char) (0xff67), (char) (0x0000)},
                                                             new[] {(char) (0xff71), (char) (0x0000)},
                                                             new[] {(char) (0xff68), (char) (0x0000)},
                                                             new[] {(char) (0xff72), (char) (0x0000)},
                                                             new[] {(char) (0xff69), (char) (0x0000)},
                                                             new[] {(char) (0xff73), (char) (0x0000)},
                                                             new[] {(char) (0xff6a), (char) (0x0000)},
                                                             new[] {(char) (0xff74), (char) (0x0000)},
                                                             new[] {(char) (0xff6b), (char) (0x0000)},
                                                             new[] {(char) (0xff75), (char) (0x0000)},
                                                             new[] {(char) (0xff76), (char) (0x0000)},
                                                             new[] {(char) (0xff76), (char) (0xff9e)},
                                                             new[] {(char) (0xff77), (char) (0x0000)},
                                                             new[] {(char) (0xff77), (char) (0xff9e)},
                                                             new[] {(char) (0xff78), (char) (0x0000)},
                                                             new[] {(char) (0xff78), (char) (0xff9e)},
                                                             new[] {(char) (0xff79), (char) (0x0000)},
                                                             new[] {(char) (0xff79), (char) (0xff9e)},
                                                             new[] {(char) (0xff7a), (char) (0x0000)},
                                                             new[] {(char) (0xff7a), (char) (0xff9e)},
                                                             new[] {(char) (0xff7b), (char) (0x0000)},
                                                             new[] {(char) (0xff7b), (char) (0xff9e)},
                                                             new[] {(char) (0xff7c), (char) (0x0000)},
                                                             new[] {(char) (0xff7c), (char) (0xff9e)},
                                                             new[] {(char) (0xff7d), (char) (0x0000)},
                                                             new[] {(char) (0xff7d), (char) (0xff9e)},
                                                             new[] {(char) (0xff7e), (char) (0x0000)},
                                                             new[] {(char) (0xff7e), (char) (0xff9e)},
                                                             new[] {(char) (0xff7f), (char) (0x0000)},
                                                             new[] {(char) (0xff7f), (char) (0xff9e)},
                                                             new[] {(char) (0xff80), (char) (0x0000)},
                                                             new[] {(char) (0xff80), (char) (0xff9e)},
                                                             new[] {(char) (0xff81), (char) (0x0000)},
                                                             new[] {(char) (0xff81), (char) (0xff9e)},
                                                             new[] {(char) (0xff6f), (char) (0x0000)},
                                                             new[] {(char) (0xff82), (char) (0x0000)},
                                                             new[] {(char) (0xff82), (char) (0xff9e)},
                                                             new[] {(char) (0xff83), (char) (0x0000)},
                                                             new[] {(char) (0xff83), (char) (0xff9e)},
                                                             new[] {(char) (0xff84), (char) (0x0000)},
                                                             new[] {(char) (0xff84), (char) (0xff9e)},
                                                             new[] {(char) (0xff85), (char) (0x0000)},
                                                             new[] {(char) (0xff86), (char) (0x0000)},
                                                             new[] {(char) (0xff87), (char) (0x0000)},
                                                             new[] {(char) (0xff88), (char) (0x0000)},
                                                             new[] {(char) (0xff89), (char) (0x0000)},
                                                             new[] {(char) (0xff8a), (char) (0x0000)},
                                                             new[] {(char) (0xff8a), (char) (0xff9e)},
                                                             new[] {(char) (0xff8a), (char) (0xff9f)},
                                                             new[] {(char) (0xff8b), (char) (0x0000)},
                                                             new[] {(char) (0xff8b), (char) (0xff9e)},
                                                             new[] {(char) (0xff8b), (char) (0xff9f)},
                                                             new[] {(char) (0xff8c), (char) (0x0000)},
                                                             new[] {(char) (0xff8c), (char) (0xff9e)},
                                                             new[] {(char) (0xff8c), (char) (0xff9f)},
                                                             new[] {(char) (0xff8d), (char) (0x0000)},
                                                             new[] {(char) (0xff8d), (char) (0xff9e)},
                                                             new[] {(char) (0xff8d), (char) (0xff9f)},
                                                             new[] {(char) (0xff8e), (char) (0x0000)},
                                                             new[] {(char) (0xff8e), (char) (0xff9e)},
                                                             new[] {(char) (0xff8e), (char) (0xff9f)},
                                                             new[] {(char) (0xff8f), (char) (0x0000)},
                                                             new[] {(char) (0xff90), (char) (0x0000)},
                                                             new[] {(char) (0xff91), (char) (0x0000)},
                                                             new[] {(char) (0xff92), (char) (0x0000)},
                                                             new[] {(char) (0xff93), (char) (0x0000)},
                                                             new[] {(char) (0xff6c), (char) (0x0000)},
                                                             new[] {(char) (0xff94), (char) (0x0000)},
                                                             new[] {(char) (0xff6d), (char) (0x0000)},
                                                             new[] {(char) (0xff95), (char) (0x0000)},
                                                             new[] {(char) (0xff6e), (char) (0x0000)},
                                                             new[] {(char) (0xff96), (char) (0x0000)},
                                                             new[] {(char) (0xff97), (char) (0x0000)},
                                                             new[] {(char) (0xff98), (char) (0x0000)},
                                                             new[] {(char) (0xff99), (char) (0x0000)},
                                                             new[] {(char) (0xff9a), (char) (0x0000)},
                                                             new[] {(char) (0xff9b), (char) (0x0000)},
                                                             new[] {(char) (0x30ee), (char) (0x0000)},
                                                             new[] {(char) (0xff9c), (char) (0x0000)},
                                                             new[] {(char) (0x30f0), (char) (0x0000)},
                                                             new[] {(char) (0x30f1), (char) (0x0000)},
                                                             new[] {(char) (0xff66), (char) (0x0000)},
                                                             new[] {(char) (0xff9d), (char) (0x0000)},
                                                             new[] {(char) (0xff73), (char) (0xff9e)},
                                                             new[] {(char) (0xff76), (char) (0x0000)},
                                                             new[] {(char) (0xff79), (char) (0x0000)}
                                                          };

      /// <summary>
      ///   mapping table of Japanese characters #2
      ///   convert from zenkaku (exceptional) to hankaku
      /// 
      ///   row index: no special meaning
      ///   column #1: zenkaku(full-width) code
      ///   column #2: hankaku(half-width) code
      /// </summary>
      internal static readonly char[][] tableExceptZen2Han = new[]
                                                                {
                                                                   new[] {(char) (0x2018), (char) (0x0060)},
                                                                   new[] {(char) (0x2019), (char) (0x0027)},
                                                                   new[] {(char) (0x201D), (char) (0x0022)},
                                                                   new[] {(char) (0x3000), (char) (0x0020)},
                                                                   new[] {(char) (0x3001), (char) (0xFF64)},
                                                                   new[] {(char) (0x3002), (char) (0xFF61)},
                                                                   new[] {(char) (0x300C), (char) (0xFF62)},
                                                                   new[] {(char) (0x300D), (char) (0xFF63)},
                                                                   new[] {(char) (0x30FB), (char) (0xFF65)},
                                                                   new[] {(char) (0x30FC), (char) (0xFF70)},
                                                                   new[] {(char) (0xFFE5), (char) (0x005C)}
                                                                };

      /// <summary>
      ///   mapping table of Japanese characters #3
      ///   convert from hankaku (0xff61 ~ 0xff9f) to zenkaku
      /// 
      ///   row index: hankaku(half-width) code -(minus) 0xff61
      ///   column:    zenkaku(full-width) code
      /// </summary>
      internal static readonly char[] tableHan2Zen = new[]
                                                        {
                                                           (char) (0x3002), (char) (0x300c), (char) (0x300d),
                                                           (char) (0x3001), (char) (0x30fb), (char) (0x30f2),
                                                           (char) (0x30a1), (char) (0x30a3), (char) (0x30a5),
                                                           (char) (0x30a7), (char) (0x30a9), (char) (0x30e3),
                                                           (char) (0x30e5), (char) (0x30e7), (char) (0x30c3),
                                                           (char) (0x30fc), (char) (0x30a2), (char) (0x30a4),
                                                           (char) (0x30a6), (char) (0x30a8), (char) (0x30aa),
                                                           (char) (0x30ab), (char) (0x30ad), (char) (0x30af),
                                                           (char) (0x30b1), (char) (0x30b3), (char) (0x30b5),
                                                           (char) (0x30b7), (char) (0x30b9), (char) (0x30bb),
                                                           (char) (0x30bd), (char) (0x30bf), (char) (0x30c1),
                                                           (char) (0x30c4), (char) (0x30c6), (char) (0x30c8),
                                                           (char) (0x30ca), (char) (0x30cb), (char) (0x30cc),
                                                           (char) (0x30cd), (char) (0x30ce), (char) (0x30cf),
                                                           (char) (0x30d2), (char) (0x30d5), (char) (0x30d8),
                                                           (char) (0x30db), (char) (0x30de), (char) (0x30df),
                                                           (char) (0x30e0), (char) (0x30e1), (char) (0x30e2),
                                                           (char) (0x30e4), (char) (0x30e6), (char) (0x30e8),
                                                           (char) (0x30e9), (char) (0x30ea), (char) (0x30eb),
                                                           (char) (0x30ec), (char) (0x30ed), (char) (0x30ef),
                                                           (char) (0x30f3), (char) (0x309b), (char) (0x309c)
                                                        };

      private readonly ExpressionEvaluator _expressionEvaluator;

      /// <summary>
      ///   CTOR
      /// </summary>
      protected internal ExpressionLocalJpn(ExpressionEvaluator expressionEvaluator)
      {
         _expressionEvaluator = expressionEvaluator;
      }

      /// <summary>
      ///   HAN: Convert a string from zenkaku(full-width) to hankaku(half-width)
      /// </summary>
      /// <param name = "strVal:">zenkaku string
      /// </param>
      /// <returns> hanakaku string
      /// </returns>
      protected internal String eval_op_han(String strVal)
      {
         StringBuilder strbufZenkaku = new StringBuilder(StrUtil.rtrim(strVal));
         StringBuilder strbufHankaku = new StringBuilder(0);
         char cLetter;
         String strRet = null;

         for (int i = 0; i < strbufZenkaku.Length; i++)
         {
            cLetter = strbufZenkaku[i];

            // digit, alphabet and etc.
            if (0xff01 <= cLetter && cLetter <= 0xff5e && cLetter != 0xff3c && cLetter != 0xff40)
            {
               strbufHankaku.Append((char) (cLetter - 0xfee0));
            }
               // katakana
            else if (0x309b <= cLetter && cLetter <= 0x30f6)
            {
               strbufHankaku.Append(tableZen2Han[cLetter - 0x309b][0]);

               if (tableZen2Han[cLetter - 0x309b][1] != 0x0000)
                  strbufHankaku.Append(tableZen2Han[cLetter - 0x309b][1]);
            }
               // others
            else
            {
               int j;
               for (j = 0; j < tableExceptZen2Han.Length; j++)
               {
                  if (cLetter == tableExceptZen2Han[j][0])
                  {
                     strbufHankaku.Append(tableExceptZen2Han[j][1]);
                     break;
                  }
               }

               if (j == tableExceptZen2Han.Length)
                  strbufHankaku.Append(cLetter);
            }
         }

         strRet = strbufHankaku.ToString();

         strbufZenkaku = null;
         strbufHankaku = null;

         return strRet;
      }

      /// <summary>
      ///   ZEN/ZENS: Convert a string from hankaku(half-width) to zenkaku(full-width)
      /// </summary>
      /// <param name = "strVal:">hankaku string
      /// </param>
      /// <param name = "mode:">specify how to convert a hankaku space
      ///   0 = to hankaku space x 1
      ///   1 = to zenkaku space x 1
      ///   2 = to hankaku space x 2
      /// </param>
      /// <returns> zenkaku string
      /// </returns>
      protected internal String eval_op_zens(String strVal, int mode)
      {
         StringBuilder strbufHankaku = new StringBuilder(StrUtil.rtrim(strVal));
         StringBuilder strbufZenkaku = new StringBuilder(0);
         char cLetter;
         char cCombine;
         String strRet = null;

         for (int i = 0; i < strbufHankaku.Length; i++)
         {
            cLetter = strbufHankaku[i];

            // space, digit, alphabet and etc.
            if (0x0020 <= cLetter && cLetter <= 0x007e)
            {
               switch (cLetter)
               {
                  case (char) (0x0020): // space
                     if (mode == 1)
                     {
                        strbufZenkaku.Append((char) 0x3000); // zenkaku space x 1/BrowserClient/
                     }
                     else if (mode == 2)
                     {
                        strbufZenkaku.Append((char) 0x0020); // hankaku space x 2
                        strbufZenkaku.Append((char) 0x0020);
                     }
                        /* mode == 0 */
                     else
                     {
                        strbufZenkaku.Append((char) 0x0020); // hankaku space x 1
                     }
                     break;


                  case (char) (0x0022): // "
                     strbufZenkaku.Append((char) 0x201d);
                     break;


                  case (char) (0x0027): // '
                     strbufZenkaku.Append((char) 0x2019);
                     break;


                  case (char) (0x005c): // \
                     strbufZenkaku.Append((char) 0xffe5);
                     break;


                  case (char) (0x0060): // `
                     strbufZenkaku.Append((char) 0x2018);
                     break;


                  default:
                     strbufZenkaku.Append((char) (cLetter + 0xfee0));
                     break;
               }
            }
               // kutouten, katakana and etc.
            else if (0xff61 <= cLetter && cLetter <= 0xff9f)
            {
               cCombine = (char) (0x0000); // flag of dakuon or han-dakuon

               if ((i + 1) < strbufHankaku.Length)
               {
                  // check the next letter
                  cCombine = strbufHankaku[i + 1];
                  if (cCombine != 0xff9e && cCombine != 0xff9f)
                     cCombine = (char) (0x0000);
               }

               if (cCombine == 0xff9e)
                  // symbol of dakuon
               {
                  if (0xff76 <= cLetter && cLetter <= 0xff84 || 0xff8a <= cLetter && cLetter <= 0xff8e)
                     strbufZenkaku.Append((char) (tableHan2Zen[cLetter - 0xff61] + 0x001));
                  else if (cLetter == 0xff73)
                     strbufZenkaku.Append((char) (0x30f4));
                  else
                     cCombine = (char) (0x0000); // the symbol is independent
               }
               else if (cCombine == 0xff9f)
                  // symbol of han-dakuon
               {
                  if (0xff8a <= cLetter && cLetter <= 0xff8e)
                     strbufZenkaku.Append((char) (tableHan2Zen[cLetter - 0xff61] + 0x002));
                  else
                     cCombine = (char) (0x0000); // the symbol is independent
               }

               if (cCombine != 0x0000)
                  i++;
               else
                  strbufZenkaku.Append(tableHan2Zen[cLetter - 0xff61]);
            }
               // others
            else
            {
               strbufZenkaku.Append(cLetter);
            }
         }

         strRet = strbufZenkaku.ToString();

         strbufHankaku = null;
         strbufZenkaku = null;

         return strRet;
      }

      /// <summary>
      ///   ZIMERead: return composition string in IME
      /// </summary>
      protected internal String eval_op_zimeread(int dummy)
      {
         String strRet = null;
         UtilImeJpn utilImeJpn = Manager.UtilImeJpn;

         if (utilImeJpn != null)
            strRet = utilImeJpn.StrImeRead;

         return strRet;
      }

      /// <summary>
      ///   ZKANA: Convert a string from hiragana to katakana (and vice versa)
      /// </summary>
      /// <param name = "strVal:">hankaku(or katakana) string
      /// </param>
      /// <param name = "mode:">specify the direction of conversion
      ///   0 = from hiragana to katakana
      ///   1 = from katakana to hiragana
      /// </param>
      /// <returns> katakana(or hiragana) string
      /// </returns>
      protected internal String eval_op_zkana(String strVal, int mode)
      {
         StringBuilder strbufConverted = new StringBuilder(StrUtil.rtrim(strVal));
         char cLetter;
         String strRet = null;

         for (int i = 0; i < strbufConverted.Length; i++)
         {
            cLetter = strbufConverted[i];

            if (mode == 0)
               // hiragana --> katakana
            {
               if (0x3041 <= cLetter && cLetter <= 0x3093)
                  strbufConverted[i] = (char) (cLetter + 0x0060);
            }
               // katakana --> hiragana
            else
            {
               if (0x30A1 <= cLetter && cLetter <= 0x30f3)
                  strbufConverted[i] = (char) (cLetter - 0x0060);
            }
         }

         strRet = strbufConverted.ToString();
         strbufConverted = null;

         return strRet;
      }

      /// <summary>
      ///   JCDOW: Japanese version of CDOW
      /// </summary>
      protected internal void eval_op_jcdow(ExpressionEvaluator.ExpVal resVal, NUM_TYPE val1,
                                            DisplayConvertor displayConvertor)
      {
         _expressionEvaluator.eval_op_date_str(resVal, val1, "SSSSSST", displayConvertor);
      }

      /// <summary>
      ///   JMONTH: Japanese version of NMONTH
      /// </summary>
      protected internal void eval_op_jmonth(ExpressionEvaluator.ExpVal resVal, ExpressionEvaluator.ExpVal val1)
      {
         if (val1.MgNumVal == null)
         {
            _expressionEvaluator.SetNULL(resVal, StorageAttribute.ALPHA);
            return;
         }
         val1.MgNumVal = _expressionEvaluator.mul_add(val1.MgNumVal, 31, -30);
         _expressionEvaluator.eval_op_month(resVal, val1);
         int month = resVal.MgNumVal.NUM_2_LONG();

         resVal.Attr = StorageAttribute.ALPHA;
         resVal.StrVal = UtilDateJpn.convertStrMonth(month);
      }

      /// <summary>
      ///   JNDOW: Japanese version of NDOW
      /// </summary>
      protected internal void eval_op_jndow(ExpressionEvaluator.ExpVal resVal, ExpressionEvaluator.ExpVal val1,
                                            DisplayConvertor displayConvertor)
      {
         if (val1.MgNumVal == null)
         {
            _expressionEvaluator.SetNULL(resVal, StorageAttribute.ALPHA);
            return;
         }
         val1.MgNumVal = _expressionEvaluator.mul_add(val1.MgNumVal, 0, 6);
         eval_op_jcdow(resVal, val1.MgNumVal, displayConvertor);
      }

      /// <summary>
      ///   JYEAR: Return Japanese year of an era
      /// </summary>
      protected internal void eval_op_jyear(ExpressionEvaluator.ExpVal resVal, ExpressionEvaluator.ExpVal val1)
      {
         _expressionEvaluator.eval_op_date_brk(resVal, val1.MgNumVal, 4);
      }

      /// <summary>
      ///   JGENGO: Return Japanese gengo (the name of an era)
      /// </summary>
      protected internal void eval_op_jgengo(ExpressionEvaluator.ExpVal resVal, NUM_TYPE val1, NUM_TYPE val2,
                                             DisplayConvertor displayConvertor)
      {
         String strFormat;
         int intType;

         resVal.Attr = StorageAttribute.ALPHA;
         if (val1 == null || val2 == null)
         {
            _expressionEvaluator.SetNULL(resVal, StorageAttribute.ALPHA);
            return;
         }

         intType = val2.NUM_2_LONG();

         if (intType >= 4)
            strFormat = "JJJJ";
         else if (intType >= 2)
            strFormat = "JJ";
         else if (intType >= 1)
            strFormat = "J";
         else
         {
            resVal.StrVal = "";
            return;
         }

         _expressionEvaluator.eval_op_date_str(resVal, val1, strFormat, displayConvertor);
      }
   }
}