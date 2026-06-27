# Corex — PAUL STATE

## Position actuelle

```
Phase 01 — Hardware Detection
PLAN ──▶ APPLY ──▶ UNIFY
  ✓        ✓        ✓     [Loop fermé — prêt pour F02]
```

## Phase 01 : Hardware Detection — COMPLÈTE

| Item | Statut |
|------|--------|
| F01 — Détection WMI | ✓ Livré |
| Tests 31/31 | ✓ Verts |
| Squash merge → dev | ✓ `aae0bdd` |
| Hotfix CI release | ✓ `c2cd2a0` |
| Markdown lint | ✓ `d326490` |
| SUMMARY | ✓ `.paul/phases/01-hardware-detection/01-01-SUMMARY.md` |

## Backlog Features V1

| Feature | Statut | Branch |
|---------|--------|--------|
| F01 — Détection hardware WMI | ✓ Livré — dev | — |
| F02 — Moteur règles conditionnelles | ⏳ Prochaine | — |
| F03 — Tweaks gaming | ⏳ Backlog | — |
| F04 — UI Dashboard | ⏳ Backlog | — |
| F05–F22 | ⏳ Backlog | — |

## Points ouverts

| Ref | Description | Cible |
|-----|-------------|-------|
| V2-01 | WmiCache distribué inter-sessions | V2 |
| F04-01 | Bandeau UI "composant matériel non détecté" | F04 |
| V1.1-01 | Détection réseau (adaptateur, débit) | V1.1 |
| V1.1-02 | Détection OS (version, BIOS, Secure Boot) | V1.1 |

## Prochaine action

**F02 — Moteur de règles conditionnelles**

Séquence obligatoire :
1. PAUL Plan F02 (fournir contexte backlog)
2. `/plan-eng-review` — design technique ITweakRule / TweakDefinition / IsApplicableCore
3. Context7 — vérifier APIs utilisées
4. `lance Superpowers TDD` — cycle Clarify → Plan → Test → Implement → Verify
5. PAUL Unify F02

## Arrêté à

Session du 2026-06-23 22:30 UTC+2
Résumé : `.paul/phases/01-hardware-detection/01-01-SUMMARY.md`

## Règles session actives

- ❌ Commit interdit avant `lance Superpowers TDD` explicite
- ❌ Context7 obligatoire avant tout fichier `Corex.Engine/`
- ❌ `dev`, `staging`, `main` — suppression absolument interdite
