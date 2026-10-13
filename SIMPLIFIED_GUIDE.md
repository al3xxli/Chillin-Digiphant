# How to Download and Play DigiPhant Kart

DigiPhant Kart is a game created in Unity. You don't need programming experience to play, but you will need to install a few things on your computer first.

The game currently opens through Unity rather than as a regular computer app. Once you have everything installed, you can return to Unity whenever you want to play.

These instructions are for 64-bit Windows computers. Mac and Linux computers require a different setup.

## Step 1: Install Unity

Unity is the program that runs DigiPhant Kart.

1. Go to [the Unity download page](https://unity.com/download).

1. Click Download Unity Hub.

1. Open the downloaded file and follow the instructions to install it.

1. Open Unity Hub and create a free account if asked.

1. Click Installs, then Install Editor.

1. Install Unity version 6000.6.3f1. If you don't see it listed, look for it in the Unity Download Archive.

Make sure you install this exact version because it is the version used to create the game.

## Step 2: Install Git

Git is a program that helps your computer download the files needed for the game.

1. Go to [the Git download page](https://git-scm.com/downloads).

1. Select Windows and download the installer.

1. Open the downloaded file.

1. Follow the instructions. You can keep the default settings.

You won't need to open Git after installing it.

## Step 3: Download DigiPhant Kart

1. Go to [the DigiPhant Kart project on GitHub](https://github.com/al3xxli/Chillin-Digiphant).

1. Click the green Code button.

1. Select Download ZIP.

1. Open your Downloads folder.

1. Find the ZIP file you downloaded.

1. Right-click the file and select Extract All.

1. Click Extract.

A new folder will appear containing the game files.

Open that folder. You should see folders named Assets, Packages, and ProjectSettings.

If you don't see these, open the folder inside it until you find them.

This is the main DigiPhant Kart folder. You'll need it for the next steps.

## Step 4: Prepare the Game

Before opening DigiPhant Kart, you need to run a setup file. This installs the elephant and the additional software that allows the camera to recognize your movements.

1. Open the main DigiPhant Kart folder.

1. Make sure you can see the file named Setup-DigiPhant.ps1.

1. Click the address bar at the top of the folder window, where the folder location is displayed.

1. Type powershell and press Enter.

1. A PowerShell window will open.

1. Copy the following command, paste it into the window, and press Enter.

```powershell
.\Setup-DigiPhant.ps1
```

Your computer will begin preparing the game.

Stay connected to the internet and wait until the process finishes.

If Windows displays a message saying the script is blocked by an execution policy, use this command instead:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Setup-DigiPhant.ps1
```

Only run the setup file from the DigiPhant Kart project you intentionally downloaded.

Before moving on: Make sure the setup finishes without reporting a failure. If an error appears, don't close the PowerShell window. The error message can help identify what needs to be fixed.

## Step 5: Open the Game in Unity

1. Open Unity Hub.

1. Click Add, then Add project from disk.

1. Find the main DigiPhant Kart folder you downloaded.

1. Select the folder containing Assets, Packages, and ProjectSettings.

1. Open the project using Unity version 6000.6.3f1.

Unity will now load the game.

The first time you open it, Unity may need several minutes to download additional files and prepare the project. Keep your internet connection on and wait for the loading process to finish.

If Unity reports errors, allow the import process to finish before trying to fix anything.

## Step 6: Open the Racetrack

Once Unity finishes loading, find the Project panel, usually near the bottom of the screen.

Open these folders in order:

Assets → StudentWork → Scenes

Find the file named Digiphant-Kart and double-click it.

Next, look at the menu at the top of Unity and select:

DigiPhant → Kart → Validate scene

This checks whether the scene is set up correctly.

If Unity reports a problem, review the message before continuing.

When everything is ready, click the Play button (the triangle at the top of Unity).

The game should now appear in the Game window.

## Step 7: Try the Game with Your Keyboard

We recommend testing the game with your keyboard before trying the camera controls.

1. Select Keys from the game menu.

1. Click inside the Game window.

1. Press Enter to start the race.

1. Use the arrow keys to move and steer.

1. Press E to jump and hold C to duck.

If your elephant moves, turns, and jumps, the basic game is working.

## Step 8: Set Up the Camera for Team Play

To play with three people, you will need a working webcam.

First, make sure Windows allows the game to use your camera.

1. Open Windows Settings.

1. Select Privacy & security.

1. Select Camera.

1. Make sure camera access is turned on.

1. Make sure Let desktop apps access your camera is turned on.

Close any other programs that might be using your webcam, such as Zoom or Teams.

Return to DigiPhant Kart.

1. Select Camera.

1. Choose 3 players.

1. Select Body + shoulders + hand.

1. Position the players from left to right as shown in the camera preview.

1. Make sure Player 1's shoulders, hands, and hips, Player 2's shoulders, and Player 3's hand are visible. Player 1's feet do not need to be visible.

1. Click Start with Camera.

The game will begin a 10-second countdown.

During this time, everyone should remain still with their hands down, feet planted, and shoulders relaxed. This allows the game to recognize each player's resting position.

Once the countdown finishes, the race begins.

## Troubleshooting

### The game won't open in Unity.

Make sure you installed Unity version 6000.6.3f1 and selected the correct project folder.

### The elephant is missing.

The setup may not have finished installing the elephant files. Return to Step 4 and check for errors.

### Unity displays error messages.

Wait until Unity finishes loading. If the errors remain, check that the setup completed successfully and that you're using the correct Unity version.

### The camera isn't working.

Check your Windows camera permissions. Make sure your webcam is connected and close any other programs that might be using it.

### The game doesn't recognize your movements.

Make sure everyone is visible in the camera preview. Check the lighting and player positions. You can also use the camera settings to adjust sensitivity and recalibrate the controls.

### The keyboard doesn't respond.

Make sure you selected Keys mode and clicked inside the Game window before pressing the keys.

### The setup file won't run.

Check that you're using Windows PowerShell, that you opened it in the correct folder, and that you entered the command exactly as shown in Step 4.

For more detailed help, visit the [installation and troubleshooting guide](INSTALL.md).

## Opening the Game Again

Once the game is installed, you don't need to repeat the installation steps.

To play again:

1. Open Unity Hub.

1. Select your DigiPhant Kart project.

1. Wait for Unity to open.

1. Open Assets → StudentWork → Scenes → Digiphant-Kart if the racetrack isn't already open.

1. Click Play.

1. Choose Keys or Camera mode and start racing.

## Additional Information

- [Game files](https://github.com/al3xxli/Chillin-Digiphant)

- [Detailed controls](Assets/StudentWork/Kart/README.md)

- [Technical installation instructions](INSTALL.md)

[Back to the project README](README.md)
