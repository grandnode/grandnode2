# Diagrammes d'architecture (mermaid-cli)

Deux diagrammes : **architecture CDC (EKS)** et **architecture optimale (ECS Fargate)**. Ils mettent en évidence la différence de couche compute : Kubernetes (EKS, 3 AZ, Node Groups, HPA, Cluster Autoscaler) vs ECS Fargate (services + tâches serverless, pas de nodes).

**Installation** (une fois) :
```bash
npm install -g @mermaid-js/mermaid-cli
```

**Génération des PNG** :
```powershell
cd docs\mermaid
.\render.ps1
```

Fichiers générés dans `out/` : `01-architecture-cdc.png`, `02-architecture-optimale.png`.
