# FieldMonitoring

## Regras de alerta (limite estrito)

- Seca (umidade do solo < 30% por 24 horas)
- Calor extremo (temperatura do ar > 40°C por 4 horas)
- Geada (temperatura do ar < 2°C por 2 horas)
- Ar seco (umidade do ar < 20% por 6 horas)
- Ar úmido (umidade do ar > 90% por 12 horas)
- Limite estrito: valor igual ao limiar e condição normal (não gera alerta)

## Simular alertas via Swagger

- Swagger local: https://localhost:5001/index.html
- Endpoint de simulação: POST /api/simulation/telemetry
- Validações:
  - GET /api/fields/{fieldId}
  - GET /api/fields/{fieldId}/alerts
  - GET /api/fields/{fieldId}/alerts/history
- Payloads detalhados: [docs/alert-simulation.md](docs/alert-simulation.md)

### Modelo do corpo da requisição

```json
{
  "readingId": "read-unique-1",
  "sensorId": "sensor-1",
  "fieldId": "field-sim-1",
  "farmId": "farm-1",
  "timestamp": "2026-01-17T12:00:00-03:00",
  "soilHumidity": 50.0,
  "soilTemperature": 25.0,
  "airTemperature": 25.0,
  "airHumidity": 50.0,
  "rainMm": 0.0,
  "source": "http"
}
```

Observação: o `timestamp` deve incluir offset (ex.: `2026-01-17T12:00:00-03:00` ou `2026-01-17T15:00:00Z`).

### Como disparar um alerta

1. Envie uma leitura com timestamp no passado (agora - janela - 1h) e valor em condição de alerta.
2. Envie uma segunda leitura com timestamp atual, mantendo a condição de alerta.
3. Consulte GET /api/fields/{fieldId}/alerts.

### Como resolver um alerta

Envie uma leitura no mesmo fieldId com valor em condição normal (>= ou <= limiar, conforme a regra) e timestamp atual.

### Valores de referência por regra

Use as leituras abaixo em dois timestamps separados pela janela indicada:

- Seca: soilHumidity 20 (limiar 30), janela 24h
- Calor extremo: airTemperature 42 (limiar 40), janela 4h
- Geada: airTemperature 1 (limiar 2), janela 2h
- Ar seco: airHumidity 15 (limiar 20), janela 6h
- Ar úmido: airHumidity 95 (limiar 90), janela 12h

Recomendação: envie as leituras em ordem cronológica para evitar efeitos de reset do último timestamp válido.

## Alertas no InfluxDB (MVP)

- Eventos de alerta são projetados na measurement `field_alerts` (abertura e resolução), incluindo campo numérico `active`.
- Especificação detalhada: [docs/influx-alerts-spec.md](docs/influx-alerts-spec.md)
