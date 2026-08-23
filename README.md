# Fabrikbau-Prototyp (MonoGame)

## Aufbau

- **Engine/** – wiederverwendbare Engine-Bausteine (unabhängig vom konkreten Spiel):
  - `Transform.cs` – Position/Rotation/Skalierung
  - `GameObject.cs` – Basisklasse für alle Objekte
  - `OrbitCamera.cs` – Kamera zum Drehen/Zoomen/Verschieben
  - `VoxelGrid.cs` – Bauraster, Belegung, Welt-/Rasterkoordinaten
- **Buildings/** – die eigentlichen Spielobjekte (Miner, Belt, Storage)
- **Game1.cs** – verbindet Engine + Spiel: Rendering, Eingabe, Fabriksimulation
- **Program.cs** – Einstiegspunkt

Die Engine kennt nichts von "Miner" oder "Erz" – sie stellt nur Kamera, Transform
und Raster bereit. Das eigentliche Spiel (`Buildings/`, `Game1.cs`) baut darauf auf.
Willst du beides strikt trennen, verschiebe `Engine/` später einfach in ein eigenes
Class-Library-Projekt und referenziere es aus `FactoryGame`.

## Setup in Visual Studio

1. **.NET 8 SDK** installieren, falls noch nicht vorhanden.
2. MonoGame-Templates installieren (einmalig, in einer Konsole):
   ```
   dotnet new install MonoGame.Templates.CSharp
   ```
3. In einem Terminal **genau in den Ordner wechseln, in dem `FactoryGame.csproj` liegt**
   (z. B. `cd G:\Coding\Github\NexaForge`) und das Content-Build-Tool lokal einrichten:
   ```
   dotnet new tool-manifest
   dotnet tool install --local dotnet-mgcb
   dotnet tool restore
   ```
   Wichtig: **keine** eigene `dotnet-tools.json` von Hand anlegen oder mit `Version="3.8.*"`
   befüllen – Tool-Manifeste akzeptieren keine Wildcard-Versionen (im Gegensatz zu normalen
   NuGet-`PackageReference`-Einträgen im `.csproj`). `dotnet new tool-manifest` legt die Datei
   automatisch korrekt unter `.config\dotnet-tools.json` an, und `dotnet tool install` trägt
   die passende, aktuell existierende Version selbst ein.
4. Ordner `FactoryGame` als Projektmappe/Projekt in Visual Studio öffnen
   (Datei → Öffnen → Projekt/Projektmappe → `FactoryGame.csproj` wählen).
5. NuGet-Pakete werden beim ersten Build automatisch wiederhergestellt
   (`MonoGame.Framework.DesktopGL`, `MonoGame.Content.Builder.Task`).
6. F5 drücken zum Starten.

Falls Visual Studio den Projekttyp nicht automatisch erkennt: Rechtsklick auf die
Projektmappe → "Vorhandenes Projekt hinzufügen" → `FactoryGame.csproj`.

Falls der Build weiterhin wegen des SpriteFonts fehlschlägt: prüfe, dass unter
`.config\dotnet-tools.json` (relativ zur `.csproj`, **mit** Punkt-Ordner) tatsächlich ein
Eintrag für `dotnet-mgcb` mit einer konkreten Versionsnummer (keine `*`) steht. Liegt die
Datei stattdessen im Projekt-Wurzelverzeichnis ohne `.config`-Unterordner, lösche sie und
wiederhole Schritt 3.

## Steuerung

| Aktion | Taste/Maus |
|---|---|
| Kamera drehen | rechte Maustaste halten + bewegen |
| Zoom | Mausrad |
| Kamera verschieben | W A S D |
| Miner auswählen | 1 |
| Förderband auswählen | 2 |
| Lager auswählen | 3 |
| Gebäude platzieren | Linksklick |
| Gebäude entfernen | Rechtsklick |
| Beenden | ESC |

## Prozedurale Welt

Beim Start ruft `Game1.Initialize()` `_grid.GenerateOreDeposits(seed: 12345)` auf.
Das nutzt `Engine/NoiseGenerator.cs` (einen einfachen, deterministischen Hash-Noise),
um über das Raster verteilte, unregelmäßige Erzflecken zu erzeugen – sichtbar als
orange/braune Flächen auf dem grünen Boden. Jede Zelle hat eine begrenzte Erzmenge.

Willst du bei jedem Start eine andere Welt: `seed: 12345` z. B. durch
`seed: Environment.TickCount` ersetzen. Für "unendliches" Terrain (statt eines festen
24x24-Rasters) müsste `VoxelGrid` erweitert werden, um Chunks bei Bedarf nachzuladen.

## Wie die Fabriklogik funktioniert

- Ein **Miner** fördert Erz nur, wenn unter ihm tatsächlich ein Erzvorkommen liegt
  (siehe oben) – die Ressource ist endlich und wird beim Abbau weniger.
- Steht direkt östlich (+X) ein **Band**, fließt das geförderte Erz automatisch drauf.
- Bänder fördern in ihre `Direction` zur Nachbarzelle weiter (Band → Band → Lager).
- Ein **Lager** sammelt alles bis zur Kapazitätsgrenze.
- Aktuell fördern Bänder nur in Richtung +X – als Erweiterung könntest du z.B.
  im Spiel eine Taste zum Drehen der Bandrichtung (`belt.Direction`) einbauen.

## GUI

Oben links zeigt eine Infoleiste die gelagerte Erzmenge und die aktuelle Auswahl.
Unten links gibt es drei klickbare Buttons (Miner/Band/Lager) mit weißem Rahmen um
die aktive Auswahl – alternativ funktionieren weiterhin die Tasten 1/2/3. Klicks auf
die GUI platzieren keine Gebäude in der Welt (wird per `IsMouseOverUI` abgefangen).

## Mögliche nächste Schritte

- Bandrichtung per Taste drehen (R) statt fest +X
- Statt Farbwürfeln echte 3D-Modelle laden (`Content.Load<Model>`)
- Verschiedene Ressourcentypen (Erz, Kohle, Platten, ...) mit eigenen Erz-Schichten
- Rezepte/Fabriken, die mehrere Rohstoffe zu einem Produkt kombinieren
- Größere/mehrere Chunks für "unendliches" prozedurales Terrain
- Speichern/Laden des Spielstands (z. B. als JSON)

Hinweis: Der Code wurde von Hand geschrieben und nicht in dieser Umgebung kompiliert
(kein Zugriff auf NuGet hier). Struktur und API-Nutzung folgen dem aktuellen
MonoGame-3.8-Standard, ein kurzer Testbuild in Visual Studio ist trotzdem sinnvoll.
