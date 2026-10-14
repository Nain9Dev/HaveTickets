<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { api, ApiError } from '../services/api';
import { getOrCreateHolderId, resetHolderId } from '../services/session';
import type { TicketResponse } from '../types/contracts';

const emit = defineEmits<{
  (e: 'browse-catalog'): void;
}>();

const tickets = ref<TicketResponse[]>([]);
const isLoading = ref<boolean>(true);
const actionInProgress = ref<string | null>(null);
const errorMessage = ref<string | null>(null);
const successMessage = ref<string | null>(null);
const currentHolderId = ref<string>('');

async function loadTickets() {
  isLoading.value = true;
  errorMessage.value = null;
  currentHolderId.value = getOrCreateHolderId();

  try {
    tickets.value = await api.listTickets(currentHolderId.value);
  } catch (err) {
    if (err instanceof ApiError) {
      errorMessage.value = err.problem?.detail || err.message;
    } else {
      errorMessage.value = 'Failed to load tickets.';
    }
  } finally {
    isLoading.value = false;
  }
}

async function cancelTicket(ticket: TicketResponse) {
  if (actionInProgress.value) return;
  if (!confirm(`Cancel ticket for seat ${ticket.seat} in ${ticket.movieTitle}? This immediately releases the seat back to the catalogue.`)) {
    return;
  }

  actionInProgress.value = ticket.id;
  errorMessage.value = null;
  successMessage.value = null;

  try {
    await api.cancelTicket(ticket.id, currentHolderId.value);
    successMessage.value = `Ticket ${ticket.code} successfully cancelled. Seat ${ticket.seat} has been returned to circulation.`;
    await loadTickets();
  } catch (err) {
    if (err instanceof ApiError) {
      errorMessage.value = err.problem?.detail || err.message;
    } else {
      errorMessage.value = 'Failed to cancel ticket.';
    }
  } finally {
    actionInProgress.value = null;
  }
}

function switchHolder() {
  currentHolderId.value = resetHolderId();
  loadTickets();
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString('en-US', {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  });
}

function formatTime(iso: string): string {
  return new Date(iso).toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' });
}

onMounted(() => {
  loadTickets();
});
</script>

<template>
  <div class="wallet-view">
    <header class="wallet-header">
      <div>
        <h1 class="gradient-title header-title">My Ticket Wallet</h1>
        <p class="header-subtitle">
          Your active cinema passes, entry QR codes, and seat reservations.
        </p>
      </div>

      <div class="holder-controls">
        <span class="holder-badge">
          Holder ID: <code class="holder-code">{{ currentHolderId.substring(0, 8) }}...</code>
        </span>
        <button class="btn btn-secondary btn-sm" title="Simulate another guest session" @click="switchHolder">
          🔄 Switch Guest
        </button>
      </div>
    </header>

    <!-- Notices -->
    <div v-if="errorMessage" class="notice-box error-box">
      <span>⚠️ {{ errorMessage }}</span>
    </div>
    <div v-if="successMessage" class="notice-box success-box">
      <span>✅ {{ successMessage }}</span>
    </div>

    <!-- Loading -->
    <div v-if="isLoading" class="state-container">
      <div class="spinner"></div>
      <p>Loading your tickets...</p>
    </div>

    <!-- Empty Wallet -->
    <div v-else-if="tickets.length === 0" class="empty-wallet glass-panel">
      <span class="empty-icon">🎟️</span>
      <h2>Your wallet is empty</h2>
      <p class="empty-text">You haven't reserved any cinema tickets yet. Explore the catalogue to book your next premiere.</p>
      <button class="btn btn-primary" @click="emit('browse-catalog')">
        Explore Movies & Showtimes →
      </button>
    </div>

    <!-- Tickets List -->
    <div v-else class="tickets-stream">
      <article
        v-for="ticket in tickets"
        :key="ticket.id"
        class="ticket-pass glass-panel"
        :class="{
          'ticket-checked-in': ticket.status === 'CheckedIn',
          'ticket-cancelled': ticket.status === 'Cancelled',
        }"
      >
        <!-- Poster & Visual strip -->
        <div class="pass-visual">
          <img :src="ticket.posterUrl" :alt="ticket.movieTitle" class="pass-poster" />
        </div>

        <!-- Ticket Body Details -->
        <div class="pass-body">
          <div class="pass-head">
            <span class="badge badge-seat">SEAT {{ ticket.seat }}</span>
            <span
              class="badge"
              :class="{
                'badge-emerald': ticket.status === 'Issued',
                'badge-purple': ticket.status === 'CheckedIn',
                'badge-rose': ticket.status === 'Cancelled',
              }"
            >
              {{ ticket.status === 'Issued' ? '● Valid Ticket' : ticket.status === 'CheckedIn' ? '✓ Admitted' : '✕ Cancelled' }}
            </span>
          </div>

          <h2 class="pass-movie-title">{{ ticket.movieTitle }}</h2>
          <p class="pass-cinema">📍 {{ ticket.cinemaName }} • {{ ticket.auditoriumName }}</p>

          <div class="pass-specs">
            <div class="spec-col">
              <span class="spec-label">DATE</span>
              <span class="spec-val">{{ formatDate(ticket.startsAt) }}</span>
            </div>
            <div class="spec-col">
              <span class="spec-label">SHOWTIME</span>
              <span class="spec-val">{{ formatTime(ticket.startsAt) }}</span>
            </div>
            <div class="spec-col">
              <span class="spec-label">PRICE PAID</span>
              <span class="spec-val">{{ ticket.pricePaid.toFixed(2) }} {{ ticket.currency }}</span>
            </div>
          </div>

          <!-- Cancellation action -->
          <div v-if="ticket.status === 'Issued'" class="pass-actions">
            <button
              class="btn btn-danger btn-sm"
              :disabled="actionInProgress === ticket.id"
              @click="cancelTicket(ticket)"
            >
              <span v-if="actionInProgress === ticket.id">Cancelling...</span>
              <span v-else>Cancel Ticket & Free Seat</span>
            </button>
          </div>
        </div>

        <!-- QR Code Stub with perforation line -->
        <div class="pass-stub">
          <div class="qr-container">
            <img
              :src="api.getQrCodeUrl(ticket.id)"
              :alt="`QR Code ${ticket.code}`"
              class="qr-bitmap"
              loading="lazy"
            />
          </div>
          <span class="stub-code">{{ ticket.code }}</span>
          <span class="stub-hint">SCAN AT DOOR</span>
        </div>
      </article>
    </div>
  </div>
