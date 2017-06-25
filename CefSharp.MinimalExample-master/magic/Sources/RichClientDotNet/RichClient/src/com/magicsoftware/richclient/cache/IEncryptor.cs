using System;
using System.Collections.Generic;
using System.Text;

namespace com.magicsoftware.richclient.cache
{
   interface IEncryptor
   {
      bool EncryptionDisabled { get; set; } //if true, cached files will not be encrypted/decrypted to/from persistent storage
      byte[] EncryptionKey { get; }

      /// <param name="disableEncryption">if true, cached files will not be encrypted/decrypted to/from persistent storage.</param>
      /// <param name="encryptionKey">encryption key with which cache files will be encrypted/decrypted.</param>
      void InitializeEncryptor(bool disableEncryption, byte[] encryptionKey);

      /// <summary> encrypt content (using the encryption key set by setDefaultEncryptionKey or setEncryptionKey);</summary>
      /// <param name="content">decrypted content </param>
      /// <returns> encrypted content </returns>
      byte[] Encrypt(byte[] content);

      /// <summary> decrypt content (using the encryption key set by setDefaultEncryptionKey or setEncryptionKey);</summary>
      /// <param name="content">encrypted content </param>
      /// <returns> decrypted content </returns>
      byte[] Decrypt(byte[] content);

      /// <summary> checks if encryption key is different than the default key. </summary>
      /// <returns></returns>
      bool HasNonDefaultEncryptionKey();
   }
}
