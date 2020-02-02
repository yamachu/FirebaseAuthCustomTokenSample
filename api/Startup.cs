using System;
using System.Reflection;
using System.Linq;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using Grpc.Auth;
using Grpc.Core;
using Example.Function.Configurations;
using Example.Function.Helpers;
using Example.Function.Infrastructures;
using Example.Function.Services;

[assembly: FunctionsStartup(typeof(Example.Function.Startup))]
namespace Example.Function
{
	public class Startup : FunctionsStartup
	{
		public override void Configure(IFunctionsHostBuilder builder)
		{
			var executioncontextoptions = builder.Services.BuildServiceProvider()
				.GetService<IOptions<ExecutionContextOptions>>().Value;
			var currentDirectory = executioncontextoptions.AppDirectory;

			var config = new ConfigurationBuilder()
				.SetBasePath(currentDirectory)
				.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
				.AddEnvironmentVariables()
				.Build();

			var assembly = Assembly.GetExecutingAssembly();
			var resourceName = "Resources.firebase-adminsdk.json";

			using var stream = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.{resourceName}");
			var credential = GoogleCredential.FromStream(stream);
			var adminApp = FirebaseApp.Create(new AppOptions()
			{
				Credential = credential
			});
			var projectId = ((ServiceAccountCredential)adminApp.Options.Credential.UnderlyingCredential).ProjectId;
			var firebaseAdminAuthHandler = new FirebaseAuthenticateHttpClientHandler(
				// これだとRefresh出来ないからexpireしそう…
				new AsyncLazy<string>(() => adminApp.Options.Credential.UnderlyingCredential.GetAccessTokenForRequestAsync())
			);
			// should shutdown?
			var channel = new Channel(FirestoreClient.DefaultEndpoint.Host, FirestoreClient.DefaultEndpoint.Port, credential.ToChannelCredentials());
			var firestoreDb = FirestoreDb.Create(projectId, FirestoreClient.Create(channel));

			builder.Services.AddHttpClient(nameof(AuthTemporaryClient), c =>
			{
				c.BaseAddress = new Uri($"https://firestore.googleapis.com/v1/projects/{projectId}/databases/(default)/documents/");
			}).ConfigurePrimaryHttpMessageHandler(() => firebaseAdminAuthHandler);
			builder.Services.AddHttpClient(nameof(CustomProviderRepositoryClient), c =>
			{
				c.BaseAddress = new Uri($"https://firestore.googleapis.com/v1/projects/{projectId}/databases/(default)/documents/");
			}).ConfigurePrimaryHttpMessageHandler(() => firebaseAdminAuthHandler);
			builder.Services.AddHttpClient(nameof(LineClient), c =>
			{
				c.BaseAddress = new Uri("https://api.line.me/");
			});

			builder.Services.AddSingleton<IConfiguration>(config);

			builder.Services.AddSingleton<ILineClient, LineClient>();
			builder.Services.AddSingleton<IAuthTemporaryClient, AuthTemporaryFirestoreClient>(
				_ => new AuthTemporaryFirestoreClient(firestoreDb));
			builder.Services.AddSingleton<ICustomProviderRepositoryClient, CustomProviderRepositoryFirestoreClient>(
				_ => new CustomProviderRepositoryFirestoreClient(firestoreDb));

			builder.Services.AddTransient<ILineService, LineService>();
			builder.Services.AddTransient<IFirebaseService, FirebaseService>();

			builder.Services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));

			builder.Services
				.AddOptions<LineConfig>()
				.Configure<IConfiguration>((settings, configuration) => configuration.Bind(settings))
				.Validate((c) => !new[] { c.LineClientId, c.LineClientSecret, c.LineRedirectUrl }.Any(s => String.IsNullOrWhiteSpace(s)));
		}
	}
}