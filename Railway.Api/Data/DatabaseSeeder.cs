using Microsoft.EntityFrameworkCore;
using Railway.Api.Domain;

namespace Railway.Api.Data;

public sealed class DatabaseSeeder(RailwayDbContext db)
{
    public async Task SeedAsync()
    {
        if (await db.Trains.AnyAsync())
        {
            return;
        }

        var ndls = new Station { Id = Guid.NewGuid(), Code = "NDLS", Name = "New Delhi", City = "Delhi" };
        var mmct = new Station { Id = Guid.NewGuid(), Code = "MMCT", Name = "Mumbai Central", City = "Mumbai" };
        var mas = new Station { Id = Guid.NewGuid(), Code = "MAS", Name = "MGR Chennai Central", City = "Chennai" };
        var hwh = new Station { Id = Guid.NewGuid(), Code = "HWH", Name = "Howrah Junction", City = "Kolkata" };

        var demoUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FullName = "Demo Traveller",
            Email = "demo@example.com",
            Phone = "+91-90000-00000",
            LoyaltyTier = "Gold"
        };

        var trains = new[]
        {
            CreateTrain("12952", "Mumbai Rajdhani", ndls, mmct, new TimeOnly(16, 55), new TimeOnly(8, 35), 2840, ["1AC", "2AC", "3AC"]),
            CreateTrain("12270", "Chennai Duronto", ndls, mas, new TimeOnly(15, 55), new TimeOnly(20, 10), 2420, ["2AC", "3AC", "SL"]),
            CreateTrain("12302", "Howrah Rajdhani", ndls, hwh, new TimeOnly(16, 50), new TimeOnly(9, 55), 2710, ["1AC", "2AC", "3AC"]),
            CreateTrain("12953", "August Kranti Rajdhani", mmct, ndls, new TimeOnly(17, 10), new TimeOnly(10, 55), 2650, ["1AC", "2AC", "3AC"]),
            CreateTrain("12615", "Grand Trunk Express", mas, ndls, new TimeOnly(18, 50), new TimeOnly(6, 30), 980, ["2AC", "3AC", "SL"])
        };

        db.Stations.AddRange(ndls, mmct, mas, hwh);
        db.Users.Add(demoUser);
        db.Trains.AddRange(trains);
        await db.SaveChangesAsync();
    }

    private static Train CreateTrain(string number, string name, Station source, Station destination, TimeOnly departure, TimeOnly arrival, decimal fare, string[] classes)
    {
        var train = new Train
        {
            Id = Guid.NewGuid(),
            Number = number,
            Name = name
        };

        for (var day = 0; day < 30; day++)
        {
            train.Schedules.Add(new Schedule
            {
                Id = Guid.NewGuid(),
                Train = train,
                SourceStation = source,
                DestinationStation = destination,
                TravelDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(day)),
                DepartureTime = departure,
                ArrivalTime = arrival,
                BaseFare = fare
            });
        }

        foreach (var travelClass in classes)
        {
            var coachCount = travelClass == "SL" ? 2 : 1;
            var seatCount = travelClass == "SL" ? 36 : 24;

            for (var coachIndex = 1; coachIndex <= coachCount; coachIndex++)
            {
                var coach = new Coach
                {
                    Id = Guid.NewGuid(),
                    Train = train,
                    Code = $"{travelClass.Replace("AC", "A")}{coachIndex}",
                    TravelClass = travelClass
                };

                for (var seatNumber = 1; seatNumber <= seatCount; seatNumber++)
                {
                    coach.Seats.Add(new Seat
                    {
                        Id = Guid.NewGuid(),
                        Coach = coach,
                        Number = seatNumber,
                        BerthType = BerthFor(seatNumber)
                    });
                }

                train.Coaches.Add(coach);
            }
        }

        return train;
    }

    private static string BerthFor(int seatNumber)
    {
        return (seatNumber % 6) switch
        {
            1 => "Lower",
            2 => "Middle",
            3 => "Upper",
            4 => "Side Lower",
            5 => "Side Upper",
            _ => "Upper"
        };
    }
}
