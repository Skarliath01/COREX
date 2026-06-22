# Corex by Altysin — Claude Operational Guide

## Identité produit
**Corex** est un optimiseur PC Windows de nouvelle génération, premier produit de l'entreprise **Altysin**.
Tagline : *"Your PC. Your Core. Unleashed."*
Concurrent principal : FPSDoctor (~2 000 users FR). Différenciateur central : **tweaks adaptatifs conditionnels au hardware détecté** — jamais un tweak NVIDIA sur une machine AMD, jamais une défrag sur un SSD.

---

## Workflow en 3 couches

```
gstack   →  PAUL/GSD  →  Superpowers
Décision    Stabilité     Exécution
```

| Couche | Outil | Rôle | Quand l'utiliser |
|--------|-------|------|-----------------|
| Décision | **gstack** | Jugement multi-rôle (CEO, archi, QA) — *quoi faire* | Nouvelle feature, pivot, revue d'approche |
| Contexte | **PAUL** | Ancrage état, réconciliation plan/réalité — *ne pas dériver* | Longues sessions, reprise après pause |
| Exécution | **Superpowers** | Clarify → Plan → TDD → Implement → Verify | Toute feature, même un fix de 2 lignes |

**Règle d'or** : gstack pense, PAUL stabilise, Superpowers exécute. Ne jamais inverser l'ordre.

---

## Stack technique

```
App desktop    : C# 13 + WinUI 3 (WASDK)  — Win 10 22H2 minimum
Bas niveau     : C++ / P/Invoke pour WMI, Registry, WinAPI, GPU APIs
Installeur     : Inno Setup 6
Signature      : Certificat EV Authenticode (obligatoire dès V1)
Backend        : Node.js + Express + PostgreSQL (licences, updates, analytics)
Frontend web   : React + TypeScript (site Altysin)
Updates        : Sparkle/WinSparkle ou update silencieux maison
```

> Zéro Electron. Zéro framework lourd. Performances natives uniquement.

---

## Références fichiers

| Fichier | Contenu |
|---------|---------|
| `architecture.md` | Structure modules, couches, dépendances, patterns |
| `cicd.md` | GitHub Actions, workflows, environments, release process |
| `conventions.md` | Naming, coding style C#/C++, Git flow, PR rules |
| `modules.md` | Détail des 12 modules — scope V1/V2/V3 |
| `security.md` | Signature EV, AV whitelisting, RGPD, snapshot policy |
| `backlog.md` | 22 features V1 priorisées avec complexité et statut |

---

## Principes non négociables

1. **Snapshot obligatoire** avant toute modification système — non bypassable
2. **Tweak conditionnel** — chaque optimisation vérifie le hardware avant de s'appliquer
3. **Log horodaté** de chaque modification avec valeur avant/après
4. **Restauration sélective** — tweak par tweak ou tout d'un coup
5. **Zéro SmartScreen** — signature EV dès le premier build distribué
6. **Zéro faux positif AV** — pas d'obfuscation, pas de packing, code WinAPI documenté uniquement

---

## Priorités MVP V1 (mois 1–4)

```
P0 CRITIQUE (bloquant tout le reste)
├── M1 : Détection hardware complète (WMI/Registry)
├── M1 : Moteur de règles conditionnelles hardware→tweaks
├── M10: Snapshot Registry + restauration complète
├── M10: Infrastructure tweak (log, valeur avant/après, indicateur risque)
└── Infra: Signature EV + système licences + updates silencieux

P0 (cœur de valeur)
├── M2 : 50+ tweaks Windows système adaptatifs
├── M2 : Debloat apps préinstallées
├── M4 : Désinstalleur propre + nettoyage résidus complets
└── M9 : Profil Gaming de base (tweaks GPU NVIDIA/AMD adaptatifs)

P1 (V1 complète)
├── M7 : Dashboard monitoring temps réel CPU/GPU/RAM/SSD
├── M7 : Alertes critiques (SMART, température, RAM)
├── M7 : Score santé PC 0–100
└── M7 : Benchmark rapide avant/après
```

---

## Modèle freemium

| Tier | Prix | Contenu |
|------|------|---------|
| **Gratuit** | 0 € | Détection hardware + tweaks confidentialité/confort + debloat basique + désinstalleur simple + snapshot + alertes critiques + 3 req IA/mois |
| **Pro** | 4 €/mois ou 35 €/an | Tous profils + drivers + debloat avancé + réseau avancé + alertes illimitées + mode gaming auto + overlay + 50 req IA/mois |
| **Ultimate** | 8 €/mois ou 65 €/an | VM + Créatif + audit sécurité + monitoring 30j + prédiction SSD + test RAM + support prioritaire + IA illimitée |

---

## Indicateurs risque tweaks

- 🟢 **Safe** — Registry HKCU, services non-essentiels, nettoyage fichiers temp
- 🟡 **Modéré** — Services système, paramètres réseau, plan d'alimentation
- 🔴 **Avancé Expert** — Timer resolution, HPET, Core Parking, pagefile — double confirmation requise

---

## Compétiteur principal : FPSDoctor

**Leurs faiblesses exploitables en V1 :**
- Hardware détecté mais tweaks identiques pour tous → notre killer feature
- GPU AMD verrouillé → on supporte NVIDIA + AMD + Intel dès V1
- Zéro désinstalleur propre → notre Module 4
- Freemium trop restrictif → notre tier gratuit est plus généreux
- IA sans contexte hardware → notre IA connaît la config réelle

**Ne jamais attaquer FPSDoctor directement** dans les communications — laisser la comparaison venir des utilisateurs.

---

## Branches GitHub

```
main      ← Production (users finaux) — merge depuis staging uniquement
staging   ← Recette/QA — merge depuis dev uniquement
dev       ← Intégration — toutes les features/fix/hotfix squash mergées ici

feature/* fix/* hotfix/*  ← toujours créées depuis dev, jamais depuis staging ou main
```

**Flow obligatoire :** `feature/x` → squash merge → `dev` → merge commit → `staging` → merge commit + tag → `main` → CI release

## Session startup checklist

Avant chaque session de dev, vérifier :
- [ ] PAUL chargé avec l'état actuel du projet
- [ ] Branche feature créée depuis `develop`
- [ ] Snapshot VM de test créé
- [ ] Module cible identifié dans `backlog.md`

## Commandes gstack fréquentes

```
/plan-eng-review   → revue architecture avant d'implémenter
/plan-ceo-review   → décision produit / priorisation
/office-hours      → question ouverte multi-rôle
```

---

*Dernière mise à jour : init projet Corex — repo Skarliath01/corex*
