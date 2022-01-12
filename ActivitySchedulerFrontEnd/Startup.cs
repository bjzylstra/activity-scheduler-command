using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Blazor.FileReader;
using ActivitySchedulerFrontEnd.Services;
using Blazored.LocalStorage;

namespace ActivitySchedulerFrontEnd
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddRazorPages();
			// Limits size of file transfers
			services.AddServerSideBlazor().AddHubOptions(o =>
			{
				o.MaximumReceiveMessageSize = 10 * 1024 * 1024; // 10MB
			});
			services.AddSingleton<IActivityDefinitionService, ActivityDefinitionService>();
			services.AddSingleton<ISchedulerService, SchedulerService>();
			services.AddSingleton<IPropertyService, PropertyService>();

			services.AddFileReaderService(options => options.InitializeOnFirstCall = true);
			services.AddBlazoredLocalStorage();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
			else
			{
				app.UseExceptionHandler("/Error");
			}

			app.UseStaticFiles();

			app.UseRouting();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapBlazorHub();
				endpoints.MapFallbackToPage("/_Host");
			});
		}
	}
}
