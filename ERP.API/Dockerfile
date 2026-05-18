FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["ERP.API/ERP.API.csproj", "ERP.API/"]
COPY ["ERP.Core/ERP.Core.csproj", "ERP.Core/"]
COPY ["ERP.Infrastructure/ERP.Infrastructure.csproj", "ERP.Infrastructure/"]
COPY ["ERP.Shared/ERP.Shared.csproj", "ERP.Shared/"]

RUN dotnet restore "ERP.API/ERP.API.csproj"

COPY . .
WORKDIR /src/ERP.API
RUN dotnet publish "ERP.API.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 80
ENTRYPOINT ["dotnet", "ERP.API.dll"]
