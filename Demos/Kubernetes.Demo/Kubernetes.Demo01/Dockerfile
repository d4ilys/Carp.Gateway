#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Demos/Kubernetes.Demo/Kubernetes.Demo01/Kubernetes.Demo01.csproj", "Demos/Kubernetes.Demo/Kubernetes.Demo01/"]
COPY ["Providers/Daily.Carp.Provider.Kubernetes/Daily.Carp.Provider.Kubernetes.csproj", "Providers/Daily.Carp.Provider.Kubernetes/"]
COPY ["Daily.Carp/Daily.Carp.csproj", "Daily.Carp/"]
RUN dotnet restore "Demos/Kubernetes.Demo/Kubernetes.Demo01/Kubernetes.Demo01.csproj"
COPY . .
WORKDIR "/src/Demos/Kubernetes.Demo/Kubernetes.Demo01"
RUN dotnet build "Kubernetes.Demo01.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Kubernetes.Demo01.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Kubernetes.Demo01.dll"]