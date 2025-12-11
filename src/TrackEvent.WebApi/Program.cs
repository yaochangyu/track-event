using Microsoft.EntityFrameworkCore;
using TrackEvent.WebApi.Handlers;
using TrackEvent.WebApi.Infrastructure.Data;
using TrackEvent.WebApi.Infrastructure.Repositories;
using TrackEvent.WebApi.Middlewares;
using DotNetEnv;

namespace TrackEvent.WebApi;

public class Program
{
    public static void Main(string[] args)
    {
        // 載入 .env 檔案（如果存在），僅當帶有 --local 參數時
        // if (args != null && Array.IndexOf(args, "--local") >= 0)
        // {
        //     Env.Load();
        // }
        Env.Load();
        var builder = WebApplication.CreateBuilder(args);

        // 設定 JSON 序列化選項（snake_case）
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy =
                    System.Text.Json.JsonNamingPolicy.SnakeCaseLower;
            });

        // 設定 PostgreSQL
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        builder.Services.AddDbContext<TrackEventDbContext>(options =>
            options.UseNpgsql(connectionString));

        // 註冊 Repositories
        builder.Services.AddScoped<IUserEventRepository, UserEventRepository>();

        // 註冊 Handlers
        builder.Services.AddScoped<TrackEventHandler>();

        // API 文件
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new()
            {
                Title = "TrackEvent API",
                Version = "v1",
                Description = "使用者行為事件追蹤 API"
            });
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "TrackEvent API v1");
            });
        }

        // 全域例外處理 Middleware（必須在最前面）
        app.UseExceptionHandlingMiddleware();

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
