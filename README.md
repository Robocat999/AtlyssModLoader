# AtlyssModLoader
The AtlyssModLoader is a project encompassing both a code injector and a mod loader for the game ATLYSS by Kiseff. Wishlist [ATLYSS on Steam](https://store.steampowered.com/app/2768430/ATLYSS/)!

The AtlyssLoaderInjector is used to splice in the startup code for the AtlyssModLoader in to ATLYSS at runtime. This is neccsary for the AtlyssModLoader to function.

The AtlyssModLoader uses the Harmony library to patch in .dll files located in a designated mod folder.

Note that the AtlyssModLoader is designed with the Patreon builds in mind. While it should work with the demo, be aware that suppourt is not targeted at that version of the game.

## Intial Setup
1. Download the latest AtlyssModdingPackage from the __releases tab__.
2. Unzip the file in to your ATLYSS folder. It is often found in a directory such as "C:\SteamLibrary\steamapps\common\ATLYSS".
3. Ensure the AtlyssLoaderInjector executable is *directly* in the ATLYSS folder. It should be in the same folder as "ATLYSS.exe". 
4. Ensure the AtlyssModLoader.dll was placed in "ATLYSS/ATLYSS_Data/Managed" directory. Several suppourting .dlls should will also be present.
5. Open the AtlyssLoaderInjector folder, and launch "AtlyssLoaderInjector.exe". Follow its instructions.
6. Open ATLYSS to generate the Mods folder. Alternativly, create the folder yourself *directly* in the ATLYSS folder.

Once the intial setup is complete, the mod loader will load any .dll files in the Mods folder in to the game. These .dll should be mods you understand and trust.

Note that any updates to ATLYSS will require a rerun of injector. Just repeat step 5. Don't worry about accidently injecting too many times, as the injector does a check to prevent that.

## Creating Mods
Please see the [Wiki](https://github.com/Robocat999/AtlyssModLoader/wiki) for information on creating mods.

## Current State of AtlyssModLoader
The AtlyssModLoader can do the following:
- Load .dll files (mods) in to the game at startup.
- Load ordering through a JSON file.

The AtlyssModLoader is at present fairly bare bones. However, the following are recognized as core features and *may* be added in future updates:
- In-game mode of managing active mods and their load order.
- In-game indicator of mods being loaded at game start-up.
- Convient ways to find and download mods.

Please recognize that this project is done in unpaid freetime as pleased. No feature is a guarantee.

## Community
Get offical updates on Atlyss in the [Offical KisSoft Discord](https://discord.com/invite/vScP5yUbtk)!

Intested in modding? Join us in the [Unoffical Community Discord Server](https://discord.gg/NWCqdufJ3X)!
  
Try out ATLYSS on [itch.io](https://kiseff.itch.io/atlyss) or [Steam](https://store.steampowered.com/app/2768430/ATLYSS/)! 
  Please wishlist on Steam, it's a great help!
  
Want to see more? Get in on early testing by subscribing to Kissef on [Patreon](https://www.patreon.com/Kiseff)! Patreon builds are a load of fun and see frequent updates! Plus, subscribers get access to exclusive discord channels!

This project is *unofficial*, please do not bother the developer about this project. 
