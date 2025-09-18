import { fileURLToPath, URL } from 'node:url';
import vue from '@vitejs/plugin-vue';
import type { UserConfig as VitestUserConfig } from 'vitest/config';

// https://vite.dev/config/
const config = {
  plugins: [vue()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url))
    }
  },
  server: {
    host: '0.0.0.0',
    port: 5173
  },
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: [],
    coverage: {
      enabled: false
    }
  }
} as VitestUserConfig;

export default config;
