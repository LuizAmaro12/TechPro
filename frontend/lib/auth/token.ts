// O access token vive SOMENTE em memória (decisão aprovada #4): nunca em
// localStorage/sessionStorage, onde qualquer XSS o exfiltraria. Ao recarregar
// a página ele se perde — o AuthProvider o recupera via cookie de refresh.
let accessToken: string | null = null;

export function obterAccessToken(): string | null {
  return accessToken;
}

export function definirAccessToken(token: string | null): void {
  accessToken = token;
}
