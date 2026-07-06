#!/bin/bash
echo "=== Iniciando MongoDB ==="
sudo mkdir -p /data/db && sudo chown -R $USER /data/db
mongod --dbpath /data/db > /tmp/mongod.log 2>&1 &
sleep 4

echo "=== Iniciando API ==="
cd /workspaces/mundial2026Web/APi_Mundial2026/TupApi
dotnet run > /tmp/api.log 2>&1 &
sleep 8

echo "=== Verificando partidos ==="
TOTAL=104
if [ "" = "0" ] || [ -z "" ]; then
    echo "=== Cargando 104 partidos ==="
    curl -s -X POST http://localhost:5123/api/partido/seed/reset       -H "Content-Type: application/json"       -d @/workspaces/mundial2026Web/APi_Mundial2026/TupApi/seed_mundial_104.json
    echo ""
    echo "=== Partidos cargados ==="
else
    echo "=== Ya hay  partidos en la base de datos ==="
fi

echo "=== Iniciando MVC ==="
cd /workspaces/mundial2026Web/TUPMundial.Web
dotnet run
