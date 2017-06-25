using System;
using System.Collections.Generic;
using System.IO;
using com.magicsoftware.httpclient.utils.compression.LZMA;
using util.com.magicsoftware.util;

namespace com.magicsoftware.httpclient.utils.compression
{
   /// <summary> This is interface class to be implemented by Factory classes.
   /// The class encapsulates Count () and GetAvaliableCompressor() function to be  
   /// overridden by subclasses. While function Release () is concrete, which calls Compressor’s Release
   /// function.</summary>
   internal abstract class CompressorsFactory
   {
      internal abstract ushort count();
      internal abstract Compressor getAvailableCompressor();

      internal void release(Compressor compressor)
      {
         compressor.release();
      }
   }

   /// <summary>This class is inherited from CompressorFactory. It implements reusing the compressor 
   /// object. It maintains a list of compressors, when requested it returns a available compressor 
   /// from list. In case no compressor is available then it allows the subclass to create
   /// a new compressor and adds the newly created object in the list for reusing.</summary>
   internal abstract class ReusableCompressorsFactory : CompressorsFactory
   {
      private readonly List<ReusableCompressor> _compressors = new List<ReusableCompressor>();

      /// <summary>abstract method to be overridden by actual compressor subclass.</summary>
      /// <returns>ReusableCompressor</returns>
      protected abstract ReusableCompressor createCompressor();

      internal override ushort count()
      {
         return (ushort) _compressors.Count;
      }

      /// <summary>Returns a comoressor. If compressor is avaliable in the list
      /// then returns from the list otherwise creates a new compressor and adds in the list
      /// then return.</summary>
      /// <returns>Compressor</returns>
      internal override Compressor getAvailableCompressor()
      {
         ReusableCompressor compressor = null;

         lock (this)
         {
            foreach (ReusableCompressor item in _compressors)
            {
               if (!item.inuse)
               {
                  compressor = item;
                  compressor.inuse = true;
                  break;
               }
            }
         }

         if (compressor == null)
         {
            compressor = createCompressor();
            compressor.inuse = true;
            lock (this)
            {
               _compressors.Add(compressor);
            }
         }

         return compressor;
      }
   }

   /// <summary>This is factory class for LZMA compressor. It is implements abstract factory class
   /// CompressorFactory.  This class maintains a list of LZMACompressor objects. 
   /// The overridden Create () tries to reuse the object, in case none of the objects are 
   /// available then it creates new LZMACompressor and return, the newly created object 
   /// will be added in the list.</summary>
   internal class LZMACompressorsFactory : ReusableCompressorsFactory
   {
      private static LZMACompressorsFactory _lzmaCmpFactory;

      private LZMACompressorsFactory()
      {
         _lzmaCmpFactory = this;
      }

      protected override ReusableCompressor createCompressor()
      {
         return new LZMACompressor();
      }

      /// <summary> Returns the single instance of the LZMACompressorsFactory class.
      /// In case this instance does not exist it creates that instance.
      /// </summary>
      /// <returns> LZMACompressorsFactory instance
      /// </returns>
      internal static LZMACompressorsFactory Instance
      {
         get
         {
            if (_lzmaCmpFactory == null)
            {
               lock (typeof(LZMACompressorsFactory))
               {
                  if (_lzmaCmpFactory == null)
                     _lzmaCmpFactory = new LZMACompressorsFactory();
               }
            }
            return _lzmaCmpFactory;
         }
      }
   }

   /// <summary>This is a new abstract class to be implemented by all compressors. It provides following functions to be overridden.
   /// 1)	Compress (…) : Compresses the data by algorithm used by subclass Compressor
   /// 2)	DeCompress (…) : Decompresses the data by algorithm used by subclass Compressor
   /// 3)	Release (): Makes the compressor available for another compression. 
   /// This function is called by CompressorFactory class.</summary>
   internal abstract class Compressor
   {
      internal abstract byte[] compress(byte[] content, String CompressionLevel);
      internal abstract byte[] decompress(byte[] content);

      // release the compressor for use of other threads
      internal abstract void release();
   }

   /// <summary>This class creates an interface for all Reusable compressors. 
   /// All reusable compressors must be inherited  from this class. ReusableCompressor is 
   /// derived from Compressor. It implements the Release method while compress and decompress
   /// are left to be implemented by concrete compressors.</summary>
   internal abstract class ReusableCompressor : Compressor
   {
      internal bool inuse { get; set; }

      internal override void release()
      {
         inuse = false;
      }
   }

   /// <summary>This class is designed to implement the compression / decompression behaviour using
   /// the Lzma algorithm. It inherits the Compressor class and ultimately overrides 
   /// the compress () / decompress () functions.</summary>
   internal class LZMACompressor : ReusableCompressor
   {
      private readonly Encoder _internalCompressor;

      /// <summary>Creates LZMA encoder and set its default values. Since this is going to be
      /// a reusble object an encoder with default values will be available.</summary>
      internal LZMACompressor()
      {
         // Create encoder
         _internalCompressor = new Encoder();
         // Set default properties.
         SetEncoderProperties(_internalCompressor);
      }

