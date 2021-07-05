using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorCanvas.Shared
{
	public class ClipRect
	{
		public static readonly int[][] rects = 
		{
			new int[4],
			new int[8],
			new int[12],
			new int[16],
			new int[20],
			new int[24],
			new int[28],
			new int[32],
			new int[36]
		};

		private int _currentRect = 0;
		public int CurrentRect => _currentRect;

		// Could set only the largest list, and copy back
		// to the appropriate size until we have enough arguments
		// supported in the unmarshalled interop
		public ClipRect()
		{
			_currentRect = 0;
			ResetClipRects();
		}

		public bool HasRedrawList => _currentRect != 0;

		public int[] GetRedrawList()
		{
			if( _currentRect == 0)
			{
				throw new Exception("Have not set any rectangles");
			}
			return rects[_currentRect-1];
		}

		public void SetNextClipRect(int x1, int y1, int x2, int y2)
		{
			// Could look to see if an existing rectangle plus this
			// rectangle is completely overlapping (union)
			if( _currentRect > 0)
			{
				var all = rects[rects.Length-1];
				for(int i = 0, offset = 0; i < _currentRect; i++, offset+=4)
				{
					if( x1 >= all[offset] &&
						y1 >= all[offset+1] &&
						x2 <= all[offset+2] &&
						y2 <= all[offset+3] )
					{
						//Console.WriteLine($"Skipping {x1},{y1} {x2},{y2} by {all[offset]}, {all[offset+1]} {all[offset+2]} {all[offset+3]}");
						return;
					}

					// Horizontal extension  ... doesn't work bc gap
					// x1 y1
					//       x2 y1
					// New x1 == old x2 + 1 &&
					// and New y1 = old y1
					// and New y2 = old y2
					if ( x1 - 1 == all[offset+2] &&
						 y1 == all[offset+1] &&
						 y2 == all[offset+3])
					{
						//Console.WriteLine($"Extending {all[offset]}, {all[offset+1]} {all[offset+2]} {all[offset+3]} to {x2}");
						all[offset+2] = x2;
						return;
					}
				}
			}

			Set(CurrentRect, x1, y1, x2, y2);
			_currentRect++;
			if( _currentRect == rects.Length)
			{
				throw new Exception("Not enough clip rectangles");
			}
		}

		public void ResetClipRects()
		{
			_currentRect = 0;
			Reset(0);
		}

		private void Reset(int first)
		{
			Set(first, int.MinValue, int.MinValue, int.MaxValue, int.MaxValue);
			if( first < rects.Length-1)
			{
				Reset(first+1);
			}
		}

		private void Set(int first, int x1, int y1, int x2, int y2)
		{
			var j = first * 4;
			for (var i = first; i < rects.Length; i++)
			{
				var r = rects[i];
				var k = j;
				r[k++] = x1;
				r[k++] = y1;
				r[k++] = x2;
				r[k] = y2;
			}
		}
	}
}
