using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Enyim.Corn
{
	// https://docs.microsoft.com/en-us/dotnet/api/system.timezoneinfo.transitiontime.isfixeddaterule?redirectedfrom=MSDN&view=netframework-4.8
	internal static class TimeZoneExtensions
	{
		// the point before DST/Standard change
		public static DateTimeOffset GetCurrentPeriodEnd(this TimeZoneInfo timeZone, DateTime value)
		{
			var next = GetAdjustmentDate(timeZone, value);
			if (next == DateTime.MaxValue) throw new InvalidOperationException("date is not ambigous");

			var list = timeZone.GetAmbiguousTimeOffsets(value);
			var offset = list[0] == timeZone.GetUtcOffset(next) ? list[1] : list[0];

			return new DateTimeOffset(next, offset);
		}

		public static TimeSpan GetOtherAmbigousOffset(this TimeZoneInfo timeZone, DateTimeOffset value)
		{
			var list = timeZone.GetAmbiguousTimeOffsets(value);

			return list[0] == value.Offset ? list[1] : list[0];
		}

		// the exact point of the DST/Standard start
		public static DateTimeOffset GetNextPeriodStart(this TimeZoneInfo timeZone, DateTime value)
		{
			var next = GetAdjustmentDate(timeZone, value);
			if (next == DateTime.MaxValue) throw new InvalidOperationException("date is not ambigous");

			return new DateTimeOffset(next, timeZone.GetUtcOffset(next));
		}

		private static DateTime GetAdjustmentDate(this TimeZoneInfo timeZone, DateTime value)
		{
			var adjustments = timeZone.GetAdjustmentRules();
			if (adjustments.Length == 0) return DateTime.MaxValue;

			var year = value.Year;
			TimeZoneInfo.AdjustmentRule? adjustment = null;

			foreach (var rule in adjustments)
			{
				if (rule.DateStart.Year <= year && rule.DateEnd.Year >= year)
				{
					adjustment = rule;
					break;
				}
			}

			if (adjustment == null) return DateTime.MaxValue;

			var transitionStart = ConvertTransitionTime(year, adjustment.DaylightTransitionStart);
			var transitionEnd = ConvertTransitionTime(year, adjustment.DaylightTransitionEnd);

			if (transitionStart >= value)
			{
				// if adjusment start date is greater than input date then this should be the next transition date
				return transitionStart;
			}

			if (transitionEnd >= value)
			{
				// otherwise adjustment end date should be the next transition date
				return transitionEnd;
			}

			// then it should be the next year's DaylightTransitionStart

			year++;
			foreach (var rule in adjustments)
			{
				// Determine if this adjustment rule covers year desired
				if (rule.DateStart.Year <= year && rule.DateEnd.Year >= year)
				{
					adjustment = rule;
					break;
				}
			}

			return ConvertTransitionTime(year, adjustment.DaylightTransitionStart);
		}

		private static DateTime ConvertTransitionTime(int year, TimeZoneInfo.TransitionTime transitionTime)
		{
			if (transitionTime.IsFixedDateRule)
				return new DateTime(year, transitionTime.Month, transitionTime.Day);

			var calendar = CultureInfo.CurrentCulture.Calendar;

			// Get first day of week for transition
			// For example, the 3rd week starts no earlier than the 15th of the month
			var startOfWeek = (transitionTime.Week * 7) - 6;

			// What day of the week does the month start on?
			var firstDayOfWeek = (int)calendar.GetDayOfWeek(new DateTime(year, transitionTime.Month, 1));

			// Determine how much start date has to be adjusted
			var changeDayOfWeek = (int)transitionTime.DayOfWeek;
			var transitionDay = (firstDayOfWeek <= changeDayOfWeek)
									? startOfWeek + (changeDayOfWeek - firstDayOfWeek)
									: startOfWeek + (7 - firstDayOfWeek + changeDayOfWeek);

			// Adjust for months with no fifth week
			if (transitionDay > calendar.GetDaysInMonth(year, transitionTime.Month))
				transitionDay -= 7;

			return new DateTime(year, transitionTime.Month, transitionDay, transitionTime.TimeOfDay.Hour, transitionTime.TimeOfDay.Minute, transitionTime.TimeOfDay.Second);
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
