import type {
  CheckInRequest,
  CheckInResponse,
  MovieDetailResponse,
  MovieSummaryResponse,
  OrderResponse,
  PagedResponse,
  ProblemResponse,
  PurchaseTicketsRequest,
  SeatMapResponse,
  TicketResponse,
} from '../types/contracts';

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly problem: ProblemResponse | null,
    message?: string
  ) {
    super(message || problem?.detail || `HTTP error ${status}`);
    this.name = 'ApiError';
  }
}

async function request<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
  const url = endpoint.startsWith('/') ? endpoint : `/api/v1/${endpoint}`;
  const response = await fetch(url, {
    ...options,
    headers: {
      Accept: 'application/json',
      'Content-Type': 'application/json',
      ...options.headers,
    },
  });

  if (!response.ok) {
    let problem: ProblemResponse | null = null;
    try {
      problem = (await response.json()) as ProblemResponse;
    } catch {
      // non-JSON response body
    }
    throw new ApiError(response.status, problem);
  }

  if (response.status === 204) {
    return undefined as unknown as T;
  }

  return (await response.json()) as T;
}

export const api = {
  // Catalog
  async listMovies(params?: {
    search?: string;
    genre?: string;
    page?: number;
    pageSize?: number;
  }): Promise<PagedResponse<MovieSummaryResponse>> {
    const query = new URLSearchParams();
    if (params?.search) query.set('search', params.search);
    if (params?.genre) query.set('genre', params.genre);
    if (params?.page) query.set('page', params.page.toString());
    if (params?.pageSize) query.set('pageSize', params.pageSize.toString());
    const qs = query.toString();
    return request<PagedResponse<MovieSummaryResponse>>(`/api/v1/movies${qs ? `?${qs}` : ''}`);
  },

  async getMovie(movieId: string): Promise<MovieDetailResponse> {
    return request<MovieDetailResponse>(`/api/v1/movies/${movieId}`);
  },

  async listGenres(): Promise<string[]> {
    return request<string[]>('/api/v1/genres');
  },

  async getSeatMap(showtimeId: string): Promise<SeatMapResponse> {
    return request<SeatMapResponse>(`/api/v1/showtimes/${showtimeId}/seat-map`);
  },

  // Ticketing
  async purchaseTickets(req: PurchaseTicketsRequest): Promise<OrderResponse> {
    return request<OrderResponse>('/api/v1/orders', {
      method: 'POST',
      body: JSON.stringify(req),
    });
  },

  async listTickets(holderId: string): Promise<TicketResponse[]> {
    return request<TicketResponse[]>(`/api/v1/tickets?holderId=${encodeURIComponent(holderId)}`);
  },

  async cancelTicket(ticketId: string, holderId: string): Promise<void> {
    return request<void>(
      `/api/v1/tickets/${ticketId}?holderId=${encodeURIComponent(holderId)}`,
      { method: 'DELETE' }
    );
  },

  async checkIn(req: CheckInRequest): Promise<CheckInResponse> {
    return request<CheckInResponse>('/api/v1/tickets/check-in', {
      method: 'POST',
      body: JSON.stringify(req),
    });
  },

  getQrCodeUrl(ticketId: string): string {
    return `/api/v1/tickets/${ticketId}/qr`;
  },
};
