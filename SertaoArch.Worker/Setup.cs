﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using RabbitMQ.Client;
using SertaoArch.Contracts.AppObject;
using SertaoArch.Worker;
using SertaoArch.Worker.Comsumers;


public class Startup
{
    public IConfiguration Configuration { get; set; }
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddHostedService<Worker<CreateUserConsumer,UserContract>>();
        services.AddTransient<CreateUserConsumer>();

        services.AddSingleton(async sp =>
        {
            var factory = new ConnectionFactory()
            {
                HostName = Configuration["RabbitMQ:HostName"] ?? "localhost",
                Port = int.TryParse(Configuration["RabbitMQ:Port"], out var port) ? port : 5672,
                UserName = Configuration["RabbitMQ:UserName"] ?? "admin",
                Password = Configuration["RabbitMQ:Password"] ?? "admin"
            };

            return await factory.CreateConnectionAsync();
        });

        services.AddScoped(async sp =>
        {
            var connection = sp.GetRequiredService<IConnection>();
            return await connection.CreateChannelAsync();
        });

        services.AddSingleton<IMongoClient>(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var connectionString = configuration["MongoDB:ConnectionString"];
            return new MongoClient(connectionString);
        });

        services.AddHealthChecks().AddRabbitMQ(provider =>
        provider.GetRequiredService<IConnection>());
    }
}
