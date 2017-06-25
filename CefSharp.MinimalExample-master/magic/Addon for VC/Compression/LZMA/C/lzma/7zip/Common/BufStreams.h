// BufStreams.h
#include <strstream>
#include "../IStream.h"
#include "../../Common/MyCom.h"
using namespace std;

//----------------------------------------------------------------------------------------- 
// New class to implement compressed / decompressed InStream
//----------------------------------------------------------------------------------------- 
class CInByteStream:
   public ISequentialInStream,
   public CMyUnknownImp
{     
   strstream *is;
   // varible to store inputMessage size
   UInt32 streamSize;

public: 
   // Function returns the input message size.
   UInt32 getStreamSize()
   {       
      return streamSize;
   }  

   MY_UNKNOWN_IMP2(IInStream, IStreamGetSize)

      // Default constructor.
      CInByteStream() { }

   // parameterized constructor
   CInByteStream(char *buf, long len)
   {
      is = new strstream (buf, len, ios_base::binary);
      streamSize = len;
   }

   ~CInByteStream() 
   {
      delete is;
   }

   STDMETHOD(Read)(void *data, UInt32 size, UInt32 *processedSize);
};


//----------------------------------------------------------------------------------------- 
// New class to implement compressed / decompressed OutStream
//----------------------------------------------------------------------------------------- 
class COutByteStream:
   public ISequentialOutStream,
   public CMyUnknownImp
{
private :
   strstream *os; 

public:   
   // while compressing / decompressing, ProcessedSize variable is used to store
   // actual processed bytes.
   UInt64 ProcessedSize;
   COutByteStream() 
   {
      os = new strstream();
      ProcessedSize = 0; 
   }
   ~COutByteStream() 
   {
      delete os;
   }
   MY_UNKNOWN_IMP2(IInStream, IStreamGetSize)

      STDMETHOD(Write)(const void *data, UInt32 size, UInt32 *processedSize); 

   // Function is used to get compressed / decompressed data back in Mbuffer object.
   void *GetData (long *len)
   {
      *len = (long)os->pcount();
      return os->rdbuf()->str();
   }

   /*---------------------------------------------------------------------------*/
   /* strstream manages its own memory as long as you don't request physical    */
   /* address of the memory (by calling rdbuf()->str()). Once it return you the */
   /* physical address it can not allocate more memory because it may require   */
   /* moving the memory and hence strstream goes in to freeze state and you are */
   /* not allowed to write more bytes to the stream.In addition the stream is no*/
   /* longer responsible for cleaning up the storage.By Calling UnFreeze        */
   /* (freeze(0)) we are giving control back to strstream. Now it will clean    */
   /* storage and you are allowed to write more bytes to the stream. Note that  */
   /* after calling UnFreeze do not use the previosuly returned char pointer it */
   /* may not be reliable after adding more bytes.                              */
   /* Ref : http://www.codeguru.com/cpp/tic/tic0191.shtml                       */
   /* Use this function always after calling GetData ()                         */
   /*---------------------------------------------------------------------------*/
   void  UnFreeze ()
   {
      os->rdbuf()->freeze(false);
   }
};
//----------------------------------------------------------------------------------------- 



