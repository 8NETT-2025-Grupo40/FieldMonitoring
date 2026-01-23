# Simulação de alertas (Swagger)

## Base

- Swagger local: https://localhost:5001/index.html
- Endpoint de simulação: POST /api/simulation/telemetry
- Validações:
  - GET /api/fields/{fieldId}
  - GET /api/fields/{fieldId}/alerts
  - GET /api/fields/{fieldId}/alerts/history

## Observações

- Envie as leituras em ordem cronológica por fieldId.
- O uso de timestamps passados permite simular janelas longas sem esperar horas reais.
- O timestamp deve incluir offset (ex.: 2026-01-17T12:00:00-03:00 ou 2026-01-17T15:00:00Z).
- Limite estrito: valor igual ao limiar e condição normal (não gera alerta).

## Seca (umidade do solo < 30% por 24h)

### Leitura 1 (início)
```json
{
  "readingId": "seca-1",
  "sensorId": "sensor-1",
  "fieldId": "field-swagger-seca",
  "farmId": "farm-swagger-1",
  "timestamp": "2026-01-16T11:00:00-03:00",
  "soilHumidity": 25.0,
  "soilTemperature": 25.0,
  "airTemperature": 25.0,
  "airHumidity": 50.0,
  "rainMm": 0.0,
  "source": "http"
}
```

### Leitura 2 (gera alerta)
```json
{
  "readingId": "seca-2",
  "sensorId": "sensor-1",
  "fieldId": "field-swagger-seca",
  "farmId": "farm-swagger-1",
  "timestamp": "2026-01-17T12:00:00-03:00",
  "soilHumidity": 20.0,
  "soilTemperature": 25.0,
  "airTemperature": 25.0,
  "airHumidity": 50.0,
  "rainMm": 0.0,
  "source": "http"
}
```

### Leitura 3 (resolve)
```json
{
  "readingId": "seca-3",
  "sensorId": "sensor-1",
  "fieldId": "field-swagger-seca",
  "farmId": "farm-swagger-1",
  "timestamp": "2026-01-17T12:01:00-03:00",
  "soilHumidity": 40.0,
  "soilTemperature": 25.0,
  "airTemperature": 25.0,
  "airHumidity": 50.0,
  "rainMm": 0.0,
  "source": "http"
}
```

## Calor extremo (ar > 40°C por 4h)

### Leitura 1 (início)
```json
{
  "readingId": "calor-1",
  "sensorId": "sensor-1",
  "fieldId": "field-swagger-calor-extremo",
  "farmId": "farm-swagger-1",
  "timestamp": "2026-01-17T07:00:00-03:00",
  "soilHumidity": 50.0,
  "soilTemperature": 25.0,
  "airTemperature": 42.0,
  "airHumidity": 50.0,
  "rainMm": 0.0,
  "source": "http"
}
```

### Leitura 2 (gera alerta)
```json
{
  "readingId": "calor-2",
  "sensorId": "sensor-1",
  "fieldId": "field-swagger-calor-extremo",
  "farmId": "farm-swagger-1",
  "timestamp": "2026-01-17T12:00:00-03:00",
  "soilHumidity": 50.0,
  "soilTemperature": 25.0,
  "airTemperature": 43.0,
  "airHumidity": 50.0,
  "rainMm": 0.0,
  "source": "http"
}
```

### Leitura 3 (resolve)
```json
{
  "readingId": "calor-3",
  "sensorId": "sensor-1",
  "fieldId": "field-swagger-calor-extremo",
  "farmId": "farm-swagger-1",
  "timestamp": "2026-01-17T12:01:00-03:00",
  "soilHumidity": 50.0,
  "soilTemperature": 25.0,
  "airTemperature": 38.0,
  "airHumidity": 50.0,
  "rainMm": 0.0,
  "source": "http"
}
```

## Geada (ar < 2°C por 2h)

### Leitura 1 (início)
```json
{
  "readingId": "geada-1",
  "sensorId": "sensor-1",
  "fieldId": "field-swagger-geada",
  "farmId": "farm-swagger-1",
  "timestamp": "2026-01-17T09:00:00-03:00",
  "soilHumidity": 50.0,
  "soilTemperature": 25.0,
  "airTemperature": 1.0,
  "airHumidity": 50.0,
  "rainMm": 0.0,
  "source": "http"
}
```

