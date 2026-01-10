# FieldMonitoring — Arquitetura de Dados + Contratos (Guia de Implementação)

Este documento é para um desenvolvedor que **ainda não tem contexto** do projeto.
Ele descreve **o que construir** no FieldMonitoring (nível alto), com foco em:

- **como os dados serão armazenados** (Time-series/NoSQL + SQL Server)
- **quais responsabilidades ficam em cada banco**
- **quais “contratos” (ports) o núcleo da aplicação deve expor** (arquitetura hexagonal)
- **fluxos de leitura/escrita** que o MVP precisa suportar
- **notas importantes** para evitar armadilhas comuns (idempotência, ordem temporal, dashboard AMG)

> Não há código aqui. A implementação deve seguir os princípios e decisões deste guia.

---

## 1) Contexto rápido (o que é FieldMonitoring)

O **FieldMonitoring** é o “cérebro” do sistema:
- recebe **leituras de sensores** (via mensageria, vindas do TelemetryIntake)
- grava **histórico** para gráficos
- calcula e mantém **status atual do talhão** (Normal / AlertaDeSeca / RiscoDePraga / SemDadosRecentes)
- gera e mantém **alertas** (ativos e histórico)
- expõe dados para o **Amazon Managed Grafana (AMG)**

---

## 2) Decisão central: separar “histórico” de “estado atual”

### 2.1 Time-series / NoSQL (somente leituras)
**Uso:** armazenar **pontos no tempo** (umidade/temp/chuva) para gráficos e agregações.

- ideal para: queries por período, downsampling (média por hora/dia), retenção baseada em tempo
- não deve armazenar: status atual, alertas, regras, idempotência (isso é “estado derivado”)

**Candidato:** InfluxDB (preferencial) ou MongoDB Time-Series (alternativa).

### 2.2 SQL Server (operacional/derivado)
**Uso:** armazenar o que é “decisão/estado atual” e precisa de consistência.

- status atual por talhão
- alertas ativos/histórico
- idempotência (`readingId`)
- regras configuráveis
- (opcional) falhas para reprocessamento

---

## 3) Diagrama (alto nível)

```txt
TelemetryIntake -> (SNS/SQS) -> FieldMonitoring (Worker + API no mesmo container)
                           |
                           v
                 +------------------+        +-------------------+
                 | Time-series DB   |        | SQL Server        |
                 | (leituras)       |        | (estado/derivado) |
                 +------------------+        +-------------------+

AMG (Grafana) lê:
- gráficos: Time-series DB
- cards/listas: SQL Server (status/alertas)
```

---

## 4) Modelo de dados — Time-series (InfluxDB)

> Use InfluxDB se possível, pois ele encaixa naturalmente em consultas de séries temporais e dashboards.

### 4.1 Bucket
- `telemetry` (retenção: 30–90 dias no MVP; ajustar conforme necessidade)

### 4.2 Measurement
- `field_readings`

### 4.3 Tags (dimensões)
Tags são “chaves de filtro/agrupamento”. Mantenha **poucas e estáveis** para evitar explosão de cardinalidade.

- `farmId`
- `fieldId`
- *(opcional)* `source` = `http|mqtt`
- *(opcional)* `sensorId` **somente** se isso for realmente usado no produto

**Evitar como tag:**
- `readingId` (muito cardinal)
- valores “livres” que mudam sempre

### 4.4 Fields (valores)
- `soilMoisturePercent` (float)
- `temperatureC` (float)
- `rainMm` (float)

### 4.5 Timestamp
- usar o `timestamp` da leitura (hora da medição), não hora de chegada.

### 4.6 Exemplo conceitual de ponto
- measurement: `field_readings`
- tags: `farmId=F1`, `fieldId=T1`, `source=http`
- fields: `soilMoisturePercent=27.3`, `temperatureC=33.1`, `rainMm=0`
- time: `2026-01-06T14:22:00Z`

### 4.7 Consultas esperadas (para gráficos)
- histórico do talhão: `fieldId` + período (`from/to`)
- agregação por janela (hora/dia): média, mín, máx
- última leitura por talhão (opcional — mas preferimos manter também no SQL Server)

---

## 5) Modelo de dados — Time-series (MongoDB) [alternativa]

Se optar por Mongo time-series, o desenho funcional é:

- Collection: `field_readings_ts` (time-series)
- `timeField`: `timestamp`
- `metaField`: `meta`

Documento (alto nível):
```json
{
  "timestamp": "2026-01-06T14:22:00Z",
  "meta": { "farmId": "F1", "fieldId": "T1", "source": "http" },
  "soilMoisturePercent": 27.3,
  "temperatureC": 33.1,
  "rainMm": 0
}
```

