FROM node:22-alpine AS frontend
WORKDIR /frontend
COPY frontend/package*.json ./
RUN npm ci --no-audit --no-fund
COPY frontend/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/NinaERP.API/NinaERP.API.csproj", "NinaERP.API/"]
COPY ["src/NinaERP.Application/NinaERP.Application.csproj", "NinaERP.Application/"]
COPY ["src/NinaERP.Infrastructure/NinaERP.Infrastructure.csproj", "NinaERP.Infrastructure/"]
COPY ["src/NinaERP.Domain/NinaERP.Domain.csproj", "NinaERP.Domain/"]
COPY ["src/NinaERP.Contracts/NinaERP.Contracts.csproj", "NinaERP.Contracts/"]
RUN dotnet restore "NinaERP.API/NinaERP.API.csproj"
COPY src/ ./
COPY --from=frontend /frontend/dist /src/NinaERP.API/wwwroot
WORKDIR "/src/NinaERP.API"
RUN dotnet publish -c Release --no-restore -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production \
    Kestrel__Endpoints__Http__Url=http://0.0.0.0:8080 \
    Database__ApplyMigrationsOnStartup=false \
    Seed__DemoData=false
COPY --from=build /app/publish .
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "NinaERP.API.dll"]
