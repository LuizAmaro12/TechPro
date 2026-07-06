"use client";

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useRef,
  useState,
} from "react";
import {
  postApiAuthLogin,
  postApiAuthLogout,
  postApiAuthRefresh,
  postApiAuthRegistrar,
  type AuthResponse,
  type LoginRequest,
  type RegistrarRequest,
  type UsuarioResponse,
} from "@/lib/api-client/gerado";
import { definirAccessToken } from "@/lib/auth/token";

type ContextoAuth = {
  usuario: UsuarioResponse | null;
  /** true enquanto o bootstrap (refresh via cookie) ainda não terminou. */
  carregando: boolean;
  entrar: (dados: LoginRequest) => Promise<void>;
  cadastrar: (dados: RegistrarRequest) => Promise<void>;
  sair: () => Promise<void>;
};

const Contexto = createContext<ContextoAuth | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [usuario, setUsuario] = useState<UsuarioResponse | null>(null);
  const [carregando, setCarregando] = useState(true);
  const renovacaoRef = useRef<number | undefined>(undefined);

  const limparSessao = useCallback(() => {
    window.clearTimeout(renovacaoRef.current);
    definirAccessToken(null);
    setUsuario(null);
  }, []);

  const aplicarSessao = useCallback(
    // Function expression nomeada: o agendamento de renovação chama a si
    // mesma sem referenciar o valor retornado pelo useCallback.
    function aplicar(auth: AuthResponse) {
      definirAccessToken(auth.accessToken ?? null);
      setUsuario(auth.usuario ?? null);

      // Renova 1 min antes de expirar: o access token vive só em memória,
      // então sem isso a sessão morreria a cada 15 minutos.
      if (auth.expiraEm) {
        const emMs = new Date(auth.expiraEm).getTime() - Date.now() - 60_000;
        window.clearTimeout(renovacaoRef.current);
        if (emMs > 0) {
          renovacaoRef.current = window.setTimeout(async () => {
            try {
              const resposta = await postApiAuthRefresh();
              if (resposta.status === 200) aplicar(resposta.data);
            } catch {
              limparSessao();
            }
          }, emMs);
        }
      }
    },
    [limparSessao],
  );

  // Bootstrap: tenta restaurar a sessão pelo cookie httpOnly de refresh.
  useEffect(() => {
    let ativo = true;
    (async () => {
      try {
        const resposta = await postApiAuthRefresh();
        if (ativo && resposta.status === 200) aplicarSessao(resposta.data);
      } catch {
        // Sem cookie válido: segue anônimo.
      } finally {
        if (ativo) setCarregando(false);
      }
    })();
    return () => {
      ativo = false;
      window.clearTimeout(renovacaoRef.current);
    };
  }, [aplicarSessao]);

  const entrar = useCallback(
    async (dados: LoginRequest) => {
      const resposta = await postApiAuthLogin(dados);
      if (resposta.status === 200) aplicarSessao(resposta.data);
    },
    [aplicarSessao],
  );

  const cadastrar = useCallback(
    async (dados: RegistrarRequest) => {
      const resposta = await postApiAuthRegistrar(dados);
      if (resposta.status === 201) aplicarSessao(resposta.data);
    },
    [aplicarSessao],
  );

  const sair = useCallback(async () => {
    try {
      await postApiAuthLogout();
    } catch {
      // Mesmo se a API falhar, a sessão local é encerrada.
    }
    limparSessao();
  }, [limparSessao]);

  return (
    <Contexto.Provider value={{ usuario, carregando, entrar, cadastrar, sair }}>
      {children}
    </Contexto.Provider>
  );
}

export function useAuth(): ContextoAuth {
  const contexto = useContext(Contexto);
  if (!contexto) {
    throw new Error("useAuth precisa estar dentro de <AuthProvider>.");
  }
  return contexto;
}