---

## 6) Modelo de dados — SQL Server (operacional/derivado)

### 6.1 `Fields` (Aggregate Root - 1 linha por talhão)
**Objetivo:** Persistir o aggregate Field com seu estado completo (status + estado de regras).

> **MUDANÇA IMPORTANTE (MVP):** FieldStatusCurrent foi **REMOVIDO**. Agora usamos apenas a tabela `Fields` que persiste o aggregate Field completo. Isso simplifica a arquitetura eliminando duplicação.

Campos:
- `FieldId` (PK, string)
- `FarmId` (string, 100 chars)
- `Status` (string, 50 chars - Normal / DryAlert / PestRisk)
- `StatusReason` (string, 500 chars, nullable)
- `LastReadingAt` (datetime, nullable)
- `LastSoilMoisture` (double, nullable - valor convertido de Value Object)
- `LastTemperature` (double, nullable - valor convertido de Value Object)
- `LastRain` (double, nullable - valor convertido de Value Object)
- `LastTimeAboveDryThreshold` (datetime, nullable - estado da regra de seca)
- `UpdatedAt` (datetime, required)

Índices:
- `(FarmId)` para overview de fazenda
- `(Status)` para filtros/contagens
- `(FarmId, Status)` composto para queries otimizadas

**Relação:** 
- `Fields` 1:N `Alerts` (cascade delete)

---

### 6.2 `Alerts` (ativos e histórico)
**Objetivo:** alertas com ciclo de vida.

Campos sugeridos:
- `AlertId` (PK)
- `FarmId`
- `FieldId`
- `AlertType` (Dryness / PestRisk / ...)
- `Severity` (opcional)
- `Status` (Active / Resolved)
- `Reason` (texto curto)
- `StartedAt`
- `ResolvedAt` (nullable)
- `CreatedAt`

Índices úteis:
- `(FarmId, Status)` para listar ativos
- `(FieldId, StartedAt)` para histórico por talhão

---

### 6.3 `ProcessedReadings` (idempotência)
**Objetivo:** evitar duplicidade (a mesma leitura não deve ser aplicada duas vezes).

Campos:
- `ReadingId` (PK, string, 200 chars)
- `FieldId` (string, 100 chars)
- `ProcessedAt` (datetime)
- `Source` (string, 20 chars - enum: Http/Mqtt)

Índice:
- `(FieldId)` para queries por talhão

Regra funcional:
- se `ReadingId` já existe → **ignorar** a leitura (não duplicar ponto nem alertas)

---

### 6.4 `Rules` - **REMOVIDO NO MVP**

> **MUDANÇA (MVP):** A tabela `Rules` foi **REMOVIDA** para simplificar. No MVP, usamos regra de seca hardcoded (30% por 24h) criada em tempo de execução. A configuração de regras fica como evolução futura.

**Regra atual (hardcoded):**
- Tipo: Dryness
- Threshold: 30.0%
- Janela: 24 horas
- Criada via: `Rule.CreateDefaultDrynessRule()`

---

### 6.5 `FieldRuleState` - **INTEGRADO ao Field aggregate**

> **MUDANÇA (MVP):** A tabela `FieldRuleState` foi **REMOVIDA**. O estado incremental das regras agora está **dentro do aggregate Field**, especificamente na propriedade `LastTimeAboveDryThreshold`.

**Como funciona agora:**
- Estado da regra de seca é mantido em `Fields.LastTimeAboveDryThreshold`
- Alertas ativos são rastreados via coleção `Alerts` do aggregate
- Não há flag `DryAlertActive` separado - calculado dinamicamente via `_dryAlertActive` (campo privado do aggregate)

Lógica funcional (seca) permanece a mesma:
- se leitura >= threshold → atualiza `LastTimeAboveDryThreshold` e resolve alerta (se ativo)
- se leitura < threshold → se agora - `LastTimeAboveDryThreshold` >= janela → abre/mantém alerta

---

### 6.6 `ProcessingFailures` - **NÃO IMPLEMENTADO NO MVP**
- `ReadingId` (PK)
- `FieldId`
- `ProcessedAt`
- `Source` (http/mqtt)

Regra funcional:
- se `ReadingId` já existe → **ignorar** a leitura (não duplicar ponto nem alertas)

---

### 6.4 `Rules` (configurável)
**Objetivo:** regras de negócio sem recompilar.

