FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY DotNet8WebAPI.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish DotNet8WebAPI.csproj -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out ./
ENTRYPOINT ["dotnet", "DotNet8WebAPI.dll"]
