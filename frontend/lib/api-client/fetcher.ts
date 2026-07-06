import { obterAccessToken } from "@/lib/auth/token";

const BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5080";

/** Erro HTTP da API com o ProblemDetails retornado pelo back-end. */
export class ApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly problema: unknown,
  ) {
    super(extrairMensagem(status, problema));
    this.name = "ApiError";
  }
}

function extrairMensagem(status: number, problema: unknown): string {
  if (status === 429) {
    return "Muitas tentativas em sequência. Aguarde um instante e tente de novo.";
  }
  if (problema && typeof problema === "object") {
    const detalhes = problema as { title?: string; errors?: Record<string, string[]> };
    const primeiroErro = detalhes.errors
      ? Object.values(detalhes.errors).flat()[0]
      : undefined;
    if (primeiroErro) return primeiroErro;
    if (detalhes.title) return detalhes.title;
  }
  return `Erro ${status} ao falar com a API.`;
}

/**
 * Mutator usado por todo o cliente gerado pelo orval: injeta a base URL, o
 * Bearer token da memória e `credentials: include` (o cookie httpOnly de
 * refresh só viaja para /api/auth/*, limitado pelo Path do próprio cookie).
 *
 * Contrato do orval (httpClient fetch): resolve com `{ data, status }` em
 * sucesso; aqui, respostas não-2xx VIRAM exceção (ApiError) para cair no
 * caminho de erro do TanStack Query em vez de poluir o tipo de sucesso.
 */
export async function apiFetch<T>(url: string, options: RequestInit = {}): Promise<T> {
  const token = obterAccessToken();

  const resposta = await fetch(`${BASE_URL}${url}`, {
    ...options,
    credentials: "include",
    headers: {
      ...options.headers,
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
  });

  const temJson = resposta.headers.get("content-type")?.includes("json");
  const dados = temJson ? await resposta.json() : undefined;

  if (!resposta.ok) {
    throw new ApiError(resposta.status, dados);
  }

  return { data: dados, status: resposta.status } as T;
}
