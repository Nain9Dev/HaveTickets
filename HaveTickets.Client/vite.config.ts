import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [vue()],
  server: {
    port: 5391,
    strictPort: true,
    proxy: {
      '/api': {
        target: 'http://localhost:8190',
        changeOrigin: true,
      },
    },
  },
  preview: {
    port: 5391,
    strictPort: true,
  },
});
