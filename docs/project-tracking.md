# Project Tracking – Online Boutique → Black Friday Demo

**Current phase:** Semaine 1 (Setup & Fondations)  
**Last updated:** 2025-02-03

---

## Semaine 1 : Setup & Fondations — Budget : 200€

| Statut   | Tâche |
|----------|--------|
| ✅ Fait  | Déploiement local de Online Boutique (Docker Compose) |
| ⬜ À faire | Migration vers AWS : création du cluster EKS |
| ⬜ À faire | Infrastructure as Code : premiers modules Terraform |
| ⬜ À faire | Setup du monitoring basique (CloudWatch) |
| ✅ Fait  | Premier test de charge : 1 000 utilisateurs (initié et exécuté) |

**Livrable :** Infrastructure fonctionnelle avec documentation

---

## Semaine 2 : Hardening & Optimisation — Budget : 500€

| Statut   | Tâche |
|----------|--------|
| ⬜ À faire | Déploiement Multi-AZ (3 zones de disponibilité) |
| ⬜ À faire | Security hardening : WAF, Security Groups, IAM |
| ⬜ À faire | Observabilité complète : Prometheus + Grafana + Jaeger |
| ⬜ À faire | Configuration auto-scaling (HPA, Cluster Autoscaler) |
| ✅ Fait  | Tests de charge progressifs : 5K → 20K → 50K utilisateurs |

**Livrable :** Architecture sécurisée et scalable, dashboards Grafana

---

## Semaine 3 : Pre-Black Friday — Budget : 600€

| Statut   | Tâche |
|----------|--------|
| ⬜ À faire | Chaos engineering : injection de pannes contrôlées |
| ⬜ À faire | Optimisation des coûts (Spot Instances, rightsizing) |
| ⬜ À faire | Répétition générale : test à 70K utilisateurs |
| ⬜ À faire | Rédaction des runbooks et procédures d'incident |
| ⬜ À faire | Préparation de la War Room |

**Livrable :** Système prêt pour la démo, documentation complète

---

## Jours 1–2 Démo : BLACK FRIDAY — Budget : 200€

**Le Jour J :** 8 heures de simulation en conditions réelles

| Jour | Activité |
|------|----------|
| Jour 1 | Montée en charge progressive jusqu'à 90K utilisateurs |
| Jour 1 | Injection d'incidents en temps réel par le formateur |
| Jour 1 | War Room : gestion de crise en équipe |
| Jour 2 | Post-mortem et présentation des résultats |

---

## Budget total prévu

| Phase        | Budget |
|-------------|--------|
| Semaine 1   | 200€   |
| Semaine 2   | 500€   |
| Semaine 3   | 600€   |
| Démo (J1–J2)| 200€   |
| **Total**   | **1 500€** |

---

## Notes Semaine 1

- **Docker Compose :** Déploiement local Online Boutique opérationnel.
- **Test 1K :** Script `load-test/run-1k.ps1` utilisé (k6, scenario 1k, ramp 2m → 1000 VUs, palier 3m, ramp down 1m). Exécution initiée et réalisée.
- **Tests de charge progressifs :** 5K → 20K → 50K utilisateurs réalisés (Semaine 2, fait en avance).
- **Prochaines étapes :** EKS, Terraform, CloudWatch, puis finalisation doc d’infrastructure.
