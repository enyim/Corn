using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Enyim.Corn
{
	partial class CronExpression
	{
		private const ulong Rule_Zero = 1 << 0;
		private const ulong Rule_One = 1 << 1;

		public static readonly CronExpression EveryMinute = new CronExpression(FieldDescriptor.Minute.All, FieldDescriptor.Hour.All, FieldDescriptor.DayOfMonth.All, FieldDescriptor.Month.All, FieldDescriptor.DayOfWeek.All, true);
		public static readonly CronExpression EveryHour = new CronExpression(Rule_Zero, FieldDescriptor.Hour.All, FieldDescriptor.DayOfMonth.All, FieldDescriptor.Month.All, FieldDescriptor.DayOfWeek.All, true);
		public static readonly CronExpression EveryDay = new CronExpression(Rule_Zero, Rule_Zero, FieldDescriptor.DayOfMonth.All, FieldDescriptor.Month.All, FieldDescriptor.DayOfWeek.All, false);
		public static readonly CronExpression EveryMonth = new CronExpression(Rule_Zero, Rule_Zero, Rule_One, FieldDescriptor.Month.All, FieldDescriptor.DayOfWeek.All, false);
		public static readonly CronExpression EveryYear = new CronExpression(Rule_Zero, Rule_Zero, Rule_One, Rule_One, FieldDescriptor.DayOfWeek.All, false);
		public static readonly CronExpression EveryWeek = new CronExpression(Rule_Zero, Rule_Zero, FieldDescriptor.DayOfMonth.All, FieldDescriptor.Month.All, Rule_Zero, false);

		public static CronExpression Parse(string value)
		{
			return Parse(value.AsSpan());
		}

		public static CronExpression Parse(ReadOnlySpan<char> span)
		{
			if (!TryParse(span, out var retval))
				throw new FormatException("String was not a valid cron expression");

			return retval;
		}

		private static CronExpression? TryParseMacros(ReadOnlySpan<char> s)
		{
			var reader = s;

			if (ReadChar(ref reader, '@'))
			{
				if (reader.CompareTo("every_minute", StringComparison.OrdinalIgnoreCase) == 0) return EveryMinute;
				if (reader.CompareTo("hourly", StringComparison.OrdinalIgnoreCase) == 0) return EveryHour;
				if (reader.CompareTo("daily", StringComparison.OrdinalIgnoreCase) == 0) return EveryDay;
				if (reader.CompareTo("midnight", StringComparison.OrdinalIgnoreCase) == 0) return EveryDay;
				if (reader.CompareTo("monthly", StringComparison.OrdinalIgnoreCase) == 0) return EveryMonth;
				if (reader.CompareTo("yearly", StringComparison.OrdinalIgnoreCase) == 0) return EveryYear;
				if (reader.CompareTo("annually", StringComparison.OrdinalIgnoreCase) == 0) return EveryYear;
				if (reader.CompareTo("weekly", StringComparison.OrdinalIgnoreCase) == 0) return EveryWeek;
			}

			return null;
		}

		public static bool TryParse(ReadOnlySpan<char> s, [NotNullWhen(true)] out CronExpression? result)
		{
			var reader = s.Trim();

			var macro = TryParseMacros(reader);
			if (macro != null)
			{
				result = macro;
				return true;
			}

			var hasInterval = false;

			if (TryParse(FieldDescriptor.Minute, ref reader, out var ruleMinute, ref hasInterval)
					&& WhiteSpaceAtLeastOnce(ref reader)
				&& TryParse(FieldDescriptor.Hour, ref reader, out var ruleHour, ref hasInterval)
					&& WhiteSpaceAtLeastOnce(ref reader)
				&& TryParse(FieldDescriptor.DayOfMonth, ref reader, out var ruleDayOfMonth, ref hasInterval)
					&& WhiteSpaceAtLeastOnce(ref reader)
				&& TryParse(FieldDescriptor.Month, ref reader, out var ruleMonth, ref hasInterval)
					&& WhiteSpaceAtLeastOnce(ref reader)
				&& TryParse(FieldDescriptor.DayOfWeekParse, ref reader, out var ruleDayOfWeek, ref hasInterval)
				&& reader.IsEmpty)
			{
				// Sunday is both 0 and 7
				if (ruleDayOfWeek.IsNthBitSet(7))
				{
					ruleDayOfWeek |= Rule_Zero;
					ruleDayOfWeek &= ~(1u << 7);
				}

				result = new CronExpression(ruleMinute, ruleHour, ruleDayOfMonth, ruleMonth, ruleDayOfWeek, hasInterval);

				return true;
			}

			result = default;
			return false;
		}

		private static bool WhiteSpaceAtLeastOnce(ref ReadOnlySpan<char> s)
		{
			var i = 0;
			while (i < s.Length && Char.IsWhiteSpace(s[i])) i++;

			if (i > 0)
			{
				s = s.Slice(i);
				return true;
			}

			return false;
		}

		private static bool TryParse(FieldDescriptor descriptor, ref ReadOnlySpan<char> s, out ulong rule, ref bool hasInterval)
		{
			rule = 0;

			return TryReadList(descriptor, ref s, ref rule, ref hasInterval);
		}

		private static bool TryReadList(FieldDescriptor descriptor, ref ReadOnlySpan<char> s, ref ulong rule, ref bool hasInterval)
		{
			var reader = s;
			var didFirst = false;

			// rule,rule,rule,...
			while (!reader.IsEmpty)
			{
				if (!didFirst) didFirst = true;
				else if (!ReadChar(ref reader, ','))
					break;

				if (ReadRule(descriptor, ref reader, ref rule, ref hasInterval))
					continue;

				return false;
			}

			if (!didFirst) return false;

			s = reader;
			return true;
		}

		private static bool ReadRule(FieldDescriptor descriptor, ref ReadOnlySpan<char> s, ref ulong rule, ref bool hasInterval)
		{
			var reader = s;

			// ?|*[/d]
			if (TryReadStar(descriptor, ref s, ref rule))
			{
				if (descriptor.TracksInterval) hasInterval = true;
				return true;
			}

			// from[-to][/step]
			if (!ReadScalar(descriptor, ref reader, out var start)) return false;

			// from/step
			if (TryReadStep(ref reader, out var step))
			{
				SetRange(ref rule, Math.Max(start, descriptor.Min), descriptor.Max, step);
				if (descriptor.TracksInterval) hasInterval = true;
			}
			// from-to
			else if (ReadChar(ref reader, '-'))
			{
				if (!ReadScalar(descriptor, ref reader, out var stop)) return false;
				if (stop < descriptor.Min || start > descriptor.Max) return false;

				// from-to/step
				if (!TryReadStep(ref reader, out step)) step = 1;
				SetRange(ref rule, Math.Max(start, descriptor.Min), Math.Min(stop, descriptor.Max), step);
				if (descriptor.TracksInterval) hasInterval = true;
			}
			else
			{
				// single value
				if (start < descriptor.Min || start > descriptor.Max) return false;
				rule |= 1UL << start;
			}

			s = reader;
			return true;
		}

		private static bool ReadScalar(FieldDescriptor descriptor, ref ReadOnlySpan<char> s, out int result)
		{
			if (TryReadNN(ref s, out result)) return true;

			if (s.Length >= 3 && descriptor.HasNames)
			{
				var index = descriptor.ValueOf(s.Slice(0, 3));
				if (index > -1)
				{
					s = s.Slice(3);
					result = index;
					return true;
				}
			}

			result = default;
			return false;
		}

		private static bool TryReadStar(FieldDescriptor descriptor, ref ReadOnlySpan<char> s, ref ulong rule)
		{
			// ? does not support steps
			if (ReadChar(ref s, '?'))
			{
				rule = descriptor.All;
				return true;
			}

			var reader = s;

			// *[/dd]
			if (!ReadChar(ref reader, '*')) return false;

			if (TryReadStep(ref reader, out var step))
			{
				// never fails, as no matter how large `step` is, min is always set in the range
				SetRange(ref rule, descriptor.Min, descriptor.Max, step);
			}
			else
			{
				rule = descriptor.All;
			}

			s = reader;
			return true;
		}

		private static bool TryReadStep(ref ReadOnlySpan<char> s, out int step)
		{
			var reader = s;

			if (ReadChar(ref reader, '/')
				&& TryReadNN(ref reader, out step))
			{
				s = reader;
				return true;
			}

			step = default;
			return false;
		}

		private static bool TryReadNN(ref ReadOnlySpan<char> s, out int step)
		{
			if (s.Length > 0)
			{
				var a = s[0];

				if (a >= '0' && a <= '9')
				{
					s = s.Slice(1);
					step = a - '0';

					if (s.Length > 0)
					{
						var b = s[0];
						if (b >= '0' && b <= '9')
						{
							s = s.Slice(1);
							step = (step * 10) + (b - '0');
						}
					}

					return true;
				}
			}

			step = 0;
			return false;
		}

		private static bool ReadChar(ref ReadOnlySpan<char> s, char c)
		{
			if (s.Length > 0 && s[0] == c)
			{
				s = s.Slice(1);
				return true;
			}

			return false;
		}

		private static void SetRange(ref ulong input, int min, int max, int step)
		{
			for (var i = min; i <= max; i += step)
			{
				input |= 1UL << i;
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
