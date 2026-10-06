# Trinos Sporenmesser

Ein lokales Windows-Programm zum manuellen Vermessen von Pilzsporen auf Mikroskopbildern.

Von **Mario Tripodi** mit Unterstützung künstlicher Intelligenz (KI) entwickelt.

## Funktionen

- Gedrehte Messrechtecke: Längsachse ziehen, anschließend die Breite festlegen.
- Messungen auswählen, verschieben, korrigieren, löschen und rückgängig machen.
- Kalibrierung mit einem Maßstabsbalken; Referenzen zur späteren Verwendung speichern.
- Messreihen aus mehreren Bildern, mit dem jeweiligen Maßstab pro Bild.
- Länge, Breite, Quotient und geschätztes Volumen; statistische Auswertung.
- Stacheln als eigene Linienmessungen mit getrennter Auswertung.
- Export als TXT und CSV sowie beschriftete Bildkopien als JPG und PNG.
- Verschiebbare Maßbeschriftungen und einstellbare Nachkommastellen.

Originalbilder bleiben unverändert. Bilder und Messungen werden lokal verarbeitet.

## Benutzung

Die vollständige Anleitung befindet sich im Ordner [docs](docs/).
Die aktuelle vorbereitete Version ist **1.5.1026.2**.

Das Programm nutzt Windows Forms und .NET Framework. Die ausgelieferte EXE benötigt keinen eigenen Installer.
Linux/Wine ist nicht offiziell unterstützt: Je nach Umgebung wurden Grafikfehler gemeldet.

## Selbst erstellen

Unter Windows mit installiertem .NET-Framework-C#-Compiler im Projektordner ausführen:

```powershell
.\build.ps1
```

Die EXE wird im Ordner `dist` erzeugt. Es werden keine zusätzlichen Bibliothekspakete heruntergeladen.
Der GitHub-Workflow erstellt denselben Quellcode automatisch unter Windows und stellt die EXE als Build-Artefakt bereit.

## Messwerte und Statistik

Die Genauigkeit hängt von Bildqualität, korrekter Kalibrierung und manueller Auswahl ab.
Das Volumen wird als Rotationsellipsoid geschätzt: `V = pi / 6 * L * B²`; die Tiefe wird gleich der Breite angenommen.
Die 95-%-Mittelwertintervalle beruhen auf der Student-t-Verteilung und der Stichproben-Standardabweichung.
Die ausgegebenen angenäherten Populationsgrenzen sind `Mittelwert ± t * Standardabweichung`;
sie sind kein exaktes statistisches 95/95-Toleranzintervall.

## Entwicklung

Rückmeldungen und Verbesserungsvorschläge sind willkommen. Bitte bei Fehlern Programmversion,
Betriebssystem und die Schritte zum Auftreten nennen. Beispielbilder nur einstellen, wenn sie veröffentlicht werden dürfen.

Bekannt: Stacheln und Sporen verwenden in dieser Version noch einen gemeinsamen Nummernvorrat.
Eine unabhängige Nummerierung der Stacheln ist für eine folgende Änderung vorgesehen.

## Signierung

Die aktuelle Version ist **nicht digital signiert**. Eine Aufnahme bei der SignPath Foundation ist geplant,
aber noch nicht beantragt oder zugesagt. Die Veröffentlichung des Quellcodes allein beseitigt keine Windows-Warnungen.

## Lizenz

Dieses Projekt steht unter der **GNU General Public License, Version 3 (GPL-3.0-only)**.
Der vollständige Lizenztext steht in [LICENSE](LICENSE).
Weitergegebene bearbeitete Versionen müssen ebenfalls die GPLv3 einhalten und ihren entsprechenden Quellcode verfügbar machen.
Kommerzielle Weitergabe ist unter diesen Bedingungen ebenfalls erlaubt.

Copyright (C) 2026 Mario Tripodi. Bereitgestellt ohne Gewährleistung gemäß der Lizenz.

## Kontakt

Mario Tripodi · [mario.tripodi1974@gmail.com](mailto:mario.tripodi1974@gmail.com)
