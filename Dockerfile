FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS publish
COPY ./src/ ./src/
RUN dotnet publish "/src/BirdsiteLive/BirdsiteLive.csproj" -c Release -o /app/publish
RUN dotnet publish "/src/BSLManager/BSLManager.csproj" -r linux-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BirdsiteLive.dll"]
