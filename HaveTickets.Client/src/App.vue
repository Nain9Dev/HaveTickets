<script setup lang="ts">
import { ref } from 'vue';
import CatalogView from './views/CatalogView.vue';
import MovieDetailView from './views/MovieDetailView.vue';
import WalletView from './views/WalletView.vue';
import DoorScannerView from './views/DoorScannerView.vue';
import type { OrderResponse } from './types/contracts';
import { currentLocale, setLocale, t } from './locales/i18n';

type Tab = 'catalog' | 'wallet' | 'scanner';

const activeTab = ref<Tab>('catalog');
const selectedMovieId = ref<string | null>(null);

function goToCatalog() {
  selectedMovieId.value = null;
  activeTab.value = 'catalog';
}

function onSelectMovie(movieId: string) {
  selectedMovieId.value = movieId;
}

function onOrderPlaced(_order: OrderResponse) {
  selectedMovieId.value = null;
  activeTab.value = 'wallet';
}
</script>

<template>
  <div class="app-root">
    <!-- Top Navigation Bar -->
    <header class="navbar glass-panel">
      <div class="nav-container">
        <div class="brand" @click="goToCatalog">
          <div class="brand-logo">🎟️</div>
          <div class="brand-text">
            <span class="brand-title">HaveTickets</span>
            <span class="brand-tagline">{{ t('brandTagline') }}</span>
          </div>
        </div>

        <nav class="nav-links">
          <button
            class="nav-tab"
            :class="{ active: activeTab === 'catalog' && !selectedMovieId }"
            @click="goToCatalog"
          >
            {{ t('movies') }}
          </button>
          <button
            class="nav-tab"
            :class="{ active: activeTab === 'wallet' }"
            @click="selectedMovieId = null; activeTab = 'wallet'"
          >
            {{ t('myPasses') }}
          </button>
          <button
            class="nav-tab scanner-tab"
            :class="{ active: activeTab === 'scanner' }"
            @click="selectedMovieId = null; activeTab = 'scanner'"
          >
            {{ t('doorTurnstile') }}
          </button>

          <div class="lang-switch-group" role="group" aria-label="Language selector">
            <button
              type="button"
              class="lang-btn"
              :class="{ active: currentLocale === 'es' }"
              @click="setLocale('es')"
              aria-label="Español de España"
            >
              ES
            </button>
            <span class="lang-divider">|</span>
            <button
              type="button"
              class="lang-btn"
              :class="{ active: currentLocale === 'en' }"
              @click="setLocale('en')"
              aria-label="English"
            >
              EN
            </button>
          </div>
        </nav>
      </div>
    </header>

    <!-- Main Content Area -->
    <main class="main-content">
      <transition name="fade" mode="out-in">
        <component
          :is="selectedMovieId ? MovieDetailView : activeTab === 'catalog' ? CatalogView : activeTab === 'wallet' ? WalletView : DoorScannerView"
          :movie-id="selectedMovieId"
          @select-movie="onSelectMovie"
          @back="goToCatalog"
          @order-placed="onOrderPlaced"
          @browse-catalog="goToCatalog"
        />
      </transition>
    </main>

    <!-- Footer -->
    <footer class="app-footer">
      <div class="footer-container">
        <p class="footer-copy">
          HaveTickets Studio • Dual Frontend: Blazor WASM (port 5390) & Vue 3 + TypeScript (port 5391)
        </p>
        <p class="footer-arch">
          Backend: ASP.NET Core 10 Minimal APIs (port 8190) • Clean Architecture • SQLite Partial Unique Indices • Deterministic Domain Rules
        </p>
      </div>
    </footer>
  </div>
</template>

<style scoped>
.app-root {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
}

.navbar {
  position: sticky;
  top: 1rem;
  z-index: 100;
  margin: 1rem 1.5rem 2rem;
  border-radius: var(--radius-md);
  padding: 0.75rem 1.75rem;
}

.nav-container {
  display: flex;
  justify-content: space-between;
  align-items: center;
  max-width: 1300px;
  margin: 0 auto;
  width: 100%;
}

.brand {
  display: flex;
  align-items: center;
  gap: 0.85rem;
  cursor: pointer;
}

.brand-logo {
  font-size: 1.8rem;
  line-height: 1;
}

.brand-text {
  display: flex;
  flex-direction: column;
}

.brand-title {
  font-family: var(--font-heading);
  font-size: 1.35rem;
  font-weight: 800;
  letter-spacing: -0.02em;
  background: linear-gradient(135deg, #ffffff 40%, #c4b5fd 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
}

.brand-tagline {
  font-size: 0.7rem;
  font-weight: 600;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--text-dim);
}

.nav-links {
  display: flex;
  gap: 0.5rem;
}

.nav-tab {
  padding: 0.55rem 1.15rem;
  font-size: 0.88rem;
  font-weight: 600;
  border-radius: var(--radius-full);
  color: var(--text-muted);
  transition: all 0.2s ease;
}

.nav-tab:hover {
  color: var(--text-main);
  background: rgba(255, 255, 255, 0.06);
}

.nav-tab.active {
  color: #fff;
  background: rgba(139, 92, 246, 0.2);
  border: 1px solid rgba(139, 92, 246, 0.4);
  box-shadow: 0 0 12px var(--accent-glow);
}

.scanner-tab.active {
  background: rgba(16, 185, 129, 0.2);
  border-color: rgba(16, 185, 129, 0.4);
  box-shadow: 0 0 12px rgba(16, 185, 129, 0.35);
}

.lang-switch-group {
  display: inline-flex;
  align-items: center;
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-full);
  padding: 2px 8px;
  gap: 4px;
  margin-left: 0.5rem;
}

.lang-btn {
  background: none;
  border: none;
  color: var(--text-dim);
  font-family: var(--font-mono);
  font-size: 0.75rem;
  font-weight: 600;
  cursor: pointer;
  padding: 2px 4px;
  border-radius: 4px;
  transition: all 0.15s ease;
}

.lang-btn:hover {
  color: #ffffff;
}

.lang-btn.active {
  color: #a78bfa;
  background: rgba(167, 139, 250, 0.18);
}

.lang-divider {
  color: var(--border-subtle);
  font-size: 0.75rem;
  font-family: var(--font-mono);
  user-select: none;
}

.main-content {
  flex-grow: 1;
  max-width: 1300px;
  width: 100%;
  margin: 0 auto;
  padding: 0 1.5rem 3rem;
}

.app-footer {
  margin-top: auto;
  padding: 2.5rem 1.5rem;
  border-top: 1px solid var(--border-subtle);
  background: rgba(7, 9, 14, 0.75);
  text-align: center;
}

.footer-container {
  max-width: 900px;
  margin: 0 auto;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.footer-copy {
  font-size: 0.85rem;
  color: var(--text-muted);
  font-weight: 500;
}

.footer-arch {
  font-size: 0.75rem;
  color: var(--text-dim);
  font-family: var(--font-mono);
}

/* Transitions */
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s ease, transform 0.2s ease;
}

.fade-enter-from {
  opacity: 0;
  transform: translateY(6px);
}

.fade-leave-to {
  opacity: 0;
  transform: translateY(-6px);
}

@media (max-width: 768px) {
  .nav-container {
    flex-direction: column;
    gap: 1rem;
  }
}
</style>
