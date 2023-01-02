using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace QuizService;

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
        services.AddMvc();
        //TODO I would prefer to create a seperate class for database initialization. Call it here with i.e. DBInitializer.InitializeDb()
        services.AddSingleton(InitializeDb());
        services.AddControllers();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }

    //TODO I would prefer to have this in a seperate class i.e. DBInitializer.cs
    private IDbConnection InitializeDb()
    {
        //TODO I would prefer to have the connection string in either application settings, or a seperate configuration file
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        // Migrate up
        var assembly = typeof(Startup).GetTypeInfo().Assembly;
        var migrationResourceNames = assembly.GetManifestResourceNames()
            .Where(x => x.EndsWith(".sql"))
            .OrderBy(x => x);
        if (!migrationResourceNames.Any()) throw new System.Exception("No migration files found!");
        foreach (var resourceName in migrationResourceNames)
        {
            var sql = GetResourceText(assembly, resourceName);
            var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        return connection;
    }

    //TODO I would prefer to have this in a seperate class together with InitializeDb() i.e DBInitializer.cs
    private static string GetResourceText(Assembly assembly, string resourceName)
    {
        using (var stream = assembly.GetManifestResourceStream(resourceName))
        {
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}