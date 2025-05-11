using UltimateTicTacToe.API.Hubs;
using UltimateTicTacToe.Core.Configuration;
using UltimateTicTacToe.Core.Services;
using UltimateTicTacToe.Storage.Services;

namespace WebApplication1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            #region Event Store
            
            builder.Services.Configure<EventStoreSettings>(builder.Configuration.GetSection("EventStoreSettings"));
            builder.Services.AddSingleton<IEventStore, MongoEventStore>();

            #endregion

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddControllers();
            builder.Services.AddSignalR();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowLocalhost8080_Only",
                    builder => builder
                        .WithOrigins("http://localhost:8080")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials());
            });

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                app.UseDeveloperExceptionPage();
            }
            
            app.UseRouting();

            app.UseCors("AllowLocalhost8080_Only");

            app.UseAuthorization();
            app.UseHttpsRedirection();

            app.MapHub<GameHub>("game-hub").RequireCors("AllowLocalhost8080_Only");
            app.MapControllers();

            app.Run();
        }
    }
}
