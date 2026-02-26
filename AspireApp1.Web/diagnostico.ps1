# Script de diagnóstico de telemetría para Aspire
# Ejecutar desde el directorio AspireApp1.Web

Write-Host "`n🔍 === DIAGNÓSTICO DE TELEMETRÍA ASPIRE ===" -ForegroundColor Cyan
Write-Host ""

# 1. Verificar archivo .env
Write-Host "1️⃣  VERIFICANDO ARCHIVO .ENV..." -ForegroundColor Yellow
if (Test-Path ".env") {
    Write-Host "   ✅ Archivo .env encontrado" -ForegroundColor Green

    $envContent = Get-Content ".env" -Raw

    if ($envContent -match "VITE_OTEL_EXPORTER_OTLP_ENDPOINT=(.+)") {
        $endpoint = $matches[1].Trim()
        Write-Host "   ✅ VITE_OTEL_EXPORTER_OTLP_ENDPOINT: $endpoint" -ForegroundColor Green
    } else {
        Write-Host "   ❌ VITE_OTEL_EXPORTER_OTLP_ENDPOINT no encontrado" -ForegroundColor Red
    }

    if ($envContent -match "VITE_OTEL_SERVICE_NAME=(.+)") {
        $serviceName = $matches[1].Trim()
        Write-Host "   ✅ VITE_OTEL_SERVICE_NAME: $serviceName" -ForegroundColor Green
    } else {
        Write-Host "   ⚠️  VITE_OTEL_SERVICE_NAME no encontrado" -ForegroundColor Yellow
    }
} else {
    Write-Host "   ❌ Archivo .env NO encontrado" -ForegroundColor Red
    Write-Host "   → Crea el archivo .env con las variables necesarias" -ForegroundColor Yellow
}

Write-Host ""

# 2. Verificar conectividad al endpoint OTLP
Write-Host "2️⃣  VERIFICANDO CONECTIVIDAD AL ENDPOINT OTLP..." -ForegroundColor Yellow

if ($endpoint) {
    # Extraer host y puerto del endpoint
    if ($endpoint -match "http://([^:]+):(\d+)") {
        $host = $matches[1]
        $port = $matches[2]

        try {
            $tcpClient = New-Object System.Net.Sockets.TcpClient
            $tcpClient.Connect($host, $port)
            $tcpClient.Close()
            Write-Host "   ✅ Puerto $port está accesible en $host" -ForegroundColor Green
        } catch {
            Write-Host "   ❌ No se puede conectar a $host`:$port" -ForegroundColor Red
            Write-Host "   → El Dashboard de Aspire NO está ejecutándose" -ForegroundColor Yellow
            Write-Host "   → O el puerto es incorrecto" -ForegroundColor Yellow
        }
    }
} else {
    Write-Host "   ⚠️  No se pudo verificar (endpoint no configurado)" -ForegroundColor Yellow
}

Write-Host ""

# 3. Verificar si hay procesos de .NET ejecutándose
Write-Host "3️⃣  VERIFICANDO ASPIRE APPHOST..." -ForegroundColor Yellow

$dotnetProcesses = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue
if ($dotnetProcesses) {
    Write-Host "   ✅ Procesos .NET encontrados: $($dotnetProcesses.Count)" -ForegroundColor Green
    Write-Host "   → Es probable que el AppHost esté ejecutándose" -ForegroundColor Green
} else {
    Write-Host "   ❌ No hay procesos .NET ejecutándose" -ForegroundColor Red
    Write-Host "   → Ejecuta el AppHost: cd ../AspireApp1.AppHost && dotnet run" -ForegroundColor Yellow
}

Write-Host ""

# 4. Verificar node_modules
Write-Host "4️⃣  VERIFICANDO DEPENDENCIAS..." -ForegroundColor Yellow

if (Test-Path "node_modules") {
    Write-Host "   ✅ node_modules encontrado" -ForegroundColor Green
} else {
    Write-Host "   ❌ node_modules NO encontrado" -ForegroundColor Red
    Write-Host "   → Ejecuta: pnpm install" -ForegroundColor Yellow
}

Write-Host ""

# 5. Verificar si el servidor de desarrollo está ejecutándose
Write-Host "5️⃣  VERIFICANDO SERVIDOR DE DESARROLLO..." -ForegroundColor Yellow

$viteProcess = Get-Process -Name "node" -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowTitle -like "*vite*" }
$portInUse = Get-NetTCPConnection -LocalPort 5173 -ErrorAction SilentlyContinue

if ($portInUse) {
    Write-Host "   ✅ Puerto 5173 en uso (servidor ejecutándose)" -ForegroundColor Green
} else {
    Write-Host "   ❌ Puerto 5173 NO está en uso" -ForegroundColor Red
    Write-Host "   → Ejecuta: pnpm dev" -ForegroundColor Yellow
}

Write-Host ""

# 6. Resumen y próximos pasos
Write-Host "6️⃣  RESUMEN Y PRÓXIMOS PASOS:" -ForegroundColor Yellow
Write-Host ""
Write-Host "   Para continuar el diagnóstico:" -ForegroundColor White
Write-Host "   1. Asegúrate de que el servidor esté ejecutándose: pnpm dev" -ForegroundColor White
Write-Host "   2. Abre http://localhost:5173 en el navegador" -ForegroundColor White
Write-Host "   3. Abre DevTools (F12) → Console" -ForegroundColor White
Write-Host "   4. Verifica que veas: '✅ OpenTelemetry inicializado correctamente'" -ForegroundColor White
Write-Host "   5. Ve a Network tab y filtra: 'traces'" -ForegroundColor White
Write-Host "   6. Haz click en un botón de la app" -ForegroundColor White
Write-Host "   7. Deberías ver: POST http://localhost:4318/v1/traces [200]" -ForegroundColor White
Write-Host ""
Write-Host "   📚 Guía completa: NO_LLEGAN_TRAZAS.md" -ForegroundColor Cyan
Write-Host "   🧪 Test visual: http://localhost:5173/test-env.html" -ForegroundColor Cyan
Write-Host ""

Write-Host "=" -NoNewline -ForegroundColor Cyan
Write-Host ("=" * 59) -ForegroundColor Cyan
Write-Host ""

