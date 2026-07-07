#!/bin/bash
echo "=== Instalando MongoDB ==="
sudo apt-get update -qq
sudo apt-get install -y gnupg curl
curl -fsSL https://www.mongodb.org/static/pgp/server-7.0.asc | sudo gpg -o /usr/share/keyrings/mongodb-server-7.0.gpg --dearmor
echo "deb [ arch=amd64,arm64 signed-by=/usr/share/keyrings/mongodb-server-7.0.gpg ] https://repo.mongodb.org/apt/ubuntu jammy/mongodb-org/7.0 multiverse" | sudo tee /etc/apt/sources.list.d/mongodb-org-7.0.list
sudo apt-get update -qq && sudo apt-get install -y mongodb-org
echo "=== Iniciando MongoDB ==="
sudo mkdir -p /data/db && sudo chown -R codespace /data/db
mongod --dbpath /data/db > /tmp/mongod.log 2>&1 &
sleep 4
echo "=== Iniciando API ==="
cd /workspaces/mundial2026Web/APi_Mundial2026/TupApi
dotnet run > /tmp/api.log 2>&1 &
sleep 10
echo "=== Verificando partidos ==="
TOTAL=0
if [ "" = "0" ]; then
    echo "=== Cargando 104 partidos ==="
    curl -s -X POST http://localhost:5123/api/partido/seed/reset -H "Content-Type: application/json" -d @/workspaces/mundial2026Web/APi_Mundial2026/TupApi/seed_mundial_104.json
    echo ""
fi
echo "===  partidos en base de datos ==="
echo "=== Iniciando MVC en puerto 8080 ==="
cd /workspaces/mundial2026Web/TUPMundial.Web
ASPNETCORE_URLS="http://0.0.0.0:8080" dotnet run
