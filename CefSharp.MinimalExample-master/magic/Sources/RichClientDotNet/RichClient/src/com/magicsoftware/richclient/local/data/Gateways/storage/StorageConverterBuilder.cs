using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.util;

namespace com.magicsoftware.richclient.local.data.gateways.storage
{
   /// <summary>
   /// Builds storage converter
   /// </summary>
   internal class StorageConverterBuilder
   {
      /// <summary>
      /// Builds storage converter
      /// </summary>
      /// <returns></returns>
      internal StorageConverter Build()
      {
         StorageConverter storageConverter = new StorageConverter();

         storageConverter.Add(new StorageUnicodeZString());
         storageConverter.Add(new StorageAlphaZString());
         storageConverter.Add(new StorageNumericSigned());
         storageConverter.Add(new StorageNumericFloat());
         storageConverter.Add(new StorageNumericString());
         storageConverter.Add(new StorageBooleanInteger());
         storageConverter.Add(new StorageDateString());
         storageConverter.Add(new StorageDateInteger());
         storageConverter.Add(new StorageTimeString());
         storageConverter.Add(new StorageTimeInteger());
         storageConverter.Add(new StorageBlobBinary());
         storageConverter.Add(new StorageBlobAnsi());
         storageConverter.Add(new StorageBlobUnicode());

         return storageConverter;
      }
   }
}
