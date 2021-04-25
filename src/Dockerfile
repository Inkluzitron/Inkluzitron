FROM mcr.microsoft.com/dotnet/sdk:5.0-alpine AS build

ENV DOTNET_PRINT_TELEMETRY_MESSAGE 'false'
WORKDIR /source
COPY Inkluzitron/*.csproj .
RUN dotnet restore -r linux-musl-x64
COPY Inkluzitron/* ./
RUN dotnet publish -c release -o /app -r linux-musl-x64 --self-contained false --no-restore

FROM mcr.microsoft.com/dotnet/runtime:5.0-alpine-amd64
WORKDIR /app
# Temporary hack (Rewrite after PR #12 finished.)
RUN mkdir Assets
COPY --from=build /source/*.gif Assets/
COPY --from=build /app .

ENTRYPOINT ["./Inkluzitron"]
