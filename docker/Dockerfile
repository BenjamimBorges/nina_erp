FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/NinaERP.API/NinaERP.API.csproj", "NinaERP.API/"]
COPY ["src/NinaERP.Application/NinaERP.Application.csproj", "NinaERP.Application/"]
COPY ["src/NinaERP.Infrastructure/NinaERP.Infrastructure.csproj", "NinaERP.Infrastructure/"]
COPY ["src/NinaERP.Domain/NinaERP.Domain.csproj", "NinaERP.Domain/"]
COPY ["src/NinaERP.Contracts/NinaERP.Contracts.csproj", "NinaERP.Contracts/"]
RUN dotnet restore "NinaERP.API/NinaERP.API.csproj"
COPY src/ .
WORKDIR "/src/NinaERP.API"
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "NinaERP.API.dll"]
