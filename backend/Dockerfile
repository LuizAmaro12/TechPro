FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/TechPro.Api/TechPro.Api.csproj src/TechPro.Api/
RUN dotnet restore src/TechPro.Api/TechPro.Api.csproj
COPY src/ src/
RUN dotnet publish src/TechPro.Api/TechPro.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "TechPro.Api.dll"]
