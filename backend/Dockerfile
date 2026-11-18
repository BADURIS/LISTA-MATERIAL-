# API LMaterial (.NET 9) empacotada para rodar como container na Vercel (ou em qualquer host Docker).
# A Vercel informa a porta em $PORT; fora dela a API escuta na 8080.

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restaura dependências primeiro para aproveitar o cache das camadas
COPY LMaterial.sln ./
COPY LMaterial.API/LMaterial.API.csproj LMaterial.API/
COPY LMaterial.Application/LMaterial.Application.csproj LMaterial.Application/
COPY LMaterial.Domain/LMaterial.Domain.csproj LMaterial.Domain/
COPY LMaterial.Infrastructure/LMaterial.Infrastructure.csproj LMaterial.Infrastructure/
RUN dotnet restore LMaterial.API/LMaterial.API.csproj

COPY LMaterial.API/ LMaterial.API/
COPY LMaterial.Application/ LMaterial.Application/
COPY LMaterial.Domain/ LMaterial.Domain/
COPY LMaterial.Infrastructure/ LMaterial.Infrastructure/
RUN dotnet publish LMaterial.API/LMaterial.API.csproj -c Release -o /app --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Fonte para os relatórios em PDF (PDFsharp/MigraDoc procura Liberation Sans no Linux)
RUN apt-get update \
    && apt-get install -y --no-install-recommends fonts-liberation \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app ./

ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

# A Vercel define PORT; o HTTPS é feito por ela, então a API escuta só em HTTP
CMD ["sh", "-c", "ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080} exec dotnet LMaterial.API.dll"]
