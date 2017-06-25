using System;
namespace com.magicsoftware.richclient.util
{
	
	
	/// <summary>Class for JAVA 1.1 containing static methods to sort an arbitrary array
	/// of JAVA objects which implement the IComparable interface.
	/// 
	/// </summary>
	internal class HeapSort
	{
		private static int left(int i)
		{
			return 2 * i + 1;
		}
		private static int right(int i)
		{
			return 2 * i + 2;
		}
		
		/// <summary>Sort array, rearranging the entries in array to be in
		/// sorted order.
		/// 
		/// </summary>
		
		internal static void  sort(IComparable[] array)
		{
			int sortsize = array.Length;
			int i, top, t, largest, l, r, here;
			IComparable temp;
			
			if (sortsize <= 1)
			{
				return ;
			}
			
			top = sortsize - 1;
			t = sortsize / 2;
			
			do 
			{
				--t;
				largest = t;
				
				/* heapify */
				
				do 
				{
					i = largest;
					l = left(largest);
					r = right(largest);
					
					if (l <= top)
					{
						if (array[l].CompareTo(array[i]) > 0)
							largest = l;
					}
					if (r <= top)
					{
						if (array[r].CompareTo(array[largest]) > 0)
							largest = r;
					}
					if (largest != i)
					{
						temp = array[largest];
						array[largest] = array[i];
						array[i] = temp;
					}
				}
				while (largest != i);
			}
			while (t > 0);
			
			t = sortsize;
			
			do 
			{
				--top;
				--t;
				
				here = t;
				
				temp = array[here];
				array[here] = array[0];
				array[0] = temp;
				
				largest = 0;
				
				do 
				{
					i = largest;
					l = left(largest);
					r = right(largest);
					
					if (l <= top)
					{
						if (array[l].CompareTo(array[i]) > 0)
							largest = l;
					}
					if (r <= top)
					{
						if (array[r].CompareTo(array[largest]) > 0)
							largest = r;
					}
					if (largest != i)
					{
						temp = array[largest];
						array[largest] = array[i];
						array[i] = temp;
					}
				}
				while (largest != i);
			}
			while (t > 1);
		}
		
		/// <summary>Sort array, returning an array of integer indices which
		/// access array at various offsets.
		/// 
		/// array[idx[i]] will be the i'th ranked element of array.
		/// e.g.  array[idx[0]] is the smallest element,
		/// array[idx[1]] is the next smallest and so on.
		/// 
		/// array is left in its original order.
		/// 
		/// goofy name of method is because JAVA doesn't allow
		/// methods with the same names and parameters to have
		/// different return types.
		/// 
		/// </summary>
		internal static int[] sortI(IComparable[] array)
		{
			int[] idx = new int[array.Length];
			sort(array, idx);
			return idx;
		}
		
		/// <summary>Sort array, placing an array of integer indices into the
		/// array idx.  If idx is smaller than array, an
		/// IndexOutOfBoundsException will be thrown.
		/// 
		/// array[idx[i]] will be the i'th ranked element of array.
		/// e.g.  array[idx[0]] is the smallest element,
		/// array[idx[1]] is the next smallest and so on.
		/// 
		/// array is left in its original order.
		/// 
		/// </summary>
		internal static void  sort(IComparable[] array, int[] idx)
		{
			int sortsize = array.Length;
			int i, top, t, largest, l, r, here;
			int temp;
			
			if (sortsize <= 1)
			{
				if (sortsize == 1)
					idx[0] = 0;
				return ;
			}
			
			top = sortsize - 1;
			t = sortsize / 2;
			
			for (i = 0; i < sortsize; ++i)
				idx[i] = i;
			
			do 
			{
				--t;
				largest = t;
				
				/* heapify */
				
				do 
				{
					i = largest;
					l = left(largest);
					r = right(largest);
					
					if (l <= top)
					{
						if (array[idx[l]].CompareTo(array[idx[i]]) > 0)
							largest = l;
					}
					if (r <= top)
					{
						if (array[idx[r]].CompareTo(array[idx[largest]]) > 0)
							largest = r;
					}
					if (largest != i)
					{
						temp = idx[largest];
						idx[largest] = idx[i];
						idx[i] = temp;
					}
				}
				while (largest != i);
			}
			while (t > 0);
			
			t = sortsize;
			
			do 
			{
				--top;
				--t;
				
				here = t;
				
				temp = idx[here];
				idx[here] = idx[0];
				idx[0] = temp;
				
				largest = 0;
				
				do 
				{
					i = largest;
					l = left(largest);
					r = right(largest);
					
					if (l <= top)
					{
						if (array[idx[l]].CompareTo(array[idx[i]]) > 0)
							largest = l;
					}
					if (r <= top)
					{
						if (array[idx[r]].CompareTo(array[idx[largest]]) > 0)
							largest = r;
					}
					if (largest != i)
					{
						temp = idx[largest];
						idx[largest] = idx[i];
						idx[i] = temp;
					}
				}
				while (largest != i);
			}
			while (t > 1);
		}
	}
}