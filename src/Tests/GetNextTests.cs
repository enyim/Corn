using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace Enyim.Corn.Tests
{
	public class GetNextTests
	{
		[Theory]
		[InlineData("* * * * *", "2019-01-01 10:00", "2019-01-01 10:01")]
		[InlineData("1 2 3 4 5", "2019-01-01 10:00", "2020-04-03 02:01")]
		[InlineData("2 * * * *", "2019-01-01 10:10", "2019-01-01 11:02")]
		[InlineData("* 2 * * *", "2019-01-01 10:10", "2019-01-02 02:00")]
		[InlineData("* * 4 * *", "2019-01-01 10:10", "2019-01-04 00:00")]
		[InlineData("* * 4 * *", "2019-01-08 10:10", "2019-02-04 00:00")]
		[InlineData("* * * 4 *", "2019-01-01 10:10", "2019-04-01 00:00")]
		[InlineData("* * * 4 *", "2019-01-03 10:10", "2019-04-01 00:00")]
		[InlineData("* * * 4 *", "2019-10-01 10:10", "2020-04-01 00:00")]
		[InlineData("* * * 4 *", "2019-10-03 10:10", "2020-04-01 00:00")]
		public void Calculating_NextValue_WithoutTimezone_Should_Succeed(string expression, string current, string expected)
		{
			static DateTime Parse(string value) => DateTime.SpecifyKind(DateTime.ParseExact(value, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture), DateTimeKind.Utc);

			var cron = CronExpression.Parse(expression);
			Assert.True(cron.TryGetNext(Parse(current), out var next));
			Assert.Equal(Parse(expected), next);
		}
	}
}
