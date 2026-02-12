# ===============================
# Stage 1: Runtime
# ===============================
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base

# Suporte a globalização (formatação de moeda, datas)
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Segurança: roda como usuário não-root que já existe na imagem
USER app
WORKDIR /app

# Kestrel ouvindo em todas as interfaces na porta 5066
ENV ASPNETCORE_URLS=http://+:5066
# Documenta no Docker que o container expõe a porta 5066
EXPOSE 5066

# ===============================
# Stage 2: Build/Publish
# ===============================
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
ARG BUILD_CONFIGURATION=Release

WORKDIR /src

# Copia a solução e os .csproj para aproveitar o cache do Docker
COPY ["src/FieldMonitoring.Api/FieldMonitoring.Api.csproj", "src/FieldMonitoring.Api/"]
COPY ["src/FieldMonitoring.Application/FieldMonitoring.Application.csproj", "src/FieldMonitoring.Application/"]
COPY ["src/FieldMonitoring.Domain/FieldMonitoring.Domain.csproj", "src/FieldMonitoring.Domain/"]
COPY ["src/FieldMonitoring.Infrastructure/FieldMonitoring.Infrastructure.csproj", "src/FieldMonitoring.Infrastructure/"]

# Restaura dependências do projeto de API (puxa o grafo inteiro)
RUN dotnet restore "src/FieldMonitoring.Api/FieldMonitoring.Api.csproj"

# Copia o restante do código
COPY . .

# Publica a API (Release) para uma pasta única
WORKDIR "/src/src/FieldMonitoring.Api"
RUN dotnet publish "FieldMonitoring.Api.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false

# ===============================
# Stage 3: Final (imagem enxuta)
# ===============================
FROM base AS final
WORKDIR /app

# Copia artefatos publicados
COPY --from=build /app/publish .

# Entry point
ENTRYPOINT ["dotnet", "FieldMonitoring.Api.dll"]
