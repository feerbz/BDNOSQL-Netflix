FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Se copia todo antes de publicar: si .NET 10 restaura sin ver los .razor, falta blazor.web.js.
COPY . .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Netflix.App.dll"]
