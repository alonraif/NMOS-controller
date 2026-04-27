import { defineConfig, loadEnv } from "vite";
import react from "@vitejs/plugin-react";
export default defineConfig(function (_a) {
    var _b;
    var mode = _a.mode;
    var env = loadEnv(mode, ".", "");
    var backendProxyBase = (_b = env.BACKEND_PROXY_BASE) !== null && _b !== void 0 ? _b : "http://127.0.0.1:8080";
    return {
        plugins: [react()],
        server: {
            host: "0.0.0.0",
            port: 5173,
            proxy: {
                "/api": {
                    target: backendProxyBase,
                    changeOrigin: true,
                },
            },
        },
        preview: {
            host: "0.0.0.0",
            port: 4173,
        },
    };
});
