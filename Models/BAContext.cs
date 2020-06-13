using System;
using Microsoft.EntityFrameworkCore;

namespace BankAccounts.Models
{
    public class BAContext : DbContext
    {
        public BAContext(DbContextOptions options) : base(options) { }

        public DbSet<User> Users { get; set; }
    }
}

