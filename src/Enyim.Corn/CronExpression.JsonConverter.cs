using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Enyim.Corn
{
	[JsonConverter(typeof(CronExpressionConverterParse))]
	partial class CronExpression
	{
		private class CronExpressionConverterParse: JsonConverter<CronExpression>
		{
			public override CronExpression Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => CronExpression.Parse(reader.GetString()); // TODO span<byte>
			public override void Write(Utf8JsonWriter writer, CronExpression value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString());
		}

		private class CronExpressionConverterNum: JsonConverter<CronExpression>
		{
			private static readonly JsonEncodedText AttrMinute = JsonEncodedText.Encode("m");
			private static readonly JsonEncodedText AttrHour = JsonEncodedText.Encode("h");
			private static readonly JsonEncodedText AttrDayOfMonth = JsonEncodedText.Encode("dm");
			private static readonly JsonEncodedText AttrMonth = JsonEncodedText.Encode("M");
			private static readonly JsonEncodedText AttrDayOfWeek = JsonEncodedText.Encode("dw");
			private static readonly JsonEncodedText AttrHasInterval = JsonEncodedText.Encode("i");

			public override CronExpression Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
			{
				Expect(ref reader, JsonTokenType.StartObject);

				var ruleMinute = ExpectUInt64(ref reader, AttrMinute);
				var ruleHour = ExpectUInt64(ref reader, AttrHour);
				var ruleDayOfMonth = ExpectUInt64(ref reader, AttrDayOfMonth);
				var ruleMonth = ExpectUInt64(ref reader, AttrMonth);
				var ruleDayOfWeek = ExpectUInt64(ref reader, AttrDayOfWeek);
				var hasInterval = ExpectBool(ref reader, AttrHasInterval);

				Expect(ref reader, JsonTokenType.EndObject);

				return new CronExpression(ruleMinute, ruleHour, ruleDayOfMonth, ruleMonth, ruleDayOfWeek, hasInterval);
			}

			private static ulong ExpectUInt64(ref Utf8JsonReader reader, JsonEncodedText propName)
			{
				if (reader.Read()
					&& reader.TokenType == JsonTokenType.PropertyName
					&& reader.ValueTextEquals(propName.EncodedUtf8Bytes)
					&& reader.Read()
					&& reader.TryGetUInt64(out var retval))
				{
					return retval;
				}

				throw new InvalidOperationException($"Expected uint64 property `{propName}` but got {reader.TokenType} -- {reader.GetString()}");
			}

			private static bool ExpectBool(ref Utf8JsonReader reader, JsonEncodedText propName)
			{
				if (reader.Read()
					&& reader.TokenType == JsonTokenType.PropertyName
					&& reader.ValueTextEquals(propName.EncodedUtf8Bytes)
					&& reader.Read())
				{
					switch (reader.TokenType)
					{
						case JsonTokenType.True: return true;
						case JsonTokenType.False: return false;
					}
				}

				throw new InvalidOperationException($"Expected boolean property `{propName}` but got {reader.TokenType} -- {reader.GetString()}");
			}

			private static void Expect(ref Utf8JsonReader reader, JsonTokenType token, bool advance = true)
			{
				if ((advance && !reader.Read()) || reader.TokenType != token)
					throw new InvalidOperationException($"Expected {token} but got {reader.TokenType} at {reader.TokenStartIndex}");
			}

			public override void Write(Utf8JsonWriter writer, CronExpression value, JsonSerializerOptions options)
			{
				writer.WriteStartObject();

				writer.WriteNumber(AttrMinute, value.ruleMinute);
				writer.WriteNumber(AttrHour, value.ruleHour);
				writer.WriteNumber(AttrDayOfMonth, value.ruleDayOfMonth);
				writer.WriteNumber(AttrMonth, value.ruleMonth);
				writer.WriteNumber(AttrDayOfWeek, value.ruleDayOfWeek);
				writer.WriteBoolean(AttrHasInterval, value.hasInterval);

				writer.WriteEndObject();
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
