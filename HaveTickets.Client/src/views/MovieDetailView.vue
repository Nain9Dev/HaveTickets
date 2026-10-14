<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import { api, ApiError } from '../services/api';
import { getOrCreateHolderId } from '../services/session';
import type {
  MovieDetailResponse,
  ShowtimeResponse,
  SeatMapResponse,
  OrderResponse,
} from '../types/contracts';

const props = defineProps<{
  movieId: string;
}>();

const emit = defineEmits<{
  (e: 'back'): void;
  (e: 'order-placed', order: OrderResponse): void;
}>();

const movie = ref<MovieDetailResponse | null>(null);
const selectedShowtime = ref<ShowtimeResponse | null>(null);
const seatMap = ref<SeatMapResponse | null>(null);
const selectedSeats = ref<Set<string>>(new Set());

const isLoadingMovie = ref<boolean>(true);
const isLoadingSeatMap = ref<boolean>(false);
const isPurchasing = ref<boolean>(false);
const errorMessage = ref<string | null>(null);
const purchaseSuccessMessage = ref<string | null>(null);

async function loadMovieDetails() {
  isLoadingMovie.value = true;
  errorMessage.value = null;
  try {
    movie.value = await api.getMovie(props.movieId);
    if (movie.value.showtimes.length > 0) {
      // Auto-select first showtime
      await selectShowtime(movie.value.showtimes[0]);
    }
  } catch (err) {
    if (err instanceof ApiError) {
      errorMessage.value = err.problem?.detail || err.message;
    } else {
      errorMessage.value = 'Failed to load movie details.';
    }
  } finally {
    isLoadingMovie.value = false;
  }
}

async function selectShowtime(st: ShowtimeResponse) {
  selectedShowtime.value = st;
  selectedSeats.value.clear();
  errorMessage.value = null;
  isLoadingSeatMap.value = true;
  try {
    seatMap.value = await api.getSeatMap(st.id);
  } catch (err) {
    if (err instanceof ApiError) {
      errorMessage.value = err.problem?.detail || err.message;
    } else {
      errorMessage.value = 'Failed to load seat availability.';
    }
  } finally {
    isLoadingSeatMap.value = false;
  }
}

function toggleSeat(seatId: string, isOccupied: boolean) {
  if (isOccupied || isPurchasing.value) return;

  if (selectedSeats.value.has(seatId)) {
    selectedSeats.value.delete(seatId);
    errorMessage.value = null;
  } else {
    const max = seatMap.value?.maxSeatsPerOrder || 6;
    if (selectedSeats.value.size >= max) {
      errorMessage.value = `Maximum of ${max} seats per order reached.`;
      return;
    }
    selectedSeats.value.add(seatId);
    errorMessage.value = null;
  }
}

const totalPrice = computed(() => {
  if (!selectedShowtime.value) return 0;
  return selectedSeats.value.size * selectedShowtime.value.price;
});

const groupedShowtimes = computed(() => {
  if (!movie.value) return new Map<string, ShowtimeResponse[]>();
  const map = new Map<string, ShowtimeResponse[]>();
  for (const st of movie.value.showtimes) {
    const cinemaKey = `${st.cinema.name} — ${st.cinema.city}`;
    if (!map.has(cinemaKey)) {
      map.set(cinemaKey, []);
    }
    map.get(cinemaKey)!.push(st);
  }
  return map;
});

async function purchaseTickets() {
  if (!selectedShowtime.value || selectedSeats.value.size === 0) return;

  isPurchasing.value = true;
  errorMessage.value = null;

  try {
    const holderId = getOrCreateHolderId();
    const order = await api.purchaseTickets({
      showtimeId: selectedShowtime.value.id,
      holderId,
      seats: Array.from(selectedSeats.value),
    });

    purchaseSuccessMessage.value = `Order ${order.reference} confirmed! Redirecting to your tickets...`;
    setTimeout(() => {
      emit('order-placed', order);
    }, 1200);
  } catch (err) {
    if (err instanceof ApiError) {
      if (err.status === 409) {
        errorMessage.value = 'A selected seat was just reserved by another customer. Please choose alternative seats.';
        // Refresh seat map to sync availability
        if (selectedShowtime.value) {
          selectShowtime(selectedShowtime.value);
        }
      } else {
        errorMessage.value = err.problem?.detail || err.message;
      }
    } else {
      errorMessage.value = 'An unexpected error occurred during ticket checkout.';
    }
  } finally {
    isPurchasing.value = false;
  }
}

