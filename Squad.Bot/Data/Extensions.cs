﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Squad.Bot.Logging;
using Squad.Bot.Models.AI;
using Squad.Bot.Models.Base;

namespace Squad.Bot.Data
{
    public static class Extensions
    {
        public static void CreateDbIfNotExists(this IHost host)
        {
            {
#pragma warning disable IDE0063 // Использовать простой оператор using
                using (var scope = host.Services.CreateScope())
                {
                    var services = scope.ServiceProvider;
                    var context = services.GetRequiredService<SquadDBContext>();
                    context.Database.EnsureCreated();


                    if (context.Guilds.Any())
                    {
                        return;
                    }
                    var guild = new Guilds { Id = 909801532126543874, ServerName = "Squad" };
                    context.Guilds.Add(guild);
                    context.SaveChanges();
                }
#pragma warning restore IDE0063 // Использовать простой оператор using
            }
        }
    }
}
