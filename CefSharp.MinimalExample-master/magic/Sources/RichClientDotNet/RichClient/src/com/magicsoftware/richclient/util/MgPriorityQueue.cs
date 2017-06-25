using System;

namespace com.magicsoftware.richclient.util
{
	/// <summary> 
	/// Our implementation of Priority _queue silimar to JDK 1.5 java.util.PriorityQueue. *
	/// <p>
	/// A priority _queue is unbounded, but has an internal <i>capacity</i> governing the Size of an array used to
	/// store the elements on the _queue. It is always at least as large as the _queue Size. As elements are added to
	/// a priority _queue, its capacity grows automatically. The details of the growth policy are not specified.
	/// </summary>
	internal class MgPriorityQueue
	{
		private const int DEFAULT_INITIAL_CAPACITY = 11;
		
		/// <summary> Priority _queue represented as a balanced binary heap: the two children of _queue[n] are _queue[2*n] and
		/// _queue[2*n + 1]. The priority _queue is ordered by comparator, or by the elements' natural ordering, if
		/// comparator is null: For each node n in the heap and each descendant d of n, n <= d.
		/// 
		/// The element with the lowest value is in _queue[1], assuming the _queue is nonempty. (A one-based array is
		/// used in preference to the traditional 0-based array to simplify parent and child calculations.)
		/// 
		/// _queue.length must be >= 2, even if Size == 0.
		/// </summary>
		private Object[] _queue;
		
		/// <summary> The number of elements in the priority _queue.</summary>
      internal int Size { get; private set; }
		
		/// <summary> Creates a <tt>PriorityQueue</tt> with the default initial capacity (11) that orders its elements
		/// according to their natural ordering.
		/// </summary>
		internal MgPriorityQueue()
		{
			_queue = new Object[DEFAULT_INITIAL_CAPACITY + 1];
		}
		
		/// <summary> Resize array, if necessary, to be able to hold given index</summary>
		private void  grow(int index)
		{
			int newlen = _queue.Length;
			if (index < newlen)
			// don't need to grow
				return ;
			if (index == Int32.MaxValue)
				throw new OutOfMemoryException();
			while (newlen <= index)
			{
				if (newlen >= Int32.MaxValue / 2)
				// avoid overflow
					newlen = Int32.MaxValue;
				else
					newlen <<= 2;
			}
			Object[] newQueue = new Object[newlen];
			Array.Copy(_queue, 0, newQueue, 0, _queue.Length);
			_queue = newQueue;
		}
		
		/// <summary> Inserts the specified element into this priority _queue.
		/// 
		/// </summary>
		/// <returns> <tt>true</tt>
		/// </returns>
		/// <throws>  ClassCastException </throws>
		/// <summary>            if the specified element cannot be compared with elements currently in the priority _queue
		/// according to the priority _queue's ordering.
		/// </summary>
		/// <throws>  NullPointerException </throws>
		/// <summary>            if the specified element is <tt>null</tt>.
		/// </summary>
		internal bool offer(Object o)
		{
			if (o == null)
				throw new NullReferenceException();
			
			++Size;
			
			// Grow backing store if necessary
			if (Size >= _queue.Length)
				grow(Size);
			
			_queue[Size] = o;
			fixUp(Size);
			return true;
		}
		
		internal Object peek()
		{
			if (Size == 0)
				return null;
			return _queue[1];
		}
		
		/// <summary> Returns <tt>true</tt> if _queue contains no elements.
		/// <p>
		/// 
		/// This implementation returns <tt>Size() == 0</tt>.
		/// 
		/// </summary>
		/// <returns> <tt>true</tt> if _queue contains no elements.
		/// </returns>
		internal bool isEmpty()
		{
			return Size == 0;
		}
		
		/// <summary> Removes all elements from the priority _queue. The _queue will be empty after this call returns.</summary>
		internal void  clear()
		{
			
			// Null out element references to prevent memory leak
			for (int i = 1; i <= Size; i++)
				_queue[i] = null;
			
			Size = 0;
		}
		
		internal Object poll()
		{
			if (Size == 0)
				return null;
			
			Object result = _queue[1];
			_queue[1] = _queue[Size];
			_queue[Size--] = null; // Drop extra ref to prevent memory leak
			if (Size > 1)
				fixDown(1);
			
			return result;
		}
		
		/// <summary> Establishes the heap invariant (described above) assuming the heap satisfies the invariant except
		/// possibly for the leaf-node indexed by k (which may have a nextExecutionTime less than its parent's).
		/// 
		/// This method functions by "promoting" _queue[k] up the hierarchy (by swapping it with its parent)
		/// repeatedly until _queue[k] is greater than or equal to its parent.
		/// </summary>
		private void  fixUp(int k)
		{
			while (k > 1)
			{
				int j = k >> 1;
				if (((IComparable) _queue[j]).CompareTo((IComparable) _queue[k]) <= 0)
					break;
				Object tmp = _queue[j];
				_queue[j] = _queue[k];
				_queue[k] = tmp;
				k = j;
			}
		}
		
		/// <summary> Establishes the heap invariant (described above) in the subtree rooted at k, which is assumed to satisfy
		/// the heap invariant except possibly for node k itself (which may be greater than its children).
		/// 
		/// This method functions by "demoting" _queue[k] down the hierarchy (by swapping it with its smaller child)
		/// repeatedly until _queue[k] is less than or equal to its children.
		/// </summary>
		private void  fixDown(int k)
		{
			int j;
			while ((j = k << 1) <= Size && (j > 0))
			{
				if (j < Size && ((IComparable) _queue[j]).CompareTo((IComparable) _queue[j + 1]) > 0)
					j++; // j indexes smallest kid
				
				if (((IComparable) _queue[k]).CompareTo((IComparable) _queue[j]) <= 0)
					break;
				Object tmp = _queue[j];
				_queue[j] = _queue[k];
				_queue[k] = tmp;
				k = j;
			}
		}
	}
}