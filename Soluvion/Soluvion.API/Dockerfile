FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Másoljuk a projektfájlt és visszaállítjuk a függõségeket
COPY Soluvion.API.csproj ./
RUN dotnet restore

# Másoljuk az összes többi fájlt és buildeljük az appot
COPY . ./
RUN dotnet publish -c Release -o out

# Futtató környezet
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

RUN apt-get update && apt-get install -y libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/out .

# Railway általában a PORT környezeti változót használja
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Soluvion.API.dll"]