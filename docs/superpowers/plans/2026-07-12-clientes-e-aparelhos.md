# Clientes e Aparelhos — Plano de Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Módulo 5 da Fase 1 (item 3 da ordem recomendada de `docs/fases_MVP.md`): CRM básico — clientes com filtros (ativos/inativos/VIP), aparelhos por cliente, estrutura de conta vinculada (família/empresa) com UI mínima e base de consentimento LGPD — API + testes + orval + tela.

**Architecture:** Novo módulo `Modules/Clientes/` seguindo exatamente os padrões do catálogo (Controller fino + Service com DbContext, `ITenantEntity` + GQF + `RlsHelper`, PK `int` identity, exclusão = desativação). Aparelhos como sub-recurso REST do cliente (`/api/clientes/{clienteId}/aparelhos`). Vínculo de conta é self-FK `cliente_principal_id` limitado a 1 nível (um principal não pode ser vinculado de outro).

**Tech Stack:** o mesmo da etapa do catálogo (plano 2026-07-08).

## Global Constraints

Idênticas ao plano 2026-07-08 (docs vinculantes, pt-BR, commits convencionais + Co-Authored-By, build/teste/migration no container do SDK, GQF+RLS em toda tabela de tenant, comandos de verificação idem).

**Decisões aprovadas em 2026-07-12:** cadastro completo (nome, telefone obrigatórios; e-mail, CPF, endereço, observações opcionais) + consentimento de comunicação operacional com data (módulo 14, Fase 1); aparelho com marca/modelo obrigatórios, IMEI/nº série, **senha de desbloqueio** (sensível — ver Desvios) e observações; **VIP manual** ("recorrente" será derivado quando OS existir); vínculo = **estrutura + UI mínima** (select "Vinculado a" + badge).

**Regras de negócio do vínculo:** 1 nível só — `clientePrincipalId` não pode apontar (a) para si mesmo, (b) para cliente que já tem principal, (c) para cliente de outro tenant (GQF → 400), e um cliente que possui vinculados não pode virar vinculado.

**LGPD:** exclusão de cliente é desativação; a anonimização (seção 16 do doc de stack) chega na Fase 2 trocando nome/telefone/e-mail/CPF por marcadores — o schema atual já suporta sem migração destrutiva.

---

### Task 1: Entidades + migration `Clientes` com RLS

**Files:** Create `Modules/Clientes/Cliente.cs`, `Modules/Clientes/Aparelho.cs`; Modify `Shared/Persistence/TechProDbContext.cs`; migration gerada no container; Test `tests/.../Clientes/ClientesIsolamentoTests.cs` (mesmo molde de `CatalogoIsolamentoTests`).

**Schema:**
- `clientes`: id int identity, tenant_id, nome (200, req), telefone (20, req), email (256?), cpf (14?), endereco (300?), observacoes (1000?), vip bool, ativo bool default true, cliente_principal_id int? (self-FK Restrict), consentiu_comunicacoes bool, consentimento_em timestamptz?, criado_em; índices tenant_id e (tenant_id, nome).
- `aparelhos`: id int identity, tenant_id, cliente_id (FK Cascade), marca (100, req), modelo (150, req), imei (50?), senha_desbloqueio (100?), observacoes (500?), ativo bool default true, criado_em; índices tenant_id e cliente_id.
- `RlsHelper.AplicarIsolamentoTenant` em `clientes` e `aparelhos`; verificar `relrowsecurity`/`relforcerowsecurity = t` via psql.

- [ ] Teste de isolamento (vermelho por compilação) → entidades → DbContext → suíte verde → migration no container → RLS no `Up()` → psql confere → commit `feat(clientes): entidades de cliente e aparelho com GQF e RLS`.

### Task 2: API de Clientes

**Files:** Create `Modules/Clientes/Dtos/ClienteDtos.cs`, `Validadores.cs`, `ClienteService.cs`, `ClientesController.cs`; Modify `Program.cs`; Test `tests/.../Clientes/ClientesFluxoTests.cs`.

