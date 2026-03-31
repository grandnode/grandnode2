# Deploy GrandNode (User Project)
Write-Host "Deploying GrandNode & MongoDB to Kubernetes..."
$manifest = "k8s/application/grandnode.yaml"

kubectl apply -f $manifest

Write-Host "----------------------------------------------------------------"
Write-Host "GrandNode Deployed!"
Write-Host "Wait a few minutes for pods to be 'Running'."
Write-Host "Check status: kubectl get pods"
Write-Host "Access App: http://localhost" 