      /// <summary>Set encoder's default properties.</summary>
      private static void SetEncoderProperties(ISetCoderProperties encoder)
      {
         // Default properties and values
        
         string compressionLevel = HttpClient.GetCompressionLevel().ToUpper();
         Int32 dictionary;
         switch (compressionLevel)
         {
             case HttpClientConsts.HTTP_COMPRESSION_LEVEL_MINIMUM:
               dictionary = 1 << 16;
               break;
             case HttpClientConsts.HTTP_COMPRESSION_LEVEL_MAXIMUM:
               dictionary = 1 << 23;
               break;
            default:
               if (compressionLevel != HttpClientConsts.HTTP_COMPRESSION_LEVEL_NORMAL)
                  Logger.Instance.WriteWarningToLog("Wrong compression level Set to default");
               dictionary = 1 << 20;
               break;
         }

         const int posStateBits = 2;
         const int litContextBits = 3;
         // UInt32 litContextBits = 0; // for 32-bit data
         const int litPosBits = 0;
         // UInt32 litPosBits = 2;     // for 32-bit data
         const int algorithm = 2;
         const int numFastBytes = 128;

         CoderPropID[] propIDs =
            {
               CoderPropID.DictionarySize,
               CoderPropID.PosStateBits,
               CoderPropID.LitContextBits,
               CoderPropID.LitPosBits,
               CoderPropID.Algorithm,
               CoderPropID.NumFastBytes,
               CoderPropID.MatchFinder,
               CoderPropID.EndMarker
            };
         object[] properties =
            {
               (dictionary),
               (posStateBits),
               (litContextBits),
               (litPosBits),
               (algorithm),
               (numFastBytes),
               "bt4",
               false
            };

         // Set default properties for Encoder. 
         encoder.SetCoderProperties(propIDs, properties);
      }

      /// <summary>Function to compress input byte[] contents as per compression level specified.</summary>
      /// <param name="content"> input byte[]</param>
      /// <param name="compressionLevel">compresion level as string </param>
      internal override byte[] compress(byte[] content, String compressionLevel)
      {
         Stream inStream = new MemoryStream(content);
         Stream outStream = new MemoryStream();

         // Write the encoder properties in the begining of compressed contents.
         // While decompressingdecoder reads these properties from compressed cotents.
         _internalCompressor.WriteCoderProperties(outStream);
         
         // Write the size of the uncompressed data in out stream. Decoder will
         // read this while decompressing.
         Int64 inputMessageSize = inStream.Length;
         for (int i = 0; i < 8; i++)
            outStream.WriteByte((Byte) (inputMessageSize >> (8*i)));

         // Compress the data from in stream and write the compressed data to out stream.
         _internalCompressor.Code(inStream, outStream, -1, -1, null);

         // Create a byte array to large enough to hold the compressed data.
         var byteArray = new byte[outStream.Length];

         outStream.Seek(0, SeekOrigin.Begin);

         // Read all bytes from outStream.
         outStream.Read(byteArray, 0, (int) outStream.Length);

         // update the input content byte array
         content = byteArray;

         return content;
      }

      /// <summary>Function to decompress the input byte[] array and get original string.</summary>
      /// <param name="content"> compressed byte array</param>
      internal override byte[] decompress(byte[] content)
      {
         var inStream = new MemoryStream(content);
         var outStream = new MemoryStream();

         // While compressing the properties are written at the beginign of the compressed
         // data. Here we will read those properties. The same properties will be set to
         // the decoder.
         var properties = new byte[5];
         inStream.Position = 0;
         if (inStream.Read(properties, 0, 5) != 5)
         {
            Logger.Instance.WriteExceptionToLog(new Exception("Compressed data is invalid"));
         }
         //Create decoder object and set its properties.
         var decoder = new Decoder();
         decoder.SetDecoderProperties(properties);

         // Read the original size of uncompressed data. It was written in to
         // the compressed data by compressor.
         long outSize = 0;
         for (int i = 0; i < 8; i++)
         {
            int v = inStream.ReadByte();
            if (v < 0)
            {
               Logger.Instance.WriteExceptionToLog(
                  new Exception("Can't Read first byte...Empty stream is recieved in function decompress()"));
            }
            outSize |= ((long) (byte) v) << (8*i);
         }

         // Calculate the actual size of compressed contnets.
         long compressedSize = inStream.Length - inStream.Position;

         // this is the function which decode
         decoder.Code(inStream, outStream, compressedSize, outSize, null);

         //Create byte array enough large to hold decompressed.
         var byteArray = new byte[outStream.Length];
         //Read the decompressed data.
         outStream.Position = 0;
         outStream.Read(byteArray, 0, (int) outStream.Length);

         // update the input content byte array
         content = byteArray;

         return content;
      }
   }

   internal class CompressionException : Exception
   {
      internal CompressionException(String message)
         : base(message)
      {
      }
   }
}
