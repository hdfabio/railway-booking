export type SeatAvailability = {
  scheduleId: string;
  travelDate: string;
  class: string;
  quota: string;
  confirmed: number;
  rac: number;
  waitlist: number;
  lastUpdated: string;
};

export type TrainResult = {
  scheduleId: string;
  trainId: string;
  trainNumber: string;
  name: string;
  source: { code: string; name: string; city: string };
  destination: { code: string; name: string; city: string };
  travelDate: string;
  departure: string;
  arrival: string;
  baseFare: number;
  classes: string[];
  availability: SeatAvailability;
};

export type Passenger = {
  name: string;
  age: number;
  gender: string;
  berthPreference: string;
};

export type TicketPassenger = {
  name: string;
  status: string;
  coach: string | null;
  seatNumber: number | null;
  berth: string | null;
};

export type Booking = {
  pnr: string;
  scheduleId: string;
  travelDate: string;
  class: string;
  quota: string;
  passengers: TicketPassenger[];
  fare: number;
  paymentStatus: string;
  bookingStatus: string;
};

export type CreateBookingRequest = {
  scheduleId: string;
  travelDate: string;
  class: string;
  quota: string;
  paymentMethod: string;
  passengers: Passenger[];
};

export type SearchParams = {
  source: string;
  destination: string;
  date: string;
  travelClass: string;
  quota: string;
};

export type ReportSummary = {
  totalBookings: number;
  confirmedBookings: number;
  cancelledBookings: number;
  revenue: number;
  byClass: Array<{ class: string; count: number }>;
};

export type DomainEvent = {
  type: string;
  aggregateId: string;
};

export type TokenResponse = {
  accessToken: string;
  expiresAt: string;
};

export type UserProfile = {
  userId: string;
  name: string;
  email: string;
  phone: string;
  loyaltyTier: string;
};
