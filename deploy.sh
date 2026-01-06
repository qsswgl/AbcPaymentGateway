#!/bin/bash

# 农行支付网关部署脚本
# 用途：自动部署到腾讯云服务器

set -e

# 配置
SERVER="api.qsgl.net"
SSH_KEY="K:/Key/tx.qsgl.net_id_ed25519"
SSH_USER="root"
REMOTE_DIR="/opt/payment"
PROJECT_NAME="AbcPaymentGateway"

echo "========================================="
echo "农行支付网关 - 自动部署脚本"
echo "========================================="
echo ""

# 检查本地构建
echo "步骤 1: 本地构建测试..."
dotnet build -c Release
if [ $? -eq 0 ]; then
    echo "✓ 本地构建成功"
else
    echo "✗ 本地构建失败"
    exit 1
fi

# 打包项目文件
echo ""
echo "步骤 2: 打包项目文件..."
tar -czf ${PROJECT_NAME}.tar.gz \
    --exclude='bin' \
    --exclude='obj' \
    --exclude='logs' \
    --exclude='.git' \
    --exclude='*.tar.gz' \
    .
echo "✓ 打包完成"

# 上传到服务器
echo ""
echo "步骤 3: 上传到服务器..."
ssh -i "${SSH_KEY}" ${SSH_USER}@${SERVER} "mkdir -p ${REMOTE_DIR}"
scp -i "${SSH_KEY}" ${PROJECT_NAME}.tar.gz ${SSH_USER}@${SERVER}:${REMOTE_DIR}/
echo "✓ 上传完成"

# 在服务器上解压和部署
echo ""
echo "步骤 4: 服务器端部署..."
ssh -i "${SSH_KEY}" ${SSH_USER}@${SERVER} << 'ENDSSH'
cd /opt/payment
echo "解压文件..."
tar -xzf AbcPaymentGateway.tar.gz
rm -f AbcPaymentGateway.tar.gz

echo "停止旧容器..."
docker-compose down || true

echo "构建新镜像..."
docker-compose build

echo "启动新容器..."
docker-compose up -d

echo "清理旧镜像..."
docker image prune -f

echo "等待服务启动..."
sleep 5

echo "检查服务状态..."
docker ps | grep payment

echo "测试健康检查..."
curl -f http://localhost:8080/api/payment/health || echo "警告: 健康检查失败"

ENDSSH

echo "✓ 服务器部署完成"

# 清理本地临时文件
echo ""
echo "步骤 5: 清理临时文件..."
rm -f ${PROJECT_NAME}.tar.gz
echo "✓ 清理完成"

echo ""
echo "========================================="
echo "部署完成！"
echo "========================================="
echo ""
echo "服务地址: https://payment.qsgl.net"
echo "健康检查: https://payment.qsgl.net/api/payment/health"
echo ""
echo "查看日志: ssh -i ${SSH_KEY} ${SSH_USER}@${SERVER} 'docker logs -f payment-gateway'"
echo ""
