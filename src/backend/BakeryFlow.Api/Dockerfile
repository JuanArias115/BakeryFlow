FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY BakeryFlow.sln ./
COPY BakeryFlow.Domain/BakeryFlow.Domain.csproj BakeryFlow.Domain/
COPY BakeryFlow.Application/BakeryFlow.Application.csproj BakeryFlow.Application/
COPY BakeryFlow.Infrastructure/BakeryFlow.Infrastructure.csproj BakeryFlow.Infrastructure/
COPY BakeryFlow.Api/BakeryFlow.Api.csproj BakeryFlow.Api/
RUN dotnet restore BakeryFlow.Api/BakeryFlow.Api.csproj

COPY . .
RUN dotnet publish BakeryFlow.Api/BakeryFlow.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8086
EXPOSE 8086

COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "BakeryFlow.Api.dll"]