function formatTime(iso: string): string {
  return new Date(iso).toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' });
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString('en-US', {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
  });
}

onMounted(() => {
  loadMovieDetails();
});
</script>

<template>
  <div class="movie-detail-view">
    <div class="top-nav-bar">
      <button class="btn btn-secondary btn-sm" @click="emit('back')">
        ← Back to Catalogue
      </button>
    </div>

    <!-- Error state -->
    <div v-if="errorMessage" class="notice-box error-box">
      <span>⚠️ {{ errorMessage }}</span>
    </div>

    <!-- Success state -->
    <div v-if="purchaseSuccessMessage" class="notice-box success-box">
      <span>🎉 {{ purchaseSuccessMessage }}</span>
    </div>

    <!-- Loading Movie -->
    <div v-if="isLoadingMovie" class="state-container">
      <div class="spinner"></div>
      <p>Loading showtime details...</p>
    </div>

    <div v-else-if="movie" class="detail-layout">
      <!-- Movie Showcase Hero -->
      <section class="hero-card glass-panel">
        <img :src="movie.posterUrl" :alt="movie.title" class="hero-poster" />
        <div class="hero-info">
          <div class="hero-badges">
            <span class="badge badge-purple">{{ movie.genre }}</span>
            <span class="badge badge-emerald">{{ movie.ageRating }}</span>
            <span class="meta-item">⏱️ {{ movie.durationMinutes }} minutes</span>
          </div>

          <h1 class="gradient-title hero-title">{{ movie.title }}</h1>
          <p class="hero-synopsis">{{ movie.synopsis }}</p>

          <div class="hero-attributes">
            <div class="attr-pill">
              <span class="attr-label">RELEASE DATE</span>
              <span class="attr-val">{{ movie.releaseDate }}</span>
            </div>
            <div class="attr-pill">
              <span class="attr-label">TOTAL SHOWTIMES</span>
              <span class="attr-val">{{ movie.showtimes.length }} scheduled</span>
            </div>
          </div>
        </div>
      </section>

      <!-- Showtime Selection -->
      <section class="showtimes-section">
        <h2 class="section-title">1. Select Showtime & Cinema</h2>

        <div v-if="movie.showtimes.length === 0" class="empty-notice glass-panel">
          <p>No upcoming showtimes available for this title right now.</p>
        </div>

        <div v-else class="cinema-groups">
          <div
            v-for="[cinemaName, showtimes] in groupedShowtimes"
            :key="cinemaName"
            class="cinema-group-card glass-panel"
          >
            <div class="cinema-header">
              <h3 class="cinema-title">📍 {{ cinemaName }}</h3>
              <p class="cinema-sub">{{ showtimes[0].cinema.address }}</p>
            </div>

            <div class="showtime-buttons">
              <button
                v-for="st in showtimes"
                :key="st.id"
                class="showtime-btn"
                :class="{ active: selectedShowtime?.id === st.id }"
                @click="selectShowtime(st)"
              >
                <span class="st-date">{{ formatDate(st.startsAt) }}</span>
                <span class="st-time">{{ formatTime(st.startsAt) }}</span>
                <span class="st-auditorium">{{ st.auditoriumName }}</span>
                <span class="st-price">{{ st.price.toFixed(2) }} {{ st.currency }}</span>
                <span class="st-seats">{{ st.seatsAvailable }} seats left</span>
              </button>
            </div>
          </div>
        </div>
      </section>

      <!-- Interactive Seat Selection Map -->
      <section v-if="selectedShowtime" class="seatmap-section">
        <div class="seatmap-header">
          <div>
            <h2 class="section-title">2. Choose Your Seats</h2>
            <p class="section-subtitle">
              {{ seatMap?.auditoriumName || selectedShowtime.auditoriumName }} • Cap:
              {{ seatMap?.maxSeatsPerOrder || 6 }} seats per reservation
            </p>
          </div>

          <div class="seat-legend">
            <span class="legend-item"><span class="legend-box available"></span> Available</span>
            <span class="legend-item"><span class="legend-box selected"></span> Selected</span>
            <span class="legend-item"><span class="legend-box occupied"></span> Sold</span>
          </div>
        </div>

        <div v-if="isLoadingSeatMap" class="state-container glass-panel">
          <div class="spinner"></div>
          <p>Loading real-time auditorium seat layout...</p>
        </div>

        <div v-else-if="seatMap" class="seatmap-board glass-panel">
          <!-- Screen representation -->
          <div class="screen-stage">
            <div class="screen-curve"></div>
            <span class="screen-text">CINEMA PROJECTION SCREEN</span>
          </div>

          <!-- Auditorium Seat Matrix -->
          <div
            class="seat-matrix"
            :style="{ '--cols': seatMap.seatsPerRow }"
          >
            <template v-for="r in seatMap.rowCount" :key="r">
              <div class="row-container">
                <span class="row-label">{{ String.fromCharCode(64 + r) }}</span>
                <div class="row-seats">
                  <button
                    v-for="c in seatMap.seatsPerRow"
                    :key="`${r}-${c}`"
                    type="button"
                    class="seat-cell"
                    :class="{
                      occupied: seatMap.occupiedSeats.includes(`${String.fromCharCode(64 + r)}${c}`),
                      selected: selectedSeats.has(`${String.fromCharCode(64 + r)}${c}`),
                      available: !seatMap.occupiedSeats.includes(`${String.fromCharCode(64 + r)}${c}`) && !selectedSeats.has(`${String.fromCharCode(64 + r)}${c}`),
                    }"
                    :disabled="seatMap.occupiedSeats.includes(`${String.fromCharCode(64 + r)}${c}`) || isPurchasing"
                    :aria-label="`Seat ${String.fromCharCode(64 + r)}${c}`"
                    @click="toggleSeat(`${String.fromCharCode(64 + r)}${c}`, seatMap.occupiedSeats.includes(`${String.fromCharCode(64 + r)}${c}`))"
                  >
                    {{ String.fromCharCode(64 + r) }}{{ c }}
                  </button>
                </div>
                <span class="row-label">{{ String.fromCharCode(64 + r) }}</span>
              </div>
            </template>
          </div>

          <!-- Checkout Summary Bar -->
          <div class="checkout-footer">
            <div class="checkout-meta">
              <div class="meta-selected-seats">
                <span class="meta-label">SELECTED SEATS</span>
                <span class="seats-list">
                  {{ selectedSeats.size === 0 ? 'No seats selected yet' : Array.from(selectedSeats).join(', ') }}
                </span>
              </div>
              <div class="meta-pricing">
                <span class="meta-label">TOTAL AMOUNT</span>
                <span class="total-amount">
                  {{ totalPrice.toFixed(2) }} {{ selectedShowtime.currency }}
                </span>
              </div>
            </div>

            <button
              class="btn btn-primary btn-checkout"
              :disabled="selectedSeats.size === 0 || isPurchasing"
              @click="purchaseTickets"
            >
              <span v-if="isPurchasing">Securing Seats...</span>
              <span v-else>Confirm Purchase ({{ selectedSeats.size }} Seats) →</span>
            </button>
          </div>
        </div>
      </section>
    </div>
  </div>
