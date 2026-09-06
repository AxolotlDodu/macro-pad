; ============================================================
; MacroPad.Host - Script Inno Setup
; ============================================================
; A adapter :
;   - MyPublishDir : dossier de sortie de `dotnet publish`
;   - MyAppVersion : version affichee dans l'installeur
;   - AppId (GUID) : genere le tien une fois et ne le change plus
;     (Tools > Generate GUID dans l'IDE Inno Setup)
;
; Config Discord : le Client ID / Client Secret ne sont JAMAIS embarques
; dans ce script ni dans le Setup.exe compile. Ils sont demandes a
; l'utilisateur pendant l'installation (page custom ci-dessous) et
; ecrits directement dans {app}\secrets.json sur sa machine.
; C'est le meme principe que les plugins StreamDeck/Discord existants :
; chaque utilisateur cree sa propre application sur le portail
; developpeur Discord et n'en partage jamais les identifiants.
; ============================================================

#define MyAppName "MacroPad"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Yohan"
#define MyAppExeName "MacroPad.Host.exe"

; Dossier contenant le resultat de `dotnet publish` (exe + dll + config.json + Assets\...)
#define MyPublishDir "..\host-app\MacroPad.Host\bin\Release\net10.0\win-x64\publish"

[Setup]
AppId={{B1F0B6B7-6E1C-4C1A-9B0A-8E1C9C0B2A11}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=.\Output
OutputBaseFilename=MacroPad-Setup-{#MyAppVersion}
Compression=lzma2
SolidCompression=yes
SetupIconFile=..\host-app\MacroPad.Host\Assets\tray.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern
WizardImageFile=wizard-image.bmp,wizard-image-125.bmp,wizard-image-150.bmp,wizard-image-200.bmp
WizardSmallImageFile=wizard-image-small.bmp,wizard-image-small-125.bmp,wizard-image-small-150.bmp,wizard-image-small-200.bmp
PrivilegesRequired=lowest
; Mets "admin" a la place de "lowest" si tu decides que le host doit tourner
; en administrateur pour SharpHook sur fenetres elevees.

[Languages]
Name: "french"; MessagesFile: "compiler:Languages\French.isl"

[Tasks]
Name: "desktopicon"; Description: "Créer un raccourci sur le Bureau"; GroupDescription: "Raccourcis :"; Flags: unchecked
Name: "startupicon"; Description: "Lancer MacroPad automatiquement au démarrage de Windows"; GroupDescription: "Démarrage :"; Flags: unchecked

[Files]
; Tout le contenu publié, sauf config.json et secrets.json qu'on gère à part.
; secrets.json n'est JAMAIS embarqué : voir section [Code] plus bas, qui l'écrit
; directement sur la machine de l'utilisateur à partir de sa propre saisie.
Source: "{#MyPublishDir}\*"; DestDir: "{app}"; Excludes: "config.json,secrets.json"; Flags: ignoreversion recursesubdirs createallsubdirs

; config.json : copié seulement s'il n'existe pas déjà (ne jamais écraser la config utilisateur)
Source: "{#MyPublishDir}\config.json"; DestDir: "{app}"; Flags: onlyifdoesntexist

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Configuration {#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Parameters: "--config"
Name: "{group}\Désinstaller {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

; Lancement au démarrage de Windows via le dossier Startup (plus simple à gérer/nettoyer qu'une clé Run)
Name: "{userstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: startupicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Lancer {#MyAppName} maintenant"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Nettoie le token Discord mis en cache à la désinstallation (contient un access_token)
Type: files; Name: "{app}\discord_token.json"
; Ne supprime PAS secrets.json à la désinstallation : si l'utilisateur réinstalle,
; il n'a pas besoin de ressaisir ses identifiants Discord. Décommente si tu préfères
; tout nettoyer à chaque désinstallation :
; Type: files; Name: "{app}\secrets.json"

[Code]
var
  DiscordPage: TInputQueryWizardPage;

procedure InitializeWizard;
begin
  DiscordPage := CreateInputQueryPage(wpSelectDir,
    'Configuration Discord (optionnel)',
    'Identifiants de ton application Discord',
    'Pour activer le contrôle du mute/deafen Discord depuis le MacroPad, crée une ' +
    'application sur https://discord.com/developers/applications, ajoute ' +
    'https://localhost/ comme Redirect URI dans OAuth2, puis renseigne ses identifiants ' +
    'ci-dessous.' + #13#10 + #13#10 +
    'Ces identifiants restent uniquement sur ta machine, dans secrets.json, et ne sont ' +
    'jamais inclus dans cet installeur.' + #13#10 + #13#10 +
    'Laisse les deux champs vides pour ignorer l''intégration Discord (tu pourras la ' +
    'configurer plus tard en éditant secrets.json manuellement).');

  DiscordPage.Add('Client ID :', False);
  DiscordPage.Add('Client Secret :', True); // True = champ masqué (affiché en ****)
end;

function EscapeJsonString(const S: string): string;
begin
  // Echappement minimal suffisant pour des identifiants Discord (pas de guillemets/antislash attendus,
  // mais on securise quand meme au cas ou l'utilisateur colle quelque chose d'inattendu).
  Result := S;
  StringChangeEx(Result, '\', '\\', True);
  StringChangeEx(Result, '"', '\"', True);
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ClientId, ClientSecret, JsonContent, SecretsPath: string;
begin
  if CurStep = ssPostInstall then
  begin
    ClientId := Trim(DiscordPage.Values[0]);
    ClientSecret := Trim(DiscordPage.Values[1]);

    if (ClientId <> '') and (ClientSecret <> '') then
    begin
      JsonContent :=
        '{' + #13#10 +
        '  "discordClientId": "' + EscapeJsonString(ClientId) + '",' + #13#10 +
        '  "discordClientSecret": "' + EscapeJsonString(ClientSecret) + '"' + #13#10 +
        '}';

      SecretsPath := ExpandConstant('{app}\secrets.json');
      SaveStringToFile(SecretsPath, JsonContent, False);
    end;
  end;
end;
