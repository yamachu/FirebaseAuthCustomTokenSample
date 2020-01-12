using System.Text.Json;

namespace Example.Function.Helpers
{
	static class JsonSerializerOptionsExtensions
	{
		public static JsonSerializerOptions CopyWithoutConverters(this JsonSerializerOptions self)
			=> new JsonSerializerOptions
			{
				AllowTrailingCommas = self.AllowTrailingCommas,
				DefaultBufferSize = self.DefaultBufferSize,
				DictionaryKeyPolicy = self.DictionaryKeyPolicy,
				IgnoreNullValues = self.IgnoreNullValues,
				Encoder = self.Encoder,
				IgnoreReadOnlyProperties = self.IgnoreReadOnlyProperties,
				MaxDepth = self.MaxDepth,
				PropertyNameCaseInsensitive = self.PropertyNameCaseInsensitive,
				PropertyNamingPolicy = self.PropertyNamingPolicy,
				ReadCommentHandling = self.ReadCommentHandling,
				WriteIndented = self.WriteIndented,
			};
	}
}