### Leitura 2 (gera alerta)
```json
{
  "readingId": "geada-2",
  "sensorId": "sensor-1",
  "fieldId": "field-swagger-geada",
  "farmId": "farm-swagger-1",
  "timestamp": "2026-01-17T12:00:00-03:00",
  "soilHumidity": 50.0,
  "soilTemperature": 25.0,
  "airTemperature": 0.5,
  "airHumidity": 50.0,
  "rainMm": 0.0,
  "source": "http"
}
```

### Leitura 3 (resolve)
```json
{
  "readingId": "geada-3",
  "sensorId": "sensor-1",
  "fieldId": "field-swagger-geada",
  "farmId": "farm-swagger-1",
  "timestamp": "2026-01-17T12:01:00-03:00",
  "soilHumidity": 50.0,
  "soilTemperature": 25.0,
  "airTemperature": 5.0,
  "airHumidity": 50.0,
  "rainMm": 0.0,
  "source": "http"
}
```

## Ar seco (ar < 20% por 6h)

### Leitura 1 (início)
```json
{
  "readingId": "ar-seco-1",
  "sensorId": "sensor-1",
  "fieldId": "field-swagger-ar-seco",
  "farmId": "farm-swagger-1",
  "timestamp": "2026-01-17T05:00:00-03:00",
  "soilHumidity": 50.0,
  "soilTemperature": 25.0,
  "airTemperature": 25.0,
  "airHumidity": 15.0,
  "rainMm": 0.0,
  "source": "http"
}
```

### Leitura 2 (gera alerta)
```json
{
  "readingId": "ar-seco-2",
  "sensorId": "sensor-1",
  "fieldId": "field-swagger-ar-seco",
  "farmId": "farm-swagger-1",
  "timestamp": "2026-01-17T12:00:00-03:00",
  "soilHumidity": 50.0,
  "soilTemperature": 25.0,
  "airTemperature": 25.0,
  "airHumidity": 18.0,
  "rainMm": 0.0,
  "source": "http"
}
```

### Leitura 3 (resolve)
```json
{
  "readingId": "ar-seco-3",
  "sensorId": "sensor-1",
  "fieldId": "field-swagger-ar-seco",
  "farmId": "farm-swagger-1",
  "timestamp": "2026-01-17T12:01:00-03:00",
  "soilHumidity": 50.0,
  "soilTemperature": 25.0,
  "airTemperature": 25.0,
  "airHumidity": 35.0,
  "rainMm": 0.0,
  "source": "http"
}
```

## Ar úmido (ar > 90% por 12h)

### Leitura 1 (início)
```json
{
  "readingId": "ar-umido-1",
  "sensorId": "sensor-1",
  "fieldId": "field-swagger-ar-umido",
  "farmId": "farm-swagger-1",
  "timestamp": "2026-01-16T23:00:00-03:00",
  "soilHumidity": 50.0,
  "soilTemperature": 25.0,
  "airTemperature": 25.0,
  "airHumidity": 92.0,
  "rainMm": 0.0,
  "source": "http"
}
```

### Leitura 2 (gera alerta)
```json
{
  "readingId": "ar-umido-2",
  "sensorId": "sensor-1",
  "fieldId": "field-swagger-ar-umido",
  "farmId": "farm-swagger-1",
  "timestamp": "2026-01-17T12:00:00-03:00",
  "soilHumidity": 50.0,
  "soilTemperature": 25.0,
  "airTemperature": 25.0,
  "airHumidity": 95.0,
  "rainMm": 0.0,
  "source": "http"
}
```

### Leitura 3 (resolve)
```json
{
  "readingId": "ar-umido-3",
  "sensorId": "sensor-1",
  "fieldId": "field-swagger-ar-umido",
  "farmId": "farm-swagger-1",
  "timestamp": "2026-01-17T12:01:00-03:00",
  "soilHumidity": 50.0,
  "soilTemperature": 25.0,
  "airTemperature": 25.0,
  "airHumidity": 70.0,
  "rainMm": 0.0,
  "source": "http"
}
```
