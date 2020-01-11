using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Example.Function.Configurations;
using Example.Function.DataObjects;
using Microsoft.Extensions.Options;

namespace Example.Function.Infrastructures
{
	public interface ILineClient
	{
		Task<LineTokenResponseWithIdToken> GetAccessTokenByCode(string code);
		Task<LineVerifyTokenResponse> VerifyAccessToken(string accessToken);
	}

	public class LineClient : ILineClient
	{
		private readonly HttpClient _client;
		private readonly LineConfig _lineConfig;

		public LineClient(IHttpClientFactory client, IOptions<LineConfig> lineConfig)
		{
			_client = client.CreateClient(nameof(LineClient));
			_lineConfig = lineConfig.Value;
		}

		public async Task<LineTokenResponseWithIdToken> GetAccessTokenByCode(string code)
		{
			var body = new List<KeyValuePair<string, string>>
			{
				new KeyValuePair<string, string>("grant_type", "authorization_code"),
				new KeyValuePair<string, string>("code", code),
				new KeyValuePair<string, string>("redirect_uri", _lineConfig.LineRedirectUrl),
				new KeyValuePair<string, string>("client_id", _lineConfig.LineClientId),
				new KeyValuePair<string, string>("client_secret", _lineConfig.LineClientSecret)
			};

			var content = new FormUrlEncodedContent(body);
			var result = await _client.PostAsync("oauth2/v2.1/token", content).ConfigureAwait(false);
			var responseContent = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
			return System.Text.Json.JsonSerializer.Deserialize<LineTokenResponseWithIdToken>(responseContent);
		}

		public async Task<LineVerifyTokenResponse> VerifyAccessToken(string accessToken)
		{
			var result = await _client.GetAsync($"oauth2/v2.1/verify?access_token={accessToken}").ConfigureAwait(false);
			var responseContent = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
			return System.Text.Json.JsonSerializer.Deserialize<LineVerifyTokenResponse>(responseContent);
		}
	}
}
