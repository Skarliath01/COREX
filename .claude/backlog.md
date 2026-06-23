# Corex — Backlog V1

22 features prioritisées pour le MVP (mois 1–4).
Mise à jour au fil du développement.

## Statuts
- `[ ]` À faire
- `[~]` En cours
- `[x]` Terminé
- `[!]` Bloqué

---

## P0 CRITIQUE — Fondations (Semaine 1–3)

Ces features bloquent tout le reste. Rien d'autre ne commence avant qu'elles soient terminées.

| # | Feature | Module | Complexité | Statut |
|---|---------|--------|-----------|--------|
| 01 | Détection hardware complète (WMI/Registry) — CPU, GPU, RAM, stockage, réseau, OS | M1 | Complexe | `[x]` |
| 02 | Moteur de règles conditionnelles (HardwareProfile → tweaks disponibles) | M1 | Complexe | `[ ]` |
| 03 | Snapshot Registry + restauration complète avant toute modification | M10 | Moyen | `[ ]` |
| 04 | Infrastructure tweak : log horodaté, valeur avant/après, indicateur risque 🟢🟡🔴 | M10 | Moyen | `[ ]` |

**Critère de sortie semaine 3 :** Feature 01–04 fonctionnelles, testées sur 3 configs matérielles distinctes, restauration prouvée en VM.

---

## P0 — Cœur de valeur (Semaine 4–7)

| # | Feature | Module | Complexité | Statut |
|---|---------|--------|-----------|--------|
| 05 | Tweaks confidentialité/télémétrie (DiagTrack, publicités, géoloc, WER, CEIP, NVIDIA/AMD/Intel telemetry) | M2 | Simple | `[ ]` |
| 06 | Tweaks performances système (plan d'alim., Core Parking, HPET, timer resolution, SysMain conditionnel) | M2 | Moyen | `[ ]` |
| 07 | Tweaks confort/interface (animations, FSO, Edge, Xbox Game Bar, Copilot, OneDrive) | M2 | Simple | `[ ]` |
| 08 | Debloat apps préinstallées (Candy Crush, Mixed Reality, Bing apps, Solitaire, Xbox apps inutiles) | M2 | Simple | `[ ]` |
| 09 | Nettoyage (temp, prefetch, shaders NVIDIA/AMD/DX/Vulkan, icônes, polices, Winsock) | M2 | Simple | `[ ]` |
| 21 | Système de licences + compte Altysin + updates silencieux (backend Node.js + PostgreSQL) | Infra | Complexe | `[ ]` |
| 22 | Signature EV Authenticode + installeur Inno Setup | Infra | Moyen | `[ ]` |

**Note feature 21 :** Démarrer le backend en parallèle des tweaks dès la semaine 4. Bloquant pour la distribution bêta.

---

## P1 — V1 Complète (Semaine 8–13)

| # | Feature | Module | Complexité | Statut |
|---|---------|--------|-----------|--------|
| 10 | SFC/DISM intégré + TRIM SSD + défrag HDD (conditionnel — jamais défrag SSD) | M2 | Moyen | `[ ]` |
| 11 | Désinstalleur standard + suppression résidus AppData / ProgramData / Program Files orphelins | M4 | Moyen | `[ ]` |
| 12 | Nettoyage clés registre orphelines post-désinstallation | M4 | Moyen | `[ ]` |
| 13 | Suppression services + tâches planifiées + entrées démarrage liés à l'app désinstallée | M4 | Moyen | `[ ]` |
| 14 | Rapport espace disque récupéré avant/après désinstallation + log détaillé | M4 | Simple | `[ ]` |
| 15 | Dashboard temps réel CPU/GPU/RAM/stockage (WMI + APIs GPU natifs) | M7 | Complexe | `[ ]` |
| 16 | Alertes critiques : SSD santé SMART dégradée, CPU temp repos >55°C, RAM >85% repos | M7 | Moyen | `[ ]` |
| 17 | Analyse apps démarrage avec score d'impact boot chiffré ("Discord ralentit de +3.2s") | M7 | Moyen | `[ ]` |
| 18 | Score santé PC global 0–100 mis à jour dynamiquement | M11 | Simple | `[ ]` |

---

## P1 — Finitions (Semaine 14–16)

| # | Feature | Module | Complexité | Statut |
|---|---------|--------|-----------|--------|
| 19 | Benchmark rapide CPU/RAM/SSD (comparaison avant/après optimisations) | M7 | Moyen | `[ ]` |
| 20 | Profil Gaming de base (plan perf + tweaks GPU NVIDIA/AMD adaptatifs + réseau QoS) | M9 | Moyen | `[ ]` |

---

## Ordre de développement recommandé

```
Semaine 1  : Setup projet C# + WinUI 3, structure dossiers, CI basique
Semaine 2  : Feature 01 — Détection hardware (WMI CPU + GPU + RAM)
Semaine 3  : Feature 01 suite (stockage SMART, réseau, OS) + Feature 02 (moteur règles)
Semaine 4  : Feature 03 (snapshot) + Feature 04 (infra tweak log)
Semaine 5  : Feature 05 + 07 (tweaks confidentialité + confort — les plus simples)
Semaine 6  : Feature 06 (tweaks performances avec conditionnels hardware)
Semaine 7  : Feature 08 + 09 (debloat + nettoyage) + début Feature 21 (backend)
Semaine 8  : Feature 21 suite (licences) + Feature 22 (signature + installeur)
Semaine 9  : Feature 10 (SFC/DISM/TRIM) + Feature 11 (désinstalleur core)
Semaine 10 : Feature 12 + 13 + 14 (désinstalleur complet)
Semaine 11 : Feature 15 (dashboard monitoring — le plus complexe après M1)
Semaine 12 : Feature 16 + 17 (alertes + analyse démarrage)
Semaine 13 : Feature 18 (score santé) + Feature 19 (benchmark)
Semaine 14 : Feature 20 (profil gaming) + polish UI général
Semaine 15 : Tests end-to-end sur 5 configs, bugfix, optimisation
Semaine 16 : Release bêta publique
```

---

## Définition of Done (DoD) par feature

Une feature est "terminée" quand :
- [ ] Tests unitaires écrits et au vert (coverage >80% du code nouveau)
- [ ] Testé manuellement sur Windows 10 22H2 VM
- [ ] Testé manuellement sur Windows 11 24H2 VM
- [ ] Snapshot + restauration validés si la feature modifie le système
- [ ] Zéro warning de compilation Roslyn
- [ ] PR reviewée et mergée dans `develop`
- [ ] CHANGELOG.md mis à jour

---

## Métriques de succès V1

| Métrique | Objectif |
|---------|---------|
| Téléchargements bêta | 100 à fin mois 4 |
| NPS bêta | >40 |
| BSOD signalés | 0 |
| Faux positifs AV | 0 (sur build signé) |
| Restauration réussie | 100% dans les tests |
| Temps détection hardware | <3 secondes |
| Taux conversion gratuit→Pro | >5% à fin mois 4 |
