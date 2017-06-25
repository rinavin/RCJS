using System.Text;

namespace com.magicsoftware.util
{
   /// <summary>
   /// 
   /// </summary>
   public class ISO_8859_1_Encoding
   {
#if !PocketPC
      static private readonly Encoding _encoding8859 = Encoding.GetEncoding("iso-8859-1");
#else
      static private readonly Encoding _encoding8859 = new Mobile_ISO_8859_1_Encoding();
#endif

      static public Encoding getInstance()
      {
         return _encoding8859;
      }

      private class Mobile_ISO_8859_1_Encoding : Encoding
      {
         public override int GetByteCount(char[] chars, int index, int count)
         {
            return count;
         }

         public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
         {
            for (int i = 0; i < charCount; i++)
               bytes[byteIndex + i] = (byte)chars[charIndex + i];
            return charCount;
         }

         public override int GetCharCount(byte[] bytes, int index, int count)
         {
            return count;
         }

         public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
         {
            for (int i = 0; i < byteCount; i++)
               chars[charIndex + i] = (char)bytes[byteIndex + i];
            return byteCount;
         }

         public override int GetMaxByteCount(int charCount)
         {
            return charCount;
         }

         public override int GetMaxCharCount(int byteCount)
         {
            return byteCount;
         }
      }
   }
}
