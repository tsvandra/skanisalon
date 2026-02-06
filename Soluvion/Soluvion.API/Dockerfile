FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Másoljuk a projektfájlt és visszaállítjuk a függõségeket
COPY ["Soluvion.API/Soluvion.API.csproj", "Soluvion.API/"]

RUN dotnet restore "Soluvion.API/Soluvion.API.csproj"

# Másoljuk az összes többi fájlt és buildeljük az appot
COPY . .

# Build és Publish
WORKDIR "/src/Soluvion.API"
RUN dotnet publish "Soluvion.API.csproj" -c Release -o /app/publish

# Futtató környezet
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update && apt-get install -y libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Railway általában a PORT környezeti változót használja
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Soluvion.API.dll"]