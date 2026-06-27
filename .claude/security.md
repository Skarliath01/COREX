# Corex — Sécurité

## Signature EV Authenticode

### Pourquoi c'est non négociable dès V1

Sans signature EV :

- SmartScreen bloque l'installeur avec popup rouge "Windows a protégé votre ordinateur"
- Defender peut mettre en quarantaine l'exécutable
- Malwarebytes, ESET, Kaspersky signalent des faux positifs quasi-systématiquement
- L'utilisateur doit ignorer 2–3 avertissements pour installer — taux de conversion désastreux

Avec signature EV : zéro SmartScreen, zéro faux positif AV sur les builds propres.

### Fournisseurs recommandés

| Fournisseur | Prix approximatif | Délai |
|------------|-----------------|-------|
| Sectigo (ex-Comodo) | ~300–400 €/an | 2–5 jours ouvrés |
| DigiCert | ~400–500 €/an | 1–3 jours ouvrés |
| GlobalSign | ~350–450 €/an | 2–5 jours ouvrés |

> Préférer Sectigo pour le rapport qualité/prix. Le certificat EV nécessite une vérification d'identité (passeport + justificatif entreprise ou auto-entrepreneur).

### Processus de signature dans le CI

Voir `cicd.md` — workflow release. En résumé :

1. Certificat stocké en base64 dans GitHub Secrets (`EV_CERT_BASE64`)
2. Mot de passe dans `EV_CERT_PASSWORD`
3. Signature via `signtool.exe` avec horodatage RFC 3161 (Sectigo TSA)
4. **Tous** les exécutables et DLL signés — pas uniquement l'installeur
5. Vérification automatique post-signature avant packaging

### Règles de signature

```powershell
✅ Signer : Corex.App.exe, Corex.Native.dll, CorexSetup.exe
✅ Toujours avec horodatage (le cert expire mais la signature reste valide)
✅ SHA256 pour digest ET contresignature
❌ Ne jamais distribuer un binaire non signé, même en bêta privée
```csharp

---

## Antivirus — Whitelisting et prévention faux positifs

### Causes fréquentes de faux positifs

| Cause | Notre approche |
|-------|---------------|
| Obfuscation code | Zéro obfuscation, zéro packing |
| Hook kernel non documenté | Uniquement WinAPI et WMI documentés |
| Modification Registry système | Normal pour un optimiseur — signature EV compense |
| Accès services Windows | Via API SC Manager documentée uniquement |
| Téléchargement et exécution de code | Updates signés, hash SHA256 vérifié avant exécution |

### Soumission AV labs (avant chaque release majeure)

Soumettre l'installeur signé aux labs suivants :

- **Avast** : <https://www.avast.com/submit-sample.php>
- **ESET** : <https://www.eset.com/int/submit-sample/>
- **Kaspersky** : <https://opentip.kaspersky.com/>
- **Malwarebytes** : <https://forums.malwarebytes.com/forum/122-false-positives/>
- **Windows Defender** : <https://www.microsoft.com/wdsi/filesubmission>

Délai moyen de whitelisting : 24–72h selon le lab.

### VirusTotal

Uploader systématiquement sur VirusTotal avant chaque release.
Objectif : 0/72 détections. Acceptable en V1 bêta : <3/72.
Lien VirusTotal dans les release notes pour la transparence.

---

## RGPD — Collecte de données

### Principe fondamental

**Le hardware est détecté localement et ne quitte jamais la machine sans consentement explicite.**

### Ce qui reste local (toujours)

- Profil hardware complet (CPU, GPU, RAM, stockage, réseau)
- Liste des tweaks appliqués
- Snapshots Registry
- Logs de modifications
- Score santé PC

### Ce qui est envoyé au backend (opt-in uniquement)

| Donnée | Condition | Format |
|--------|-----------|--------|
| Heartbeat de licence | Validation licence active | Hash machine anonyme, tier, version app |
| Vérification updates | Automatique | Version actuelle uniquement |
| Analytics agrégées | Opt-in explicite case à cocher install | Données anonymisées, jamais individuelles |

### Hash machine anonyme

```csharp
// Généré une seule fois, stocké localement
// Basé sur un identifiant hardware stable, jamais reversible
private string GenerateMachineHash()
{
    var rawId = GetMotherboardSerial() + GetCpuId();
    using var sha256 = SHA256.Create();
    var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawId));
    return Convert.ToHexString(bytes)[..32]; // 32 chars, anonyme
}
```

### Privacy Policy — points obligatoires

- Données collectées listées exhaustivement
- Durée de rétention explicite (licences : durée abonnement + 1 an)
- Droit d'accès, rectification, suppression (bouton dans le logiciel)
- Pas de vente à des tiers — jamais
- Hébergement en UE (OVH / Scaleway recommandés pour la conformité RGPD)
- DPO : auto-entrepreneur = responsable direct, email dédié `privacy@altysin.fr`

---

## Snapshot Policy

### Règle absolue

```text
Toute modification du système (Registry, services, tâches planifiées, fichiers système)
DOIT être précédée d'un snapshot. Cette règle n'est pas bypassable, même en mode Expert.
```text

### Ce qu'un snapshot contient

```csharp
public record SystemSnapshot
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public string Label { get; init; } = string.Empty;
    public List<RegistryEntry> RegistryEntries { get; init; } = [];
    public List<ServiceState> Services { get; init; } = [];
    public List<ScheduledTaskState> ScheduledTasks { get; init; } = [];
    public List<StartupEntry> StartupEntries { get; init; } = [];
    public string WindowsBuildVersion { get; init; } = string.Empty;
}
```

### Stockage des snapshots

- Localisation : `%APPDATA%\Altysin\Corex\Snapshots\`
- Format : JSON compressé (GZip)
- Rétention : 10 derniers snapshots conservés, au-delà suppression automatique du plus ancien
- Taille estimée : 50–200 Ko par snapshot

### Restauration

```text
Restauration complète  → toutes les entrées du snapshot, dans l'ordre inverse d'application
Restauration sélective → une entrée spécifique (Registry key ou service)
Restauration d'urgence → disponible même si l'UI ne démarre pas (outil CLI séparé)
```powershell

### Outil CLI de restauration d'urgence

Un exécutable séparé `CorexRestore.exe` installé dans `Program Files\Altysin\Corex\` :

```powershell
CorexRestore.exe --list              # Liste tous les snapshots
CorexRestore.exe --restore <id>      # Restaure un snapshot complet
CorexRestore.exe --restore-last      # Restaure le plus récent
```text

Utilisable depuis WinPE ou en mode sans échec si le PC ne démarre plus correctement.

---

## Élévation UAC

L'application requiert les droits administrateur pour fonctionner.

```xml
<!-- app.manifest -->
<requestedExecutionLevel level="requireAdministrator" uiAccess="false" />
```

**Justification :** Modification Registry HKLM, gestion services Windows, accès WMI système.

**Pas d'élévation à la volée** — l'app est lancée en admin dès le départ. Pas de pattern "lancer en user puis demander UAC pour chaque tweak" — UX dégradée et source de bugs.

---

## Checklist sécurité avant release

- [ ] Signature EV valide sur tous les binaires (signtool verify)
- [ ] VirusTotal : <3 détections
- [ ] Soumission AV labs si changement de comportement réseau/registry
- [ ] Aucune donnée hardware envoyée sans consentement (test Wireshark)
- [ ] Snapshot créé et restauration testée end-to-end
- [ ] Privacy Policy à jour avec les changements de la version
- [ ] Aucune clé API ou secret dans le code source (git grep)
- [ ] Dépendances NuGet vérifiées (dotnet list package --vulnerable)
