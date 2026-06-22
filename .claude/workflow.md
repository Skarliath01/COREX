# Corex — Workflow gstack → PAUL → Superpowers

## Principe

```
gstack   →   PAUL/GSD   →   Superpowers
Décision      Stabilité      Exécution
"quoi faire"  "ne pas        "comment faire"
              dériver"
```

**Ne jamais sauter une couche.** Coder sans avoir décidé quoi faire = dette. Décider sans ancrer l'état = drift. Exécuter sans plan = régression.

---

## Couche 1 — gstack (Décision)

### Quand l'utiliser
- Avant d'attaquer un nouveau module
- Quand une décision d'architecture a des implications long terme
- Quand tu hésites entre deux approches techniques
- Avant de changer la priorité d'une feature du backlog

### Commandes clés

```
/plan-eng-review    → Revue d'architecture — "Est-ce que mon design tient ?"
/plan-ceo-review    → Décision produit — "Est-ce que cette feature vaut le coût ?"
/office-hours       → Question ouverte multi-rôle (CEO + archi + QA simultané)
```

### Exemple Corex

```
Session gstack pour démarrer M1 :
→ /plan-eng-review : "Je veux détecter le GPU via WMI + NVAPI en C#.
   Mon fallback si NVAPI indisponible est WMI uniquement.
   Est-ce que l'interface IHardwareDetector est bien découpée ?"

Résultat attendu :
- Validation ou refactoring de l'interface
- Identification des edge cases (GPU Intel + NVIDIA en même temps ?)
- Décision sur le cache (durée, invalidation)
```

### Limite gstack

Chaque rôle gstack injecte ses propres prompts — activer trop de rôles simultanément consomme 10K+ tokens avant qu'une ligne de code soit écrite. Rester ciblé : 1–2 rôles max par session.

---

## Couche 2 — PAUL (Stabilité)

### Quand l'utiliser
- Au début de chaque session de dev pour recharger l'état
- Quand une longue session dérive de l'objectif initial
- Après une pause de plusieurs jours
- Quand les décisions s'accumulent et le contexte devient flou

### Ce que PAUL ancre pour Corex

```
État courant du backlog (features terminées / en cours / bloquées)
Décisions d'architecture prises (ex: "on utilise records immuables pour HardwareProfile")
Contraintes actives (ex: "timer resolution classé Expert, double confirmation requise")
Objectif de la session en cours
```

### Template de prompt PAUL pour démarrer une session

```
PAUL context load — Corex by Altysin

État backlog :
- Features terminées : [01, 02]
- En cours : [03 - Snapshot service]
- Bloqué : []

Décisions récentes :
- HardwareProfile = record immuable (pas de mutation)
- WMI cache 60min, invalidation manuelle uniquement
- Snapshot stocké en JSON GZip dans %APPDATA%\Altysin\Corex\Snapshots\

Objectif session aujourd'hui :
- Finir SnapshotService avec rollback sélectif
- Écrire les tests unitaires correspondants

Contrainte active :
- Toute modification système = snapshot AVANT, sans exception
```

---

## Couche 3 — Superpowers (Exécution)

### Process Superpowers — toujours dans cet ordre

```
1. CLARIFY    → Vérifier qu'on comprend exactement ce qui est demandé
2. PLAN       → Écrire le plan d'implémentation avant de coder
3. TDD        → Écrire les tests d'abord
4. IMPLEMENT  → Implémenter pour faire passer les tests
5. VERIFY     → Vérifier que tout passe, rien de cassé
```

### Exemple Corex — Feature 03 (SnapshotService)

**Étape 1 — Clarify**
```
Q: Le snapshot doit-il capturer UNIQUEMENT les clés modifiées,
   ou toutes les clés des hives HKLM\SOFTWARE et HKCU ?
R: Uniquement les clés qui seront modifiées par les tweaks sélectionnés.
   Pas de snapshot global — trop lourd et risque d'exposition de données sensibles.
```

**Étape 2 — Plan**
```
SnapshotService plan :
1. CreateAsync(label) → crée un snapshot vide, retourne ISnapshot
2. ISnapshot.RegisterKey(hive, path, valueName) → enregistre la valeur actuelle
3. ITweakEngine appelle RegisterKey avant chaque modification
4. ISnapshot.CommitAsync() → marque le snapshot comme complet, sauvegarde JSON GZip
5. ISnapshot.RollbackAsync() → restaure chaque valeur enregistrée dans l'ordre inverse
6. SnapshotManager.ListAsync() → retourne les 10 derniers snapshots
7. SnapshotManager.RestoreAsync(id) → charge et exécute le rollback d'un snapshot archivé
```

**Étape 3 — TDD**
```csharp
// Tests AVANT le code
[Fact]
public async Task CreateSnapshot_ShouldCaptureRegistryValue()
{
    // Arrange
    var service = new SnapshotService(_logger, _storage);

    // Act
    var snapshot = await service.CreateAsync("test-snapshot");
    await snapshot.RegisterKeyAsync(RegistryHive.LocalMachine, @"SOFTWARE\Test", "Value");

    // Assert
    Assert.Single(snapshot.Entries);
    Assert.Equal(@"SOFTWARE\Test", snapshot.Entries[0].Path);
}

[Fact]
public async Task Rollback_ShouldRestoreOriginalValue()
{
    // Arrange : modifier une valeur, créer snapshot avec valeur d'avant
    // Act : rollback
    // Assert : valeur restaurée
}
```

**Étape 4 — Implement**
Code pour faire passer les tests — pas plus.

**Étape 5 — Verify**
```bash
dotnet test --filter "Category=Unit&Class=SnapshotServiceTests"
# Tous au vert → PR vers develop
```

---

## Session type journalière

```
09:00  PAUL load → charger l'état, définir l'objectif du jour
09:10  gstack /plan-eng-review si décision archi nécessaire
09:30  Superpowers : Clarify → Plan → TDD → Implement → Verify
12:00  Commit WIP sur feature branch
14:00  Superpowers : suite ou nouvelle feature
17:00  PAUL update → noter les décisions prises, mettre à jour backlog.md
17:15  Commit final + push
```

---

## Anti-patterns à éviter

```
❌ Coder directement sans passer par PAUL au début de session
   → On répare des bugs qu'on avait déjà corrigés la semaine passée

❌ Utiliser gstack pour de l'exécution (lui demander d'écrire du code)
   → Consomme des tokens pour rien, c'est le rôle de Superpowers

❌ Skiper le TDD "parce que c'est un petit fix"
   → Le SnapshotService s'est cassé exactement sur un "petit fix"

❌ Merger dans main sans CI vert
   → Règle absolue, sans exception

❌ Activer 5+ rôles gstack simultanément
   → Context overflow, réponses génériques inutiles
```

---

## Checklist de fin de session

- [ ] `backlog.md` mis à jour avec le statut des features
- [ ] PAUL context sauvegardé avec les décisions du jour
- [ ] Commits pushés sur la feature branch
- [ ] Aucun TODO laissé sans issue créée
- [ ] Tests au vert sur les features touchées
