/**
 * SLA visual do Kanban (Fase 2).
 *
 * O servidor entrega `horasNaEtapa` (calculado) e `slaHoras` (limite do
 * serviço, já nulo em etapa final). Aqui só traduzimos a razão entre os dois
 * em cor — as faixas são fixas de propósito: configurar o limite *e* as faixas
 * seria ajuste sobre ajuste, sem ganho para a loja.
 */
export type SituacaoSla = "sem-sla" | "no-prazo" | "atencao" | "estourado";

const ATENCAO = 0.7;

export function situacaoSla(
  horasNaEtapa: number | undefined,
  slaHoras: number | null | undefined,
): SituacaoSla {
  if (!slaHoras || slaHoras <= 0 || horasNaEtapa == null) return "sem-sla";
  const razao = horasNaEtapa / slaHoras;
  if (razao > 1) return "estourado";
  if (razao >= ATENCAO) return "atencao";
  return "no-prazo";
}

/** Faixa colorida na lateral do card — discreta o bastante para não competir
 * com a prioridade, visível o bastante para varrer o quadro de longe. */
export const BORDA_SLA: Record<SituacaoSla, string> = {
  "sem-sla": "border-l-transparent",
  "no-prazo": "border-l-emerald-400",
  atencao: "border-l-amber-400",
  estourado: "border-l-[#E8536B]",
};

export const ROTULO_SLA: Record<SituacaoSla, string> = {
  "sem-sla": "",
  "no-prazo": "no prazo",
  atencao: "atenção",
  estourado: "atrasada",
};

/** "3h" / "2d 5h" — o técnico lê de relance, sem fazer conta. */
export function formatarTempoNaEtapa(horas: number | undefined): string {
  if (horas == null) return "";
  if (horas < 1) return "menos de 1h";
  if (horas < 24) return `${Math.floor(horas)}h`;
  const dias = Math.floor(horas / 24);
  const resto = Math.floor(horas % 24);
  return resto > 0 ? `${dias}d ${resto}h` : `${dias}d`;
}
