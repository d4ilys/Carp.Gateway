#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Test/Test.csproj", "Test/"]
COPY ["Daily.Carp.Provider.Kubernetes/Daily.Carp.Provider.Kubernetes.csproj", "Daily.Carp.Provider.Kubernetes/"]
COPY ["Daily.Carp/Daily.Carp.csproj", "Daily.Carp/"]
RUN dotnet restore "Test/Test.csproj"
COPY . .
WORKDIR "/src/Test"
RUN dotnet build "Test.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Test.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Test.dll"]