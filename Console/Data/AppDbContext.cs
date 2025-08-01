using Microsoft.EntityFrameworkCore;
using TransactionSystem.Models;

namespace TransactionSystem.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<Transaction> Transactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Name).IsRequired().HasMaxLength(100);
            entity.Property(u => u.CreatedDate).HasDefaultValueSql("GETUTCDATE()");
        });

      
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.HasIndex(a => a.AccountNumber).IsUnique();  
            entity.Property(a => a.AccountNumber).IsRequired().HasMaxLength(50);
            entity.Property(a => a.Balance).HasColumnType("decimal(18, 2)");
            entity.Property(a => a.CreatedDate).HasDefaultValueSql("GETUTCDATE()");

        
            entity.HasOne(a => a.User)
                  .WithMany(u => u.Accounts)
                  .HasForeignKey(a => a.UserId)
                  .OnDelete(DeleteBehavior.Cascade);  
        });

       
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(t => t.Timestamp).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(t => t.Description).HasMaxLength(200);

            
            entity.HasOne(t => t.Account)
                  .WithMany(a => a.Transactions)
                  .HasForeignKey(t => t.AccountId)
                  .OnDelete(DeleteBehavior.Cascade);  
        });
    }
}