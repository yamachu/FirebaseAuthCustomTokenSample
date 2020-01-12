using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Example.Function.Helpers
{
	public class FirestoreDocumentStringFieldConverter : JsonConverter<string>
	{
		public override string Read(
			ref Utf8JsonReader reader,
			Type typeToConvert,
			JsonSerializerOptions options)
		{
			if (reader.TokenType != JsonTokenType.StartObject)
			{
				throw new JsonException();
			}

			while (reader.Read())
			{
				if (reader.TokenType != JsonTokenType.PropertyName)
				{
					continue;
				}

				var propertyName = reader.GetString();
				if (propertyName == "stringValue")
				{
					if (reader.Read())
					{
						var result = reader.GetString();
						reader.Read(); // To move EndObject
						return result;
					}
				}
				else
				{
					continue;
				}
			}

			throw new JsonException();
		}

		public override void Write(
			Utf8JsonWriter writer,
			string value,
			JsonSerializerOptions options)
		{
			writer.WriteStartObject();
			writer.WriteString("stringValue", value);
			writer.WriteEndObject();
		}
	}

	public class FirestoreDocumentConverter<T> : JsonConverter<T> where T : new()
	{
		public override T Read(
			ref Utf8JsonReader reader,
			Type typeToConvert,
			JsonSerializerOptions options)
		{
			if (reader.TokenType != JsonTokenType.StartObject)
			{
				throw new JsonException();
			}

			var newOptions = options.CopyWithoutConverters().Also(o =>
			{
				foreach (var item in options.Converters.Filter(v => v.GetType() != this.GetType()))
				{
					o.Converters.Add(item);
				}
			});

			while (reader.Read())
			{
				if (reader.TokenType != JsonTokenType.PropertyName || reader.CurrentDepth != 1)
				{
					continue;
				}

				var propertyName = reader.GetString();
				if (propertyName == "fields")
				{
					if (reader.Read())
					{
						var json = JsonSerializer.Deserialize<T>(ref reader, newOptions);
						while (reader.Read()) ;
						return json;
					}
				}
				else
				{
					continue;
				}
			}

			throw new JsonException();
		}

		public override void Write(
			Utf8JsonWriter writer,
			T value,
			JsonSerializerOptions options)
		{
			var newOptions = options.CopyWithoutConverters().Also(o =>
			{
				foreach (var item in options.Converters.Filter(v => v.GetType() != this.GetType()))
				{
					o.Converters.Add(item);
				}
			});
			writer.WriteStartObject();
			writer.WritePropertyName("fields");
			JsonSerializer.Serialize(writer, value, newOptions);
			writer.WriteEndObject();
		}
	}
}