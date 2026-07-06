import { defineConfig } from "orval";

// Gera o cliente tipado + hooks TanStack Query a partir do swagger da API.
// Fluxo: docker compose up api → curl swagger.json → npm run gerar-api.
export default defineConfig({
  techpro: {
    input: "./openapi/swagger.json",
    output: {
      // Em subpasta própria: `clean` apaga a pasta do target inteira a cada
      // geração — o fetcher.ts escrito à mão precisa ficar fora dela.
      target: "./lib/api-client/gerado/index.ts",
      client: "react-query",
      httpClient: "fetch",
      clean: true,
      override: {
        mutator: {
          path: "./lib/api-client/fetcher.ts",
          name: "apiFetch",
        },
      },
    },
  },
});
