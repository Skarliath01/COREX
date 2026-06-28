# Corex — Workflow orchestration 6 outils

## Architecture des couches

```text
/office-hours        →  vision, risques, positionnement

↓

/plan-eng-review     →  design technique feature

↓

PAUL Plan            →  ancrage session (décisions intégrées)

↓

Context7             →  doc officielle APIs

↓

Superpowers TDD      →  Clarify → Plan → Test → Implement → Verify

↓

Headroom (70%)       →  déclenche Unify si saturation

↓

PAUL Unify + Memory  →  synthèse + persistance
```text

**Règle absolue : ne jamais sauter une couche.**
Coder sans PAUL Plan = drift. Utiliser une API sans Context7 = hallucination. Exécuter sans Clarify = régression.

---

## Séquence complète par feature

### 1 · gstack /office-hours — Cadrage projet

**Quand :** début de projet, début de sprint, ou quand la vision dérive.
**Rôle :** question ouverte multi-rôle (CEO + archi + QA simultané) —
élucider le produit, valider le positionnement, identifier les risques macro.
**Avant toute feature, avant tout /plan-eng-review.**

### 2 · gstack /plan-ceo-review — Décision produit

**Quand :** arbitrage priorisation backlog, pivot, décision scope V1/V2. Optionnel.
**Rôle :** lens CEO — coût / valeur / timing.

### 3 · gstack /plan-eng-review — Revue architecture

**Quand :** après /office-hours, juste avant d'implémenter une feature.
**Rôle :** valider le design technique précis de la feature —
interfaces, patterns, edge cases, thread-safety.

### 4 · PAUL Plan — Ancrage session

**Quand :** après gstack, TOUJOURS avant d'écrire du code.
Template :
  PAUL context load — Corex by Altysin
  État backlog : terminé [], en cours [Fxx], bloqué []
  Décisions récentes : [issues from /office-hours + /plan-eng-review]
  Objectif session : [Feature XX]
  Contrainte active : snapshot obligatoire avant toute modification système

### 5 · Context7 — Documentation officielle

**Quand :** après PAUL Plan, avant d'utiliser toute API ou NuGet.

### 6 · Superpowers TDD — Exécution

**Quand :** après Context7. Cycle : Clarify → Plan → Test → Implement → Verify.

### 7 · Headroom — Garde-fou contexte

**Passif.** Déclenche PAUL Unify automatiquement à 70% saturation.

### 8 · PAUL Unify + Claude Memory — Clôture

**Quand :** fin de session volontaire OU Headroom à 70%.

---

## PAUL Unify — Clôture de session

**Déclenché par :** fin de session volontaire OU Headroom à 70%.

**Ce que Unify produit :**

- Résumé des décisions prises dans la session
- Mise à jour `.claude/backlog.md` (features terminées / en cours)
- Liste des points ouverts pour la prochaine session
- Mémorisation via Claude Memory

**Template Unify :**

```text
PAUL Unify — session [date]
Terminé : [liste features / étapes]
Décisions : [liste des choix techniques actés]
Points ouverts : [ce qui reste + blocages]
Prochaine session : [objectif exact]
```text

---

## Session type

```text
[Démarrage]
  └─ Claude Memory charge l'état Corex automatiquement
  └─ PAUL Plan : coller le template, définir l'objectif

[Si nouvelle feature ou choix archi]
  └─ gstack /plan-eng-review ou /office-hours

[Avant le code]
  └─ Context7 : charger la doc des APIs concernées

[Implémentation]
  └─ Superpowers : Clarify → Plan → Test → Implement → Verify
  └─ Headroom : surveille passivement

[Fin de session ou 70% contexte]
  └─ PAUL Unify → Claude Memory sync → commit + push
```text

---

## Anti-patterns workflow

```text
❌ Ouvrir Claude Code et coder directement sans PAUL Plan
❌ Utiliser ManagementObjectSearcher sans avoir chargé la doc System.Management via Context7
❌ Écrire du code avant d'avoir écrit les tests (TDD = tests d'abord)
❌ Ignorer Headroom et continuer quand le contexte se dégrade
❌ Fermer une session sans PAUL Unify → décisions perdues
❌ Activer 5+ rôles gstack simultanément → context overflow
❌ Merger dans dev sans CI verte
```text
