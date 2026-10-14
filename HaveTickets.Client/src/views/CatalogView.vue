<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { api, ApiError } from '../services/api';
import type { MovieSummaryResponse } from '../types/contracts';

const emit = defineEmits<{
  (e: 'select-movie', movieId: string): void;
}>();

const movies = ref<MovieSummaryResponse[]>([]);
const genres = ref<string[]>([]);
const selectedGenre = ref<string>('');
const searchQuery = ref<string>('');
const isLoading = ref<boolean>(true);
const errorMessage = ref<string | null>(null);

const currentPage = ref<number>(1);
const totalPages = ref<number>(1);
const totalCount = ref<number>(0);

let searchTimeout: ReturnType<typeof setTimeout> | null = null;

async function loadGenres() {
  try {
    genres.value = await api.listGenres();
  } catch (err) {
    console.warn('Failed to load genres', err);
  }
}

async function loadMovies() {
  isLoading.value = true;
  errorMessage.value = null;
  try {
    const res = await api.listMovies({
      search: searchQuery.value || undefined,
      genre: selectedGenre.value || undefined,
      page: currentPage.value,
      pageSize: 12,
    });
    movies.value = res.items;
    currentPage.value = res.page;
    totalPages.value = res.totalPages;
    totalCount.value = res.totalCount;
  } catch (err) {
    if (err instanceof ApiError) {
      errorMessage.value = err.problem?.detail || err.message;
    } else {
      errorMessage.value = 'Failed to load movie catalogue. Ensure the API is running.';
    }
  } finally {
    isLoading.value = false;
  }
}

function onSearchInput(event: Event) {
  const target = event.target as HTMLInputElement;
  if (searchTimeout) clearTimeout(searchTimeout);
  searchTimeout = setTimeout(() => {
    searchQuery.value = target.value;
    currentPage.value = 1;
    loadMovies();
  }, 250);
}

function selectGenre(genre: string) {
  selectedGenre.value = selectedGenre.value === genre ? '' : genre;
  currentPage.value = 1;
  loadMovies();
}

function formatReleaseDate(iso: string): string {
  try {
    const [y, m, d] = iso.split('-');
    return new Date(Number(y), Number(m) - 1, Number(d)).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    });
  } catch {
    return iso;
  }
}

function formatNextShowtime(iso: string | null): string {
  if (!iso) return 'No upcoming showtimes';
  const date = new Date(iso);
  return `Next: ${date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })} at ${date.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' })}`;
}

onMounted(() => {
  loadGenres();
  loadMovies();
});
</script>

<template>
  <div class="catalog-view">
    <header class="catalog-header">
      <div>
        <h1 class="gradient-title header-title">Now Showing</h1>
        <p class="header-subtitle">
          Explore movies, select your exact auditorium seats, and receive verified digital passes.
        </p>
      </div>

      <div class="search-bar-wrapper">
        <span class="search-icon">🔍</span>
        <input
          type="search"
          placeholder="Search by title or synopsis..."
          class="search-input"
          @input="onSearchInput"
        />
      </div>
    </header>

    <!-- Genre pills filter -->
    <nav v-if="genres.length > 0" class="genre-filter" aria-label="Filter by genre">
      <button
        class="genre-pill"
        :class="{ active: selectedGenre === '' }"
        @click="selectGenre('')"
      >
        All Genres
      </button>
      <button
        v-for="genre in genres"
        :key="genre"
        class="genre-pill"
        :class="{ active: selectedGenre === genre }"
        @click="selectGenre(genre)"
      >
        {{ genre }}
      </button>
    </nav>

    <!-- Error state -->
    <div v-if="errorMessage" class="notice-box error-box">
      <span>⚠️ {{ errorMessage }}</span>
      <button class="btn btn-secondary" @click="loadMovies">Retry</button>
    </div>

    <!-- Loading state -->
    <div v-if="isLoading" class="state-container">
      <div class="spinner"></div>
      <p class="state-text">Loading movie catalogue...</p>
    </div>

    <!-- Empty state -->
    <div v-else-if="movies.length === 0" class="state-container glass-panel">
      <span class="empty-icon">🎬</span>
      <h2>No movies found</h2>
      <p class="state-text">Try adjusting your search criteria or genre filter.</p>
    </div>

    <!-- Movies Grid -->
    <div v-else class="movies-grid">
      <article
        v-for="movie in movies"
        :key="movie.id"
        class="movie-card glass-panel"
        @click="emit('select-movie', movie.id)"
      >
        <div class="poster-container">
          <img :src="movie.posterUrl" :alt="movie.title" class="poster-image" loading="lazy" />
          <div class="poster-overlay">
            <span class="overlay-cta">View Showtimes & Seats →</span>
          </div>
          <span class="age-badge">{{ movie.ageRating }}</span>
        </div>

        <div class="card-content">
          <div class="card-meta">
            <span class="badge badge-purple">{{ movie.genre }}</span>
            <span class="meta-duration">⏱️ {{ movie.durationMinutes }}m • 📅 {{ formatReleaseDate(movie.releaseDate) }}</span>
          </div>

          <h2 class="card-title">{{ movie.title }}</h2>
          <p class="card-synopsis">{{ movie.synopsis }}</p>

          <div class="card-footer">
            <span class="next-showtime">{{ formatNextShowtime(movie.nextShowtimeAt) }}</span>
            <button class="btn btn-primary btn-sm" @click.stop="emit('select-movie', movie.id)">
              Tickets
            </button>
          </div>
        </div>
      </article>
    </div>

    <!-- Pagination -->
    <footer v-if="totalPages > 1" class="pagination-bar">
      <button
        class="btn btn-secondary btn-sm"
        :disabled="currentPage <= 1 || isLoading"
        @click="currentPage--; loadMovies()"
      >
        ← Previous
      </button>
      <span class="page-indicator">Page {{ currentPage }} of {{ totalPages }} ({{ totalCount }} movies)</span>
      <button
        class="btn btn-secondary btn-sm"
        :disabled="currentPage >= totalPages || isLoading"
        @click="currentPage++; loadMovies()"
      >
        Next →
      </button>
    </footer>
  </div>
