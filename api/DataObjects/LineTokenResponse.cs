using System.Text.Json.Serialization;

namespace Example.Function.DataObjects
{
	public class LineTokenResponse
	{
		[JsonPropertyName("access_token")]
		public string AccessToken { get; set; }

		[JsonPropertyName("expires_in")]
		public long ExpiresIn { get; set; }

		[JsonPropertyName("refresh_token")]
		public string RefreshToken { get; set; }

		[JsonPropertyName("scope")]
		public string Scope { get; set; }

		[JsonPropertyName("token_type")]
		public string TokenType { get; set; }
	}

	public class LineTokenResponseWithIdToken : LineTokenResponse
	{
		[JsonPropertyName("id_token")]
		public string IdToken { get; set; }
	}
}