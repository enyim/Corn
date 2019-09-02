using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Enyim.Corn
{
	public partial class CronExpression: IEquatable<CronExpression>
	{
		private readonly ulong ruleMinute;
		private readonly ulong ruleHour;
		private readonly ulong ruleDayOfMonth;
		private readonly ulong ruleMonth;
		private readonly ulong ruleDayOfWeek;

		private readonly bool hasInterval;
		private string? toStringCache;

		internal CronExpression(ulong ruleMinute, ulong ruleHour, ulong ruleDayOfMonth, ulong ruleMonth, ulong ruleDayOfWeek, bool hasInterval)
		{
			this.ruleMinute = ruleMinute;
			this.ruleHour = ruleHour;
			this.ruleDayOfMonth = ruleDayOfMonth;
			this.ruleMonth = ruleMonth;
			this.ruleDayOfWeek = ruleDayOfWeek;
			this.hasInterval = hasInterval;
		}

		public bool TryGetNext(DateTime value, out DateTime next)
		{
			if (value.Kind != DateTimeKind.Utc) throw new ArgumentOutOfRangeException("Kind must be Utc");

			return TryGetNextInstant(value, out next);
		}

		public bool TryGetNext(DateTimeOffset value, TimeZoneInfo tz, out DateTimeOffset result)
		{
			if (tz is null) throw new ArgumentNullException(nameof(tz));

			result = DateTimeOffset.MinValue;
			var current = value.DateTime;
			var acceptCurrent = false;

			// vixie cron + wikipedia
			if (tz.IsAmbiguousTime(value))
			{
				var periodEnd = tz.GetCurrentPeriodEnd(current);
				var periodEndOffset = periodEnd.Offset;
				var periodEndValue = periodEnd.DateTime;
				var otherOffset = tz.GetOtherAmbigousOffset(periodEnd);

				// is DST?
				if (otherOffset != value.Offset)
				{
					if (!TryGetNextInstant(current, out var guess, acceptCurrent)) return false;
					if (guess < periodEndValue)
					{
						result = new DateTimeOffset(guess, periodEndOffset);
						return true;
					}

					// start of the ambigous period
					// Magadan Standard Time has +11 as base,
					// but they shifted from 12 to 10 at 2014, 10, 26, 0, 0, 0
					current = periodEnd.ToOffset(otherOffset).DateTime;
					acceptCurrent = true;
				}

				if (hasInterval)
				{
					// iterate the transition period
					if (!TryGetNextInstant(current, out var guess, acceptCurrent)) return false;
					if (guess < periodEndValue)
					{
						result = new DateTimeOffset(guess, otherOffset);
						return true;
					}
				}

				current = periodEndValue;
				acceptCurrent = true;
			}

			while (true)
			{
				if (!TryGetNextInstant(current, out var retval, acceptCurrent)) return false;

				if (tz.IsInvalidTime(retval))
				{
					current = retval;
					continue;
				}

				result = tz.IsAmbiguousTime(retval)
							? new DateTimeOffset(retval, tz.GetCurrentPeriodEnd(retval).Offset)
							: new DateTimeOffset(retval, tz.GetUtcOffset(retval));

				return true;
			}
		}

		// acceptCurrent: if the instant matches the expression it should not be incremented
		private bool TryGetNextInstant(DateTime instant, out DateTime result, bool acceptCurrent = false)
		{
			result = DateTime.MinValue;
			var calendar = CultureInfo.CurrentCulture.Calendar;

			var startYear = instant.Year;
			var startMonth = instant.Month;
			var startDay = instant.Day;
			var startHour = instant.Hour;
			var startMinute = instant.Minute;

			var newYear = startYear;
			var newMonth = startMonth;
			var newDay = startDay;
			var newHour = startHour;
			var newMinute = startMinute;

			while (true)
			{
				if ((!acceptCurrent || !ruleMinute.IsNthBitSet(newMinute))
					&& NextOrReset(ruleMinute, ref newMinute))
				{
					newHour++;
				}

				if (!ruleHour.IsNthBitSet(newHour)
					&& NextOrReset(ruleHour, ref newHour))
				{
					newDay++;
				}

				if (!ruleDayOfMonth.IsNthBitSet(newDay)
					&& NextOrReset(ruleDayOfMonth, ref newDay))
				{
					newMonth++;
				}

				if (!ruleMonth.IsNthBitSet(newMonth)
					&& NextOrReset(ruleMonth, ref newMonth))
				{
					newYear++;
				}

				// handles case where expression never matches, e.g. * * 31 2 * (== every minute on Febr 31th)
				if (newYear > DateTime.MaxValue.Year) return false;

				if (newDay <= calendar.GetDaysInMonth(newYear, newMonth)
						&& ruleDayOfWeek.IsNthBitSet(DateTimeHelpers.QuickDayOfWeekNoChecks(newYear, newMonth, newDay)))
				{
					result = new DateTime(newYear, newMonth, newDay, newHour, newMinute, 0, instant.Kind);
					return true;
				}
			}
		}

		private static bool NextOrReset(ulong storage, ref int value)
		{
			value++;

			//      value
			//     /
			// 0001100000
			// |__|
			// remainder
			//
			var remainder = storage >> value;
			// check if there are additional bits set after the current value
			if (remainder == 0)
			{
				// no, roll over
				value = storage.TrailingZeroCount();
				return true;
			}

			// return the index
			value += remainder.TrailingZeroCount();
			return false;
		}

		public override string ToString()
		{
			if (toStringCache == null)
			{
				var sb = new StringBuilder(32);

				FieldDescriptor.Minute.Format(sb, ruleMinute).Append(' ');
				FieldDescriptor.Hour.Format(sb, ruleHour).Append(' ');
				FieldDescriptor.DayOfMonth.Format(sb, ruleDayOfMonth).Append(' ');
				FieldDescriptor.Month.Format(sb, ruleMonth).Append(' ');
				FieldDescriptor.DayOfWeek.Format(sb, ruleDayOfWeek);

				toStringCache = sb.ToString();
			}

			return toStringCache;
		}

		public override int GetHashCode() => HashCode.Combine(ruleMinute, ruleHour, ruleDayOfMonth, ruleMonth, ruleDayOfWeek);
		public override bool Equals(object? obj) => Equals(obj as CronExpression);

		public bool Equals(CronExpression? other)
			=> !(other is null)
				&& (ReferenceEquals(this, other)
					|| (ruleMinute == other.ruleMinute
						&& ruleHour == other.ruleHour
						&& ruleDayOfMonth == other.ruleDayOfMonth
						&& ruleMonth == other.ruleMonth
						&& ruleDayOfWeek == other.ruleDayOfWeek));
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