</template>

<style scoped>
.wallet-view {
  display: flex;
  flex-direction: column;
  gap: 2rem;
}

.wallet-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-end;
  gap: 1.5rem;
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
}

.holder-controls {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.holder-badge {
  font-size: 0.82rem;
  color: var(--text-dim);
  background: rgba(255, 255, 255, 0.04);
  padding: 0.4rem 0.8rem;
  border-radius: var(--radius-sm);
  border: 1px solid var(--border-subtle);
}

.holder-code {
  color: #c4b5fd;
  font-family: var(--font-mono);
}

.tickets-stream {
  display: flex;
  flex-direction: column;
  gap: 1.75rem;
  max-width: 900px;
  margin: 0 auto;
  width: 100%;
}

.ticket-pass {
  display: flex;
  position: relative;
  overflow: hidden;
  transition: transform 0.2s ease, border-color 0.2s ease;
}

.ticket-pass:hover {
  transform: translateY(-2px);
  border-color: var(--border-highlight);
}

.ticket-pass.ticket-cancelled {
  opacity: 0.55;
  filter: grayscale(0.5);
}

.pass-visual {
  width: 140px;
  background: #000;
  flex-shrink: 0;
}

.pass-poster {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.pass-body {
  flex-grow: 1;
  padding: 1.5rem;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  border-right: 2px dashed rgba(255, 255, 255, 0.12);
}

.pass-head {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin-bottom: 0.5rem;
}

.badge-seat {
  background: rgba(99, 102, 241, 0.2);
  border: 1px solid rgba(99, 102, 241, 0.4);
  color: #818cf8;
  font-family: var(--font-mono);
  font-size: 0.85rem;
}

.pass-movie-title {
  font-family: var(--font-heading);
  font-size: 1.5rem;
  font-weight: 700;
  color: var(--text-main);
  line-height: 1.2;
}

.pass-cinema {
  font-size: 0.88rem;
  color: var(--text-muted);
  margin-top: 0.25rem;
}

.pass-specs {
  display: flex;
  gap: 2rem;
  margin: 1.25rem 0;
  flex-wrap: wrap;
}

.spec-col {
  display: flex;
  flex-direction: column;
}

.spec-label {
  font-size: 0.68rem;
  color: var(--text-dim);
  letter-spacing: 0.08em;
  font-weight: 600;
}

.spec-val {
  font-size: 0.95rem;
  font-weight: 600;
  color: var(--text-main);
}

.pass-actions {
  margin-top: 0.5rem;
}

.pass-stub {
  width: 180px;
  flex-shrink: 0;
  padding: 1.5rem;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  background: rgba(0, 0, 0, 0.25);
  gap: 0.5rem;
}

.qr-container {
  width: 110px;
  height: 110px;
  background: #ffffff;
  padding: 6px;
  border-radius: var(--radius-sm);
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.4);
}

.qr-bitmap {
  width: 100%;
  height: 100%;
  image-rendering: pixelated;
}

.stub-code {
  font-family: var(--font-mono);
  font-size: 0.95rem;
  font-weight: 700;
  letter-spacing: 0.12em;
  color: #c4b5fd;
}

.stub-hint {
  font-size: 0.65rem;
  letter-spacing: 0.15em;
  color: var(--text-dim);
  font-weight: 600;
}

.empty-wallet {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 5rem 2rem;
  gap: 1rem;
  text-align: center;
  max-width: 600px;
  margin: 0 auto;
}

.empty-icon {
  font-size: 3.5rem;
}

.empty-text {
  color: var(--text-muted);
  max-width: 450px;
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
  padding: 4rem;
  gap: 1rem;
}

@media (max-width: 720px) {
  .ticket-pass {
    flex-direction: column;
  }
  .pass-visual {
    width: 100%;
    height: 160px;
  }
  .pass-body {
    border-right: none;
    border-bottom: 2px dashed rgba(255, 255, 255, 0.12);
  }
  .pass-stub {
    width: 100%;
    flex-direction: row;
    gap: 1.5rem;
  }
}
</style>
