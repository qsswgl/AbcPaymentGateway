# 使用 .NET 10 SDK 作为构建镜像（支持 Native AOT）
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 安装 Native AOT 所需的工具
RUN apt-get update && apt-get install -y \
    clang \
    zlib1g-dev \
    && rm -rf /var/lib/apt/lists/*

# 复制项目文件
COPY ["AbcPaymentGateway.csproj", "./"]

# 还原依赖
RUN dotnet restore "AbcPaymentGateway.csproj"

# 复制所有文件
COPY . .

# 使用 Native AOT 发布应用
FROM build AS publish
RUN dotnet publish "AbcPaymentGateway.csproj" \
    -c Release \
    -o /app/publish \
    /p:PublishAot=true \
    /p:StripSymbols=true

# 使用更小的基础镜像运行 AOT 应用
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-alpine AS final
WORKDIR /app

# 安装运行时依赖 (包括 glibc 兼容库)
RUN apk add --no-cache \
    libgcc \
    libstdc++ \
    icu-libs \
    libc6-compat

# 设置时区为中国
ENV TZ=Asia/Shanghai
RUN apk add --no-cache tzdata && \
    ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && \
    echo $TZ > /etc/timezone

# 复制发布的 Native AOT 可执行文件
COPY --from=publish /app/publish .

# 复制 Web 目录（Swagger 文档）
COPY Web/ /app/Web/

# 创建日志目录
RUN mkdir -p /app/logs

# 创建证书目录
RUN mkdir -p /app/cert/prod /app/cert/test

# 暴露端口 (使用 HTTP，由 Traefik 处理 HTTPS)
EXPOSE 8080

# 设置环境变量
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# 运行 Native AOT 编译的可执行文件
ENTRYPOINT ["./AbcPaymentGateway"]
