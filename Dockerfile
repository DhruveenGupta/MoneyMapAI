FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first so a source-only change doesn't bust the restore cache layer.
COPY src/MoneyMap.Domain/MoneyMap.Domain.csproj src/MoneyMap.Domain/
COPY src/MoneyMap.Application/MoneyMap.Application.csproj src/MoneyMap.Application/
COPY src/MoneyMap.Persistence/MoneyMap.Persistence.csproj src/MoneyMap.Persistence/
COPY src/MoneyMap.Ai/MoneyMap.Ai.csproj src/MoneyMap.Ai/
COPY src/MoneyMap.Web/MoneyMap.Web.csproj src/MoneyMap.Web/
RUN dotnet restore src/MoneyMap.Web/MoneyMap.Web.csproj

COPY src/ src/
RUN dotnet publish src/MoneyMap.Web/MoneyMap.Web.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "MoneyMap.Web.dll"]
