# Corex — Modules

Référence rapide des 12 modules — scope par version et responsabilités techniques.

## Légende

- ✅ V1 MVP (mois 1–4)
- 🔜 V2 (mois 5–10)
- 🔮 V3 (mois 11–18+)
- ⛔ Hors scope

---

## M1 — Détection Hardware

**Statut V1 :** ✅ Complet — fondation bloquante de tout le reste

| Composant | Données détectées | Source | V1 |
|-----------|------------------|--------|-----|
| CPU | Fabricant, archi, génération, cœurs physiques/logiques, fréquence base/boost, TDP, temp, cache | WMI Win32_Processor | ✅ |
| GPU | Fabricant NVIDIA/AMD/Intel, modèle exact, VRAM, type mémoire, driver installé vs dispo, temp, usage | WMI Win32_VideoController + NVAPI/ADL | ✅ |
| RAM | Capacité, fréquence actuelle, XMP/EXPO dispo, slots occupés/libres, fabricant, timings, dual/single channel | WMI Win32_PhysicalMemory | ✅ |
| Stockage | Type NVMe/SATA SSD/HDD, modèle, santé SMART, TBW, remplissage, vitesses R/W | WMI + DeviceIoControl SMART | ✅ |
| Réseau | Type filaire/WiFi, fabricant, débit max, ping actuel, DNS utilisé | WMI Win32_NetworkAdapter | ✅ |
| OS | Version exacte, édition, archi, date install, uptime, BIOS/UEFI version, Secure Boot, TPM, mode boot | WMI Win32_OperatingSystem + Registry | ✅ |

**Classe principale :** `Corex.Core.Services.HardwareDetectionService`
**Cache :** 60 minutes, invalidé manuellement si changement hardware détecté
**Output :** `HardwareProfile` record — source de vérité pour tout le moteur de règles

---

## M2 — Optimisations Windows Système

**Statut V1 :** ✅ 50+ tweaks — cœur de valeur produit

### Sous-modules

**Confidentialité/Télémétrie** ✅ V1

- DiagTrack, dmwappushsvc, WER, CEIP, Windows Insider
- Télémétrie NVIDIA (NvTelemetryContainer), AMD (Crash Defender), Intel
- Historique activité, ID publicitaire, géolocalisation, Cortana cloud
- Sync paramètres cloud, presse-papier cloud, données manuscrites

**Confort/Interface** ✅ V1

- Effets visuels/animations Aero, transparence
- Store dans recherche, widgets W11, Copilot, OneDrive complet
- Souris gaming (accélération off, DPI constant)
- FSO par application, Xbox Game Bar (avec warning streaming)
- Edge (télémétrie, préchargement, démarrage fond)
- Debloat apps préinstallées (Candy Crush, Mixed Reality, Bing apps, Solitaire...)

**Performances** ✅ V1

