import type { NextConfig } from "next";

// URL da API para liberar no connect-src do CSP (fetch do cliente tipado).
const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5080";
const ehProducao = process.env.NODE_ENV === "production";

// Headers de segurança em todas as rotas web. O CSP protege as páginas contra
// XSS restringindo de onde scripts/estilos/conexões vêm. Next injeta estilos
// inline (por isso 'unsafe-inline' em style-src); o dev do Turbopack usa eval,
// então 'unsafe-eval' entra só fora de produção.
const csp = [
  "default-src 'self'",
  `script-src 'self' 'unsafe-inline'${ehProducao ? "" : " 'unsafe-eval'"}`,
  "style-src 'self' 'unsafe-inline'",
  "img-src 'self' data: blob:",
  "font-src 'self'",
  `connect-src 'self' ${apiUrl}`,
  "frame-ancestors 'none'",
  "base-uri 'self'",
  "form-action 'self'",
].join("; ");

const nextConfig: NextConfig = {
  async headers() {
    return [
      {
        source: "/:path*",
        headers: [
          { key: "Content-Security-Policy", value: csp },
          { key: "X-Frame-Options", value: "DENY" },
          { key: "X-Content-Type-Options", value: "nosniff" },
          { key: "Referrer-Policy", value: "strict-origin-when-cross-origin" },
          { key: "Permissions-Policy", value: "camera=(), microphone=(), geolocation=()" },
        ],
      },
    ];
  },
};

export default nextConfig;
