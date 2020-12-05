#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0-buster-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
WORKDIR /src
COPY ["JobsServer/JobsServer.csproj", "JobsServer/"]
COPY ["Hangfire.HttpJob/Hangfire.HttpJob.csproj", "Hangfire.HttpJob/"]
COPY ["Hangfire.Dashboard.BasicAuthorization/Hangfire.Dashboard.BasicAuthorization.csproj", "Hangfire.Dashboard.BasicAuthorization/"]
RUN dotnet restore "JobsServer/JobsServer.csproj"
COPY . .
WORKDIR "/src/JobsServer"
RUN dotnet build "JobsServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "JobsServer.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "JobsServer.dll"]