Campos sugeridos:
- `RuleId` (PK)
- `RuleType` (Dryness / PestRisk)
- `IsEnabled`
- `Threshold` (ex.: 30.0)
- `WindowHours` (ex.: 24)
- `UpdatedAt`

> MVP pode começar com “regras globais” (uma por tipo). Assignment por cultura/talhão pode ser evolução.

---

### 6.5 `FieldRuleState` (estado incremental das regras)
**Objetivo:** calcular regras com eficiência sem varrer 24h inteiro sempre.

Campos sugeridos (para seca):
- `FieldId` (PK)
- `LastTimeAboveDryThreshold` (timestamp)
- `DryAlertActive` (bool)
- `UpdatedAt`

Lógica funcional (seca):
- se leitura >= threshold → atualiza `LastTimeAboveDryThreshold` e resolve alerta (se ativo)
- se leitura < threshold → se agora - `LastTimeAboveDryThreshold` >= janela → abre/ mantém alerta

---

### 6.6 `ProcessingFailures` (opcional, mas recomendado)
**Objetivo:** rastrear leituras que falharam (para debug e reprocess).

Campos sugeridos:
- `FailureId` (PK)
- `ReadingId` (nullable)
- `FieldId` (nullable)
- `Reason`
- `CreatedAt`

---

## 7) Arquitetura Hexagonal — Ports & Adapters (alto nível)

O núcleo (Domain + Application) **não conhece** Influx/Mongo/SQL/SQS/HTTP.
Ele conversa com o mundo externo via **ports**.

### 7.1 Ports (interfaces do núcleo)

#### Port de Time-series (leituras)
- `ITimeSeriesReadingsStore`
  - `AppendAsync(SensorReading)` - adicionar leitura
  - `GetByPeriodAsync(fieldId, from, to)` - consultar leituras brutas
  - `GetAggregatedAsync(fieldId, from, to, interval)` - consultar com agregação (Hour/Day)

**Implementação MVP:** `InMemoryTimeSeriesAdapter` (substituir por InfluxDB/MongoDB em produção)

---

#### Ports operacionais (SQL Server)

**`IFieldRepository` (Aggregate Repository)**
- `GetByIdAsync(fieldId)` - carrega Field aggregate completo com alertas
- `GetByFarmAsync(farmId)` - carrega todos Fields de uma fazenda
- `SaveAsync(field)` - persiste Field aggregate (Field + Alerts)

> **MUDANÇA:** `IFieldStatusStore` foi **REMOVIDO**. Agora usamos `IFieldRepository` que trabalha com o aggregate Field completo (DDD puro).

**`IAlertStore` (Query Store - apenas leitura)**
- `GetActiveByFarmAsync(farmId)` - alertas ativos por fazenda
- `GetActiveByFieldAsync(fieldId)` - alertas ativos por talhão
- `GetByFieldAsync(fieldId, from, to)` - histórico por talhão
- `GetByFarmAsync(farmId, from, to)` - histórico por fazenda
- `GetByIdAsync(alertId)` - alerta específico

> **NOTA:** Alertas são **criados/resolvidos** via Field aggregate, não diretamente pelo store. O `IAlertStore` é usado apenas para **queries de leitura**.

**`IIdempotencyStore`**
- `ExistsAsync(readingId)` - verifica se foi processado
- `MarkProcessedAsync(processedReading)` - marca como processado

---

#### Ports REMOVIDOS no MVP

- ~~`IRuleStore`~~ - regras agora são hardcoded (evolução futura)
- ~~`IFieldRuleStateStore`~~ - estado integrado ao Field aggregate
- ~~`IFieldCatalog`~~ - validação de fieldId não implementada (assumimos válido)

---

### 7.2 Inbound Adapters (entradas)
- **SQS Consumer Adapter**
  - recebe mensagem `TelemetryReceived`
  - chama o use case `ProcessTelemetryReading`

- **HTTP API Adapter**
  - expõe endpoints de leitura para dashboard/integração:
    - overview da fazenda
    - status do talhão
    - histórico do talhão
    - alertas ativos e histórico

---

### 7.3 Outbound Adapters (saídas)

**Time-series:**
- `InMemoryTimeSeriesAdapter` → `ITimeSeriesReadingsStore` (MVP)
  - Usar `InfluxReadingsAdapter` ou `MongoReadingsAdapter` em produção

**SQL Server:**
- `FieldRepository` → `IFieldRepository` (persiste aggregate Field via EF Core)
- `SqlServerAlertAdapter` → `IAlertStore` (queries de leitura de alertas)
- `SqlServerIdempotencyAdapter` → `IIdempotencyStore`

