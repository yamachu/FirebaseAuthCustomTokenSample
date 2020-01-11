using System.Net.Http;
using System.Threading.Tasks;

namespace Example.Function.Helpers
{
	public class FirebaseAuthenticateHttpClientHandler : DelegatingHandler
	{
		private readonly AsyncLazy<string> token;

		public FirebaseAuthenticateHttpClientHandler(AsyncLazy<string> tokenLazyFetcher) : this(tokenLazyFetcher, new HttpClientHandler())
		{
		}

		public FirebaseAuthenticateHttpClientHandler(AsyncLazy<string> tokenLazyFetcher, HttpMessageHandler innerHandler) : base(innerHandler)
		{
			token = tokenLazyFetcher;
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
		{
			request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Value.Result);
			return base.SendAsync(request, cancellationToken);
		}
	}
}