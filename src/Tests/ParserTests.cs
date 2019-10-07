using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Xunit;

namespace Enyim.Corn.Tests
{
	public class ParserTests
	{
		[Theory]

		// basic checks
		[InlineData("1      * * * *", "1     * * * *")]
		[InlineData("1,2    * * * *", "1,2   * * * *")]
		[InlineData("2-4/2  * * * *", "2,4   * * * *")]
		[InlineData("*/60   * * * *", "0     * * * *")]
		[InlineData("0/30   * * * *", "0,30  * * * *")]
		[InlineData("2,*/60 * * * *", "0,2   * * * *")]
		[InlineData("1-5/2  * * * *", "1-5/2 * * * *")]
		[InlineData("1,*,3  * * * *", "*     * * * *")]
		[InlineData("1-10   * * * *", "1-10  * * * *")]

		[InlineData("* 1      * * *", "* 1     * * *")]
		[InlineData("* 1,2    * * *", "* 1,2   * * *")]
		[InlineData("* 2-4/2  * * *", "* 2,4   * * *")]
		[InlineData("* */60   * * *", "* 0     * * *")]
		[InlineData("* 2,*/60 * * *", "* 0,2   * * *")]
		[InlineData("* 1-5/2  * * *", "* 1-5/2 * * *")]
		[InlineData("* 1,*,3  * * *", "* *     * * *")]
		[InlineData("* 1-10   * * *", "* 1-10  * * *")]

		[InlineData("* * 1      * *", "* * 1     * *")]
		[InlineData("* * 1,2    * *", "* * 1,2   * *")]
		[InlineData("* * 2-4/2  * *", "* * 2,4   * *")]
		[InlineData("* * */60   * *", "* * 1     * *")]
		[InlineData("* * 2,*/60 * *", "* * 1,2   * *")]
		[InlineData("* * 1-5/2  * *", "* * 1-5/2 * *")]
		[InlineData("* * 1,*,3  * *", "* * *     * *")]
		[InlineData("* * 1-10   * *", "* * 1-10  * *")]

		[InlineData("* * * 1      *", "* * * 1     *")]
		[InlineData("* * * 1,2    *", "* * * 1,2   *")]
		[InlineData("* * * 2-4/2  *", "* * * 2,4   *")]
		[InlineData("* * * */60   *", "* * * 1     *")]
		[InlineData("* * * 2,*/60 *", "* * * 1,2   *")]
		[InlineData("* * * 1-5/2  *", "* * * 1-5/2 *")]
		[InlineData("* * * 1,*,3  *", "* * * *     *")]
		[InlineData("* * * 1-10   *", "* * * 1-10  *")]

		[InlineData("* * * * 1     ", "* * * * 1    ")]
		[InlineData("* * * * 1,2   ", "* * * * 1,2  ")]
		[InlineData("* * * * 2-4/2 ", "* * * * 2,4  ")]
		[InlineData("* * * * */60  ", "* * * * 0    ")]
		[InlineData("* * * * 2,*/60", "* * * * 0,2  ")]
		[InlineData("* * * * 1-5/2 ", "* * * * 1-5/2")]
		[InlineData("* * * * 1,*,3 ", "* * * * *    ")]
		[InlineData("* * * * 1-10  ", "* * * * *")]
		[InlineData("* * * * 1-5  ", "* * * * 1-5")]
		[InlineData("* * * * 7", "* * * * 0")]

		// named days
		[InlineData("* * * * Sun", "* * * * 0")]
		[InlineData("* * * * MON", "* * * * 1")]
		[InlineData("* * * * tue", "* * * * 2")]
		[InlineData("* * * * wEd", "* * * * 3")]
		[InlineData("* * * * Thu", "* * * * 4")]
		[InlineData("* * * * frI", "* * * * 5")]
		[InlineData("* * * * SaT", "* * * * 6")]
		[InlineData("* * * * Mon,Sun  ", "* * * * 0,1")]
		[InlineData("* * * * Tue-Thu  ", "* * * * 2-4")]

		// some edge cases
		[InlineData("* * * * *", "* * * * *")]
		[InlineData("*/23 */3 */10 */4 */2", "*/23 */3 */10 */4 */2")]
		[InlineData("*/23 */3 1-30 1-12 0,1,2,3,4-6", "*/23 */3 1-30 * *")]

