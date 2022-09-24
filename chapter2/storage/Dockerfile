#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["storage/storage.csproj", "storage/"]
COPY ["common/common.csproj", "common/"]
RUN dotnet restore "storage/storage.csproj"
COPY . .
WORKDIR "/src/storage"
RUN dotnet build "storage.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "storage.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "storage.dll"]