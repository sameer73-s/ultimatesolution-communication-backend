FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY UltimateSolution.Communication.slnx ./
COPY global.json ./
COPY src/UltimateSolution.Domain/ src/UltimateSolution.Domain/
COPY src/UltimateSolution.Application/ src/UltimateSolution.Application/
COPY src/UltimateSolution.Infrastructure/ src/UltimateSolution.Infrastructure/
COPY src/UltimateSolution.API/ src/UltimateSolution.API/
RUN dotnet publish src/UltimateSolution.API/UltimateSolution.API.csproj -c Release -o /app/publish --no-self-contained

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "UltimateSolution.API.dll"]