		[InlineData("1-10/2 * * * *", "1-9/2 * * * *")]
		[InlineData("? 1,2,4,8,16 ? ? ?", "* 1,2,4,8,16 * * *")]
		[InlineData("* * 1,2,4,5,10,11 * *", "* * 1,2,4,5,10,11 * *")]
		[InlineData("* * * */3,2,8,10 *", "* * * 1,2,4,7,8,10 *")]

		// macros should be expanded
		[InlineData("@every_minute", "* * * * *")]
		[InlineData("@hourly", "0 * * * *")]
		[InlineData("@daily", "0 0 * * *")]
		[InlineData("@midnight", "0 0 * * *")]
		[InlineData("@monthly", "0 0 1 * *")]
		[InlineData("@yearly", "0 0 1 1 *")]
		[InlineData("@annually", "0 0 1 1 *")]
		[InlineData("@weekly", "0 0 * * 0")]

		// extra whitespace is ignored
		[InlineData("*  *  *  *  *  ", "* * * * *")]
		[InlineData("  *  *  *  *  *", "* * * * *")]
		[InlineData("   @annually    ", "0 0 1 1 *")]

		public void Parsing_ValidExpressions_Should_Succeed(string input, string expected)
		{
			expected = Regex.Replace(expected, @"\s+", " ").Trim();

			Assert.True(CronExpression.TryParse(input, out var cron));

			Assert.Equal(expected, cron!.ToString());
			Assert.Equal(expected, CronExpression.Parse(input).ToString());
			Assert.Equal(expected, CronExpression.Parse(expected).ToString());
		}

		[Theory]
		[InlineData("*")]
		[InlineData("* *")]
		[InlineData("* * *")]
		[InlineData("* * * *")]
		[InlineData("*****")]
		[InlineData("0,1,2,61 * * * *")]
		[InlineData("* 22,23,24 * * *")]
		[InlineData("* * 30,31,32 * *")]
		[InlineData("* * 0,1,2 * *")]
		[InlineData("* * * 0,1,2 *")]
		[InlineData("* * * 11,12,13 *")]
		[InlineData("* * * * 5,6,7,8")]
		[InlineData("0- * * * *")]
		[InlineData("-1 * * * *")]
		[InlineData("1,2, * * * *")]
		[InlineData("1-3/ 1 * * *")]
		[InlineData("1//1 * * * *")]
		[InlineData("1/ 1 * * *")]
		[InlineData("1-/1 * * *")]

		[InlineData("* * * * :Su")]
		[InlineData("* * * * :mo")]
		[InlineData("* * * * on:")]
		[InlineData("* * * * n:t")]
		[InlineData("* * * * :mon")]
		[InlineData("* * * * mon:")]

		[InlineData("* * * Sun *")]
		[InlineData("* * * MON *")]
		[InlineData("* * * tue *")]
		[InlineData("* * * wEd *")]
		[InlineData("* * * Thu *")]
		[InlineData("* * * frI *")]
		[InlineData("* * * SaT *")]

		[InlineData("* * Sun * *")]
		[InlineData("* * MON * *")]
		[InlineData("* * tue * *")]
		[InlineData("* * wEd * *")]
		[InlineData("* * Thu * *")]
		[InlineData("* * frI * *")]
		[InlineData("* * SaT * *")]

		[InlineData("* Sun * * *")]
		[InlineData("* MON * * *")]
		[InlineData("* tue * * *")]
		[InlineData("* wEd * * *")]
		[InlineData("* Thu * * *")]
		[InlineData("* frI * * *")]
		[InlineData("* SaT * * *")]

		[InlineData("Sun * * * *")]
		[InlineData("MON * * * *")]
		[InlineData("tue * * * *")]
		[InlineData("wEd * * * *")]
		[InlineData("Thu * * * *")]
		[InlineData("frI * * * *")]
		[InlineData("SaT * * * *")]

		public void Parsing_InvalidExpressions_Should_Fail(string input)
		{
			Assert.False(CronExpression.TryParse(input, out var cron));
			Assert.Throws<FormatException>(() => CronExpression.Parse(input).ToString());
		}
	}
}
