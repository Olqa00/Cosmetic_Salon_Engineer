using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Engineer_MVC.Models;
using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Engineer_MVC.Data
{
    public class EngineerContext : IdentityDbContext<User>
    {
        public EngineerContext (DbContextOptions<EngineerContext> options)
            : base(options)
        {
        }

        public DbSet<Post> Post { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Post>().ToTable("Post");
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Treatment)
                .WithMany(t => t.Appointments)
                .HasForeignKey(a => a.TreatmentId);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Employee)
                .WithMany(u => u.AppointmentsMade)
                .HasForeignKey(a => a.EmployeeId);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.User)
                .WithMany(u => u.AppointmentsReceive)
                .HasForeignKey(a => a.UserId);
        }
        public DbSet<Engineer_MVC.Models.Treatment> Treatment { get; set; } = default!;
        public DbSet<Engineer_MVC.Models.Appointment> Appointment { get; set; } = default!;
        public DbSet<Engineer_MVC.Models.WorkingHours> WorkingHours { get; set; } = default!;
        public DbSet<Engineer_MVC.Models.Training> Training { get; set; } = default!;
        public DbSet<Engineer_MVC.Models.CancellationRequest> CancellationRequest { get; set; } = default!;
    }
}
