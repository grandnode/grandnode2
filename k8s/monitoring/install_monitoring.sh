#!/bin/bash

# ------------------------------------------------------------------------------
# 1. Install Helm (if not already installed)
# ------------------------------------------------------------------------------
if ! command -v helm &> /dev/null
then
    echo "Helm could not be found. Installing Helm..."
    curl https://raw.githubusercontent.com/helm/helm/main/scripts/get-helm-3 | bash
else
    echo "Helm is already installed."
fi

# ------------------------------------------------------------------------------
# 2. Add Helm Repositories
# ------------------------------------------------------------------------------
echo "Adding Helm repositories..."
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm repo add grafana https://grafana.github.io/helm-charts
helm repo update

# ------------------------------------------------------------------------------
# 3. Deploy Kube-Prometheus-Stack (Prometheus + Grafana)
# ------------------------------------------------------------------------------
echo "Deploying Kube-Prometheus-Stack..."
# Create a namespace for monitoring if it doesn't exist
kubectl create namespace monitoring --dry-run=client -o yaml | kubectl apply -f -

helm upgrade --install my-monitoring prometheus-community/kube-prometheus-stack \
  --namespace monitoring \
  --set grafana.adminPassword='admin' # CHANGE THIS IN PRODUCTION!

# ------------------------------------------------------------------------------
# 4. Deploy Loki Stack (Logs)
# ------------------------------------------------------------------------------
echo "Deploying Loki Stack..."
helm upgrade --install loki grafana/loki-stack \
  --namespace monitoring \
  --set grafana.enabled=false \
  --set prometheus.enabled=false \
  --set prometheus.alertmanager.persistentVolume.enabled=false \
  --set prometheus.server.persistentVolume.enabled=false

echo "----------------------------------------------------------------"
echo "Monitoring Stack Installed Successfully!"
echo "----------------------------------------------------------------"
echo "To access Grafana:"
echo "1. Run: kubectl port-forward svc/my-monitoring-grafana 3000:80 -n monitoring"
echo "2. Open: http://localhost:3000"
echo "3. User: admin / Password: admin"
echo "----------------------------------------------------------------"
