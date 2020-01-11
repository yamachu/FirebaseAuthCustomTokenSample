using System;
using System.Linq;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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

			builder.Services.AddSingleton<IConfiguration>(config);

			builder.Services
				.AddOptions<LineConfig>()
				.Configure<IConfiguration>((settings, configuration) => configuration.Bind(settings))
				.Validate((c) => !new[] { c.LineClientId, c.LineClientSecret, c.LineRedirectUrl }.Any(s => String.IsNullOrWhiteSpace(s)));
		}
	}
}