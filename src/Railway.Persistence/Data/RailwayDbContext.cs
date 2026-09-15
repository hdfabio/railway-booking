using Microsoft.EntityFrameworkCore;
using Railway.Persistence.Domain;

namespace Railway.Persistence;

public sealed class RailwayDbContext(DbContextOptions<RailwayDbContext> options) : DbContext(options)
{
    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
    public DbSet<Train> Trains => Set<Train>();
    public DbSet<Station> Stations => Set<Station>();
    public DbSet<Schedule> Schedules => Set<Schedule>();
    public DbSet<Coach> Coaches => Set<Coach>();
    public DbSet<Seat> Seats => Set<Seat>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingPassenger> BookingPassengers => Set<BookingPassenger>();
    public DbSet<Cancellation> Cancellations => Set<Cancellation>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PnrRecord> Pnrs => Set<PnrRecord>();
    public DbSet<RacWaitlistQueueEntry> RacWaitlistQueue => Set<RacWaitlistQueueEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApplicationUser>().ToTable("Users");
        modelBuilder.Entity<Train>().ToTable("Trains");
        modelBuilder.Entity<Station>().ToTable("Stations");
        modelBuilder.Entity<Schedule>().ToTable("Schedules");
        modelBuilder.Entity<Coach>().ToTable("Coaches");
        modelBuilder.Entity<Seat>().ToTable("Seats");
        modelBuilder.Entity<Booking>().ToTable("Bookings");
        modelBuilder.Entity<BookingPassenger>().ToTable("BookingPassengers");
        modelBuilder.Entity<Payment>().ToTable("Payments");
        modelBuilder.Entity<Cancellation>().ToTable("Cancellations");
        modelBuilder.Entity<PnrRecord>().ToTable("PNR");
        modelBuilder.Entity<RacWaitlistQueueEntry>().ToTable("RAC_WL_Queue");

        modelBuilder.Entity<ApplicationUser>().HasIndex(user => user.Email).IsUnique();
        modelBuilder.Entity<Station>().HasIndex(station => station.Code).IsUnique();
        modelBuilder.Entity<Station>().HasIndex(station => station.City);
        modelBuilder.Entity<Station>().HasIndex(station => station.Name);
        modelBuilder.Entity<Train>().HasIndex(train => train.Number).IsUnique();
        modelBuilder.Entity<PnrRecord>().HasIndex(pnr => pnr.Number).IsUnique();
        modelBuilder.Entity<Booking>().HasIndex(booking => booking.UserId);
        modelBuilder.Entity<Booking>().HasIndex(booking => booking.Status);
        modelBuilder.Entity<BookingPassenger>().HasIndex(passenger => passenger.BookingId);
        modelBuilder.Entity<BookingPassenger>().HasIndex(passenger => passenger.SeatId);

        modelBuilder.Entity<Schedule>()
            .HasOne(schedule => schedule.SourceStation)
            .WithMany()
            .HasForeignKey(schedule => schedule.SourceStationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Schedule>()
            .HasOne(schedule => schedule.DestinationStation)
            .WithMany()
            .HasForeignKey(schedule => schedule.DestinationStationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Booking>()
            .HasOne(booking => booking.PnrRecord)
            .WithOne(pnr => pnr.Booking)
            .HasForeignKey<Booking>(booking => booking.PnrRecordId);

        modelBuilder.Entity<Booking>()
            .HasOne(booking => booking.Payment)
            .WithOne(payment => payment.Booking)
            .HasForeignKey<Payment>(payment => payment.BookingId);

        modelBuilder.Entity<Booking>()
            .HasOne(booking => booking.Cancellation)
            .WithOne(cancellation => cancellation.Booking)
            .HasForeignKey<Cancellation>(cancellation => cancellation.BookingId);

        modelBuilder.Entity<Seat>()
            .HasIndex(seat => new { seat.CoachId, seat.Number })
            .IsUnique();

        modelBuilder.Entity<RacWaitlistQueueEntry>()
            .HasIndex(entry => new { entry.ScheduleId, entry.TravelClass, entry.Quota, entry.QueueType, entry.Position })
            .IsUnique();
    }
}
