# Pipeline CI/CD Unity

## Arborescence

```
.github/
  platforms.json              # LA config : plateformes, cibles Unity, canaux itch
  workflows/
    release.yml               # Point d'entree : build multi-plateformes + release GitHub
    build-unity.yml           # Reutilisable : construit UNE plateforme (runner self-hosted)
    publish-itch.yml          # Publication itch.io sur release publiee
Assets/Editor/
  BuildScript.cs              # Point d'entree du build Unity en batchmode
```

## Flux

```
Actions > Release  ->  plan (lit platforms.json)
                       build (matrice, 1 job par plateforme, runner self-hosted)
                       release (1 release GitHub, tous les zips)
                            |
                            v  (event: release published)
                       Publish to itch.io  ->  plan + push butler (1 canal par plateforme)
```

## Prerequis

### Sur le runner self-hosted (Windows)
- Git + Git LFS installes et dans le PATH.
- Unity Hub connecte, licence Personal active.
- La version Unity du projet installee via le Hub, emplacement par defaut
  (`C:\Program Files\Unity\Hub\Editor\<version>\`), avec les modules des plateformes ciblees.
- Runner enregistre avec les labels `self-hosted` et `windows`.
- Repo prive (un repo public exposerait la machine via les pull requests).

### Cote GitHub
Secrets (Settings > Secrets and variables > Actions > Secrets) :
- `BUTLER_API_KEY` : cle API itch.io (itch.io > Settings > API keys).
- `ANDROID_KEYSTORE_PASS`, `ANDROID_KEYALIAS_PASS` : seulement pour Android signe.

Variables (meme page, onglet Variables) — optionnelles :
- `ITCH_USER` : pseudo itch.io si different du proprietaire du repo.
- `ITCH_GAME` : slug itch.io si different du nom du repo (itch attend des minuscules).

### Cote projet Unity
- Les scenes a inclure doivent etre cochees dans File > Build Settings.
- `ProjectSettings/` doit etre versionne, notamment `EditorBuildSettings.asset`.

## Utilisation

1. Actions > **Release** > Run workflow.
2. `version` : vide pour `v0.0.<numero de run>`, ou une version explicite (`0.1.0`).
3. `platforms` : `all`, ou une liste (`windows,webgl`).
4. `prerelease` : coche par defaut.

La publication de la release declenche automatiquement **Publish to itch.io**.
Ce workflow peut aussi etre relance seul, avec un tag precis ou vide pour la derniere release.

## Ajouter une plateforme

Passer son `enabled` a `true` dans `.github/platforms.json`, et installer le module
correspondant sur le runner. Aucun workflow a modifier.

Pour une plateforme absente du fichier, ajouter une entree :

```json
{ "name": "mac", "unityTarget": "StandaloneOSX", "itchChannel": "osx", "enabled": true }
```

Un `itchChannel` a `null` construit la plateforme sans la publier sur itch.

## Ajouter un canal de distribution (Steam, serveur perso...)

Creer un workflow sur `release: published`, sur le modele de `publish-itch.yml`.
Le pipeline de build n'est pas a modifier : les workflows de publication ne consomment
que les zips de la release.

## Limites connues

- `build-unity.yml` suppose un runner **Windows** (PowerShell + chemin Unity Windows).
  Pour un runner Linux ou macOS, prevoir une variante du script de build.
- Les zips sont produits avec `System.IO.Compression.ZipFile` et non `Compress-Archive`,
  qui duplique les dossiers vides et fait rejeter l'archive par butler.
- Deux runs successifs avec la meme version mettent a jour la release existante
  au lieu d'en creer une nouvelle.
