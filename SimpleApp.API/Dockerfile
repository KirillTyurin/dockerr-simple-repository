﻿FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 5277

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["SimpleApp.API/SimpleApp.API.csproj", "SimpleApp.API/"]
RUN dotnet restore "SimpleApp.API/SimpleApp.API.csproj"
COPY . .
WORKDIR "/src/SimpleApp.API"
RUN dotnet build "SimpleApp.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SimpleApp.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SimpleApp.API.dll"]
