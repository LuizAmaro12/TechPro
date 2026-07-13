import { defineConfig, globalIgnores } from "eslint/config";
import nextVitals from "eslint-config-next/core-web-vitals";
import nextTs from "eslint-config-next/typescript";

const eslintConfig = defineConfig([
  ...nextVitals,
  ...nextTs,
  // Override default ignores of eslint-config-next.
  globalIgnores([
    // Default ignores of eslint-config-next:
    ".next/**",
    "out/**",
    "build/**",
    "next-env.d.ts",
    // O Turbopack do dev server no container Linux ocasionalmente materializa
    // uma pasta com o caminho Windows sanitizado (ex.: "C:ProjetosPessoal...")
    // cheia de chunks de build dentro do bind mount — é artefato, não código.
    "C\\:*/**",
    "**/.next/**",
  ]),
]);

export default eslintConfig;
