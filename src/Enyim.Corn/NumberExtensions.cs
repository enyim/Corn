using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;

namespace Enyim.Corn
{
	public static class NumberExtensions
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static int TrailingZeroCount(this ulong value)
		{
			if (Bmi1.X64.IsSupported)
			{
				return (int)Bmi1.X64.TrailingZeroCount(value);
			}
			else
			{
				// https://stackoverflow.com/questions/3465098/bit-twiddling-which-bit-is-set
				var lv = (long)value;
				return indexes[((lv & (-lv)) * 0x022fdd63cc95386dL) >> 58];
			}
		}

		private static readonly int[] indexes = new[]
		{
			 0,  1,  2, 53,  3,  7, 54, 27,
			 4, 38, 41,  8, 34, 55, 48, 28,
			62,  5, 39, 46, 44, 42, 22,  9,
			24, 35, 59, 56, 49, 18, 29, 11,
			63, 52,  6, 26, 37, 40, 33, 47,
			61, 45, 43, 21, 23, 58, 17, 10,
			51, 25, 36, 32, 60, 20, 57, 16,
			50, 31, 19, 15, 30, 14, 13, 12
		};

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static ulong ResetLowestSetBit(this ulong value)
		{
			if (Bmi1.X64.IsSupported)
			{
				return Bmi1.X64.ResetLowestSetBit(value);
			}
			else
			{
				return value & (value - 1);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static bool IsNthBitSet(this ulong value, int index)
		{
			if (Bmi1.X64.IsSupported)
			{
				return Bmi1.X64.BitFieldExtract(value, (byte)index, 1) > 0;
			}
			else
			{
				return ((value >> index) & 1) == 1;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static int Popcount(this ulong value)
		{
			if (Popcnt.X64.IsSupported)
			{
				return (int)Popcnt.X64.PopCount(value);
			}
			else
			{
				// https://stackoverflow.com/questions/2709430/count-number-of-bits-in-a-64-bit-long-big-integer
				value = (value - (value >> 1)) & 0x5555555555555555;
				value = (value & 0x3333333333333333) + ((value >> 2) & 0x3333333333333333);

				return (int)((((value + (value >> 4)) & 0xF0F0F0F0F0F0F0F) * 0x101010101010101) >> 56);
			}
		}
	}
}

#region [ License information          ]

/*

Copyright (c) 2019 Attila Kiskó, enyim.com

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

  http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

*/

#endregion
