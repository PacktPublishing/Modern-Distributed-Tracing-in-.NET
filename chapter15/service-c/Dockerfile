FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["service-c/service-c.csproj", "service-c/"]
RUN dotnet restore "service-c/service-c.csproj"
COPY . .
WORKDIR "/src/service-c"
RUN dotnet build "service-c.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "service-c.csproj" -c Release -o /app/publish /p:UseAppHost=false 

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "service-c.dll"]