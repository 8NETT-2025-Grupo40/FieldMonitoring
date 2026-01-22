# FieldMonitoring

## Regras de alerta (limite estrito)

- Seca (umidade do solo < 30% por 24 horas)
- Calor extremo (temperatura do ar > 40C por 4 horas)
- Geada (temperatura do ar < 2C por 2 horas)
- Ar seco (umidade do ar < 20% por 6 horas)
- Ar umido (umidade do ar > 90% por 12 horas)
- Limite estrito: valor igual ao limiar e condicao normal (nao gera alerta)

## Simular alertas via Swagger

- Swagger local: https://localhost:5001/index.html
- Endpoint de simulacao: POST /api/simulation/telemetry
- Validacoes:
  - GET /api/fields/{fieldId}
  - GET /api/fields/{fieldId}/alerts
  - GET /api/fields/{fieldId}/alerts/history
- Payloads detalhados: [docs/alert-simulation.md](docs/alert-simulation.md)

### Modelo do corpo da requisicao

```json
{
  "readingId": "read-unique-1",
  "sensorId": "sensor-1",
  "fieldId": "field-sim-1",
  "farmId": "farm-1",
  "timestamp": "2026-01-17T12:00:00Z",
  "soilHumidity": 50.0,
  "soilTemperature": 25.0,
  "airTemperature": 25.0,
  "airHumidity": 50.0,
  "rainMm": 0.0,
  "source": "http"
}
```

### Como disparar um alerta

1. Envie uma leitura com timestamp no passado (agora - janela - 1h) e valor em condicao de alerta.
2. Envie uma segunda leitura com timestamp atual, mantendo a condicao de alerta.
3. Consulte GET /api/fields/{fieldId}/alerts.

### Como resolver um alerta

Envie uma leitura no mesmo fieldId com valor em condicao normal (>= ou <= limiar, conforme a regra) e timestamp atual.

### Valores de referencia por regra

Use as leituras abaixo em dois timestamps separados pela janela indicada:

- Seca: soilHumidity 20 (limiar 30), janela 24h
- Calor extremo: airTemperature 42 (limiar 40), janela 4h
- Geada: airTemperature 1 (limiar 2), janela 2h
- Ar seco: airHumidity 15 (limiar 20), janela 6h
- Ar umido: airHumidity 95 (limiar 90), janela 12h

Recomendacao: envie as leituras em ordem cronologica para evitar efeitos de reset do ultimo timestamp valido.

## Alertas no InfluxDB (MVP)

- Eventos de alerta sao projetados na measurement `field_alerts` (abertura e resolucao).
- Spec detalhada: [docs/influx-alerts-spec.md](docs/influx-alerts-spec.md)
