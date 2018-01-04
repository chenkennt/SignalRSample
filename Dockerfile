FROM microsoft/aspnetcore-build:2.0 AS build-env
WORKDIR /app

# copy csproj and restore as distinct layers
COPY lib/ ./lib/
COPY nuget.config ./
RUN mkdir ChatDemo && cd ChatDemo/
COPY ChatDemo/*.csproj ./
RUN dotnet restore

# copy everything else and build
COPY ChatDemo/ ./
RUN dotnet publish -c Release -o out

# build runtime image
FROM microsoft/aspnetcore:2.0
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "ChatDemo.dll"]