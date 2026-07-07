#!/bin/bash
set -e

MONGO_PORT=27017
API_PORT=5123
WEB_PORT=7000

echo "=== Verificando MongoDB ==="
if ! command -v mongod &> /dev/null; then
    echo "MongoDB no encontrado, instalando..."
    sudo apt-get update -qq
    sudo apt-get install -y gnupg curl
    curl -fsSL https://www.mongodb.org/static/pgp/server-7.0.asc | sudo gpg --yes -o /usr/share/keyrings/mongodb-server-7.0.gpg --dearmor
    echo "deb [ arch=amd64,arm64 signed-by=/usr/share/keyrings/mongodb-server-7.0.gpg ] https://repo.mongodb.org/apt/ubuntu jammy/mongodb-org/7.0 multiverse" | sudo tee /etc/apt/sources.list.d/mongodb-org-7.0.list
    sudo apt-get update -qq && sudo apt-get install -y mongodb-org
    echo "=== MongoDB instalado ==="
else
    echo "MongoDB ya está instalado, salteando instalación."
fi

echo "=== Liberando puertos ocupados ==="
fuser -k ${MONGO_PORT}/tcp 2>/dev/null || true
fuser -k ${API_PORT}/tcp 2>/dev/null || true
fuser -k ${WEB_PORT}/tcp 2>/dev/null || true
fuser -k 5000/tcp 2>/dev/null || true
fuser -k 5099/tcp 2>/dev/null || true
pkill -f "mongod --dbpath" 2>/dev/null || true
pkill -f "dotnet.*TupApi" 2>/dev/null || true
pkill -f "dotnet.*TUPMundial.Web" 2>/dev/null || true
sleep 1

echo "=== Iniciando MongoDB (en su propia sesión, inmune a Ctrl+C) ==="
sudo mkdir -p /data/db && sudo chown -R $USER /data/db
setsid mongod --dbpath /data/db > /tmp/mongod.log 2>&1 < /dev/null &
disown

echo "Esperando a que MongoDB responda..."
for i in {1..20}; do
    if mongosh --quiet --eval "db.runCommand({ping:1})" &> /dev/null; then
        echo "MongoDB listo."
        break
    fi
    sleep 1
done

echo "=== Iniciando API en puerto $API_PORT (en su propia sesión) ==="
cd /workspaces/mundial2026Web/APi_Mundial2026/TupApi
setsid env ASPNETCORE_URLS="http://0.0.0.0:$API_PORT" dotnet run --no-launch-profile > /tmp/api.log 2>&1 < /dev/null &
disown

echo "Esperando a que la API responda en puerto $API_PORT..."
for i in {1..30}; do
    if curl -s -o /dev/null -w "%{http_code}" "http://localhost:$API_PORT/api/partido" | grep -qE "200|404"; then
        echo "API lista."
        break
    fi
    sleep 1
done

if ! curl -s -o /dev/null "http://localhost:$API_PORT/api/partido"; then
    echo "!!! La API no respondió a tiempo. Revisá /tmp/api.log para más detalle."
    tail -n 30 /tmp/api.log
fi

echo "=== Verificando partidos cargados ==="
TOTAL=$(curl -s "http://localhost:$API_PORT/api/partido" | grep -o '"numeroPartido"' | wc -l)
echo "Partidos actuales en base de datos: $TOTAL"

if [ "$TOTAL" -eq 0 ]; then
    echo "=== Cargando 104 partidos ==="
    curl -s -X POST "http://localhost:$API_PORT/api/partido/seed/reset" \
        -H "Content-Type: application/json" \
        -d @/workspaces/mundial2026Web/APi_Mundial2026/TupApi/seed_mundial_104.json
    echo ""
    echo "=== Partidos cargados ==="
else
    echo "=== Ya hay $TOTAL partidos en la base de datos ==="
fi

echo "=== Iniciando MVC en puerto $WEB_PORT ==="
echo "(Este proceso queda en primer plano. Podés cortarlo con Ctrl+C sin afectar a Mongo ni a la API.)"
cd /workspaces/mundial2026Web/TUPMundial.Web
ASPNETCORE_URLS="http://0.0.0.0:$WEB_PORT" dotnet run --no-launch-profile