</template>

<style scoped>
.catalog-view {
  display: flex;
  flex-direction: column;
  gap: 2rem;
}

.catalog-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-end;
  gap: 2rem;
  flex-wrap: wrap;
}

.header-title {
  font-size: 2.5rem;
  line-height: 1.1;
  margin-bottom: 0.5rem;
}

.header-subtitle {
  color: var(--text-muted);
  font-size: 1rem;
  max-width: 550px;
}

.search-bar-wrapper {
  position: relative;
  min-width: 300px;
  flex-grow: 1;
  max-width: 420px;
}

.search-icon {
  position: absolute;
  left: 1rem;
  top: 50%;
  transform: translateY(-50%);
  font-size: 0.9rem;
  opacity: 0.6;
}

.search-input {
  width: 100%;
  padding: 0.75rem 1rem 0.75rem 2.6rem;
  background: var(--bg-surface);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-full);
  color: var(--text-main);
  font-family: inherit;
  font-size: 0.9rem;
  transition: all 0.2s ease;
}

.search-input:focus {
  border-color: var(--accent-primary);
  background: var(--bg-surface-hover);
  box-shadow: 0 0 15px var(--accent-glow);
}

.genre-filter {
  display: flex;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.genre-pill {
  padding: 0.4rem 1rem;
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-full);
  color: var(--text-muted);
  font-size: 0.82rem;
  font-weight: 500;
  transition: all 0.2s ease;
}

.genre-pill:hover {
  background: rgba(255, 255, 255, 0.08);
  color: var(--text-main);
}

.genre-pill.active {
  background: var(--accent-primary);
  border-color: var(--accent-primary);
  color: #fff;
  box-shadow: 0 0 14px var(--accent-glow);
}

.movies-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: 1.75rem;
}

.movie-card {
  display: flex;
  flex-direction: column;
  overflow: hidden;
  cursor: pointer;
  transition: transform 0.25s cubic-bezier(0.16, 1, 0.3, 1), border-color 0.25s ease, box-shadow 0.25s ease;
}

.movie-card:hover {
  transform: translateY(-5px);
  border-color: var(--border-highlight);
  box-shadow: 0 16px 36px -10px rgba(0, 0, 0, 0.7), 0 0 20px var(--accent-glow);
}

.poster-container {
  position: relative;
  width: 100%;
  aspect-ratio: 2 / 3;
  overflow: hidden;
  background: #000;
}

.poster-image {
  width: 100%;
  height: 100%;
  object-fit: cover;
  transition: transform 0.4s ease;
}

.movie-card:hover .poster-image {
  transform: scale(1.04);
}

.poster-overlay {
  position: absolute;
  inset: 0;
  background: linear-gradient(180deg, transparent 40%, rgba(7, 9, 14, 0.95) 100%);
  display: flex;
  align-items: flex-end;
  padding: 1.25rem;
  opacity: 0;
  transition: opacity 0.2s ease;
}

.movie-card:hover .poster-overlay {
  opacity: 1;
}

.overlay-cta {
  font-size: 0.85rem;
  font-weight: 600;
  color: #c4b5fd;
}

.age-badge {
  position: absolute;
  top: 0.75rem;
  right: 0.75rem;
  padding: 0.2rem 0.5rem;
  background: rgba(0, 0, 0, 0.7);
  backdrop-filter: blur(8px);
  border: 1px solid rgba(255, 255, 255, 0.2);
  border-radius: var(--radius-sm);
  font-size: 0.72rem;
  font-weight: 700;
  color: #e2e8f0;
}

.card-content {
  padding: 1.25rem;
  display: flex;
  flex-direction: column;
  flex-grow: 1;
  gap: 0.75rem;
}

.card-meta {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.meta-duration {
  font-size: 0.8rem;
  color: var(--text-dim);
}

.card-title {
  font-family: var(--font-heading);
  font-size: 1.2rem;
  font-weight: 700;
  line-height: 1.25;
  color: var(--text-main);
}

.card-synopsis {
  font-size: 0.85rem;
  color: var(--text-muted);
  line-height: 1.45;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
  flex-grow: 1;
}

.card-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-top: auto;
  padding-top: 0.75rem;
  border-top: 1px solid var(--border-subtle);
}

.next-showtime {
  font-size: 0.78rem;
  color: #a78bfa;
  font-weight: 500;
}

.btn-sm {
  padding: 0.4rem 0.85rem;
  font-size: 0.82rem;
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

.state-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 4rem 2rem;
  gap: 1rem;
  text-align: center;
}

.empty-icon {
  font-size: 3rem;
}

.state-text {
  color: var(--text-muted);
}

.pagination-bar {
  display: flex;
  justify-content: center;
  align-items: center;
  gap: 1.5rem;
  padding: 1.5rem 0;
}

.page-indicator {
  font-size: 0.85rem;
  color: var(--text-muted);
}
</style>
