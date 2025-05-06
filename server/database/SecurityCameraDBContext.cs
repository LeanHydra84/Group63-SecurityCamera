#nullable disable

using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SecurityCameraServer
{
    internal sealed class SecurityCameraDBContext : DbContext
    {

        private string ConnectionString { get; }

        public SecurityCameraDBContext()
        {
            
            ConnectionString = WebApplication.CreateBuilder().Configuration.GetConnectionString("SecurityCameraDBContext");
            Console.WriteLine($"Connection String: '{ConnectionString}'");
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlite(ConnectionString);
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Camera> Cameras => Set<Camera>();
    }
}