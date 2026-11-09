FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY ["CoffeeNChill.Gateway.csproj", "./"]

RUN dotnet restore "CoffeeNChill.Gateway.csproj"

COPY . .

RUN dotnet publish "CoffeeNChill.Gateway.csproj" \
    -c Release \
    -o /app/publish


FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=5000

EXPOSE 5000

ENTRYPOINT ["dotnet", "CoffeeNChill.Gateway.dll"]
