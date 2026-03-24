# create the build instance
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build

ARG TARGETPLATFORM
ARG BUILDPLATFORM

WORKDIR /src

# copy solution, props and project files first for restore caching
COPY ./src/NopCommerce.sln ./src/Directory.Build.props ./
COPY ./src/Libraries/Nop.Core/Nop.Core.csproj Libraries/Nop.Core/
COPY ./src/Libraries/Nop.Data/Nop.Data.csproj Libraries/Nop.Data/
COPY ./src/Libraries/Nop.Services/Nop.Services.csproj Libraries/Nop.Services/
COPY ./src/Presentation/Nop.Web/Nop.Web.csproj Presentation/Nop.Web/
COPY ./src/Presentation/Nop.Web.Framework/Nop.Web.Framework.csproj Presentation/Nop.Web.Framework/
COPY ./src/Tests/Nop.Tests/Nop.Tests.csproj Tests/Nop.Tests/
COPY ./src/Plugins/ ./tmp-plugins/
RUN find ./tmp-plugins -name '*.csproj' | while read f; do \
      dir="Plugins/$(basename "$(dirname "$f")")"; \
      mkdir -p "$dir"; cp "$f" "$dir/"; \
    done && rm -rf ./tmp-plugins

# restore (cached unless .csproj or .sln changes)
RUN dotnet restore NopCommerce.sln

# copy all source and publish (builds only Nop.Web dependency chain)
COPY ./src ./
WORKDIR /src/Presentation/Nop.Web
RUN dotnet publish Nop.Web.csproj --no-restore -c Release -o /app/published

WORKDIR /app/published

RUN mkdir -p logs bin Plugins wwwroot/bundles wwwroot/db_backups \
             wwwroot/files/exportimport wwwroot/icons wwwroot/images/thumbs \
             wwwroot/images/uploaded wwwroot/sitemaps App_Data/DataProtectionKeys \
    && chmod 775 App_Data App_Data/DataProtectionKeys bin logs Plugins \
                 wwwroot/bundles wwwroot/db_backups wwwroot/files/exportimport \
                 wwwroot/icons wwwroot/images wwwroot/images/thumbs \
                 wwwroot/images/uploaded wwwroot/sitemaps

# create the runtime instance
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime

# add globalization support
RUN apk add --no-cache icu-libs icu-data-full
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# installs required packages
RUN apk add tiff --no-cache --repository http://dl-3.alpinelinux.org/alpine/edge/main/ --allow-untrusted
RUN apk add libgdiplus --no-cache --repository http://dl-3.alpinelinux.org/alpine/edge/community/ --allow-untrusted
RUN apk add libc-dev tzdata gcompat --no-cache

WORKDIR /app

COPY --from=build /app/published .

ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

ENTRYPOINT ["dotnet", "Nop.Web.dll"]
