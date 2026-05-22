#!/usr/bin/env bash
set -euo pipefail

GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m'

log()  { echo -e "${GREEN}[OK]${NC} $1"; }
warn() { echo -e "${YELLOW}[..] $1${NC}"; }
fail() { echo -e "${RED}[FAIL]${NC} $1"; exit 1; }

COMMAND="${1:-start}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PID_FILE="$SCRIPT_DIR/.grandnode-dev.pid"

# ── helpers ──────────────────────────────────────────────────────────────────

start_mongodb() {
    warn "Checking Docker..."
    docker info > /dev/null 2>&1 || fail "Docker Desktop is not running. Start it from the taskbar and retry."
    log "Docker is running"

    warn "Starting MongoDB container..."
    if docker ps -a --format '{{.Names}}' | grep -q '^mongodb$'; then
        docker start mongodb > /dev/null
        log "MongoDB container started (existing)"
    else
        docker run -d -p 127.0.0.1:27017:27017 --name mongodb mongo > /dev/null
        log "MongoDB container created and started"
    fi
}

stop_dev_process() {
    if [ -f "$PID_FILE" ]; then
        local pid
        pid=$(cat "$PID_FILE")
        if kill -0 "$pid" 2>/dev/null; then
            warn "Stopping local dev server (pid $pid)..."
            kill "$pid" 2>/dev/null || true
            sleep 2
            kill -9 "$pid" 2>/dev/null || true
            log "Dev server stopped"
        fi
        rm -f "$PID_FILE"
    fi
}

# ── commands ─────────────────────────────────────────────────────────────────

cmd_start() {
    start_mongodb

    warn "Starting GrandNode2 container..."
    if docker ps -a --format '{{.Names}}' | grep -q '^grandnode2$'; then
        docker start grandnode2 > /dev/null
        log "GrandNode2 container started (existing)"
    else
        docker run -d -p 80:8080 --name grandnode2 --link mongodb:mongo \
            -v grandnode_images:/app/wwwroot/assets/images \
            -v grandnode_appdata:/app/App_Data \
            grandnode/grandnode2 > /dev/null
        log "GrandNode2 container created and started"
    fi

    warn "Waiting for GrandNode2 to be ready..."
    for i in $(seq 1 20); do
        if curl -s -o /dev/null -w "%{http_code}" http://localhost | grep -qE '^[23]'; then
            log "GrandNode2 is up at http://localhost"
            break
        fi
        [ "$i" -eq 20 ] && fail "GrandNode2 did not respond after 20s. Run: docker logs grandnode2"
        sleep 1
    done

    echo ""
    echo -e "${GREEN}All services running:${NC}"
    echo "  Storefront : http://localhost"
    echo "  Admin      : http://localhost/admin  (admin@yourstore.com / Admin1234!)"
    echo "  MongoDB    : localhost:27017"
}

cmd_restart() {
    start_mongodb

    # Stop the Docker grandnode2 container so it frees port 80
    if docker ps --format '{{.Names}}' | grep -q '^grandnode2$'; then
        warn "Stopping grandnode2 Docker container..."
        docker stop grandnode2 > /dev/null
        log "Docker container stopped (MongoDB still running)"
    fi

    # Stop any previously started dev process
    stop_dev_process

    # Ensure Settings.cfg points at localhost MongoDB
    local settings="$SCRIPT_DIR/src/Web/Grand.Web/App_Data/Settings.cfg"
    if [ ! -f "$settings" ]; then
        warn "Writing Settings.cfg for local MongoDB..."
        cat > "$settings" <<'EOF'
{
  "ConnectionString": "mongodb://localhost:27017/grandnode2",
  "DbProvider": 0
}
EOF
        log "Settings.cfg created"
    fi

    warn "Building from source..."
    dotnet build "$SCRIPT_DIR/src/Web/Grand.Web" --configuration Debug --nologo -v q \
        || fail "Build failed. Fix errors above and retry."
    log "Build succeeded"

    warn "Starting local dev server on http://localhost ..."
    dotnet run --project "$SCRIPT_DIR/src/Web/Grand.Web" \
        --no-build \
        --urls "http://localhost:80" \
        > "$SCRIPT_DIR/.grandnode-dev.log" 2>&1 &
    echo $! > "$PID_FILE"
    log "Dev server starting (pid $(cat "$PID_FILE")) — logs: .grandnode-dev.log"

    warn "Waiting for app to be ready..."
    for i in $(seq 1 30); do
        if curl -s -o /dev/null -w "%{http_code}" http://localhost | grep -qE '^[23]'; then
            log "App is up at http://localhost"
            break
        fi
        [ "$i" -eq 30 ] && fail "App did not respond after 30s. Check .grandnode-dev.log"
        sleep 1
    done

    echo ""
    echo -e "${GREEN}Running from source:${NC}"
    echo "  Storefront : http://localhost"
    echo "  Admin      : http://localhost/admin"
    echo "  MongoDB    : localhost:27017 (grandnode2 db)"
    echo "  Logs       : tail -f $SCRIPT_DIR/.grandnode-dev.log"
    echo "  Stop       : ./grandnode-startup.sh stop"
}

cmd_stop() {
    stop_dev_process

    if docker ps --format '{{.Names}}' | grep -q '^grandnode2$'; then
        warn "Stopping grandnode2 Docker container..."
        docker stop grandnode2 > /dev/null
        log "grandnode2 container stopped"
    fi

    if docker ps --format '{{.Names}}' | grep -q '^mongodb$'; then
        warn "Stopping MongoDB container..."
        docker stop mongodb > /dev/null
        log "MongoDB container stopped"
    fi

    log "All stopped"
}

# ── dispatch ──────────────────────────────────────────────────────────────────

case "$COMMAND" in
    start)   cmd_start   ;;
    restart) cmd_restart ;;
    stop)    cmd_stop    ;;
    *)
        echo "Usage: $0 [start|restart|stop]"
        echo ""
        echo "  start    Start MongoDB + GrandNode2 Docker container (default)"
        echo "  restart  Stop Docker app, rebuild from source, run on http://localhost"
        echo "  stop     Stop everything (Docker containers + dev server)"
        exit 1
        ;;
esac
