# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

# copy csproj and restore as distinct layers
#COPY / /src

# run the builder
WORKDIR /src/build/builder
#RUN dotnet run --docker true
ENTRYPOINT ["dotnet", "run"]