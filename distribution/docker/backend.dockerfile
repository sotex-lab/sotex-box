FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine3.18 AS build
COPY dotnet .
RUN dotnet publish backend -o /backend

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine3.18
WORKDIR /backend
COPY --from=build /backend .
# 👇 set to use the non-root USER here
USER $APP_UID
ENTRYPOINT ["./backend"]
