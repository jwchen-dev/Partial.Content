using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Simple.Api.Core;

namespace Simple.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<S3DataSyncStorage<string>, S3DataSyncStorage<string>>(service =>
            {
                var storage = new S3DataSyncStorage<string>("91dev-ap-northeast-1-private-tw-data-sync");
                return storage;
            });

            services.AddSingleton<S3DataSyncStorage2<string>, S3DataSyncStorage2<string>>(service =>
            {
                var storage = new S3DataSyncStorage2<string>("91dev-ap-northeast-1-private-tw-data-sync");
                return storage;
            });

            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
