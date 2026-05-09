# Hinweise zum Update 1.1
In diesem Update wurden folgende Änderungen vorgenommen:
+ Anpassung der Projekteinstellung für den Doku Export
+ Neuer GitHub Action Workflow zum Veröffentlichen von NuGet Paketen
+ Aktualisierung der `new-version.yml` Datei
+ Aktualisierung der `build-test.yml` Datei

## Anpassungen der Projekteinstellung für den Doku Export
Damit im exportierten NuGet-Paket die XML-Dokumentation enthalten ist, muss die Projekteinstellung entsprechend angepasst werden.
Ansonsten werden die Dokumentationen bei der Verwendung des Paketes nicht angezeigt.
Die Einstellung kann in Visual Studio unter den Projekteigenschaften im Reiter "Build" unter "Ausgabe" aktiviert werden, indem die Option "Dokumentationsdatei" aktiviert wird.
Eine extra Pfadangabe ist nicht notwendig, da die XML-Dokumentation automatisch im selben Verzeichnis wie die DLL-Datei des Paketes abgelegt wird.

## Workflow zum Veröffentlichen von NuGet Paketen
Der neue Workflow `publish-nuget.yml` ermöglicht es, NuGet Pakete direkt aus dem Repository heraus zu veröffentlichen.
Wenn das C#-Projekt eine Klassenbibliothek enthält, die als NuGet Paket veröffentlicht werden soll, kann dieser Workflow aktiviert werden.
Die Umgebungsvariable `projFolder` im Action Workflow muss entsprechend angepasst werden, um den Pfad zum Projektordner anzugeben. Die Angabe des Ordners erfolgt relativ zum Root-Verzeichnis des Repositories, ohne führenden Slash.

## Aktaulisierung der `new-version.yml` Datei
Über den Workflow `new-version.yml` wurde die Versionierung ohne vorangestelltes "v" erstellt. Dies führte im Publish Workflow zu Problemen, da die Versionierung nicht korrekt erkannt wurde. Durch die Anpassung der `new-version.yml` Datei wird nun die Versionierung ohne "v" erstellt, was die Kompatibilität mit dem Publish Workflow sicherstellt.

## Aktualisierung der `build-test.yml` Datei
In der `build-test.yml` Datei wurde ein Schritt ergänzt um den Action Runner mit der privaten GitHub Packages Registry zu authentifizieren. Dies ist notwendig, damit die Pakete aus GitHub Packages in den Tests verwendet werden können, wenn diese als Abhängigkeiten in den Projekten definiert sind.

## Verwendung von GitHub Packages in Visual Studio
Zur Verwendung von GitHub Packages ist zwingend die Authentifizierung über ein Personal Access Token (PAT) erforderlich.
Hierzu muss im GitHub-Profil ein Personal Access Token erstellt werden, das mindestens die Berechtigung "read:packages" besitzt. Dieses Token wird dann in Visual Studio als Anmeldeinformationen für den Zugriff auf GitHub Packages verwendet.

Es gibt zum einen die Möglichkeit über eine NuGet.Config Datei im Projektverzeichnis die Authentifizierung einzurichten. Hierbei muss jedoch beachtet werden, dass sensible Informationen in der Datei gespeichert werden, was ein Sicherheitsrisiko darstellen kann.
Alternativ kann die Authentifizierung auch über die Anmeldeinformationen in Visual Studio erfolgen, was eine sicherere Methode darstellt, da die Anmeldeinformationen nicht in einer Datei gespeichert werden. Leider bietet in Visual Studio der Paketmanager keine Möglichkeit, die Anmeldeinformationen für GitHub Packages einzugeben. Über die Kommandozeile kann dies jedoch erfolgen. Hierzu in Visual Studio eine Konsole öffnen und den folgenden Befehl ausführen:

```
dotnet nuget add source "https://nuget.pkg.github.com/USERNAME/index.json" --name "GitHubPrivat" --username USERNAME --password TOKEN
```

Damit stehen die Pakete aus GitHub Packages in Visual Studio genauso zur Verfügung wie Pakete aus anderen Quellen, z.B. NuGet.org.

## Zugriff in GitHub von anderen Repositories
Wenn Pakete in Workflows in anderen Repositories verwendet werden sollen ist ebenso eine Authentifizierung notwendig. Entweder kann dies auch über ein Personal Access Token (PAT) erfolgen, das in den Secrets des Repositories hinterlegt wird. In diesem Fall muss der Zugriff auf das Token in der `publish-nuget.yml` Datei entsprechend angepasst werden, damit die Umgebungsvariable `GITHUB_TOKEN` auf das hinterlegte Secret verweist.

Falls es sich um eigene Pakete handelt, die in einem privaten Repository liegen, kann auch die Authentifizierung über den `GITHUB_TOKEN` erfolgen, der automatisch in GitHub Actions zur Verfügung steht. Hierzu muss jedoch sichergestellt werden, dass das Repository, in dem die Pakete liegen, die entsprechenden Berechtigungen für den Zugriff auf die Pakete aus anderen Repositories hat. Dies kann in den Repository-Einstellungen unter "Packages" konfiguriert werden.

Nachdem das betreffende Paket erstmalig veröffentlicht wurde, kann der Zugriff in GitHub eingestellt werden. Hierzu auf der Hauptseite des Repositorys auf "Packages" klicken, das entsprechende Paket auswählen und unter "Package settings". Unter "Manage Actions access" kann das Repository ausgewählt werden, das Zugriff auf das Paket haben soll. Zur Verwendung des Pakets reicht für das ausgewählte Repository die Berechtigung "Read access", damit können die Pakete in den Workflows des ausgewählten Repositorys verwendet werden.