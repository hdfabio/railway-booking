using Railway.Contracts;
namespace Railway.Application.Services;

public sealed class SeatReservationException(string message) : Exception(message);
