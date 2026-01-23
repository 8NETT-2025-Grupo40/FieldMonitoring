# FieldMonitoring — Relatório Funcional (Definições + User Stories)

> Documento focado **no nível funcional**: descreve o que o **FieldMonitoring** faz do ponto de vista do produto/usuário, e lista **User Stories** com critérios de aceite para orientar implementação e entrega.

---

## 1) Definições funcionais

### 1.1 O que é o FieldMonitoring
O **FieldMonitoring** é o “cérebro” do sistema: ele **transforma telemetria** (umidade, temperatura, precipitação) em informação útil para o produtor:

- **Histórico** (para gráficos)
- **Status geral** do talhão (ex.: `Normal`, `AlertaDeSeca`, `RiscoDePraga`)
- **Alertas** (ativos e histórico) baseados em regras de negócio

### 1.2 Entradas (o que chega no FieldMonitoring)
- **Leituras de sensor** (encaminhadas pela TelemetryIntake via mensageria), contendo pelo menos:
  - Identificação do talhão (`fieldId`)
  - Timestamp da leitura
  - Valores: umidade do solo, temperatura, precipitação
  - Um identificador único (`readingId`) para evitar duplicidade

> Observação funcional: o FieldMonitoring não precisa receber chamadas HTTP de sensor. Ele “recebe” leituras pelo fluxo assíncrono.

### 1.3 Saídas (o que o FieldMonitoring entrega)
Para consumo do dashboard (AMG) e do produtor:

- **Histórico** de leituras por talhão e período
- **Status atual** de cada talhão + explicação (“por quê”)
- **Alertas ativos** e **histórico de alertas**
- **Visões prontas** para o dashboard:
  - Overview de uma fazenda (talhões + status + último valor + alertas)
  - Detalhe de um talhão (histórico + status + alertas)

### 1.4 Motor de alertas (ciclo de vida)
Funcionalmente, um alerta deve ter um ciclo de vida simples:

- **Active**: a condição está ocorrendo agora
- **Resolved**: a condição cessou e o alerta foi encerrado

### 1.5 Exemplo de regra (seca)
- Se **umidade do solo < 30% por 24h** → gerar **Alerta de Seca** e atualizar status do talhão.

> A regra pode ser fixa no MVP e depois evoluir para “configurável” (threshold/janela).

### 1.6 Comportamentos importantes (para consistência)
- **Leituras fora de ordem / atrasadas**: o histórico deve respeitar o **timestamp** da leitura.
- **Leituras duplicadas**: a mesma leitura não deve criar pontos duplicados no gráfico nem alertas duplicados.
- **Sem dados recentes** *(opcional)*: sinalizar talhão sem telemetria por X horas, com “última leitura em …”.

### 1.7 Fluxo narrativo para demo (bem didático)
1. Chegam leituras com umidade baixa (< 30%) por várias horas → sistema mantém histórico.
2. Ao completar 24h contínuas < 30% → abre **Alerta de Seca** e muda status.
3. Quando a umidade sobe (≥ 30%) → resolve o alerta e status volta a normal.

---

## 2) User Stories (com critérios de aceite)

### Convenções
- **Prioridade**: Must / Should / Could
- IDs: `FM-XX` (FieldMonitoring)

---

## Épico A — Processar leituras e construir histórico

### FM-01 — Registrar leitura de sensor no histórico (**Must**)
**Como** sistema  
**Quero** registrar cada leitura recebida para um talhão  
**Para** permitir gráficos históricos e análise de regras.

**Critérios de aceite**
- Dada uma leitura com `fieldId`, `timestamp`, `soilMoisture`, `temperature`, `rain`
- Quando a leitura for processada
- Então ela deve ficar disponível no histórico daquele talhão
- E deve manter o timestamp original (não o horário de chegada)

---

### FM-02 — Consultar histórico por período (**Must**)
**Como** produtor  
**Quero** consultar o histórico de um talhão em um período  
**Para** visualizar gráficos e entender evolução.

**Critérios de aceite**
- Dado um talhão com leituras registradas
- Quando eu solicitar o histórico entre `from` e `to`
- Então devo receber apenas leituras dentro do período
- E o resultado deve vir ordenado por `timestamp`

---

## Épico B — Status do talhão (Normal / Seca / Praga)

### FM-04 — Manter status atual por talhão (**Must**)
**Como** produtor  
**Quero** ver o status atual de cada talhão  
**Para** identificar rapidamente onde preciso agir.

**Critérios de aceite**
- Dado um talhão com leituras
- Quando o sistema processar novas leituras
- Então o status atual do talhão deve ser recalculado e armazenado
- E o status deve ser um entre: `Normal`, `AlertaDeSeca`, `RiscoDePraga` (ou equivalente)

---

