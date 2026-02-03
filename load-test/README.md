# Tests de charge avec k6 (GrandNode)

Tests de charge du storefront GrandNode avec [k6](https://k6.io). Scénarios prévus pour **~90k utilisateurs** (configurable).

## Prérequis

- GrandNode en marche (ex. `docker compose up -d` à la racine du projet ; app sur http://127.0.0.1:8080).
- k6 installé en local, **ou** Docker pour lancer k6 en conteneur.

## Installer k6 (optionnel)

- **Windows (scoop) :** `scoop install k6`
- **macOS :** `brew install k6`
- **Linux :** voir [Installation k6](https://k6.io/docs/getting-started/installation/).

Sinon utiliser Docker (sans installation) : voir « Lancer avec Docker » ci-dessous.

## Lancer rapidement

Depuis le dossier `load-test/`, ou depuis la racine du projet en adaptant les chemins :

```bash
# 1k utilisateurs – test en local (montée 2 min → 1000 VUs, palier 3 min, descente 1 min)
k6 run -e SCENARIO=1k k6/storefront.js

# Par défaut : 50 VUs, 2 min, montée/descente
k6 run k6/storefront.js

# Charge personnalisée
k6 run -e VUS=200 -e DURATION=5m k6/storefront.js

# Scénario 90k : montée jusqu’à 3k VUs sur ~1 h (nombre total d’itérations >> 90k)
k6 run -e SCENARIO=90k -e BASE_URL=http://127.0.0.1:8080 k6/storefront.js
```

## Variables d’environnement

| Variable    | Défaut                 | Description |
|------------|------------------------|-------------|
| `BASE_URL` | http://127.0.0.1:8080 | URL de base de GrandNode. |
| `VUS`      | 50                    | Nombre cible d’utilisateurs virtuels (scénario par défaut). |
| `DURATION` | 2m                    | Durée du palier de charge (scénario par défaut). |
| `SCENARIO` | (aucun)               | `1k` = 1000 VUs (test local), `90k` = montée longue (5m→500, 20m→2k, 30m→3k, …). |

## 1k utilisateurs (test en local)

- **SCENARIO=1k**  
  Montée en 2 min à 1000 VUs, palier 3 min à 1000 VUs, descente 1 min. Durée totale ~6 min.  
  Idéal pour tester sur ta machine avant des scénarios plus lourds.

## 90k utilisateurs

- **SCENARIO=90k**  
  Montée : 5 min → 500 VUs, 20 min → 2000, 30 min à 3000, 10 min → 1000, 2 min → 0.  
  Durée totale ~67 minutes ; le nombre total de requêtes HTTP / itérations sera bien supérieur à 90k.

- **~90k itérations en personnalisé**  
  Pour viser ~90k itérations au total avec un nombre fixe de VUs, tu peux utiliser un exécuteur constant-vus et une durée telle que (VUs × itérations par VU) ≈ 90k, ou dupliquer le script et fixer un nombre d’itérations dans k6.

## Lancer avec Docker

Depuis la **racine du projet** (pour que `BASE_URL` puisse cibler l’hôte si besoin) :

```powershell
# Le conteneur k6 atteint l’app sur l’hôte via host.docker.internal
docker run --rm -v "${PWD}/load-test/k6:/scripts" grafana/k6 run -e BASE_URL=http://host.docker.internal:8080 /scripts/storefront.js
```

Sous Linux, utiliser `--network host` et `BASE_URL=http://127.0.0.1:8080` pour que le conteneur utilise le réseau de l’hôte.

## Ce que fait le script

- **storefront.js**  
  Chaque utilisateur virtuel en boucle : GET `/`, GET `/catalog`, GET `/search?q=test`, avec de courtes pauses aléatoires.  
  Seuils : &lt;5 % de requêtes en échec, p95 &lt; 5 s.

Si ton GrandNode utilise d’autres chemins (ex. pas de `/catalog`), adapte le script dans `k6/storefront.js` ou assouplis les checks (ex. accepter 404 pour les pages optionnelles).

## Résultats et interprétation

- **Pendant le test** tu vois la progression : VUs (utilisateurs virtuels), temps écoulé, itérations complétées. L’absence d’erreurs dans ce flux signifie que les requêtes partent et que des réponses sont reçues.
- **À la fin** (~6 min pour le scénario 1k) k6 affiche un **résumé** : itérations totales, `http_req_duration` (moyenne, p95, médiane, …), `http_req_failed` (taux), et si les **seuils** sont passés (ex. `http_req_failed` &lt; 5 %, p95 de `http_req_duration` &lt; 5 s). Descends en bas du terminal pour le voir.

Pour enregistrer le résumé dans un fichier avec Docker, monte un dossier et utilise `--out json` :

```powershell
# Depuis le dossier load-test ; crée load-test/out et y écrit le résumé
mkdir -Force out
docker run --rm -v ${PWD}:/scripts -v ${PWD}/out:/out grafana/k6 run -e SCENARIO=1k -e BASE_URL=http://host.docker.internal:8080 --out json=/out/summary.json /scripts/k6/storefront.js
```

Ensuite ouvre `load-test/out/summary.json` pour les métriques.
