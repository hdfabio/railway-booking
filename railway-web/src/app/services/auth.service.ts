import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { TokenResponse } from '../models/railway.models';
import { RailwayApiService } from './railway-api.service';

const TOKEN_KEY = 'railway_access_token';

export const DEMO_LOGIN = {
  email: 'demo@example.com',
  password: 'Demo123!'
} as const;

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = inject(RailwayApiService);

  get accessToken(): string | null {
    return sessionStorage.getItem(TOKEN_KEY);
  }

  get isAuthenticated(): boolean {
    return !!this.accessToken;
  }

  async login(email: string, password: string): Promise<TokenResponse> {
    const response = await firstValueFrom(this.api.login(email, password));
    sessionStorage.setItem(TOKEN_KEY, response.accessToken);
    return response;
  }

  async ensureDemoSession(): Promise<void> {
    if (this.isAuthenticated) {
      return;
    }

    await this.login(DEMO_LOGIN.email, DEMO_LOGIN.password);
  }

  logout(): void {
    sessionStorage.removeItem(TOKEN_KEY);
  }
}
