<script setup lang="ts">
import { ref } from 'vue';
import { api, ApiError } from '../services/api';
import type { CheckInResponse } from '../types/contracts';

const codeInput = ref<string>('');
const isScanning = ref<boolean>(false);
const scanResult = ref<CheckInResponse | null>(null);
const errorMessage = ref<string | null>(null);

async function submitScan() {
  const code = codeInput.value.trim().toUpperCase();
  if (!code) return;

  isScanning.value = true;
  errorMessage.value = null;
  scanResult.value = null;

  try {
    scanResult.value = await api.checkIn({ code });
  } catch (err) {
    if (err instanceof ApiError) {
      errorMessage.value = err.problem?.detail || err.message;
    } else {
      errorMessage.value = 'Scanner terminal error. Check network connection.';
    }
  } finally {
    isScanning.value = false;
  }
}

function resetScanner() {
  codeInput.value = '';
  scanResult.value = null;
  errorMessage.value = null;
}
</script>

<template>
  <div class="scanner-view">
    <header class="scanner-header">
      <h1 class="gradient-title header-title">Door Turnstile Inspector</h1>
      <p class="header-subtitle">
        Operator scanner terminal verifying 8-character confirmation codes and QR codes against the door admission invariants.
      </p>
    </header>

    <div class="terminal-card glass-panel">
      <form class="scan-form" @submit.prevent="submitScan">
        <label for="code-input" class="scan-label">ENTER TICKET CONFIRMATION CODE</label>
        <div class="input-group">
          <input
            id="code-input"
            v-model="codeInput"
            type="text"
            maxlength="8"
            placeholder="e.g. 7K2M9X4P"
            class="code-input"
            autocomplete="off"
            autofocus
          />
          <button
            type="submit"
            class="btn btn-primary btn-scan"
            :disabled="!codeInput.trim() || isScanning"
          >
            <span v-if="isScanning">Validating...</span>
            <span v-else>Verify & Check-In</span>
          </button>
        </div>
      </form>

      <!-- Scan result display -->
      <div v-if="scanResult" class="scan-result" :class="{ 'admitted': scanResult.admitted, 'refused': !scanResult.admitted }">
        <div class="result-status-icon">
          <span v-if="scanResult.admitted">✓</span>
          <span v-else>✕</span>
        </div>

        <div class="result-details">
          <span class="decision-badge">
            {{ scanResult.admitted ? 'ADMITTED' : 'ACCESS REFUSED' }}
          </span>

          <h2 class="result-message">{{ scanResult.message }}</h2>
          <p class="result-outcome">System rule: <code>{{ scanResult.outcome }}</code></p>

          <div v-if="scanResult.ticket" class="ticket-snapshot glass-panel">
            <div class="snapshot-row">
              <span class="label">MOVIE</span>
              <span class="val">{{ scanResult.ticket.movieTitle }}</span>
            </div>
            <div class="snapshot-row">
              <span class="label">SEAT</span>
              <span class="val highlight">Seat {{ scanResult.ticket.seat }}</span>
            </div>
            <div class="snapshot-row">
              <span class="label">AUDITORIUM</span>
              <span class="val">{{ scanResult.ticket.auditoriumName }} ({{ scanResult.ticket.cinemaName }})</span>
            </div>
            <div class="snapshot-row">
              <span class="label">STATUS</span>
              <span class="val">{{ scanResult.ticket.status }}</span>
            </div>
          </div>
        </div>

        <button class="btn btn-secondary btn-sm next-scan-btn" @click="resetScanner">
          Ready for Next Guest
        </button>
      </div>

      <div v-if="errorMessage" class="notice-box error-box">
        <span>⚠️ {{ errorMessage }}</span>
      </div>
    </div>
  </div>
</template>

<style scoped>
.scanner-view {
  display: flex;
  flex-direction: column;
  gap: 2rem;
  max-width: 800px;
  margin: 0 auto;
  width: 100%;
}

.scanner-header {
  text-align: center;
}

.header-title {
  font-size: 2.5rem;
  margin-bottom: 0.5rem;
}

.header-subtitle {
  color: var(--text-muted);
  font-size: 0.95rem;
}

.terminal-card {
  padding: 3rem 2.5rem;
  display: flex;
  flex-direction: column;
  gap: 2rem;
}

.scan-form {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.scan-label {
  font-size: 0.75rem;
  font-weight: 700;
  letter-spacing: 0.1em;
  color: var(--text-dim);
}

.input-group {
  display: flex;
  gap: 1rem;
}

.code-input {
  flex-grow: 1;
  background: rgba(0, 0, 0, 0.4);
  border: 2px solid var(--border-subtle);
  border-radius: var(--radius-sm);
  padding: 1rem 1.5rem;
  font-family: var(--font-mono);
  font-size: 1.5rem;
  font-weight: 700;
  letter-spacing: 0.25em;
  color: #fff;
  text-transform: uppercase;
  transition: all 0.2s ease;
}

.code-input:focus {
  border-color: var(--accent-primary);
  box-shadow: 0 0 20px var(--accent-glow);
}

.btn-scan {
  padding: 1rem 2rem;
  font-size: 1rem;
}

.scan-result {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 2.5rem 2rem;
  border-radius: var(--radius-md);
  text-align: center;
  gap: 1.25rem;
  animation: fadeIn 0.3s ease;
}

@keyframes fadeIn {
  from { opacity: 0; transform: translateY(10px); }
  to { opacity: 1; transform: translateY(0); }
}

.scan-result.admitted {
  background: rgba(16, 185, 129, 0.12);
  border: 1px solid rgba(16, 185, 129, 0.35);
}

.scan-result.refused {
  background: rgba(244, 63, 94, 0.12);
  border: 1px solid rgba(244, 63, 94, 0.35);
}

.result-status-icon {
  width: 70px;
  height: 70px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 2.5rem;
  font-weight: 800;
}

.admitted .result-status-icon {
  background: rgba(16, 185, 129, 0.2);
  color: #34d399;
}

.refused .result-status-icon {
  background: rgba(244, 63, 94, 0.2);
  color: #fb7185;
}

.decision-badge {
  font-size: 0.85rem;
  font-weight: 800;
  letter-spacing: 0.15em;
}

.admitted .decision-badge { color: #34d399; }
.refused .decision-badge { color: #fb7185; }

.result-message {
  font-family: var(--font-heading);
  font-size: 1.5rem;
  font-weight: 700;
  color: var(--text-main);
  margin-top: 0.25rem;
}

.result-outcome {
  font-size: 0.85rem;
  color: var(--text-dim);
}

.ticket-snapshot {
  margin-top: 1.5rem;
  padding: 1.25rem 2rem;
  width: 100%;
  max-width: 450px;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  text-align: left;
}

.snapshot-row {
  display: flex;
  justify-content: space-between;
}

.snapshot-row .label {
  font-size: 0.75rem;
  color: var(--text-dim);
  letter-spacing: 0.05em;
}

.snapshot-row .val {
  font-weight: 600;
  font-size: 0.9rem;
}

.snapshot-row .highlight {
  color: #34d399;
  font-family: var(--font-mono);
}

.next-scan-btn {
  margin-top: 1rem;
}

.notice-box {
  padding: 1rem;
  border-radius: var(--radius-sm);
}

.error-box {
  background: rgba(244, 63, 94, 0.15);
  border: 1px solid rgba(244, 63, 94, 0.35);
  color: #fda4af;
}
</style>
