# MacroPad

Macro pad DIY (ATmega32U4) et son application hôte Windows (.NET / Avalonia).

## Versions du pad

- **v2** : 10 touches + écran OLED + 2 encodeurs
- **v1** : 12 touches, sans écran

## Fonctionnalités

- Pages de configuration avec bascule manuelle ou automatique selon l'application au premier plan
- Actions : raccourci clavier, lancer un programme, ouvrir une URL, requête HTTP, changer de page
- **Sonar (SteelSeries)** : volume et mute des channels, changement de sortie/micro
  (channels : master, game, chatRender, media, aux, chatCapture)
- **Discord** : mute, deafen
- **Soundboard** : jouer un son, mute, tout arrêter, volume
- Rechargement à chaud de `config.json`

## Structure

- `firmware/` : firmware Arduino
- `host-app/MacroPad.Host/` : application hôte
- `installer/` : script Inno Setup

## Compilation

    dotnet publish -c Release -r win-x64

Puis compiler `installer/MacroPad-Setup.iss` avec Inno Setup.

## Configuration

- `config.json` : pages et assignations (modifiable via la fenêtre de configuration)
- `secrets.json` : identifiants Discord (jamais versionné)

    {
      "discordClientId": "...",
      "discordClientSecret": "..."
    }