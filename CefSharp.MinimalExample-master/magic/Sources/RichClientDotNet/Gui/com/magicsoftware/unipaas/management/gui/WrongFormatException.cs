using System;
using com.magicsoftware.util;

namespace com.magicsoftware.unipaas.management.gui
{
	/// <summary> this exception should be thrown whenever the User inserted input
	/// to field which can not be initialized by mask/format, so 
	/// after catching the exception it needs:
	/// set focus back to the field
	/// return to the field old value
	/// write in the status bar relevant message
	/// </summary>
	public class WrongFormatException : Exception
	{
		//type of the field produced the exception
		private readonly string _type;
		
		/// <summary>constructor on case of wrong RANGE ONLY
		/// to insert relevant range to the status bar
		/// </summary>
		public WrongFormatException():base()
		{
			_type = MsgInterface.STR_RNG_TXT;
		}
		
		/// <summary>constructor on case of wrong user input
		/// to insert 'Bad 'type' .'message to status bar
		/// </summary>
		public WrongFormatException(string type):base()
		{
			_type = type;
		}
		
		public string getType()
		{
			return _type;
		}
	}
	
}
