FROM centraluk.jfrog.io/glb-docker-vir/dotnet/aspnet:10.0.7-noble AS base
WORKDIR /app
EXPOSE 8080
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 libkrb5-3 \
    && rm -rf /var/lib/apt/lists/*

FROM centraluk.jfrog.io/glb-docker-vir/dotnet/sdk:10.0.203-noble AS build
WORKDIR /src

COPY ["nuget.config", "."]
COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
COPY ["src/Directory.Build.props", "src/"]
COPY ["src/Genesis.AI.Api/Genesis.AI.Api.csproj", "src/Genesis.AI.Api/"]
COPY ["src/Genesis.AI.Core/Genesis.AI.Core.csproj", "src/Genesis.AI.Core/"]
COPY ["src/Genesis.AI.Domain/Genesis.AI.Domain.csproj", "src/Genesis.AI.Domain/"]
COPY ["src/Genesis.AI.Infrastructure/Genesis.AI.Infrastructure.csproj", "src/Genesis.AI.Infrastructure/"]

RUN --mount=type=secret,id=JF_USER \
    --mount=type=secret,id=JF_TOKEN \
    --mount=type=secret,id=GIT_TOKEN \
    export JFROG_USER=$(cat /run/secrets/JF_USER) && \
    export JFROG_TOKEN=$(cat /run/secrets/JF_TOKEN) && \
    export GIT_TOKEN=$(cat /run/secrets/GIT_TOKEN) && \
    dotnet restore "src/Genesis.AI.Api/Genesis.AI.Api.csproj" --configfile nuget.config

COPY ["src/", "src/"]

RUN dotnet build "src/Genesis.AI.Api/Genesis.AI.Api.csproj" -c Release --no-restore

FROM build AS publish
RUN dotnet publish "src/Genesis.AI.Api/Genesis.AI.Api.csproj" -c Release -o /app/publish --no-build

# Generate the OpenAPI spec for APIM. The build workflow extracts it via
# artifact-paths and raises a PR into emisgroup/apim. The Swashbuckle CLI boots
# the published app to read its Swagger document, so:
#   * the CLI version MUST match the app's Swashbuckle.AspNetCore version (9.0.1)
#     or the Swagger assembly fails to load;
#   * it runs from the project source directory so appsettings.json (with its
#     DefaultConnection) is the content root and the host builds without a
#     database (the DbContext is lazy, so nothing is contacted);
#   * placeholder auth config stops JWT bootstrap from failing the host build;
#   * DOTNET_ROLL_FORWARD lets the .NET 9 CLI run on the .NET 10 runtime.
# Written under the publish output so it lands at
# /app/docs/openapi/specification.json in the final image.
RUN --mount=type=secret,id=JF_USER \
    --mount=type=secret,id=JF_TOKEN \
    export JFROG_USER=$(cat /run/secrets/JF_USER) && \
    export JFROG_TOKEN=$(cat /run/secrets/JF_TOKEN) && \
    export Authentication__Authority="https://placeholder.example.com/v2.0/" && \
    export Authentication__Audience="placeholder-audience" && \
    export DOTNET_ROLL_FORWARD=Major && \
    dotnet tool install --global Swashbuckle.AspNetCore.Cli --version 9.0.1 --configfile nuget.config && \
    mkdir -p /app/publish/docs/openapi && \
    cd src/Genesis.AI.Api && \
    /root/.dotnet/tools/swagger tofile \
    --output /app/publish/docs/openapi/specification.json \
    /app/publish/Genesis.AI.Api.dll v1

FROM base AS final
WORKDIR /app

# Network tools for troubleshooting
USER root
RUN apt-get update && apt-get install -y --no-install-recommends \
    curl \
    netcat-openbsd \
    net-tools \
    iputils-ping \
    dnsutils \
    postgresql-client \
    jq \
    && rm -rf /var/lib/apt/lists/*


COPY --from=publish /app/publish .
COPY --from=public.ecr.aws/dynatrace/dynatrace-codemodules:1.329.67.20260112-133153-dotnet / /
ENV LD_PRELOAD=/opt/dynatrace/oneagent/agent/lib64/liboneagentproc.so

USER app
ENTRYPOINT ["dotnet", "Genesis.AI.Api.dll"]
