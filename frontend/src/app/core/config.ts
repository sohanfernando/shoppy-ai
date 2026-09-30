import { environment } from '../../environments/environment';

// Matches the "http" profile in backend/Properties/launchSettings.json (dev);
// swapped for environment.prod.ts's origin in production builds.
export const API_ORIGIN = environment.apiOrigin;

export const API_BASE_URL = `${API_ORIGIN}/api`;

export const NOTIFICATIONS_HUB_URL = `${API_ORIGIN}/hubs/notifications`;

export const APP_NAME = 'ShoppyAI';

export const CURRENCY_SYMBOL = 'Rs.';
