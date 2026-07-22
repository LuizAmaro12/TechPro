import type { EtapaOrdemServico } from "@/lib/api-client/gerado";

// Ordem canônica do fluxo (módulo 3). A coluna "Agendado" do Kanban vem dos
// agendamentos (ainda não são OS) — por isso não está neste enum.
export const ETAPAS_OS: { valor: EtapaOrdemServico; rotulo: string }[] = [
  { valor: "CheckInRealizado", rotulo: "Check-in realizado" },
  { valor: "NaFila", rotulo: "Na fila" },
  { valor: "EmDiagnostico", rotulo: "Em diagnóstico" },
  { valor: "AguardandoAprovacao", rotulo: "Aguardando aprovação" },
  { valor: "AguardandoPeca", rotulo: "Aguardando peça" },
  { valor: "EmReparo", rotulo: "Em reparo" },
  { valor: "EmTeste", rotulo: "Em teste" },
  { valor: "ProntoParaRetirada", rotulo: "Pronto para retirada" },
  { valor: "Entregue", rotulo: "Entregue" },
  { valor: "Cancelado", rotulo: "Cancelado" },
];

export function rotuloDaEtapa(etapa: string | undefined): string {
  return ETAPAS_OS.find((e) => e.valor === etapa)?.rotulo ?? etapa ?? "";
}

export const ROTULOS_PRIORIDADE: Record<string, string> = {
  Baixa: "baixa",
  Normal: "normal",
  Alta: "alta",
};

export const ROTULOS_PAGAMENTO: Record<string, string> = {
  NaoPago: "não pago",
  Parcial: "parcial",
  Pago: "pago",
};

export const ROTULOS_APROVACAO: Record<string, string> = {
  Pendente: "pendente",
  Aprovado: "aprovado",
  Recusado: "recusado",
};

export const ROTULOS_STATUS_ORCAMENTO: Record<string, string> = {
  Rascunho: "rascunho",
  Enviado: "aguardando resposta",
  Aprovado: "aprovado",
  Recusado: "recusado",
};

export const ROTULOS_FORMA_PAGAMENTO: Record<string, string> = {
  Dinheiro: "dinheiro",
  Pix: "Pix",
  CartaoDebito: "cartão de débito",
  CartaoCredito: "cartão de crédito",
  Outro: "outro",
};

export const ROTULOS_EVENTO_COMUNICACAO: Record<string, string> = {
  AgendamentoConfirmado: "agendamento confirmado",
  AgendamentoLembrete: "lembrete de agendamento",
  OrdemServicoCriada: "OS criada",
  OrcamentoDisponivel: "orçamento disponível",
  OrcamentoAprovado: "orçamento aprovado",
  OrcamentoRecusado: "orçamento recusado",
  ProntoParaRetirada: "pronto para retirada",
  PedidoAvaliacao: "pedido de avaliação",
};

export const ROTULOS_STATUS_MENSAGEM: Record<string, string> = {
  Enviada: "enviada",
  Simulada: "simulada",
  Suprimida: "suprimida (sem consentimento)",
  Desativada: "desativada nas configurações",
  Falhou: "falhou",
};
