using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Example.Function.DataObjects;
using Example.Function.Helpers;

namespace Example.Function.Infrastructures
{
	public interface IAuthTemporaryClient
	{
		Task StoreAuthTemporary(string userId, AuthTemporary authTemp);
		Task<AuthTemporary> RestoreAuthTemporary(string userId);
	}

	public class AuthTemporaryClient : IAuthTemporaryClient
	{
		private readonly HttpClient _client;

		private const string AuthTempKey = "authtemp";

		public AuthTemporaryClient(IHttpClientFactory clientFactory)
		{
			_client = clientFactory.CreateClient(nameof(AuthTemporaryClient));
		}

		public async Task<AuthTemporary> RestoreAuthTemporary(string userId)
		{
			var result = await _client.GetAsync($"{AuthTempKey}/{userId}").ConfigureAwait(false);
			var responseContent = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
			var json = System.Text.Json.JsonSerializer.Deserialize<AuthTemporary>(responseContent, new JsonSerializerOptions().Also(j =>
			{
				j.Converters.Add(new FirestoreDocumentConverter<AuthTemporary>());
				j.PropertyNameCaseInsensitive = true;
			}));

			return json;
		}

		public async Task StoreAuthTemporary(string userId, AuthTemporary authTemp)
		{
			var json = JsonSerializer.Serialize(authTemp, new JsonSerializerOptions().Also(j =>
			{
				j.Converters.Add(new FirestoreDocumentConverter<AuthTemporary>());
			}));
			var content = new StringContent(json, Encoding.UTF8, "application/json");

			await _client.PatchAsync($"{AuthTempKey}/{userId}", content).ConfigureAwait(false);
		}
	}

	public class AuthTemporaryFirestoreClient : IAuthTemporaryClient
	{
		private readonly IFirestoreProvider _firestoreProvider;

		private const string AuthTempKey = "authtemp";

		public AuthTemporaryFirestoreClient(IFirestoreProvider firestoreProvider)
		{
			_firestoreProvider = firestoreProvider;
		}

		public async Task<AuthTemporary> RestoreAuthTemporary(string userId)
		{
			var doc = _firestoreProvider.Instance.Document($"{AuthTempKey}/{userId}");
			var snapshot = await doc.GetSnapshotAsync();
			return snapshot.ConvertTo<AuthTemporary>();
		}

		public async Task StoreAuthTemporary(string userId, AuthTemporary authTemp)
		{
			var doc = _firestoreProvider.Instance.Document($"{AuthTempKey}/{userId}");
			await doc.SetAsync(authTemp);
		}
	}
}