</template>

<style scoped>
.movie-detail-view {
  display: flex;
  flex-direction: column;
  gap: 2rem;
}

.top-nav-bar {
  display: flex;
}

.detail-layout {
  display: flex;
  flex-direction: column;
  gap: 2.5rem;
}

.hero-card {
  display: flex;
  gap: 2.5rem;
  padding: 2.5rem;
  overflow: hidden;
  position: relative;
}

.hero-poster {
  width: 240px;
  height: 350px;
  object-fit: cover;
  border-radius: var(--radius-sm);
  box-shadow: 0 12px 32px rgba(0, 0, 0, 0.6);
  flex-shrink: 0;
}

.hero-info {
  display: flex;
  flex-direction: column;
  justify-content: center;
  gap: 1rem;
}

.hero-badges {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.meta-item {
  font-size: 0.85rem;
  color: var(--text-dim);
}

.hero-title {
  font-size: 2.75rem;
  line-height: 1.15;
}

.hero-synopsis {
  color: var(--text-muted);
  font-size: 1rem;
  line-height: 1.6;
  max-width: 750px;
}

.hero-attributes {
  display: flex;
  gap: 1.5rem;
  margin-top: 0.5rem;
}

.attr-pill {
  display: flex;
  flex-direction: column;
  background: rgba(255, 255, 255, 0.03);
  border: 1px solid var(--border-subtle);
  padding: 0.5rem 1rem;
  border-radius: var(--radius-sm);
}

.attr-label {
  font-size: 0.68rem;
  color: var(--text-dim);
  letter-spacing: 0.08em;
  font-weight: 600;
}

.attr-val {
  font-size: 0.9rem;
  font-weight: 600;
  color: var(--text-main);
}

.section-title {
  font-family: var(--font-heading);
  font-size: 1.5rem;
  font-weight: 700;
  margin-bottom: 0.25rem;
}

.section-subtitle {
  color: var(--text-muted);
  font-size: 0.9rem;
}

.cinema-groups {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
  margin-top: 1rem;
}

.cinema-group-card {
  padding: 1.5rem;
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.cinema-title {
  font-size: 1.15rem;
  font-weight: 600;
}

.cinema-sub {
  font-size: 0.85rem;
  color: var(--text-dim);
}

.showtime-buttons {
  display: flex;
  flex-wrap: wrap;
  gap: 1rem;
}

.showtime-btn {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 0.75rem 1.25rem;
  background: rgba(255, 255, 255, 0.03);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-sm);
  transition: all 0.2s ease;
  min-width: 140px;
}

.showtime-btn:hover {
  background: rgba(255, 255, 255, 0.08);
  border-color: rgba(255, 255, 255, 0.2);
  transform: translateY(-2px);
}

.showtime-btn.active {
  background: rgba(139, 92, 246, 0.15);
  border-color: var(--accent-primary);
  box-shadow: 0 0 16px var(--accent-glow);
}

.st-date {
  font-size: 0.75rem;
  color: var(--text-dim);
}

.st-time {
  font-family: var(--font-heading);
  font-size: 1.25rem;
  font-weight: 700;
  color: var(--text-main);
  margin: 0.15rem 0;
}

.st-auditorium {
  font-size: 0.75rem;
  color: var(--text-muted);
}

.st-price {
  font-size: 0.82rem;
  font-weight: 600;
  color: #a78bfa;
  margin-top: 0.35rem;
}

.st-seats {
  font-size: 0.7rem;
  color: #10b981;
}

.seatmap-section {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.seatmap-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-end;
  flex-wrap: wrap;
  gap: 1rem;
}

.seat-legend {
  display: flex;
  gap: 1.25rem;
  font-size: 0.85rem;
  color: var(--text-muted);
}

.legend-item {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.legend-box {
  width: 14px;
  height: 14px;
  border-radius: 3px;
  display: inline-block;
}

.legend-box.available {
  background: rgba(255, 255, 255, 0.15);
  border: 1px solid rgba(255, 255, 255, 0.3);
}

.legend-box.selected {
  background: var(--accent-primary);
  box-shadow: 0 0 6px var(--accent-glow);
}

.legend-box.occupied {
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid rgba(255, 255, 255, 0.05);
}

.seatmap-board {
  padding: 3rem 2rem 2rem;
  display: flex;
  flex-direction: column;
  align-items: center;
}

.screen-stage {
  width: 80%;
  max-width: 600px;
  margin-bottom: 3.5rem;
  text-align: center;
}

.screen-curve {
  height: 6px;
  width: 100%;
  background: linear-gradient(90deg, transparent, #8b5cf6 30%, #a78bfa 50%, #8b5cf6 70%, transparent);
  border-radius: 50%;
  box-shadow: 0 0 20px rgba(139, 92, 246, 0.8), 0 0 40px rgba(139, 92, 246, 0.4);
}

.screen-text {
  display: block;
  font-size: 0.72rem;
  letter-spacing: 0.25em;
  color: var(--text-dim);
  margin-top: 0.75rem;
  font-weight: 600;
}

.seat-matrix {
  display: flex;
  flex-direction: column;
  gap: 0.6rem;
  margin-bottom: 3rem;
  overflow-x: auto;
  max-width: 100%;
  padding: 0.5rem;
}

.row-container {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.row-label {
  font-family: var(--font-mono);
  font-size: 0.85rem;
  font-weight: 700;
  color: var(--text-dim);
  width: 20px;
  text-align: center;
}

.row-seats {
  display: flex;
  gap: 0.5rem;
}

.seat-cell {
  width: 36px;
  height: 34px;
  border-radius: var(--radius-sm);
  font-size: 0.72rem;
  font-family: var(--font-mono);
  font-weight: 600;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.15s cubic-bezier(0.16, 1, 0.3, 1);
}

.seat-cell.available {
  background: rgba(255, 255, 255, 0.08);
  border: 1px solid rgba(255, 255, 255, 0.2);
  color: var(--text-main);
}

.seat-cell.available:hover {
  background: rgba(139, 92, 246, 0.35);
  border-color: #a78bfa;
  transform: translateY(-2px);
  box-shadow: 0 4px 10px rgba(139, 92, 246, 0.4);
}

.seat-cell.selected {
  background: var(--accent-primary);
  border: 1px solid #c4b5fd;
  color: #fff;
  transform: scale(1.08);
  box-shadow: 0 0 14px var(--accent-glow);
}

.seat-cell.occupied {
  background: rgba(255, 255, 255, 0.02);
  border: 1px solid rgba(255, 255, 255, 0.04);
  color: rgba(255, 255, 255, 0.15);
  cursor: not-allowed;
}

.checkout-footer {
  width: 100%;
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding-top: 1.5rem;
  border-top: 1px solid var(--border-subtle);
  flex-wrap: wrap;
  gap: 1.5rem;
}

.checkout-meta {
  display: flex;
  gap: 3rem;
  flex-wrap: wrap;
}

.meta-selected-seats, .meta-pricing {
  display: flex;
  flex-direction: column;
}

.meta-label {
  font-size: 0.7rem;
  color: var(--text-dim);
  letter-spacing: 0.08em;
  font-weight: 600;
}

.seats-list {
  font-family: var(--font-mono);
  font-size: 1.05rem;
  font-weight: 600;
  color: #c4b5fd;
}

.total-amount {
  font-family: var(--font-heading);
  font-size: 1.5rem;
  font-weight: 800;
  color: #fff;
}

.btn-checkout {
  padding: 0.85rem 2rem;
  font-size: 1rem;
}

.notice-box {
  padding: 1rem 1.5rem;
  border-radius: var(--radius-sm);
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.error-box {
  background: rgba(244, 63, 94, 0.15);
  border: 1px solid rgba(244, 63, 94, 0.35);
  color: #fda4af;
}

.success-box {
  background: rgba(16, 185, 129, 0.15);
  border: 1px solid rgba(16, 185, 129, 0.35);
  color: #6ee7b7;
}

.state-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 3rem;
  gap: 1rem;
}

@media (max-width: 850px) {
  .hero-card {
    flex-direction: column;
    align-items: center;
    text-align: center;
  }
  .hero-attributes {
    justify-content: center;
  }
  .checkout-footer {
    flex-direction: column;
    align-items: stretch;
  }
}
</style>
