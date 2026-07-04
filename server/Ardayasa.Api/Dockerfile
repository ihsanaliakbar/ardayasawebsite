FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Ardayasa.Domain/Ardayasa.Domain.csproj Ardayasa.Domain/
COPY Ardayasa.Application/Ardayasa.Application.csproj Ardayasa.Application/
COPY Ardayasa.Infrastructure/Ardayasa.Infrastructure.csproj Ardayasa.Infrastructure/
COPY Ardayasa.Api/Ardayasa.Api.csproj Ardayasa.Api/
RUN dotnet restore Ardayasa.Api/Ardayasa.Api.csproj

COPY . .
RUN dotnet publish Ardayasa.Api/Ardayasa.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/storage
EXPOSE 8080
ENTRYPOINT ["dotnet", "Ardayasa.Api.dll"]
