---
paths: ["Corex.Tests/**"]
---

# Règles — Tests xUnit (TDD obligatoire)

## Cycle Superpowers — ordre strict sans exception

```csharp
1. Clarify    → poser TOUTES les questions ambiguës AVANT d'écrire une ligne
2. Plan       → lister les cas de test, classes, interfaces à créer
3. Test       → écrire les tests complets (ils DOIVENT être rouges au premier run)
4. Implement  → code minimal qui fait passer les tests — rien de plus
5. Verify     → dotnet test vert + dotnet format --verify-no-changes
```text

Ne jamais passer à Implement sans que les tests soient écrits et compilent.

## Catégories obligatoires sur chaque test

```csharp
[Trait("Category", "Unit")]        // Tests sans dépendance externe — Moq uniquement
[Trait("Category", "Integration")] // Tests avec Registry réel, fichiers, services Windows
```

Filtrer en CI : `--filter "Category=Unit"` (CI rapide) | `--filter "Category=Integration"` (VM)

## Nommage — convention stricte

```text
[MethodName]_[Scenario]_[ExpectedResult]

DetectGpu_OnNvidiaCard_ReturnsNvidiaVendor
DetectStorage_OnNvmeDrive_ReturnsNvmeType
DisableSysMain_WhenStorageIsHdd_DoesNotApply
ApplyNvidiaTweak_WhenGpuIsAmd_SkipsSilently
Snapshot_WhenApplyFails_RollsBackAllChanges
```csharp

## Structure Arrange / Act / Assert

```csharp
[Fact]
[Trait("Category", "Unit")]
public async Task DetectGpu_OnNvidiaCard_ReturnsNvidiaVendor()
{
    // Arrange
    var wmiMock = new Mock<IWmiProvider>();
    wmiMock.Setup(w => w.QueryAsync("Win32_VideoController", It.IsAny<CancellationToken>()))
           .ReturnsAsync(HardwareFixtures.NvidiaRtx4070Wmi());
    var detector = new HardwareDetectionService(wmiMock.Object, Mock.Of<ILogger<HardwareDetectionService>>());

    // Act
    var result = await detector.DetectGpuAsync(CancellationToken.None);

    // Assert
    Assert.Equal(GpuVendor.Nvidia, result.Vendor);
    Assert.Equal("NVIDIA GeForce RTX 4070", result.Name);
}
```

## Fixtures dans Corex.Tests/Unit/Fixtures/

```csharp
public static class HardwareFixtures
{
    // Profils GPU — couvrir les 4 cas
    public static ManagementBaseObject[] NvidiaRtx4070Wmi()  => ...
    public static ManagementBaseObject[] AmdRx7900Wmi()      => ...
    public static ManagementBaseObject[] IntelArcA770Wmi()   => ...
    public static ManagementBaseObject[] IntelIrisXeWmi()    => ... // GPU intégré

    // Profils stockage — couvrir les 3 cas
    public static ManagementBaseObject[] NvmeSsdWmi()        => ...
    public static ManagementBaseObject[] SataSsdWmi()        => ...
    public static ManagementBaseObject[] HddWmi()            => ...
}
```

## Règle tweak — tester le cas SKIP systématiquement

```csharp
[Fact]
[Trait("Category", "Unit")]
public void DisableSysMain_WhenStorageIsHdd_DoesNotApply()
{
    // Un tweak conditionnel DOIT retourner false sur le mauvais hardware
    // Jamais d'exception — skip silencieux
    var hw = new HardwareProfile { PrimaryStorage = new StorageInfo("WD Blue", StorageType.Hdd, ...) };
    var tweak = new DisableSysMainTweak();

    Assert.False(tweak.IsApplicable(hw));
}
```

## DoD d'un test — Definition of Done

- [ ] `[Trait("Category", "Unit")]` ou `[Trait("Category", "Integration")]` présent
- [ ] Nommage `Method_Scenario_ExpectedResult`
- [ ] Arrange / Act / Assert séparés avec commentaires
- [ ] Cas SKIP testé pour chaque tweak conditionnel
- [ ] `dotnet test --filter "Category=Unit"` : vert sans hardware réel
- [ ] Coverage >80% sur le code nouveau du module
