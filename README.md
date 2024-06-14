# SpotifyMod

![License](https://img.shields.io/badge/license-MIT-blue.svg)

## Overview

SpotifyMod is a fun and convenient integration built on top of the ZML template mod, designed to give users more control over their Spotify music without the need for additional hardware like stream decks. This mod is specifically tailored for Spotify Premium users, providing easy access to playback controls and playlist management within the game.

## Features

- **Playback Control**: Start, pause, skip, and go to the previous song.
- **Volume Control**: Increase and decrease the volume.
- **Playlist Management**: Display the "Up Next" playlist and play random songs from your library.
- **Spotify Authentication**: Authenticate with your Spotify Premium account to enable all features.

## Requirements

- **Spotify Premium**: This mod requires a Spotify Premium account to function.
- **ZML Template Mod**: The mod is built on the ZML template mod and requires it to be installed.
- **ZML File**: This mod won't work unless you have the `.zm` file posted in the ZML Discord server. This source file is for those who want to see how to create it for BepInEx. All you have to do is create the GUI etc.

## Installation

1. **Download the Mod**: Clone or download this repository.
2. **Add References**:
   - From the `Drift Racing Online_Data/Managed` folder:
     - `Assembly-CSharp.dll`
     - `UnityEngine.dll`
     - `UnityEngine.CoreModule.dll`
   - From the `ZML/core` folder:
     - `ZML.API.dll`
3. **Build and Install**: Build the mod using your preferred IDE and place the compiled mod in the appropriate mods folder for your game.

## Usage

1. **Configure Spotify Credentials**:
   - Create a `config.json` file in the mods folder with your Spotify client ID and client secret:
     ```json
     {
       "ClientId": "your-spotify-client-id",
       "ClientSecret": "your-spotify-client-secret"
     }
     ```
2. **Launch the Game**: Start the game and load the mod.
3. **Authenticate with Spotify**: Use the "Spotify auth" button in the mod's UI to authenticate with your Spotify Premium account.
4. **Control Your Music**: Use the various buttons provided in the mod's UI to control playback, volume, and view your "Up Next" playlist.

## Key Bindings

- **Start Song**: Key binding to start the current song.
- **Pause Song**: Key binding to pause the current song.
- **Skip Song**: Key binding to skip to the next song.
- **Previous Song**: Key binding to go back to the previous song.
- **Increase Volume**: Key binding to increase the volume.
- **Decrease Volume**: Key binding to decrease the volume.

## Contributing

Feel free to fork this repository and contribute by submitting pull requests. Any enhancements, bug fixes, or general improvements are welcome.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Disclaimer

This mod is created for fun and is not affiliated with Spotify. It is intended for use by Spotify Premium users who want more control over their music while gaming.

---

Enjoy the mod and happy listening!
