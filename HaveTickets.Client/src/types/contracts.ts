export interface MovieSummaryResponse {
  id: string;
  title: string;
  synopsis: string;
  posterUrl: string;
  durationMinutes: number;
  releaseDate: string; // ISO Date YYYY-MM-DD
  genre: string;
  ageRating: string;
  showtimeCount: number;
  nextShowtimeAt: string | null; // ISO DateTime
}

export interface CinemaResponse {
  id: string;
  name: string;
  city: string;
  address: string;
}

export interface ShowtimeResponse {
  id: string;
  startsAt: string;
  price: number;
  currency: string;
  cinema: CinemaResponse;
  auditoriumName: string;
  capacity: number;
  seatsAvailable: number;
  salesOpen: boolean;
}

export interface MovieDetailResponse {
  id: string;
  title: string;
  synopsis: string;
  posterUrl: string;
  durationMinutes: number;
  releaseDate: string;
  genre: string;
  ageRating: string;
  showtimes: ShowtimeResponse[];
}

export interface SeatMapResponse {
  showtimeId: string;
  movieTitle: string;
  posterUrl: string;
  startsAt: string;
  cinemaName: string;
  auditoriumName: string;
  rowCount: number;
  seatsPerRow: number;
  price: number;
  currency: string;
  salesOpen: boolean;
  maxSeatsPerOrder: number;
  occupiedSeats: string[];
}

export interface PurchaseTicketsRequest {
  showtimeId: string;
  holderId: string;
  seats: string[];
}

export interface TicketResponse {
  id: string;
  code: string;
  status: 'Issued' | 'CheckedIn' | 'Cancelled';
  seat: string;
  pricePaid: number;
  currency: string;
  issuedAt: string;
  checkedInAt: string | null;
  showtimeId: string;
  startsAt: string;
  movieTitle: string;
  posterUrl: string;
  cinemaName: string;
  auditoriumName: string;
}

export interface OrderResponse {
  id: string;
  reference: string;
  placedAt: string;
  total: number;
  currency: string;
  tickets: TicketResponse[];
}

export interface CheckInRequest {
  code: string;
}

export interface CheckInResponse {
  admitted: boolean;
  outcome: string;
  message: string;
  ticket: TicketResponse | null;
}

export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface ProblemResponse {
  errorCode: string;
  detail: string;
  status: number;
  instance: string;
}
