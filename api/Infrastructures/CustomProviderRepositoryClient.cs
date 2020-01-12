using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Example.Function.DataObjects;
using Example.Function.Helpers;

namespace Example.Function.Infrastructures
{
	public interface ICustomProviderRepositoryClient
	{
		Task<CustomProviderDocument> FetchFirebaseUidByCustomProviderId(string customProviderId, string provider);
		Task StoreCustomProviderId(string userId, CustomProviderDocument customProviderDoc, string provider);
	}

	public class CustomProviderRepositoryClient : ICustomProviderRepositoryClient
	{
		private readonly HttpClient _client;

		public CustomProviderRepositoryClient(IHttpClientFactory clientFactory)
		{
			_client = clientFactory.CreateClient(nameof(CustomProviderRepositoryClient));
		}

		public async Task<CustomProviderDocument> FetchFirebaseUidByCustomProviderId(string customProviderId, string provider)
		{
			var result = await _client.GetAsync($"providers/{provider}/entries/{customProviderId}").ConfigureAwait(false);
			var responseContent = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
			if (responseContent == "{}\n")
			{
				return null;
			}
			return System.Text.Json.JsonSerializer.Deserialize<CustomProviderDocument>(responseContent, new JsonSerializerOptions().Also(j =>
			{
				j.Converters.Add(new FirestoreDocumentConverter<CustomProviderDocument>());
				j.PropertyNameCaseInsensitive = true;
			}));
		}

		public async Task StoreCustomProviderId(string userId, CustomProviderDocument customProviderDoc, string provider)
		{
			var firebaseProviderJson = JsonSerializer.Serialize(new CustomProviderDocument { Id = userId }, new JsonSerializerOptions().Also(j =>
			{
				j.Converters.Add(new FirestoreDocumentConverter<CustomProviderDocument>());
			}));
			var firebaseProviderJsonContent = new StringContent(firebaseProviderJson, Encoding.UTF8, "application/json");
			var firebaseProviderResponse = await _client.PatchAsync($"providers/{provider}/entries/{customProviderDoc.Id}", firebaseProviderJsonContent).ConfigureAwait(false);

			var customProviderJson = JsonSerializer.Serialize(customProviderDoc, new JsonSerializerOptions().Also(j =>
			{
				j.Converters.Add(new FirestoreDocumentConverter<CustomProviderDocument>());
			}));
			var customProviderJsonContent = new StringContent(customProviderJson, Encoding.UTF8, "application/json");
			var customProviderReponse = await _client.PatchAsync($"users/{userId}/providers/{provider}", customProviderJsonContent).ConfigureAwait(false);
		}
	}
}
