FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
ENV DOTNET_PRINT_TELEMETRY_MESSAGE 'false'

WORKDIR /source
COPY Inkluzitron/*.csproj Inkluzitron/
RUN dotnet restore Inkluzitron/ -r linux-x64
COPY . .
RUN dotnet publish Inkluzitron/ -c release -o /app -r linux-x64 --self-contained false --no-restore

FROM mcr.microsoft.com/dotnet/runtime:5.0-buster-slim
RUN sed -i'.bak' 's/$/ contrib/' /etc/apt/sources.list
RUN apt-get update && apt-get -y install libgdiplus tzdata ttf-mscorefonts-installer fontconfig fonts-open-sans fonts-symbola && apt-get clean
ENV TZ=Europe/Prague
WORKDIR /app
COPY --from=build /app .
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone

ENTRYPOINT ["./Inkluzitron"]
