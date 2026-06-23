# TODOS

## Hardware Detection (M1)

- [ ] **[P1] Integration tests** — `[Category=Integration]` avec profils `NvidiaGamingRig`, `AmdRig`, `LaptopDualGpu` sur VMs réelles avant dev→staging
- [ ] **[P1] Primary disk selection** — Utiliser `MSFT_Disk.IsBoot` plutôt que la taille pour identifier le disque système (évite NVMe boot + HDD stockage → mauvais type détecté)
- [ ] **[P2] Registry GPU index** — Mapper `HardwareInformation.qwMemorySize` par `DriverDesc` plutôt que par index ordinal pour garantir le bon slot PnP (OEM systems)
- [ ] **[P2] CancellationToken propagation** — Plomber `ct` dans les appels WMI synchrones (actuellement `Task.Run(DetectCpu, ct)` ne cancelle pas l'exécution en cours)
- [ ] **[P3] RefreshAsync V2** — Implémenter le vrai refresh WMI (invalider cache + re-détecter) — V1 no-op intentionnel

## Completed

<!-- Items complétés lors du merge avec version et date -->
