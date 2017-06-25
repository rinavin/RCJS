using System;
namespace com.magicsoftware.richclient.util
{
	
	/// <summary> 
	/// A Queue that additionally supports operations that wait for the queue to 
	/// become non-empty when retrieving an element, and wait for space to become 
	/// available in the queue when storing an element.
	/// </summary>
	internal interface MgBlockingQueue
	{
		/// <summary> Inserts the specified element into this queue, if possible.  When
		/// using queues that may impose insertion restrictions (for
		/// example capacity bounds), method <tt>offer</tt> is generally
		/// preferable to method {@link Collection#add}, which can fail to
		/// insert an element only by throwing an exception.
		/// 
		/// </summary>
		/// <param name="Object">o the element to add.
		/// </param>
		/// <returns> <tt>true</tt> if it was possible to add the element to
		/// this queue, else <tt>false</tt>
		/// </returns>
		/// <throws>  NullPointerException if the specified element is <tt>null</tt> </throws>
		bool offer(Object o);
		
		/// <summary> Adds the specified element to this queue, waiting if necessary for
		/// space to become available.
		/// </summary>
		/// <param name="Object">o the element to add
		/// </param>
		/// <throws>  InterruptedException if interrupted while waiting. </throws>
		/// <throws>  NullPointerException if the specified element is <tt>null</tt>. </throws>
		void  put(Object o);
		
		/// <summary> Waits till an element is pushed in to this queue and notifys as soon as 
		/// the element is added to this queue
		/// </summary>
		void  waitForElement();
	}
}