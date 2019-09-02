using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Enyim.Corn
{
	internal sealed class FieldDescriptor
	{
		private const string Days = "SUN:MON:TUE:WED:THU:FRI:SAT";
		private const string Months = "JAN:FEB:MAR:APR:MAY:JUN:JUL:AUG:SEP:OCT:NOV:DEC";

		public static readonly FieldDescriptor Minute = new FieldDescriptor(0, 59, default, true);
		public static readonly FieldDescriptor Hour = new FieldDescriptor(0, 23, default, true);

		public static readonly FieldDescriptor DayOfMonth = new FieldDescriptor(1, 31, default);
		public static readonly FieldDescriptor Month = new FieldDescriptor(1, 12, Months.AsMemory());
		public static readonly FieldDescriptor DayOfWeekParse = new FieldDescriptor(0, 7, Days.AsMemory()); // so that 7 can be parsed as 0
		public static readonly FieldDescriptor DayOfWeek = new FieldDescriptor(0, 6, Days.AsMemory()); // 7 will always be converted into 0

		public readonly int Min;
		public readonly int Max;
		public readonly ulong All;
		public readonly bool HasNames;
		public readonly bool TracksInterval;

		private readonly ReadOnlyMemory<char> alternateValues;

		private FieldDescriptor(int min, int max, ReadOnlyMemory<char> alternateValues, bool tracksInterval = false)
		{
			Min = min;
			Max = max;
			All = ((1UL << (max - min + 1)) - 1) << min;
			HasNames = alternateValues.Length > 0;
			TracksInterval = tracksInterval;

			this.alternateValues = alternateValues;
		}

		public int ValueOf(ReadOnlySpan<char> value)
		{
			var index = alternateValues.Span.IndexOf(value, StringComparison.OrdinalIgnoreCase);

			// only return full matches of (^|:)\w\w\w(:|$)
			return (index & 3) == 0 ? (index >> 2) : -1;
		}

		public StringBuilder Format(StringBuilder buffer, ulong value)
		{
			if (value == 0) throw new InvalidOperationException();

			// *
			if (value == All)
				return buffer.Append('*');

			// value
			var popcnt = value.Popcount();
			if (popcnt < 3)
			{
				// a
				var lowest = value.TrailingZeroCount();
				buffer.Append(lowest);

				// a,b
				if (popcnt == 2)
				{
					var highest = value.ResetLowestSetBit();
					buffer.Append(',').Append(highest.TrailingZeroCount());
				}

				return buffer;
			}

			var list = ToArray(value);
			var diff = new int[list.Length - 1];
			var isSame = 0;

			// calculate the difference between the items
			for (var i = 0; i < list.Length - 1; i++)
			{
				var d = list[i + 1] - list[i];
				diff[i] = d;
				isSame |= d;
			}

			// */step or a-b/step
			if (isSame == diff[0])
			{
				return AppendRange(buffer, Min, Max, list[0], list[^1], diff[0]);
			}

			// several of a,b & a-b/step
			var isFirst = true;

			for (var i = 0; i < list.Length; i++)
			{
				var first = i;
				var last = i;

				while (last < diff.Length && diff[last] == diff[first]) last++;

				if (last == diff.Length) last--;

				if (isFirst) isFirst = false;
				else buffer.Append(',');

				if (last - first <= 1)
				{
					buffer.Append(list[first]);
				}
				else
				{
					AppendRange(buffer, Min, Max, list[first], list[last], diff[first]);
					i = last;
				}
			}

			return buffer;
		}

		private int[] ToArray(ulong value)
		{
			var retval = new int[value.Popcount()];

			var max = (uint)retval.Length & (~(uint)3);
			uint i;

			for (i = 0; i < max; i += 4)
			{
				retval[i] = value.TrailingZeroCount();
				value = value.ResetLowestSetBit();

				retval[i + 1] = value.TrailingZeroCount();
				value = value.ResetLowestSetBit();

				retval[i + 2] = value.TrailingZeroCount();
				value = value.ResetLowestSetBit();

				retval[i + 3] = value.TrailingZeroCount();
				value = value.ResetLowestSetBit();
			}

			switch (retval.Length - max)
			{
				case 3: goto L3;
				case 2: goto L2;
				case 1: goto L1;
				default: goto L0;
			}

		L3:
			retval[i++] = value.TrailingZeroCount();
			value = value.ResetLowestSetBit();
		L2:
			retval[i++] = value.TrailingZeroCount();
			value = value.ResetLowestSetBit();
		L1:
			retval[i++] = value.TrailingZeroCount();
		L0:
			return retval;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		private static StringBuilder AppendRange(StringBuilder sb, int fieldMin, int fieldMax, int min, int max, int step)
		{
			if ((min == fieldMin && max == fieldMax)
				|| (min == fieldMin && (max + step) > fieldMax))
			{
				sb.Append('*');
			}
			else
			{
				sb.Append(min).Append('-').Append(max);
			}

			if (step > 1)
			{
				return sb.Append('/').Append(step);
			}

			return sb;
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
