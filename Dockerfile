FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["EXE101-Mascoteach-Backend.sln", "./"]
COPY ["Mascoteach.API/Mascoteach.API.csproj", "Mascoteach.API/"]
COPY ["Mascoteach.Data/Mascoteach.Data.csproj", "Mascoteach.Data/"]
COPY ["Mascoteach.Service/Mascoteach.Service.csproj", "Mascoteach.Service/"]
RUN dotnet restore "EXE101-Mascoteach-Backend.sln"

COPY . .
WORKDIR "/src"
RUN dotnet build "EXE101-Mascoteach-Backend.sln" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EXE101-Mascoteach-Backend.sln" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Mascoteach.API.dll"]
