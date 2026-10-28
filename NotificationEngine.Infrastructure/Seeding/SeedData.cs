using Microsoft.EntityFrameworkCore;
using NotificationEngine.Core.Seeding;
using NotificationEngine.Core.Model;

namespace SchedulerEngine.Data.Seeding
{
    public static class DataSeed
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Language>().HasData(InitialSeedData.Languages);
            modelBuilder.Entity<LocalizableFields>().HasData(InitialSeedData.LocalizableFields);
            modelBuilder.Entity<PartyRoleType>().HasData(InitialSeedData.PartyRoleTypes);
            modelBuilder.Entity<RelatedPartyType>().HasData(InitialSeedData.RelatedPartyTypes);
            modelBuilder.Entity<MessageTemplate>().HasData(InitialSeedData.MessageTemplates);
        }
    }
}