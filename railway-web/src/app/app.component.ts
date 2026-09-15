import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import {
  Booking,
  CreateBookingRequest,
  DomainEvent,
  Passenger,
  ReportSummary,
  SearchParams,
  TrainResult,
  UserProfile
} from './models/railway.models';
import { AuthService } from './services/auth.service';
import { RailwayApiService } from './services/railway-api.service';
import { injectHttpRequest } from './utils/http-request';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './app.component.html'
})
export class AppComponent implements OnInit {
  private readonly api = inject(RailwayApiService);
  private readonly auth = inject(AuthService);
  private readonly request = injectHttpRequest();

  search: SearchParams = {
    source: 'NDLS',
    destination: 'MMCT',
    date: new Date().toISOString().slice(0, 10),
    travelClass: '3AC',
    quota: 'GENERAL'
  };
  passenger: Passenger = { name: 'Anita Rao', age: 64, gender: 'Female', berthPreference: 'Lower' };
  trains: TrainResult[] = [];
  selectedTrain: TrainResult | null = null;
  bookingList: Booking[] = [];
  pnrLookup = '';
  report: ReportSummary | null = null;
  events: DomainEvent[] = [];
  currentUser: UserProfile | null = null;
  busy = false;
  errorMessage = '';

  async ngOnInit(): Promise<void> {
    await this.runAsync('Sign-in failed.', async () => {
      await this.auth.ensureDemoSession();
      this.currentUser = await this.request(this.api.getCurrentUser());
    });
    await this.findTrains();
    await this.loadDashboard();
  }

  async findTrains(): Promise<void> {
    await this.runAsync('Could not load trains.', async () => {
      const trains = await this.request(this.api.searchTrains(this.search));
      this.trains = trains;
      this.selectedTrain = trains[0] ?? null;
    });
  }

  selectTrain(train: TrainResult): void {
    this.selectedTrain = train;
  }

  async book(): Promise<void> {
    if (!this.selectedTrain) {
      return;
    }

    const request: CreateBookingRequest = {
      scheduleId: this.selectedTrain.scheduleId,
      travelDate: this.search.date,
      class: this.search.travelClass,
      quota: this.search.quota,
      paymentMethod: 'UPI',
      passengers: [this.passenger]
    };

    await this.runAsync('Booking failed.', async () => {
      const booking = await this.request(this.api.createBooking(request));
      this.bookingList = [booking, ...this.bookingList];
      this.pnrLookup = booking.pnr;
      await this.findTrains();
      await this.loadDashboard();
    });
  }

  async lookupPnr(): Promise<void> {
    const pnr = this.pnrLookup.trim();
    if (!pnr) {
      return;
    }

    await this.runAsync('PNR not found or lookup failed.', async () => {
      const booking = await this.request(this.api.getBookingByPnr(pnr));
      this.bookingList = [booking];
    });
  }

  async cancel(booking: Booking): Promise<void> {
    await this.runAsync('Cancellation failed.', async () => {
      const updatedBooking = await this.request(this.api.cancelBooking(booking.pnr));
      const index = this.bookingList.findIndex(item => item.pnr === updatedBooking.pnr);
      if (index !== -1) {
        this.bookingList[index] = updatedBooking;
      }
      await this.findTrains();
      await this.loadDashboard();
    });
  }

  async loadDashboard(): Promise<void> {
    await this.runAsync('Could not refresh dashboard.', async () => {
      const { report, bookings, events } = await this.request(
        forkJoin({
          report: this.api.getReportSummary(),
          bookings: this.api.getBookings(),
          events: this.api.getRecentEvents()
        })
      );
      this.report = report;
      this.bookingList = bookings;
      this.events = events;
    });
  }

  private async runAsync(message: string, action: () => Promise<void>): Promise<void> {
    this.errorMessage = '';
    this.busy = true;
    try {
      await action();
    } catch (error) {
      console.error(error);
      this.errorMessage = message;
    } finally {
      this.busy = false;
    }
  }
}
