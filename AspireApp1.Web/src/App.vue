<template>
  <div class="page">
    <header class="hero">
      <h1>AspireApp1 Web</h1>
      <p>Vue 3 Composition API client for DotNetApi.</p>
    </header>

    <section class="card">
      <h2>API preview</h2>
      <p class="hint">Future data will be loaded from DotNetApi.</p>

      <div class="actions">
        <button type="button" @click="loadHello" :disabled="loadingHello">
          {{ loadingHello ? "Loading..." : "Load /hello" }}
        </button>
        <button type="button" @click="loadFastApi" :disabled="loadingFastApi">
          {{ loadingFastApi ? "Loading..." : "Load /call-fastapi" }}
        </button>
      </div>

      <pre v-if="error" class="error">{{ error }}</pre>
      <pre v-if="response" class="response">{{ response }}</pre>
    </section>
  </div>
</template>

<script setup lang="ts">
import { ref } from "vue";
import { trackButtonClick, trackApiCall } from "./telemetry-helpers";

const loadingHello = ref(false);
const loadingFastApi = ref(false);
const error = ref<string>("");
const response = ref<string>("");

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:51270";

const request = async (
  path: string,
  setLoading: (value: boolean) => void
): Promise<void> => {
  setLoading(true);
  error.value = "";
  response.value = "";

  try {
    // Enviar telemetría de la llamada API
    const data = await trackApiCall(path, 'GET', async () => {
      const res = await fetch(`${apiBaseUrl}${path}`);
      if (!res.ok) {
        throw new Error(`Request failed (${res.status})`);
      }
      return res.json();
    });

    response.value = JSON.stringify(data, null, 2);
  } catch (err) {
    error.value = err instanceof Error ? err.message : String(err);
  } finally {
    setLoading(false);
  }
};

const loadHello = () => {
  // Enviar telemetría del click del botón
  trackButtonClick('load-hello', {
    'button.action': 'load-api-data',
    'api.endpoint': '/hello'
  });
  request("/hello", (value) => (loadingHello.value = value));
};

const loadFastApi = () => {
  // Enviar telemetría del click del botón
  trackButtonClick('load-fastapi', {
    'button.action': 'load-api-data',
    'api.endpoint': '/call-fastapi'
  });
  request("/call-fastapi", (value) => (loadingFastApi.value = value));
};
</script>
