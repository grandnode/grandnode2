# Implémentation : Déploiement de la présentation et de la documentation via AWS/Terraform

Ce document décrit comment ajouter la **présentation** (HTML/CSS, éventuellement un peu de JS) et la **documentation** (contenu statique) au déploiement AWS existant pour qu’elles soient servies aux côtés de l’application principale et des médias. Présentation et documentation sont déployées dans le bucket S3 médias existant sous des préfixes de chemin ; aucune nouvelle ressource Terraform, seule une étape de synchronisation CI.

---

## État actuel

- **Application** : ECS + ALB (conteneur GrandNode).
- **Médias** : bucket S3 (`media_bucket_name`) avec **une distribution CloudFront** ; `default_root_object = "index.html"` ; bucket privé, OAC CloudFront.
- **Pipeline de déploiement** : GitHub Actions build l’image Docker, pousse vers ECR, force un redéploiement ECS. Aucune étape ne synchronise actuellement les assets statiques vers S3.

Les assets de présentation et de documentation vivront sous :

- `docs/presentation/` — HTML, CSS, JS minimal optionnel.
- `docs/documentation/` — documentation statique (ex. HTML, images).

Les deux seront déployés dans le **même bucket que les médias**, sous les préfixes `presentation/` et `documentation/`, et servis en HTTPS via la distribution CloudFront existante.

---

## Même bucket S3 médias existant chemin

- **Principe** : Envoyer la présentation et la documentation dans le **bucket S3 médias existant** sous les préfixes de clé `presentation/` et `documentation/`.
- **URLs** : `https://<domaine-cloudfront>/presentation/`, `https://<domaine-cloudfront>/documentation/`.
- **Terraform** : Aucune nouvelle ressource. Seul le pipeline CI doit synchroniser `docs/presentation/` et `docs/documentation/` vers ce bucket.
- **À garder en tête** : Médias de l’app (uploads utilisateur) et contenu statique (présentation/docs) partagent le même bucket ; si le bucket ou la structure des chemins change, présentation et documentation sont impactées.

---

## Implémentation Terraform

Aucune modification Terraform nécessaire. Le bucket médias et la distribution CloudFront existants servent déjà tout contenu du bucket ; il suffit d’y déposer les objets sous `presentation/` et `documentation/`.

---

## CI/CD — Synchronisation vers S3

- **Quand** : Même workflow que le déploiement de l’app (ex. au push sur `main`) ou un job dédié ; un même job peut lancer les tests puis synchroniser les deux dossiers.
- **Cible** : Le bucket médias existant (variable Terraform `media_bucket_name` / secret ou variable CI `MEDIA_BUCKET` ou équivalent).
- **Commandes** :
  - `aws s3 sync docs/presentation/ s3://<bucket-medias>/presentation/ --delete` (et idem pour `docs/documentation/` → `s3://<bucket-medias>/documentation/`).
  - Définir `Content-Type` (ex. `text/html` pour `.html`, `text/css` pour `.css`) via `--content-type` ou métadonnées.
- **Cache** : `Cache-Control` (ex. `max-age=3600`) pour les assets statiques ; cache court ou pas de cache en phase d’itération si besoin.
- **IAM** : Réutiliser les identifiants AWS déjà utilisés pour ECR/ECS ; la politique IAM doit autoriser `s3:PutObject`, `s3:DeleteObject`, `s3:ListBucket` sur le bucket médias.

---

## Contenu et structure (rappel)

- **Présentation** : `docs/presentation/` — HTML, CSS, JS optionnel ; point d’entrée typiquement `index.html` (URL : `/presentation/` ou `/presentation/index.html`).
- **Documentation** : `docs/documentation/` — pages et assets statiques ; même principe, point d’entrée et chemins à définir à l’ajout du contenu.

---

## Prochaines étapes

1. Ajouter ou étendre le workflow de déploiement (ex. GitHub Actions) pour synchroniser `docs/presentation/` et `docs/documentation/` vers le bucket médias sous les préfixes `presentation/` et `documentation/`.
2. Ajouter le contenu de présentation et de documentation sous `docs/presentation/` et `docs/documentation/` lorsque c’est prêt (demandé séparément).