**Contrato:**
- `ClienteRequest(Nome, Telefone, Email?, Cpf?, Endereco?, Observacoes?, Vip, Ativo=true, ClientePrincipalId?, ConsentiuComunicacoes)`
- `ClienteResponse(Id, Nome, Telefone, Email, Cpf, Endereco, Observacoes, Vip, Ativo, ClientePrincipal: VinculoResponse?, ConsentiuComunicacoes, ConsentimentoEm, QuantidadeAparelhos)` com `VinculoResponse(Id, Nome)`
- `ClienteDetalheResponse` = ClienteResponse + `Aparelhos: AparelhoResponse[]`
- `GET /api/clientes?busca=&somenteVip=&incluirInativos=&pagina=&tamanhoPagina=` (busca em nome/telefone/cpf) → `PaginaResponse<ClienteResponse>`; `GET /{id}` → detalhe; `POST` 201; `PUT /{id}` 200/400/404; `DELETE /{id}` desativa 204.
- Validador: nome/telefone NotEmpty; e-mail válido quando informado; CPF = 11 dígitos (ignorando máscara) quando informado; `ConsentimentoEm` setado pelo service quando `ConsentiuComunicacoes` vira true.

- [ ] Testes (vermelho): CRUD completo; isolamento entre empresas (404); filtros VIP/busca/inativos; regras do vínculo (si mesmo, 2 níveis, outro tenant → 400); validação (sem nome, cpf inválido → 400); 401 sem token → implementar → suíte verde → commit `feat(clientes): crud de clientes com filtros, vinculo e consentimento`.

### Task 3: API de Aparelhos (sub-recurso)

**Files:** Create `Modules/Clientes/Dtos/AparelhoDtos.cs` (`AparelhoRequest(Marca, Modelo, Imei?, SenhaDesbloqueio?, Observacoes?, Ativo=true)`, `AparelhoResponse(...)`); ampliar `ClienteService` (ou `AparelhoService`); `AparelhosController` com rota `api/clientes/{clienteId:int}/aparelhos`; Test no mesmo arquivo.

- `POST` 201 (404 se cliente não é do tenant), `PUT /{id}` 200/404, `DELETE /{id}` desativa 204. Aparelhos aparecem no `GET /api/clientes/{id}`.
- [ ] Testes (vermelho): adicionar/editar/desativar; aparelho de cliente de outra empresa → 404 → implementar → suíte verde → commit `feat(clientes): aparelhos por cliente como sub-recurso`.

### Task 4: Contrato orval

- [ ] `docker compose up -d --build api` → snapshot swagger → `npm run gerar-api` → `tsc --noEmit` → conferir hooks (`useGetApiClientes`, `usePostApiClientesClienteIdAparelhos`, ...) → commit `feat(frontend): cliente tipado regenerado com endpoints de clientes`.

### Task 5: Front — /clientes

**Files:** Modify `(empreendedor)/layout.tsx` (link "Clientes"); Create `lib/validators/clientes.ts` (esquemaCliente, esquemaAparelho); Create `(empreendedor)/clientes/page.tsx`.

- Tabela: nome (+badges VIP/vinculado/inativo), telefone, e-mail, nº de aparelhos, ações. Busca + checkboxes "somente VIP"/"mostrar inativos". Form card no padrão das telas do catálogo: campos completos, checkbox consentimento, select "Vinculado a" (apenas clientes ativos sem vínculo e sem dependentes, excluindo o próprio). Ao editar, seção "Aparelhos" com lista + mini-form inline (marca, modelo, IMEI, senha, observações) e desativar por item.
- [ ] `tsc` + lint + build limpos → `docker compose restart frontend` → conferência manual → commit `feat(frontend): tela de clientes com aparelhos e vinculo familiar`.

### Task 6: E2E + documentação

- [ ] Script `fluxo-clientes.mjs` no scratchpad (registrar conta → criar cliente completo com consentimento → criar 2º cliente vinculado ao 1º → adicionar aparelho → filtro VIP → desativar → filtro inativos) com screenshots + JSON de evidência.
- [ ] `docs/progresso.md`: nova subseção da etapa (evidência, contagem de testes), seção "O que está construído" += Clientes/CRM, Desvios += senha de desbloqueio (dado sensível em texto — candidata a criptografia de campo quando houver exigência; criptografia em repouso do provedor já cobre o disco), Próximos passos → Agenda/portal de agendamento.
- [ ] Commit `docs: progresso da etapa clientes com evidencias e2e`.
