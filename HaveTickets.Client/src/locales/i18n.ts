import { ref } from 'vue';

export type Locale = 'es' | 'en';

const STORAGE_KEY = 'nain_locale';
const EVENT_NAME = 'nain:locale-change';

function getInitialLocale(): Locale {
  if (typeof window === 'undefined') return 'es';
  const stored = localStorage.getItem(STORAGE_KEY);
  if (stored === 'es' || stored === 'en') return stored;
  return 'es';
}

export const currentLocale = ref<Locale>(getInitialLocale());

export function setLocale(locale: Locale) {
  if (currentLocale.value === locale) return;
  currentLocale.value = locale;
  if (typeof window !== 'undefined') {
    localStorage.setItem(STORAGE_KEY, locale);
    window.dispatchEvent(new CustomEvent(EVENT_NAME, { detail: { locale } }));
  }
}

if (typeof window !== 'undefined') {
  window.addEventListener(EVENT_NAME, (event: Event) => {
    const custom = event as CustomEvent<{ locale?: Locale }>;
    const target = custom.detail?.locale;
    if (target === 'es' || target === 'en') {
      currentLocale.value = target;
    }
  });
}

export const translations = {
  es: {
    movies: '🎬 Cartelera',
    myPasses: '🎟️ Mis Entradas',
    doorTurnstile: '🚪 Torno de Acceso',
    brandTagline: 'Estudio de Cine NainDev',
    footerCopy: 'HaveTickets Studio • Frontend Dual: Blazor WASM (puerto 5390) y Vue 3 + TypeScript (puerto 5391)',
    footerArch: 'Backend: ASP.NET Core 10 Minimal APIs (puerto 8190) • Clean Architecture • Índices Parciales SQLite • Reglas Deterministas',
  },
  en: {
    movies: '🎬 Movies',
    myPasses: '🎟️ My Passes',
    doorTurnstile: '🚪 Door Turnstile',
    brandTagline: 'NainDev Cinema Studio',
    footerCopy: 'HaveTickets Studio • Dual Frontend: Blazor WASM (port 5390) & Vue 3 + TypeScript (port 5391)',
    footerArch: 'Backend: ASP.NET Core 10 Minimal APIs (port 8190) • Clean Architecture • SQLite Partial Unique Indices • Deterministic Domain Rules',
  },
} as const;

export type TranslationKey = keyof typeof translations.es;

export function t(key: TranslationKey): string {
  const loc = currentLocale.value;
  return translations[loc][key] ?? translations.es[key] ?? key;
}
