// BufStreams.cpp

#include "BufStreams.h"
#include <iostream>
#include <fstream>

using namespace std;


static inline HRESULT ConvertBoolToHRESULT(bool result)
{
  #ifdef _WIN32
  if (result)
    return S_OK;
  DWORD lastError = ::GetLastError();
  if (lastError == 0)
    return E_FAIL;
  return HRESULT_FROM_WIN32(lastError);
  #else
  return result ? S_OK: E_FAIL;
  #endif
} 

//-----------------------------------------------------------------------------------------
// Function reads the compressed / decompressed contents to output stream 
//-----------------------------------------------------------------------------------------
STDMETHODIMP CInByteStream::Read(void *data, UInt32 size, UInt32 *processedSize)
{ 
   UInt32 realProcessedSize = size;
   bool result = false;
   is->read((char *)data, size);
   if(size)
      result = true;
   realProcessedSize = (UInt32)is->gcount(); 

   if(processedSize != NULL)
      *processedSize = realProcessedSize;
   return ConvertBoolToHRESULT(result);
}

//-----------------------------------------------------------------------------------------
//  Function writes the compressed / decompressed contents to output stream 
//-----------------------------------------------------------------------------------------
STDMETHODIMP COutByteStream::Write(const void *data, UInt32 size, UInt32 *processedSize)
{ 
   UInt32 realProcessedSize = size;   
   bool result = os->write((char *)data,size) ? true : false ;  
   ProcessedSize += realProcessedSize;
   if(processedSize != NULL)
      *processedSize = realProcessedSize;
   return ConvertBoolToHRESULT(result); 
  
  
}
//-----------------------------------------------------------------------------------------  
