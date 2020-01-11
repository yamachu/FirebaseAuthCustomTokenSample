using System;
using System.Reflection;
using System.Linq;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Example.Function.Configurations;

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
			var adminApp = FirebaseApp.Create(new AppOptions()
			{
				Credential = GoogleCredential.FromStream(stream)
			});

			builder.Services.AddSingleton<IConfiguration>(config);

			builder.Services
				.AddOptions<LineConfig>()
				.Configure<IConfiguration>((settings, configuration) => configuration.Bind(settings))
				.Validate((c) => !new[] { c.LineClientId, c.LineClientSecret, c.LineRedirectUrl }.Any(s => String.IsNullOrWhiteSpace(s)));
		}
	}
}