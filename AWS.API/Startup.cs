using Amazon.Runtime;
using Amazon.S3;




namespace AWS.API
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

       
        public void ConfigureServices(IServiceCollection services)
        {
            var accessKey = Environment.GetEnvironmentVariable("AWSS3AccessKey");
            var secreatKey = Environment.GetEnvironmentVariable("AWSS3SecretKey");
            var region = Environment.GetEnvironmentVariable("AWSS3Region");

            var credentials = new BasicAWSCredentials(accessKey, secreatKey);
            var config = new AmazonS3Config
            {
                RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region)
            };
            var s3Client = new AmazonS3Client(credentials, config);

            services.AddSingleton<IAmazonS3>(s3Client);

            services.AddControllers();
            

        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }


            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
