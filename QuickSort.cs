using System;
using System.Collections;

namespace Core
{
	// quicksort algorithm is generalized with IComparer and ISwapper
	// System.Collections.IComparer and System.Collections.Comparer already exists,

	public interface ISwapper
	{
		void Swap(IList array, int left, int right);
	}

	public class Swapper : ISwapper
	{
		public Swapper() {}

		public static readonly Swapper Default = new Swapper();

		#region ISwapper Members
		public void Swap(IList array, int left, int right)
		{
			object swap  = array[left];
			array[left]  = array[right];
			array[right] = swap;
		}
		#endregion
	}

	/// <summary>
	/// Summary description for QuickSort.
	/// </summary>
	public class QuickSort
	{
		/// <summary>
		/// The Sort method does a quicksort using the built in comparer and swapper
		/// </summary>
		/// <param name="array">The array to sort.</param>
		public static void Sort(IList array)
		{
			QuickSort.Sort(array, Comparer.Default, Swapper.Default, 0, array.Count-1);
		}

		/// <summary>
		/// Specifies both my comparer and my swapper
		/// </summary>
		/// <param name="array">The array to sort.</param>
		/// <param name="comparer">The custom comparer.</param>
		/// <param name="swapper">The custom swapper.</param>
		public static void Sort(IList array, IComparer comparer, ISwapper swapper)
		{
			QuickSort.Sort(array, comparer, swapper, 0, array.Count-1);
		}

		private static void Sort(IList array, IComparer comparer, ISwapper swapper, int lower, int upper)
		{
			// Check for non-base case
			if (lower < upper)
			{
				// Split and sort partitions
				int split = QuickSort.Pivot(array, comparer, swapper, lower, upper);
				QuickSort.Sort(array, comparer, swapper, lower, split-1);
				QuickSort.Sort(array, comparer, swapper, split+1, upper);
			}
		}

		private static int Pivot(IList array, IComparer comparer, ISwapper swapper, int lower, int upper)
		{
			// Pivot with first element
			int left=lower+1;
			object pivot=array[lower];
			int right=upper;

			// Partition array elements
			while (left <= right)
			{
				// Find item out of place
				while ( (left <= right) && (comparer.Compare(array[left], pivot) <= 0) )
					++left;

				while ( (left <= right) && (comparer.Compare(array[right], pivot) > 0) )
					--right;

				// Swap values if necessary
				if (left < right)
				{
					swapper.Swap(array, left, right);
					++left;
					--right;
				}
			}

			// Move pivot element
			swapper.Swap(array, lower, right);
			return right;
		}
	}
}