### FM-05 — Explicar o “porquê” do status (**Must**)
**Como** produtor  
**Quero** ver uma explicação curta do motivo do status  
**Para** confiar no sistema e tomar decisão.

**Critérios de aceite**
- Dado um talhão em `AlertaDeSeca`
- Quando eu consultar o status
- Então devo receber uma explicação (ex.: “umidade < 30% há 26h”)
- E essa explicação deve apontar quais medições/regras motivaram o status

---

### FM-06 — Status “Sem dados recentes” (**Should**)
**Como** produtor  
**Quero** identificar talhões sem telemetria recente  
**Para** saber quando um sensor pode estar fora do ar.

**Critérios de aceite**
- Dado um talhão sem novas leituras por X horas (configurável)
- Quando eu consultar o overview/status
- Então o sistema deve indicar “Sem dados recentes” (ou flag equivalente)
- E deve informar “última leitura em …”

---

## Épico C — Alertas (criar, manter, resolver)

### FM-07 — Gerar alerta de seca (**Must**)
**Como** produtor  
**Quero** receber um alerta quando o talhão estiver em condição de seca  
**Para** agir antes de perder produtividade.

**Critérios de aceite**
- Dado leituras com umidade abaixo de 30% por uma janela contínua de 24h
- Quando essa condição for atingida
- Então deve ser criado um alerta ativo do tipo `Seca`
- E o talhão deve passar para status `AlertaDeSeca`

---

### FM-08 — Resolver alerta quando condição cessar (**Must**)
**Como** produtor  
**Quero** que o alerta seja encerrado quando a condição voltar ao normal  
**Para** não ficar com alerta “preso”.

**Critérios de aceite**
- Dado um alerta de seca ativo
- Quando uma leitura indicar que a condição não está mais presente (ex.: umidade ≥ 30%)
- Então o alerta deve ser marcado como resolvido
- E o status do talhão deve voltar para `Normal` (se não houver outro alerta ativo)

---

### FM-09 — Listar alertas ativos (**Must**)
**Como** produtor  
**Quero** ver uma lista de alertas ativos da minha fazenda  
**Para** priorizar as ações do dia.

**Critérios de aceite**
- Dado múltiplos talhões com e sem alertas
- Quando eu solicitar “alertas ativos” por fazenda
- Então devo receber apenas os alertas ainda ativos
- E cada item deve incluir: `fieldId`, tipo, início, severidade (se existir) e resumo

---

### FM-10 — Histórico de alertas (**Must**)
**Como** produtor  
**Quero** consultar o histórico de alertas (resolvidos e ativos)  
**Para** acompanhar recorrência e sazonalidade.

**Critérios de aceite**
- Dado alertas abertos e resolvidos ao longo do tempo
- Quando eu solicitar o histórico por período
- Então devo receber alertas do período, com status (ativo/resolvido)
- E com timestamps de início e fim (quando aplicável)

---

### FM-11 — Reconhecer (ack) um alerta (**Could**)
**Como** produtor  
**Quero** marcar um alerta como “reconhecido”  
**Para** indicar que eu já vi e estou tratando.

**Critérios de aceite**
- Dado um alerta ativo
- Quando eu marcar como reconhecido
- Então o alerta deve registrar o momento do reconhecimento
- E o alerta continua ativo até a condição cessar (não “resolve” automaticamente)

---

## Épico D — Visões prontas pro dashboard (AMG)

### FM-12 — Overview da fazenda (**Must**)
**Como** produtor  
**Quero** ver uma visão geral por fazenda com todos os talhões  
**Para** enxergar rapidamente o panorama.

**Critérios de aceite**
- Dada uma fazenda com N talhões
- Quando eu solicitar o overview
- Então devo receber uma lista com, por talhão:
  - status atual
  - última leitura (valores + timestamp)
  - contagem de alertas ativos
- E deve ser possível filtrar/ordenar por status

---

### FM-13 — Detalhe do talhão (**Must**)
**Como** produtor  
**Quero** abrir um talhão e ver detalhe completo  
**Para** investigar e decidir ações.

**Critérios de aceite**
- Dado um talhão existente
- Quando eu solicitar o detalhe do talhão
- Então devo receber:
  - status atual + explicação
  - alertas ativos
  - histórico de medições (ou link/parâmetros para consulta de histórico)

---

## Épico E — Confiabilidade de processamento (funcional)

### FM-14 — Evitar duplicidade de leitura (**Must**)
**Como** sistema  
**Quero** impedir que a mesma leitura seja registrada duas vezes  
**Para** não distorcer gráficos e não duplicar alertas.

**Critérios de aceite**
- Dado que uma leitura possui um identificador único (`readingId`)
- Quando a mesma leitura chegar novamente
- Então ela não deve gerar novo ponto no histórico
- E não deve causar criação duplicada de alertas/status

