// Helpers de data da agenda: tudo em "data local" (YYYY-MM-DD), sem timezone —
// a agenda é hora de parede da loja, igual ao back-end (DateOnly/TimeOnly).

export function paraIso(data: Date): string {
  const ano = data.getFullYear();
  const mes = String(data.getMonth() + 1).padStart(2, "0");
  const dia = String(data.getDate()).padStart(2, "0");
  return `${ano}-${mes}-${dia}`;
}

export function deIso(iso: string): Date {
  const [ano, mes, dia] = iso.split("-").map(Number);
  return new Date(ano, mes - 1, dia);
}

export function somarDias(iso: string, dias: number): string {
  const data = deIso(iso);
  data.setDate(data.getDate() + dias);
  return paraIso(data);
}

export function hojeIso(): string {
  return paraIso(new Date());
}

/** Domingo da semana da data (a semana do calendário começa no domingo). */
export function inicioDaSemana(iso: string): string {
  const data = deIso(iso);
  return somarDias(iso, -data.getDay());
}

export function inicioDoMes(iso: string): string {
  return `${iso.slice(0, 7)}-01`;
}

export function fimDoMes(iso: string): string {
  const data = deIso(inicioDoMes(iso));
  data.setMonth(data.getMonth() + 1);
  data.setDate(0);
  return paraIso(data);
}

/** Ex.: "09:00:00" → "09:00" (a API fala TimeOnly com segundos). */
export function horaCurta(hora: string): string {
  return hora.slice(0, 5);
}

export function formatarDataCurta(iso: string): string {
  return deIso(iso).toLocaleDateString("pt-BR", { day: "2-digit", month: "2-digit" });
}

export function formatarDataLonga(iso: string): string {
  return deIso(iso).toLocaleDateString("pt-BR", {
    weekday: "long",
    day: "numeric",
    month: "long",
  });
}

export const DIAS_SEMANA_CURTOS = ["dom", "seg", "ter", "qua", "qui", "sex", "sáb"];
export const DIAS_SEMANA_LONGOS = [
  "Domingo",
  "Segunda-feira",
  "Terça-feira",
  "Quarta-feira",
  "Quinta-feira",
  "Sexta-feira",
  "Sábado",
];
