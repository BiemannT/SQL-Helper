# CSharp-Template
Vorlage für ein .NET-Projekt in C#, bestehend aus einer Klassenbibliothek und einem zugehörigen Testprojekt.

# TODO
Nachfolgende Schritte müssen durchgeführt werden, nachdem dieses Template-Projekt angewendet wurde.

## Einstellungen GitHub-Repository
In den Repository Eigenschaften eine Beschreibung und Topics hinterlegen und die Anzeige **Deployments** deaktivieren.

In den allgemeinen Settings vom Repository unter **Pull Requests** nur die Option **Allow merge commits** aktivieren und als Standard Nachricht die Option **Pull request title** auswählen.
Zudem die Optionen **Automatically delete head branches**, **Include Git LFS objects in archives** und **Auto-close issues with emrged linked pull requests** aktivieren.

## Projektname ändern
Projektmappe in Visual Studio öffnen und im Projektmappen-Explorer die Haupt-Projektmappe auswählen.
Anschließend in den Eigenschaften den Projektnamen unter **Name** eintragen. Die Haupt-Projektmappen-Datei .slnx wird daraufhin umbenannt.
Nun müssen noch die einzelnen Projekte umbenannt werden. Hierzu gibt es von Visual Studio keine automatische Funktion, daher sind folgende Schritte notwendig:
1. Im Explorer die Ordnernamen der Projekte anpassen.
1. Die `*.csproj`-Dateien in den jeweiligen Ordnern umbenennen.
1. In der `.slnx`-Datei die Projektnamen und Pfade anpassen. Hierzu die Datei mit einem Texteditor öffnen und die entsprechenden Einträge anpassen.

## Projekteigenschaften ändern
Projektmappe in Visual Studio öffnen und die **Eigenschaften** des Hauptprojekts (Klassenbibliothek) öffnen.
- Unter **Anwendung** den **Assemblynamen** und den **Standardnamespace** anpassen.
- Unter **Paket** die **Paket-ID**, **Beschreibung**, **Repository-Url** und die **Tags** anpassen, ggf. das Jahr bei **Copyright** anpassen.

## Test Projekt anpassen
1. Projektmappe in Visual Studio öffnen und die **Eigenschaften** des Testprojekts öffnen.
1. Unter **Anwendung** den **Assemblynamen** und den **Standardnamespace** anpassen.
1. Projektverweis auf die Klassenbibliothek ergänzen

## Workflow `publish-nuget.yml` anpassen
Wenn das Projekt als NuGet Paket veröffentlicht werden soll, muss der Workflow `publish-nuget.yml` aktiviert und entsprechend angepasst werden. Der Pfad zum Projektordner wird  Angabe in der Umgebungsvariable `projFolder` angegeben. Die Angabe des Ordners erfolgt relativ zum Root-Verzeichnis des Repositories, ohne führenden Slash.