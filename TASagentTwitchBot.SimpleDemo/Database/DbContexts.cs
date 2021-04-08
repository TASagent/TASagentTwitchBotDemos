﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace TASagentTwitchBot.SimpleDemo.Database
{
    //Create the database 
    public class DatabaseContext : Core.Database.BaseDatabaseContext
    {
        public DbSet<SupplementalData> SupplementalData { get; set; }

        public async Task<SupplementalData> GetSupplementalDataAsync(Core.Database.User user)
        {
            SupplementalData userSupplementalData = SupplementalData.FirstOrDefault(x => x.UserId == user.UserId);

            if (userSupplementalData is null)
            {
                userSupplementalData = new SupplementalData()
                {
                    User = user
                };

                SupplementalData.Add(userSupplementalData);

                await SaveChangesAsync();
            }

            return userSupplementalData;
        }

        public SupplementalData GetSupplementalData(Core.Database.User user)
        {
            SupplementalData userSupplementalData = SupplementalData.FirstOrDefault(x => x.UserId == user.UserId);

            if (userSupplementalData is null)
            {
                userSupplementalData = new SupplementalData()
                {
                    User = user
                };

                SupplementalData.Add(userSupplementalData);

                SaveChanges();
            }

            return userSupplementalData;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            BGC.IO.DataManagement.defaultDirectory = "TASagentBotDemo";
            options.UseSqlite($"Data Source={BGC.IO.DataManagement.PathForDataFile("Config", "data.sqlite")}");
        }
    }

    public class SupplementalData
    {
        public int SupplementalDataId { get; set; }

        public int UserId { get; set; }
        public Core.Database.User User { get; set; }

        public int PointsSpent { get; set; }
        public DateTime? LastPointsSpentUpdate { get; set; }
    }
}