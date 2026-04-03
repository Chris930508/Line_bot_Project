using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Line_bot.Models;

public partial class WebContext : DbContext
{
    public WebContext(DbContextOptions<WebContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Check> Check { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Check>(entity =>
        {
           
            entity.Property(e => e.Checktime).HasColumnType("datetime");
            entity.Property(e => e.Distance).HasColumnType("decimal(10, 2)");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
