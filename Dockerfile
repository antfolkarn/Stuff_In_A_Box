# syntax=docker/dockerfile:1

# 1) Build the React SPA (Vite outputs to ../wwwroot -> /wwwroot)
FROM node:20-alpine AS spa
WORKDIR /spa
COPY src/StuffInABox.Web/ClientApp/package*.json ./
RUN npm ci
COPY src/StuffInABox.Web/ClientApp/ ./
RUN npm run build

# 2) Publish the .NET app
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY StuffInABox.slnx ./
COPY src/ ./src/
# Bring in the built SPA so it's included in the publish output
COPY --from=spa /wwwroot ./src/StuffInABox.Web/wwwroot
RUN dotnet publish src/StuffInABox.Web/StuffInABox.Web.csproj -c Release -o /app

# 3) Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "StuffInABox.Web.dll"]
