import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";

export default defineConfig({
  plugins: [vue()],
  server: {
    host: true,
    port: 5173,
    strictPort: true
  },
  define: {
    // Exponer la URL del API al navegador para el proxy de telemetría
    'window.API_BASE_URL': JSON.stringify(process.env.services__dotnet_api__https__0 || process.env.services__dotnet_api__http__0 || 'http://localhost:5000'),
  }
});
