#!/bin/bash
#
# ABC Payment Gateway 快速恢复脚本
# 用途: 快速恢复ABC支付网关服务
# 使用: bash restore-abc-payment.sh
#

set -e

echo "========================================="
echo "  ABC Payment Gateway 恢复脚本"
echo "========================================="
echo ""

# 配置变量
CONTAINER_NAME="abc-payment-gateway"
IMAGE_NAME="abc-payment-gateway:stable-with-openssl-fix"
BACKUP_TAR="/root/backups/abc-payment-gateway-stable-openssl-fix.tar"
APP_DIR="/opt/abc-payment"
CERT_DIR="/opt/abc-payment/cert"
OPENSSL_CONF="/opt/abc-payment/openssl_custom.conf"

# 检查备份镜像是否存在
echo "📦 检查Docker镜像..."
if docker images | grep -q "abc-payment-gateway.*stable-with-openssl-fix"; then
    echo "✅ 镜像已存在"
else
    echo "⚠️  镜像不存在，尝试导入..."
    if [ -f "$BACKUP_TAR" ]; then
        docker load -i "$BACKUP_TAR"
        echo "✅ 镜像导入成功"
    else
        echo "❌ 备份文件不存在: $BACKUP_TAR"
        exit 1
    fi
fi

# 检查必要的目录和文件
echo ""
echo "📁 检查必要的文件..."
if [ ! -d "$APP_DIR" ]; then
    echo "❌ 应用目录不存在: $APP_DIR"
    exit 1
fi

if [ ! -d "$CERT_DIR" ]; then
    echo "❌ 证书目录不存在: $CERT_DIR"
    exit 1
fi

if [ ! -f "$OPENSSL_CONF" ]; then
    echo "⚠️  OpenSSL配置不存在，创建..."
    cat > "$OPENSSL_CONF" << 'EOF'
openssl_conf = openssl_init

[openssl_init]
ssl_conf = ssl_sect

[ssl_sect]
system_default = system_default_sect

[system_default_sect]
Options = UnsafeLegacyRenegotiation
MinProtocol = TLSv1
MaxProtocol = TLSv1.2
EOF
    echo "✅ OpenSSL配置已创建"
fi

# 停止并删除旧容器
echo ""
echo "🛑 停止旧容器..."
if docker ps -a | grep -q "$CONTAINER_NAME"; then
    docker stop "$CONTAINER_NAME" 2>/dev/null || true
    docker rm "$CONTAINER_NAME" 2>/dev/null || true
    echo "✅ 旧容器已删除"
else
    echo "ℹ️  没有旧容器"
fi

# 启动新容器
echo ""
echo "🚀 启动新容器..."
docker run -d --name "$CONTAINER_NAME" \
  --network traefik-net \
  --restart unless-stopped \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ASPNETCORE_URLS=http://+:5000 \
  -e ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
  -e OPENSSL_CONF=/opt/app/openssl_custom.conf \
  -v "$APP_DIR":/opt/app \
  -w /opt/app \
  -l "traefik.enable=true" \
  -l "traefik.http.routers.abc-payment.rule=Host(\`payment.qsgl.net\`)" \
  -l "traefik.http.routers.abc-payment.entrypoints=websecure" \
  -l "traefik.http.routers.abc-payment.tls.certresolver=letsencrypt" \
  -l "traefik.http.services.abc-payment.loadbalancer.server.port=5000" \
  "$IMAGE_NAME" \
  dotnet AbcPaymentGateway.dll

echo "✅ 容器启动成功"

# 等待服务启动
echo ""
echo "⏳ 等待服务启动..."
sleep 5

# 检查容器状态
echo ""
echo "🔍 检查容器状态..."
if docker ps | grep -q "$CONTAINER_NAME"; then
    echo "✅ 容器运行中"
    docker ps | grep "$CONTAINER_NAME"
else
    echo "❌ 容器未运行"
    echo ""
    echo "查看日志:"
    docker logs "$CONTAINER_NAME" --tail 50
    exit 1
fi

# 查看最新日志
echo ""
echo "📋 最新日志 (最后20行):"
echo "----------------------------------------"
docker logs "$CONTAINER_NAME" --tail 20
echo "----------------------------------------"

echo ""
echo "========================================="
echo "  ✅ 恢复完成！"
echo "========================================="
echo ""
echo "服务信息:"
echo "  - 容器名称: $CONTAINER_NAME"
echo "  - 镜像: $IMAGE_NAME"
echo "  - 域名: https://payment.qsgl.net"
echo ""
echo "验证命令:"
echo '  curl -X POST https://payment.qsgl.net/api/payment/abc/pagepay \'
echo '    -H "Content-Type: application/json" \'
echo '    -d '"'"'{"merchantId":"103881636900016","amount":1.00,"orderNo":"TEST12345","orderDesc":"测试","notifyUrl":"https://payment.qsgl.net/notify","merchantSuccessUrl":"https://payment.qsgl.net/success","merchantErrorUrl":"https://payment.qsgl.net/error"}'"'"
echo ""
echo "查看日志:"
echo "  docker logs $CONTAINER_NAME -f"
echo ""
