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

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=public.ecr.aws/dynatrace/dynatrace-codemodules:1.329.67.20260112-133153-dotnet / /
ENV LD_PRELOAD=/opt/dynatrace/oneagent/agent/lib64/liboneagentproc.so
ENTRYPOINT ["dotnet", "Genesis.AI.Api.dll"]
