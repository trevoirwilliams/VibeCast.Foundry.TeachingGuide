FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore VibeCast.sln
RUN dotnet publish src/VibeCast.Web/VibeCast.Web.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
RUN addgroup --system vibecast && adduser --system --ingroup vibecast vibecast
COPY --from=build /app/publish .
RUN mkdir -p /app/.vibecast && chown -R vibecast:vibecast /app
USER vibecast
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "VibeCast.Web.dll"]
