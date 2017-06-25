using System;

namespace com.magicsoftware.richclient.data
{
	/// <summary> this exception should be thrown whenever the Data View tries to access a record
	/// outside of its bounds
	/// </summary>
	internal class RecordOutOfDataViewException : ApplicationException
	{
      internal enum ExceptionType
      {
         TOP,
         BOTTOM,
         REC_SUFFIX_FAILED,
         NONE
      }

		// MEMBERS
		private readonly ExceptionType _type = ExceptionType.NONE;
		
		/// <summary> CTOR</summary>
		/// <param name="type">the type </param>
		internal RecordOutOfDataViewException(ExceptionType type) :
         base((type == ExceptionType.REC_SUFFIX_FAILED)? "Record Suffix Failed" : "Record out of the " + (type == ExceptionType.TOP ? "top" : "bottom") + " bound of the Data View")
		{
			_type = type;
		}
		
		/// <summary> get the end of the Data View: T for top end, B for bottom end</summary>
		internal ExceptionType getEnd()
		{
			return _type;
		}
		
		/// <summary> returns true if record is not found in the dataview</summary>
		/// <returns>
		/// </returns>
		internal bool noRecord()
		{
			return (_type == ExceptionType.TOP || _type == ExceptionType.BOTTOM);
		}
	}
}