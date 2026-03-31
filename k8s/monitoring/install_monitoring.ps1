# Check if Helm is installed
if (-not (Get-Command helm -ErrorAction SilentlyContinue)) {
    Write-Host "Helm not found. Installing via Chocolatey..."
    if (Get-Command choco -ErrorAction SilentlyContinue) {
        choco install kubernetes-helm -y
    } else {
        Write-Host "Chocolatey not found. Please install Helm manually: https://helm.sh/docs/intro/install/"
        exit 1
    }
}

# Add Repos
Write-Host "Adding Helm repositories..."
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm repo add grafana https://grafana.github.io/helm-charts
helm repo update

# Install Kube-Prometheus-Stack
Write-Host "Deploying Kube-Prometheus-Stack..."
$namespace = "monitoring"
if (-not (kubectl get namespace $namespace -ErrorAction SilentlyContinue)) {
    kubectl create namespace $namespace
}

helm upgrade --install my-monitoring prometheus-community/kube-prometheus-stack `
  --namespace $namespace `
  --set grafana.adminPassword='admin'

# Install Loki
Write-Host "Deploying Loki Stack..."
helm upgrade --install loki grafana/loki-stack `
  --namespace $namespace `
  --set grafana.enabled=false `
  --set prometheus.enabled=false `
  --set prometheus.alertmanager.persistentVolume.enabled=false `
  --set prometheus.server.persistentVolume.enabled=false `
  --set loki.isDefault=false `
  --set loki.image.tag=2.9.3

Write-Host "----------------------------------------------------------------"
Write-Host "Monitoring Stack Installed Successfully!"
Write-Host "----------------------------------------------------------------"
Write-Host "To access Grafana:"
Write-Host "1. Run: kubectl port-forward svc/my-monitoring-grafana 3000:80 -n monitoring"
Write-Host "2. Open: http://localhost:3000"
Write-Host "User: admin / Password: admin"
