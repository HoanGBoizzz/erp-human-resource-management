FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY QLNS_BE/QLNS_BE.csproj QLNS_BE/
RUN dotnet restore QLNS_BE/QLNS_BE.csproj
COPY . .
WORKDIR /src/QLNS_BE
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/wwwroot/uploads
# Render.com inject PORT env var; fallback 8080 cho local/VPS
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}
EXPOSE 8080
ENTRYPOINT ["dotnet", "QLNS_BE.dll"]