- Plan alimentation Haute Performance / Performances Optimales
- Hibernation (libère hiberfil.sys jusqu'à 16+ Go)
- Apps arrière-plan Store
- SysMain/SuperFetch — conditionnel : activé si HDD, désactivé si NVMe rapide
- Windows Search indexing — conditionnel selon stockage
- Fast Startup (désactivé — cause conflits drivers)
- Win32PrioritySeparation (priorité premier plan)
- HPET — conditionnel selon CPU détecté
- Timer resolution 1ms — classé 🔴 Expert
- Core Parking — conditionnel AMD
- Pagefile optimisation selon RAM détectée

**Nettoyage** ✅ V1

- Prefetch, cache Windows, dossiers temp système/utilisateur
- Caches shaders DirectX, Vulkan, OpenGL, NVIDIA, AMD, Intel
- Winsock + pile TCP/IP reset
- Cache icônes, cache polices
- SFC /scannow, DISM /RestoreHealth
- TRIM SSD, défrag HDD (conditionnel — jamais défrag SSD)

**Classe principale :** `Corex.Core.Rules.WindowsTweakLibrary`
Chaque tweak implémente `ITweak` avec `IsApplicable(HardwareProfile)` obligatoire.

---

## M3 — Gestion Drivers

**Statut :** 🔜 V2

- Inventaire complet drivers installés
- Comparaison version installée vs stable disponible (scraping officiel NVIDIA/AMD/Intel)
- Distinction "dernière version" vs "version stable recommandée gaming"
- Désinstallation propre via DDU intégré ou externe
- Détection drivers corrompus / fantômes (ghosted devices)
- Suppression drivers fantômes
- Rollback 1 clic avec historique
- Sauvegarde locale avant mise à jour
- Planification hors session gaming
- Alerte si GPU driver >60 jours

---

## M4 — Désinstalleur Complet

**Statut V1 :** ✅ Core — différenciateur fort vs FPSDoctor

**V1 inclut :**

- Désinstallation standard + suppression résidus AppData (Local, Roaming, LocalLow), ProgramData, Program Files orphelins
- Nettoyage clés registre orphelines
- Nettoyage raccourcis orphelins (Bureau, Menu Démarrer, Barre des tâches)
- Suppression tâches planifiées liées à l'app
- Suppression services Windows créés par l'app
- Suppression entrées démarrage (Run, RunOnce)
- Rapport espace disque récupéré avant/après
- Log détaillé de tout ce qui a été supprimé

**V2 ajoute :**

- Désinstallation par lot (sélection multiple)
- Mode désinstallation forcée (apps sans uninstaller fonctionnel)
- Détection logiciels jamais lancés depuis >6 mois
- Détection logiciels en doublon
- Nettoyage variables d'environnement PATH orphelines

**Classe principale :** `Corex.Core.Services.UninstallerService`

---

## M5 — Réseau Avancé

**Statut V1 :** ✅ Basique (Winsock reset, TCP/IP reset, IPv6 conditionnel)
**Statut V2 :** 🔜 Complet

**V1 :**

- Réinitialisation Winsock + pile TCP/IP
- Désactivation économies énergie carte réseau
- IPv6 — conditionnel si cause latence

**V2 :**

- Benchmark DNS automatique (Cloudflare, Google, OpenDNS, Quad9, NextDNS, FAI locaux)
- DNS over HTTPS (DoH)
- Optimisation MTU selon connexion
- CTCP/CUBIC selon Windows
- TCP/IP (nagling, buffers, receive window)
- QoS gaming (DSCP, priorisation paquets)
- Analyse connexions sortantes par application
- Surveillance bande passante par processus
- Optimisation WiFi (scan auto off, bande 2.4/5/6 GHz)

---

## M6 — Optimisations Gaming Avancées

**Statut V1 :** ✅ Profil Gaming de base
**Statut V2 :** 🔜 Mode gaming auto + overlay

**V1 :**

- Profil Gaming : plan performance + tweaks GPU NVIDIA/AMD adaptatifs + réseau QoS basique
- Tweaks NVIDIA Control Panel par jeu (prefer max performance, shader cache, GSYNC)
- Tweaks AMD Radeon Software (Anti-lag, Chill off, image sharpening)
- FSO désactivation par jeu

**V2 :**

- Mode Gaming auto au lancement d'un jeu détecté (Steam, Epic, GOG, Battle.net, EA App, Xbox, Ubisoft)
- Profils par jeu avec tweaks moteur spécifiques (Unreal, Unity, Source 2, id Tech)
- Overlay FPS/températures/RAM intégré (sans MSI Afterburner)
- Précompilation shaders, nettoyage caches shaders corrompus
- Gestion affinité CPU par jeu
- Priorisation processus jeu (Real-Time / High selon jeu)
- Désactivation notifications + Windows Update pendant session gaming

**V3 :**

- Intégration Steam API — bibliothèque jeux + tweaks spécifiques top 100 jeux
- DLSS/FSR/XeSS — guide activation par jeu

---

## M7 — Monitoring et Alertes

**Statut V1 :** ✅ Dashboard basique + alertes critiques
**Statut V2 :** 🔜 Historique 30j + alertes avancées

**V1 :**

- Dashboard temps réel CPU/GPU/RAM/stockage
- Alertes critiques : SSD santé SMART dégradée, CPU temp repos >55°C, RAM >85% repos
- Analyse apps démarrage avec score d'impact boot chiffré
- Score santé PC 0–100
- Benchmark rapide CPU/RAM/SSD (avant/après)

**V2 :**

- Historique températures 24h/7j/30j
- Détection dégradation thermique progressive (pasta à refaire, ventilateurs)
- Prédiction durée de vie SSD (TBW restants + rythme écriture actuel)
- Alertes proactives : SSD >80%, driver GPU >60j, cache shaders >2 Go, boot >45s
- Rapport hebdomadaire santé système
- Test stabilité RAM intégré (MemTest léger non destructif)
- Gestionnaire services catégorisé par risque

---

## M8 — Audit Sécurité

**Statut :** 🔮 V3

- Vérification Secure Boot, TPM 2.0, Defender, pare-feu, Windows Update
- Détection PUP et adware
- Gestionnaire extensions navigateur Chrome/Firefox/Edge avec score risque
- Analyse apps démarrage suspectes
- Détection accès caméra/micro non justifié
- Vérification certificats racines Windows
- Analyse connexions sortantes suspectes
- Aucun logiciel tiers de sécurité installé — rapport uniquement

---

## M9 — Profils d'Usage Intelligents

**Statut V1 :** ✅ Profil Gaming de base
**Statut V2 :** 🔜 VM + Créatif complets

**Gaming V1 :** Plan performance + tweaks GPU adaptatifs + réseau QoS
**VM V2 :** Scheduler CPU équitable, RAM dynamique, isolation I/O, gestion Hyper-V/VMware/VirtualBox/QEMU
**Créatif V2 :** Adobe cache SSD, GPU accéléré, priorité processus Adobe, DAW optimization (FL Studio, Ableton, Reaper, Pro Tools — ASIO, buffer audio, MMCSS)

---

## M10 — Transparence et Restauration

**Statut V1 :** ✅ Complet — fondation de confiance

- Pour chaque tweak : clé Registry exacte, valeur avant/après, gain attendu chiffré, indicateur risque 🟢🟡🔴
- Snapshot Windows automatique avant chaque session
- Log horodaté complet avec valeur originale conservée
- Bouton "Tout restaurer" permanent dans le dashboard
- Restauration sélective tweak par tweak
- Mode Expert déblocable pour tweaks 🔴 (double confirmation)
- Export rapport modifications en PDF (V2)

**Classe principale :** `Corex.Core.Services.SnapshotService`

---

## M11 — Gamification et Rétention

**Statut V1 :** ✅ Score santé + basique
**Statut V2 :** 🔜 Succès complets + rapport avant/après

- Score santé PC 0–100 mis à jour dynamiquement ✅ V1
- Système succès thématisé Altysin (bronze/argent/or/platine) 🔜 V2
- Progression visible optimisations appliquées ✅ V1
- Rapport avant/après avec gains mesurés (FPS, boot time, RAM libérée, espace récupéré) ✅ V1
- Notifications nouvelles optimisations disponibles 🔜 V2

---

## M12 — Écosystème Altysin

**Statut :** 🔮 V3

- Intégration site Altysin : config détectée → composants obsolètes → configurateur pré-rempli
- Configurateur PC : usage + budget → config optimisée + liens affiliés (Amazon, LDLC, Materiel.net)
- IA contextuelle hardware-aware (connaît la config, répond avec contexte réel machine)
- Programme affilié créateurs (liens trackés + commission)
- API publique Corex pour intégrateurs
