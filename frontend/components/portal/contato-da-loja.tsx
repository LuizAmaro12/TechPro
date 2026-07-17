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
    <div className="mt-4 rounded-2xl border border-[#14162B]/8 bg-[#F7F7F9] p-4 text-sm">
      {temContato && (
        <div className="flex flex-wrap gap-x-4 gap-y-1 text-[#6B7280]">
          {contato?.telefone && (
            <span>
              <span className="text-[#8B8D98]">Telefone:</span>{" "}
              <span className="font-medium text-[#14162B]">{contato.telefone}</span>
            </span>
          )}
          {contato?.email && (
            <span>
              <span className="text-[#8B8D98]">E-mail:</span>{" "}
              <span className="font-medium text-[#14162B]">{contato.email}</span>
            </span>
          )}
          {contato?.endereco && (
            <span>
              <span className="text-[#8B8D98]">Endereço:</span>{" "}
              <span className="font-medium text-[#14162B]">{contato.endereco}</span>
            </span>
          )}
        </div>
      )}
      {contato?.politicas && (
        <p className={`whitespace-pre-line text-[#6B7280] ${temContato ? "mt-2" : ""}`}>
          <span className="text-[#8B8D98]">Políticas:</span> {contato.politicas}
        </p>
      )}
    </div>
  );
}
