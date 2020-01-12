using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using LanguageExt;
using static LanguageExt.Prelude;
using Example.Function.DataObjects;
using Example.Function.Infrastructures;
using Example.Function.Services;

namespace Example.Function
{
	public class LineTokenVerifier
	{
		private readonly ILineService _lineService;
		private readonly IFirebaseService _firebaseService;
		private readonly IAuthTemporaryClient _authTempClient;

		public LineTokenVerifier(ILineService lineService, IFirebaseService firebaseAdminService, IAuthTemporaryClient authTemporaryClient)
		{
			_lineService = lineService;
			_firebaseService = firebaseAdminService;
			_authTempClient = authTemporaryClient;
		}

		// https://developers.line.biz/ja/docs/line-login/web/integrate-line-login/
		// Generate `https://access.line.me/oauth2/v2.1/authorize` url
		[FunctionName("GenerateAuthorizeRequestUrl")]
		public async Task<IActionResult> GenerateAuthorizeRequestUrlHandler(
			[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
			ILogger log)
		{
			log.LogInformation("Start generating authorize request for line.");
			return await (
				from token in ParseAuthorizationToken(req)
				let authTemp = AuthTemporary.GenerateRandomAuthTemporary()
				from _ in TryAsync(() => _authTempClient.StoreAuthTemporary(token.Uid, authTemp).ToUnit())
					.ToEither(e => e.Exception.IfNone(new Exception(e.Message)))
				let url = _lineService.GetAuthorizeRequestURL(authTemp)
				select url
			).Case.ConfigureAwait(false) switch
			{
				RightCase<Exception, string>(var t) => new OkObjectResult(new { Url = t }),
				// Todo: log exception
				_ => new BadRequestObjectResult("error occured, please see logs"),
			};
		}

		[FunctionName("LineTokenVerify")]
		public async Task<IActionResult> LineTokenVerifyHandler(
			[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
			ILogger log)
		{
			log.LogInformation("Start verifying line oauth code.");
			return await (
				from token in ParseAuthorizationToken(req)
				from requestBody in TryAsync(() =>
				{
					using var sr = new StreamReader(req.Body);
					return sr.ReadToEndAsync();
				}).ToEither(e => e.Exception.IfNone(new Exception(e.Message)))
				from json in Try(() => System.Text.Json.JsonSerializer.Deserialize<LineTokenRequest>(requestBody, new System.Text.Json.JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				})).ToEither((e) => new Exception("invalid json object", e)).ToAsync()
				from authTemp in TryAsync(() => _authTempClient.RestoreAuthTemporary(token.Uid)).ToEither(e => e.Exception.IfNone(new Exception(e.Message)))
				from _ in (authTemp.State == json.State)
					? RightAsync<Exception, Unit>(Unit.Default)
					: LeftAsync<Exception, Unit>(new Exception("State is not matched"))
				from lineIdentity in TryAsync(() => _lineService.GetLineIdentify(json.Code, authTemp.Nonce))
					.ToEither(e => e.Exception.IfNone(new Exception(e.Message)))
				from customToken in TryAsync(() => _firebaseService.CreateCustomToken(token.Uid, lineIdentity.Id, "line"))
					.ToEither(e => e.Exception.IfNone(new Exception(e.Message)))
				select customToken
			).Case switch
			{
				RightCase<Exception, string>(var t) => new OkObjectResult(new { CustomToken = t }),
				// Todo: log exception
				_ => new BadRequestObjectResult("error occured, please see logs"),
			};
		}

		private EitherAsync<Exception, (bool IsAnonymous, string Uid)> ParseAuthorizationToken(HttpRequest req) =>
			from result in req.Headers.TryGetValue("Authorization").Map(t => t.ToString().Replace("Bearer ", ""))
					.MapAsync(_firebaseService.TokenVerify).ToEither(new Exception("require Authorization Header"))
			select result;
	}
}
