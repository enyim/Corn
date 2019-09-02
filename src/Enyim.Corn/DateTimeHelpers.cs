using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Enyim.Corn
{
	public static class DateTimeHelpers
	{
		private static readonly int[] DaysInAYear = new int[] { 0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365 };
		private static readonly int[] DaysInALeapYear = new int[] { 0, 31, 60, 91, 121, 152, 182, 213, 244, 274, 305, 335, 366 };

		public static int QuickDayOfWeekNoChecks(int year, int month, int day) => (DaysSinceBeginning(year, month, day) + 1) % 7;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int DaysSinceBeginning(int year, int month, int day)
		{
			var days = IsLeapYear(year) ? DaysInALeapYear : DaysInAYear;
			year--;

			var retval = (year * 365)
							+ (year / 4)
							- (year / 100)
							+ (year / 400)
							+ days[month - 1]
							+ day
							- 1;

			return retval;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsLeapYear(int year)
			=> (year & 3) == 0
				&& ((year % 100 == 0)
						? year % 400 == 0
						: true);
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
