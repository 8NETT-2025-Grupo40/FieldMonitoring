# Simulacao de alertas (Swagger)

## Base

- Swagger local: https://localhost:5001/index.html
- Endpoint de simulacao: POST /api/simulation/telemetry
- Validacoes:
  - GET /api/fields/{fieldId}
  - GET /api/fields/{fieldId}/alerts
  - GET /api/fields/{fieldId}/alerts/history

## Observacoes

- Envie as leituras em ordem cronologica por fieldId.
- O uso de timestamps passados permite simular janelas longas sem esperar horas reais.
- Limite estrito: valor igual ao limiar e condicao normal (nao gera alerta).

## Seca (umidade do solo < 30% por 24h)

### Leitura 1 (inicio)
```json
{
  "readingId": "seca-1",
  "sensorId": "sensor-1",
  "fieldId": "field-swagger-seca",
  "farmId": "farm-swagger-1",
  "timestamp": "2026-01-16T11:00:00Z",
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
  "timestamp": "2026-01-17T12:00:00Z",
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
  "timestamp": "2026-01-17T12:01:00Z",
  "soilHumidity": 40.0,
  "soilTemperature": 25.0,
  "airTemperature": 25.0,
  "airHumidity": 50.0,
  "rainMm": 0.0,
  "source": "http"
}
```

## Calor extremo (ar > 40C por 4h)

### Leitura 1 (inicio)
```json
{
  "readingId": "calor-1",
  "sensorId": "sensor-1",
  "fieldId": "field-swagger-calor-extremo",
  "farmId": "farm-swagger-1",
  "timestamp": "2026-01-17T07:00:00Z",
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
  "timestamp": "2026-01-17T12:00:00Z",
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
  "timestamp": "2026-01-17T12:01:00Z",
  "soilHumidity": 50.0,
  "soilTemperature": 25.0,
  "airTemperature": 38.0,
  "airHumidity": 50.0,
  "rainMm": 0.0,
  "source": "http"
}
```

## Geada (ar < 2C por 2h)

### Leitura 1 (inicio)
```json
{
  "readingId": "geada-1",
  "sensorId": "sensor-1",
  "fieldId": "field-swagger-geada",
  "farmId": "farm-swagger-1",
  "timestamp": "2026-01-17T09:00:00Z",
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
  "timestamp": "2026-01-17T12:00:00Z",
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
  "timestamp": "2026-01-17T12:01:00Z",
  "soilHumidity": 50.0,
  "soilTemperature": 25.0,
  "airTemperature": 5.0,
  "airHumidity": 50.0,
  "rainMm": 0.0,
  "source": "http"
}
```

## Ar seco (ar < 20% por 6h)

### Leitura 1 (inicio)
```json
{
  "readingId": "ar-seco-1",
  "sensorId": "sensor-1",
  "fieldId": "field-swagger-ar-seco",
  "farmId": "farm-swagger-1",
  "timestamp": "2026-01-17T05:00:00Z",
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
  "timestamp": "2026-01-17T12:00:00Z",
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
  "timestamp": "2026-01-17T12:01:00Z",
  "soilHumidity": 50.0,
  "soilTemperature": 25.0,
  "airTemperature": 25.0,
  "airHumidity": 35.0,
  "rainMm": 0.0,
  "source": "http"
}
```

## Ar umido (ar > 90% por 12h)

### Leitura 1 (inicio)
```json
{
  "readingId": "ar-umido-1",
  "sensorId": "sensor-1",
  "fieldId": "field-swagger-ar-umido",
  "farmId": "farm-swagger-1",
  "timestamp": "2026-01-16T23:00:00Z",
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
  "timestamp": "2026-01-17T12:00:00Z",
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
  "timestamp": "2026-01-17T12:01:00Z",
  "soilHumidity": 50.0,
  "soilTemperature": 25.0,
  "airTemperature": 25.0,
  "airHumidity": 70.0,
  "rainMm": 0.0,
  "source": "http"
}
```
