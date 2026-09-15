import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  Booking,
  CreateBookingRequest,
  DomainEvent,
  ReportSummary,
  SearchParams,
  TokenResponse,
  TrainResult,
  UserProfile
} from '../models/railway.models';

@Injectable({ providedIn: 'root' })
export class RailwayApiService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  login(email: string, password: string): Observable<TokenResponse> {
    return this.http.post<TokenResponse>(`${this.base}/api/auth/login`, { email, password });
  }

  getCurrentUser(): Observable<UserProfile> {
    return this.http.get<UserProfile>(`${this.base}/api/users/me`);
  }

  searchTrains(params: SearchParams): Observable<TrainResult[]> {
    const query = new HttpParams()
      .set('source', params.source)
      .set('destination', params.destination)
      .set('date', params.date)
      .set('class', params.travelClass)
      .set('quota', params.quota);

    return this.http.get<TrainResult[]>(`${this.base}/api/trains/search`, { params: query });
  }

  getBookings(): Observable<Booking[]> {
    return this.http.get<Booking[]>(`${this.base}/api/bookings`);
  }

  getBookingByPnr(pnr: string): Observable<Booking> {
    return this.http.get<Booking>(`${this.base}/api/bookings/${encodeURIComponent(pnr)}`);
  }

  createBooking(request: CreateBookingRequest): Observable<Booking> {
    return this.http.post<Booking>(`${this.base}/api/bookings`, request);
  }

  cancelBooking(pnr: string): Observable<Booking> {
    return this.http.post<Booking>(`${this.base}/api/bookings/${encodeURIComponent(pnr)}/cancel`, {});
  }

  getReportSummary(): Observable<ReportSummary> {
    return this.http.get<ReportSummary>(`${this.base}/api/reports/summary`);
  }

  getRecentEvents(): Observable<DomainEvent[]> {
    return this.http.get<DomainEvent[]>(`${this.base}/api/events`);
  }
}
