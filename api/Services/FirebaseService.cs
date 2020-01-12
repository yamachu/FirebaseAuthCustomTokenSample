using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Example.Function.DataObjects;
using Example.Function.Infrastructures;
using FirebaseAdmin.Auth;

namespace Example.Function.Services
{
	public interface IFirebaseService
	{
		Task<string> CreateCustomToken(string firebaseUid, string customId, string provider);
		Task<(bool IsAnonymous, string Uid)> TokenVerify(string jwtToken);
	}

	public class FirebaseService : IFirebaseService
	{
		private readonly ICustomProviderRepositoryClient _repositoryClient;

		public FirebaseService(ICustomProviderRepositoryClient repositoryClient)
		{
			_repositoryClient = repositoryClient;
		}

		public async Task<string> CreateCustomToken(string firebaseUid, string customId, string provider)
		{
			var alredyRegisteredId = await _repositoryClient.FetchFirebaseUidByCustomProviderId(customId, provider);
			if (alredyRegisteredId == null)
			{
				await _repositoryClient.StoreCustomProviderId(firebaseUid, new CustomProviderDocument { Id = customId }, provider);
			}

			return await FirebaseAuth.DefaultInstance.CreateCustomTokenAsync(alredyRegisteredId?.Id ?? firebaseUid, new Dictionary<string, object>
			{
				["provider"] = provider
			}).ConfigureAwait(false);
		}

		public async Task<(bool IsAnonymous, string Uid)> TokenVerify(string jwtToken)
		{
			var verifiedIdToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(jwtToken).ConfigureAwait(false);
			var hasAnonymousField = verifiedIdToken.Claims.TryGetValue("isAnonymous", out var isAnonymous);

			return (
				hasAnonymousField ? Convert.ToBoolean(isAnonymous) : false,
				verifiedIdToken.Uid
			);
		}
	}
}