---

### FM-15 — Tratar leituras fora de ordem (**Must**)
**Como** sistema  
**Quero** lidar com leituras que chegam atrasadas ou fora de ordem  
**Para** manter a consistência do histórico.

**Critérios de aceite**
- Dado que uma leitura pode chegar com timestamp anterior à última processada
- Quando uma leitura atrasada for processada
- Então ela deve entrar no histórico no lugar correto (por timestamp)
- E o status/alerta deve permanecer coerente (sem “quebrar” o estado)

---

### FM-16 — Registrar falhas de processamento (**Should**)
**Como** time do projeto  
**Quero** identificar leituras que falharam no processamento  
**Para** corrigir e reprocessar sem perder dados.

**Critérios de aceite**
- Dada uma leitura inválida ou inconsistente
- Quando o processamento falhar
- Então deve existir um registro de falha com motivo (ex.: “fieldId desconhecido”)
- E deve ser possível reprocessar após correção (mesmo que manual no MVP)

---

## Épico F — Regras configuráveis (diferencial DDD)

### FM-17 — Configurar regra de seca (**Should**)
**Como** administrador do sistema (ou equipe)  
**Quero** configurar o threshold e a janela da regra de seca  
**Para** adaptar o sistema a cenários/culturas diferentes.

**Critérios de aceite**
- Dada uma regra de seca configurável (threshold e janela)
- Quando eu alterar a configuração
- Então novas avaliações devem usar a nova regra
- E a regra deve ser exibida de forma legível (“umidade < X por Y”)

---

### FM-18 — Regra de “risco de praga” simples (**Could**)
**Como** produtor  
**Quero** receber um alerta de risco de praga baseado em condições simples  
**Para** agir preventivamente.

**Critérios de aceite**
- Dada uma regra simples (ex.: temperatura alta + umidade alta por X horas)
- Quando a condição for atendida
- Então deve ser criado alerta `RiscoDePraga`
- E o status do talhão deve refletir isso

---

## 3) Recorte sugerido de MVP (para planejamento)

### ✅ Implementado no MVP (Código Completo)

**Épico A — Histórico:**
- ✅ FM-01: Registrar leitura no histórico (InMemory time-series)
- ✅ FM-02: Consultar histórico por período
- ✅ FM-03: Agregação por hora/dia

**Épico B — Status:**
- ✅ FM-04: Manter status atual (Normal/DryAlert via aggregate Field)
- ✅ FM-05: Explicar "porquê" do status (StatusReason)
- ⚠️ FM-06: Status "Sem dados recentes" (arquitetura pronta, não implementado)

**Épico C — Alertas:**
- ✅ FM-07: Gerar alerta de seca (automático após 24h < 30%)
- ✅ FM-08: Resolver alerta quando condição cessar
- ✅ FM-09: Listar alertas ativos
- ✅ FM-10: Histórico de alertas
- ✅ FM-11: Reconhecer (ack) alerta via API

**Épico D — Visões:**
- ✅ FM-12: Overview da fazenda (GET /api/farms/{farmId}/overview)
- ✅ FM-13: Detalhe do talhão (GET /api/fields/{fieldId})

**Épico E — Confiabilidade:**
- ✅ FM-14: Evitar duplicidade (IIdempotencyStore + ProcessedReadings)
- ✅ FM-15: Tratar leituras fora de ordem (timestamp preservado)
- ❌ FM-16: Registrar falhas (não implementado - simplificação MVP)

**Épico F — Regras:**
- ⚠️ FM-17: Configurar regra de seca (hardcoded 30%/24h - evolução futura)
- ❌ FM-18: Regra de risco de praga (não implementado)

---

### 📊 Status de Implementação

**Funcionalidades Completas:** 11/15 (73%)
**Testes:** 45 testes (36/45 passando - 80%)

**Desvios do Plano Original:**
1. **Regras hardcoded** em vez de tabela configurável (simplificação MVP)
2. **FieldStatus removido** - arquitetura DDD pura com aggregate Field
3. **ProcessingFailures não implementado** - pode ser adicionado se necessário
4. **Status "Sem dados recentes"** - lógica pronta mas não ativa
5. **Time-series InMemory** - migrar para InfluxDB em produção

---

### 🎯 Próximas Evoluções (Pós-MVP)

**Alta Prioridade:**
- Migrar time-series para InfluxDB
- Implementar flag "Sem dados recentes" (NoRecentData)
- Corrigir 9 testes falhando (persistência Field)

**Média Prioridade:**
- Tabela Rules configurável via API/UI
- ProcessingFailures para debug
- Autenticação AWS Cognito (estrutura existe)

**Baixa Prioridade:**
- Alertas de risco de praga (PestRisk)
- Dashboard AMG customizado
- Regras por cultura/região
