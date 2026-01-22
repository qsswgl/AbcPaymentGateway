# 使用 .NET 10 SDK 作为构建镜像
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 复制项目文件
COPY ["AbcPaymentGateway.csproj", "./"]

# 还原依赖
RUN dotnet restore "AbcPaymentGateway.csproj"

# 复制所有文件
COPY . .

# 发布应用（标准 JIT 编译）
FROM build AS publish
RUN dotnet publish "AbcPaymentGateway.csproj" \
    -c Release \
    -o /app/publish \
    /p:PublishAot=false

# 使用 ASP.NET Core runtime 镜像
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app

# 设置时区为中国
ENV TZ=Asia/Shanghai
RUN apk add --no-cache tzdata && \
    ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && \
    echo $TZ > /etc/timezone

# 配置 OpenSSL 以支持旧版 SSL 重新协商（兼容农行服务器）
# 参考: https://github.com/openssl/openssl/issues/17979
ENV OPENSSL_CONF=/etc/ssl/openssl-custom.cnf
RUN echo -e 'openssl_conf = openssl_init\n\
[openssl_init]\n\
ssl_conf = ssl_sect\n\
\n\
[ssl_sect]\n\
system_default = system_default_sect\n\
\n\
[system_default_sect]\n\
Options = UnsafeLegacyRenegotiation' > /etc/ssl/openssl-custom.cnf

# 复制发布的文件
COPY --from=publish /app/publish .

# 复制 Web 目录（Swagger 文档）
COPY Web/ /app/Web/

# 创建日志目录
RUN mkdir -p /app/logs

# 创建证书目录（实际证书文件将通过 volume 挂载）
RUN mkdir -p /app/cert/prod /app/cert/test

# 暴露端口 (使用 HTTP，由 Traefik 处理 HTTPS)
EXPOSE 8080

# 设置环境变量
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# 运行应用
ENTRYPOINT ["dotnet", "AbcPaymentGateway.dll"]
