---
paths: ["src/Corex.App/**"]
---

# Règles — UI WinUI 3 (Corex.App)

## MVVM strict — zéro logique métier en code-behind

```csharp
// ✅ ViewModel — toute la logique ici
[ObservableProperty] private HardwareProfile? _profile;
[ObservableProperty] private bool _isLoading;
[RelayCommand] private async Task RefreshAsync(CancellationToken ct) =>
    Profile = await _detector.DetectAsync(ct);

// ❌ Code-behind .xaml.cs — interdit
private async void Button_Click(object sender, RoutedEventArgs e)
{
    Profile = await _detector.DetectAsync(); // ← jamais ici
}
```

ViewModels héritent de `ObservableObject` (CommunityToolkit.Mvvm).
Attributs : `[ObservableProperty]`, `[RelayCommand]`, `[NotifyCanExecuteChangedFor]`.

## Bindings — x:Bind uniquement (pas Binding)

```xml
<!-- ✅ x:Bind : compile-time, typé, performant -->
<TextBlock Text="{x:Bind ViewModel.Profile.Cpu.Vendor, Mode=OneWay}"/>
<Button Command="{x:Bind ViewModel.RefreshCommand}"/>
<ProgressRing IsActive="{x:Bind ViewModel.IsLoading, Mode=OneWay}"/>

<!-- ❌ Binding : runtime, non typé, interdit dans Corex -->
<TextBlock Text="{Binding Profile.Cpu.Vendor}"/>
```

## Naming des contrôles — PascalCase type+description

```xml
<Button x:Name="ApplyTweaksButton"/>
<ListView x:Name="TweakListView"/>
<TextBlock x:Name="HealthScoreText"/>
<ProgressRing x:Name="LoadingRing"/>
```

## Chargement async — toujours indiquer l'état

```xml
<!-- Afficher ProgressRing pendant les opérations async -->
<Grid>
    <StackPanel Visibility="{x:Bind ViewModel.IsContentVisible, Mode=OneWay}">
        <!-- contenu principal -->
    </StackPanel>
    <ProgressRing IsActive="{x:Bind ViewModel.IsLoading, Mode=OneWay}"
                  HorizontalAlignment="Center" VerticalAlignment="Center"/>
</Grid>
```

## Palette Corex (App.xaml — ne pas hardcoder dans les pages)

```xml
<!-- Couleurs Altysin — définies dans App.xaml uniquement -->
<!-- Primaire : #6B2FCC (violet Altysin) -->
<!-- Accent : #00D4AA -->
<!-- Ressources typographiques et thème dans App.xaml ou dictionnaires Resources/ -->
```

## Indicateurs de risque tweak en UI

```xml
<!-- 🟢 Safe -->  <Ellipse Fill="#22C55E" Width="8" Height="8"/>
<!-- 🟡 Modéré --> <Ellipse Fill="#F59E0B" Width="8" Height="8"/>
<!-- 🔴 Expert -->  <Ellipse Fill="#EF4444" Width="8" Height="8"/>
```

Tweaks 🔴 : afficher un dialogue de confirmation explicite avant d'activer.
Jamais activer un tweak Expert silencieusement.
