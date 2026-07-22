import type { LojaContatoResponse } from "@/lib/api-client/gerado";

/**
 * Contato e políticas da loja no portal do cliente final (módulo 13).
 * Some inteiro quando a loja ainda não preencheu nada.
 */
export function ContatoDaLoja({ contato }: { contato?: LojaContatoResponse }) {
  const temContato = contato?.telefone || contato?.email || contato?.endereco;
  if (!temContato && !contato?.politicas) {
    return null;
  }

  return (
    <div className="mt-4 rounded-2xl border border-borda bg-sutil p-4 text-sm">
      {temContato && (
        <div className="flex flex-wrap gap-x-4 gap-y-1 text-tinta-suave">
          {contato?.telefone && (
            <span>
              <span className="text-tinta-fraca">Telefone:</span>{" "}
              <span className="font-medium text-tinta">{contato.telefone}</span>
            </span>
          )}
          {contato?.email && (
            <span>
              <span className="text-tinta-fraca">E-mail:</span>{" "}
              <span className="font-medium text-tinta">{contato.email}</span>
            </span>
          )}
          {contato?.endereco && (
            <span>
              <span className="text-tinta-fraca">Endereço:</span>{" "}
              <span className="font-medium text-tinta">{contato.endereco}</span>
            </span>
          )}
        </div>
      )}
      {contato?.politicas && (
        <p className={`whitespace-pre-line text-tinta-suave ${temContato ? "mt-2" : ""}`}>
          <span className="text-tinta-fraca">Políticas:</span> {contato.politicas}
        </p>
      )}
    </div>
  );
}
