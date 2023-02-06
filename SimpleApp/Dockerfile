FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 5097

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["SimpleApp/SimpleApp.csproj", "SimpleApp/"]
RUN dotnet restore "SimpleApp/SimpleApp.csproj"
COPY . .
WORKDIR "/src/SimpleApp"
RUN dotnet build "SimpleApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SimpleApp.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SimpleApp.dll"]