**Adapters REMOVIDOS:**
- ~~`SqlServerStatusAdapter`~~ - substituído por `FieldRepository`
- ~~`SqlServerRulesAdapter`~~ - regras hardcoded no MVP
- ~~`SqlServerRuleStateAdapter`~~ - estado no aggregate Field

---

## 8) Fluxos que devem funcionar no MVP

### 8.1 Fluxo: processar leitura (worker)
Ao receber uma leitura (mensagem do SQS via `TelemetryReceivedMessage`):

1. **Validação**: verifica campos obrigatórios e ranges válidos
2. **Idempotência**: se `readingId` já processado → retorna `Skipped` e finaliza
3. **Time-series**: grava ponto no histórico via `ITimeSeriesReadingsStore.AppendAsync()`
4. **Aggregate**: 
   - Carrega ou cria Field aggregate via `IFieldRepository.GetByIdAsync()`
   - Executa `field.ProcessReading(reading, drynessRule)`
     - Atualiza estado interno (LastSoilMoisture, LastTemperature, LastRain, etc.)
     - Atualiza `LastTimeAboveDryThreshold`
     - Avalia regra de seca
     - Cria/resolve alertas conforme necessário
     - Atualiza status e razão
5. **Persistência**: salva aggregate completo via `IFieldRepository.SaveAsync(field)`
   - EF Core persiste Field (status + estado) + Alerts em transação atômica
6. **Idempotência**: marca `readingId` como processado via `IIdempotencyStore.MarkProcessedAsync()`
**Endpoint:** `GET /api/farms/{farmId}/overview`

