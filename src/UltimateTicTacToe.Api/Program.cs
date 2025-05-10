using UltimateTicTacToe.API.Hubs;

namespace WebApplication1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

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
