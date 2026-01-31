import { createApp } from "vue";
import App from "./App.vue";
import "./style.css";
import { initTelemetry } from "./telemetry";

// Inicializar OpenTelemetry antes de crear la aplicación Vue
initTelemetry();

createApp(App).mount("#app");
