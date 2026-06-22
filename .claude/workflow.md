# Corex — Workflow orchestration 6 outils

## Architecture des couches

```
┌─────────────────────────────────────────────────────┐
│  COUCHE DÉCISION          gstack                    │
│  /office-hours · /plan-eng-review · /plan-ceo-review│
└────────────────────────┬────────────────────────────┘
                         │
┌────────────────────────▼────────────────────────────┐
│  COUCHE SESSION          PAUL  ←──────  Claude Memory│
│  Plan → Apply → Unify         (persistance cross-    │
│                                session)              │
└────────────────────────┬────────────────────────────┘
                         │
┌────────────────────────▼────────────────────────────┐
│  COUCHE CONTEXTE         Headroom + Context7         │
│  Headroom : surveille 70% saturation                │
│  Context7 : doc officielle NuGet/API avant code     │
└────────────────────────┬────────────────────────────┘
                         │
┌────────────────────────▼────────────────────────────┐
│  COUCHE EXÉCUTION        Superpowers (TDD)           │
│  Clarify → Plan → Test → Implement → Verify         │
└─────────────────────────────────────────────────────┘
```

**Règle absolue : ne jamais sauter une couche.**
Coder sans PAUL Plan = drift. Utiliser une API sans Context7 = hallucination. Exécuter sans Clarify = régression.

---

## Séquence complète par feature

### 1 · gstack — Décision architecture
**Quand :** début de feature, choix structurant, pivot.
**Commandes :**
- `/office-hours` → question ouverte multi-rôle (CEO + archi + QA simultané)
- `/plan-eng-review` → revue design avant d'implémenter
- `/plan-ceo-review` → décision produit / priorisation backlog

**Limite :** 1–2 rôles max par session — au-delà, réponses trop génériques.

**Exemple Corex :**
```
/plan-eng-review : "Je veux détecter le GPU via WMI + NVAPI.
Fallback si NVAPI indisponible : WMI seul.
IHardwareDetector est-elle bien découpée pour mocker en tests ?"
```

---

### 2 · PAUL Plan — Ancrage session
**Quand :** TOUJOURS en premier avant d'écrire du code. Sans exception.

**Template à coller en début de session Claude Code :**
```
PAUL context load — Corex by Altysin
État backlog : terminé [F01], en cours [F02], bloqué []
Décisions récentes : [HardwareProfile = record immuable · WMI cache 60min]
Objectif session : [Feature XX — description précise]
Contrainte active : snapshot obligatoire avant toute modification système
```

**Rôle :** empêche le context drift — si la session dévie, revenir au Plan.

---

### 3 · Claude Memory — Persistance cross-session
**Quand :** automatique au démarrage. Manuel quand une décision clé doit survivre.

**Exemples de ce qui doit être mémorisé :**
- Conventions actées (`HardwareProfile` = record immuable)
- Décisions d'architecture (`WmiCache` = singleton, durée 60 min)
- Anti-patterns découverts (`ManagementObjectSearcher` doit être dans `using`)

**En fin de session :** demander explicitement `"mémorise les décisions de cette session"`.

---

### 4 · Headroom — Garde-fou contexte
**Quand :** passif — surveille automatiquement. Actif à 70% de saturation.

**Comportement à 70% :**
1. Stopper l'implémentation en cours
2. Déclencher PAUL Unify immédiatement
3. Sauvegarder l'état dans `.claude/backlog.md`
4. Ouvrir une nouvelle session avec PAUL Plan rechargé

**Signe que Headroom devrait sonner :** les réponses deviennent moins précises, le contexte des décisions précédentes semble oublié.

---

### 5 · Context7 — Documentation officielle
**Quand :** AVANT d'utiliser toute API, NuGet ou feature du framework.
**Jamais** écrire du code sur une API sans avoir chargé sa doc.

**Exemples Corex :**
```
# Avant d'implémenter HardwareDetector
use context7 → System.Management (ManagementObjectSearcher, WQL syntax)
use context7 → CommunityToolkit.Mvvm (ObservableProperty, RelayCommand)
use context7 → WinUI3 NavigationView (pour Corex.App)
use context7 → xUnit (Trait, Theory, InlineData)
```

**Pourquoi :** élimine les hallucinations d'API. Une signature inventée = build cassé + debug inutile.

---

### 6 · Superpowers TDD — Exécution
**Quand :** APRÈS PAUL Plan + Context7. Jamais avant.

**Cycle obligatoire — dans cet ordre strict :**

```
CLARIFY   →  Poser toutes les questions ambiguës AVANT de coder
PLAN      →  Lister les classes, interfaces, méthodes à créer
TEST      →  Écrire les tests xUnit complets (rouges au premier run)
IMPLEMENT →  Code minimal qui fait passer les tests — rien de plus
VERIFY    →  dotnet test vert + dotnet format --verify-no-changes
```

**Exemple Corex — Feature F01 HardwareDetector :**
```
CLARIFY : "Le GPU intégré Intel doit-il être détecté en même temps que le GPU dédié NVIDIA ?"
PLAN    : IHardwareDetector + HardwareDetectionService + WmiQuery + CpuInfo/GpuInfo/RamInfo/StorageInfo
TEST    : DetectGpu_OnNvidiaCard_ReturnsNvidiaVendor · DetectGpu_OnIntelIntegrated_ReturnsIntelVendor
IMPLEMENT : ManagementObjectSearcher avec using + cache Lazy<HardwareProfile>
VERIFY  : dotnet test --filter "Category=Unit" → vert
```

---

## PAUL Unify — Clôture de session

**Déclenché par :** fin de session volontaire OU Headroom à 70%.

**Ce que Unify produit :**
- Résumé des décisions prises dans la session
- Mise à jour `.claude/backlog.md` (features terminées / en cours)
- Liste des points ouverts pour la prochaine session
- Mémorisation via Claude Memory

**Template Unify :**
```
PAUL Unify — session [date]
Terminé : [liste features / étapes]
Décisions : [liste des choix techniques actés]
Points ouverts : [ce qui reste + blocages]
Prochaine session : [objectif exact]
```

---

## Session type

```
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
```

---

## Anti-patterns workflow

```
❌ Ouvrir Claude Code et coder directement sans PAUL Plan
❌ Utiliser ManagementObjectSearcher sans avoir chargé la doc System.Management via Context7
❌ Écrire du code avant d'avoir écrit les tests (TDD = tests d'abord)
❌ Ignorer Headroom et continuer quand le contexte se dégrade
❌ Fermer une session sans PAUL Unify → décisions perdues
❌ Activer 5+ rôles gstack simultanément → context overflow
❌ Merger dans dev sans CI verte
```