**Query:** `GetFarmOverviewQuery`
- Carrega todos Fields da fazenda via `IFieldRepository.GetByFarmAsync(farmId)`
- Para cada Field, retorna:
  - FieldId, FarmId
  - Status atual (Normal/DryAlert)
  - StatusReason
  - Última leitura (timestamp + valores de SoilMoisture, Temperature, Rain)
  - Contagem de alertas ativos (via `fi)
**Endpoint:** `GET /api/fields/{fieldId}/history?from={from}&to={to}&aggregation={none|hour|day}`

**Query:** `GetFieldHistoryQuery`
- Se `aggregation=none`: chama `ITimeSeriesReadingsStore.GetByPeriodAsync()` → retorna leituras brutas
- Se `aggregation=hour|day`: chama `ITimeSeriesReadingsStore.GetAggregatedAsync()` → retorna agregações (média/min/max)

**Fonte de dados:** Time-series DB (InMemory no MVP, InfluxDB em produção)
**Endpoints:**
- `GET /api/farms/{farmId}/alerts` - alertas ativos da fazenda
- `GET /api/fields/{fieldId}/alerts` - alertas ativos do talhão

**Query:** `GetActiveAlertsQuery`
- `ExecuteByFarmAsync(farmId)` → `IAlertStore.GetActiveByFarmAsync()`
- `ExecuteByFieldAsync(fieldId)` → `IAlertStore.GetActiveByFieldAsync()`

**Fonte de dados:** SQL Server (`Alerts` table filtrado por `Status = Active`)

**DTO retornado:** `AlertDto[]` com informações completas (AlertType, Reason, StartedAt, etc.
- `ReadingAggregationDto[]` (agregações)
**DTO retornado:** `FarmOverviewDto` contendo lista de `FieldOverviewDto`

---

### 8.2 Fluxo: dashboard overview (API)
- retorna lista de talhões com:
  - status atual
  - última leitura
  - contagem de alertas ativos

Fonte de dados: **SQL Server** (`FieldStatusCurrent` + `Alerts`)

---

### 8.3 Fluxo: histórico do talhão (API ou AMG direto)
- query do período e agregação (hora/dia)

Fonte de dados: **Time-series DB**

---
Status de Implementação MVP

### ✅ Implementado (MVP Completo)
- [x] Persistir leitura no time-series (InMemory - trocar por InfluxDB)
- [x] Idempotência por `readingId` no SQL Server (`ProcessedReadings`)
- [x] Field aggregate com status e estado de regra (`Fields` table)
- [x] Criar/Resolver alertas de seca via aggregate
- [x] Consultas para: overview / detalhe / alertas ativos
- [x] Consulta de histórico por período (com agregação Hour/Day)
- [x] Worker SQS consumindo mensagens `TelemetryReceived`
- [x] API REST completa com 5 controllers (Farms, Fields, Alerts, Health, Simulation)
- [x] EF Core com migrations (SQL Server + InMemory para testes)
- [x] 45 testes (24 domínio + 3 aplicação + 18 integração)

### 🔄 Simplificações do MVP
- [x] Regras hardcoded (`Rule.CreateDefaultDrynessRule()`) - configuração fica para depois
- [x] Time-series InMemory - substituir por InfluxDB/MongoDB em produção
- [x] Sem tabelas `FieldStatusCurrent`, `FieldRuleState`, `Rules` - arquitetura simplificada com aggregate puro
- [x] Sem registro de falhas (`ProcessingFailures`) - adicionar se necessário

### 📋 Próximos Passos (Evolução)
- [ ] Trocar InMemory por InfluxDB para time-series
- [ ] Implementar regras configuráveis (tabela `Rules` + UI)
- [ ] Adicionar flag "Sem dados recentes" (NoRecentData status)
- [ ] Implementar `ProcessingFailures` para debug/reprocessamento
- [ ] Regras de praga (PestRisk alerts)
- [ ] Autenticação/Autorização AWS Cognito (estrutura já existea de idempotência.

## 11) Decisões Técnicas do MVP (Implementadas)

1. **Time-series:** InMemory (MVP) → migrar para InfluxDB (produção)
2. **Retenção do histórico:** Indefinida no InMemory → configurar 30-90 dias no InfluxDB
3. **Agregação:** Hora e Dia implementados (enum `AggregationInterval`)
4. **Arquitetura:** DDD com aggregate Field (sem FieldStatus separado)
5. **Regras:** Hardcoded no MVP (`30% por 24h`)
6. **Messaging:** AWS SQS com long polling (20s wait time)
7. **Banco SQL:** EF Core 10 com SQL Server (InMemory para testes)

---

## 12) Contratos de Mensagem e API

### Contrato de Mensagem SQS: `TelemetryReceivedMessage`
```json
{
  "readingId": "string (único para idempotência)",
  "fieldId": "string",
  "farmId": "string",
  "timestamp": "ISO 8601 datetime",
  "soilMoisturePercent": "double (0-100)",
  "temperatureC": "double",
  "rainMm": "double (>=0)",
  "source": "Http | Mqtt"
}
```

### Endpoints de API Implementados

**Farms:**
- `GET /api/farms/{farmId}/overview` → `FarmOverviewDto`
- `GET /api/farms/{farmId}/alerts` → `AlertDto[]` (ativos)
- `GET /api/farms/{farmId}/alerts/history?from&to` → `AlertDto[]`

**Fields:**
- `GET /api/fields/{fieldId}` → `FieldDetailDto`
- `GET /api/fields/{fieldId}/history?from&to&aggregation` → `ReadingDto[]` ou `ReadingAggregationDto[]`
- `GET /api/fields/{fieldId}/alerts` → `AlertDto[]` (ativos)
- `GET /api/fields/{fieldId}/alerts/history?from&to` → `AlertDto[]`

**Alerts:**
- `GET /api/alerts/{alertId}` → `AlertDto`

**Simulation (debug):**
- `POST /api/simulation/telemetry` → `ProcessingResult` (simula recebimento via HTTP)

**Health:**
- `GET /api/health` → `{ status, timestamp }`
- `MODE=worker` → só worker

> Isso permite separar escala futuramente sem mudar código do núcleo.

---

## 10) Checklist para o dev (entregáveis mínimos)

### Must (MVP)
- [ ] Persistir leitura no time-series
- [ ] Idempotência por `readingId` no SQL Server
- [ ] Calcular/atualizar `FieldStatusCurrent`
- [ ] Criar/Resolver alertas (seca)
- [ ] Consultas para: overview / status / alertas ativos
- [ ] Consulta de histórico por período (com agregação opcional)

### Should (diferencial)
- [ ] Regras configuráveis (`Rules`)
- [ ] Estado incremental (`FieldRuleState`)
- [ ] Sem dados recentes (flag)
- [ ] Registro de falhas (`ProcessingFailures`)

---

## 11) Perguntas em aberto (decisões rápidas)
1. **Time-series escolhido:** InfluxDB ou MongoDB time-series?
2. **Retenção do histórico:** 30/90/180 dias no MVP?
3. **Agregação para gráficos:** por hora é suficiente?
4. **Ack de alertas:** vai existir no MVP ou fica como extra?

---

Se este documento estiver ok, o próximo passo é produzir:
- um “contrato de mensagem” `TelemetryReceived` (campos e versão)
- um “contrato de consulta” (quais endpoints e parâmetros) no mesmo nível alto
- e um diagrama de sequência do fluxo de processamento (para o relatório).
