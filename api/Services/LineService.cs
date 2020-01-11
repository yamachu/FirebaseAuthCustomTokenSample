using System;
using System.Text;
using System.Threading.Tasks;
using Example.Function.Configurations;
using Example.Function.DataObjects;
using Example.Function.Infrastructures;
using Example.Function.Helpers;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Example.Function.Services
{
	public interface ILineService
	{
		Task<LineIdentify> GetLineIdentify(string code, string nonce);
		string GetAuthorizeRequestURL(AuthTemporary authTemp);
	}

	public class LineService : ILineService
	{
		private readonly ILineClient _client;
		private readonly LineConfig _lineConfig;

		public LineService(ILineClient client, IOptions<LineConfig> lineConfig)
		{
			_client = client;
			_lineConfig = lineConfig.Value;
		}

		private System.Security.Claims.ClaimsPrincipal VerifyJWT(string jwtToken, string channelId, string clientSecretKey)
		{
			var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clientSecretKey));
			var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
			var validationParams = new TokenValidationParameters
			{
				ValidIssuer = "https://access.line.me",
				IssuerSigningKey = key,
				ValidAudience = channelId,
			};

			return handler.ValidateToken(jwtToken, validationParams, out SecurityToken _);
		}

		public async Task<LineIdentify> GetLineIdentify(string code, string nonce)
		{
			var tokenResponse = await _client.GetAccessTokenByCode(code);
			var accessTokenVerifyResponse = await _client.VerifyAccessToken(tokenResponse.AccessToken);
			if (accessTokenVerifyResponse.ClientId != _lineConfig.LineClientId) throw new Exception("clientId does not match");

			var claims = VerifyJWT(tokenResponse.IdToken, _lineConfig.LineClientId, _lineConfig.LineClientSecret);
			if (claims.FindFirst("nonce").Value != nonce) throw new Exception("nonce does not match");

			return new LineIdentify
			{
				// sub
				Id = claims.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value,
				AccessToken = tokenResponse.AccessToken
			};
		}

		public string GetAuthorizeRequestURL(AuthTemporary authTemp)
			=> new System.UriBuilder("https://access.line.me/oauth2/v2.1/authorize")
			{
				Query = System.Web.HttpUtility.ParseQueryString(String.Empty).Also(v =>
				{
					v.Add("response_type", "code");
					v.Add("client_id", _lineConfig.LineClientId);
					v.Add("redirect_uri", _lineConfig.LineRedirectUrl);
					v.Add("state", authTemp.State);
					v.Add("scope", String.Join(" ", new[] { "profile", "openid" }));
					v.Add("nonce", authTemp.Nonce);
				}).ToString()
			}.Uri.ToString().Replace("+", "%20");
	}
}
