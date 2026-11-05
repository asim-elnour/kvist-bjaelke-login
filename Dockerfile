# 1. Build the React page
FROM oven/bun:1 AS client
WORKDIR /client
COPY client/package.json client/bun.lock ./
RUN bun install --frozen-lockfile
COPY client/ .
RUN bun run build

# 2. Build the C# server
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY server/Security/Security.csproj Security/
COPY server/DataAccess/DataAccess.csproj DataAccess/
RUN dotnet restore Security/Security.csproj
COPY server/ .
RUN dotnet publish Security/Security.csproj -c Release -o /out --no-restore

# 3. The container that runs: the server, with the built page in wwwroot
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /out ./
COPY --from=client /client/dist ./wwwroot
RUN mkdir -p /app/data && chown app:app /app/data
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_HTTP_PORTS=8000
USER app
EXPOSE 8000
ENTRYPOINT ["dotnet", "Security.dll"]