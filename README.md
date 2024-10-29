# They Are Billions Mod Sample Project

This project provides a sample Mod for *They Are Billions*, designed to help players quickly create and load their own Mods, as well as demonstrate some of the game’s features.

*[中文版README请点击这里](#中文说明)*

## Quick Guide to Creating and Loading a Mod

### Steps
1. Create a `.NET Framework` DLL project and reference `TABModLoader.dll`.
   - This DLL file will automatically extract to the `/Mods` folder in the game's root directory once you subscribe to the `TAB Mod Loader` and launch the game.

2. Define a class that inherits from the abstract class `TABModLoader.ModBase` and implement the `OnLoad` method.
   - This method takes a `TABModLoader.ModInfos` parameter that contains Steam Workshop information such as title, description, author, path, etc.

3. Compile and generate the Mod DLL file, then use the Windows right-click menu to select "Send to -> Compressed (zipped) folder." Change the `.zip` extension to `.jpg`.
   - Place the generated file in your *They Are Billions* map project folder and upload it to the Steam Workshop.

Once complete, other players need only subscribe to the Mod, and it will automatically load upon game start.

---

## Special Notes

- The `ModInfos` path provided is for Steam Workshop, not the DLL load path. To access the DLL load path, it is located at `GameRoot/Mods/ModInfos.SteamID/`.

- The ModLoader comes with libraries such as `Harmony` and `Newtonsoft.Json`. **Please avoid packaging these libraries with the Mod DLL in the ZIP file**, as they will automatically extract and load. Including them could cause duplicate loading issues.

- The game code is obfuscated and difficult to read. However, i provide a de-obfuscated game executable (`TheyAreBillions-modifiedVisibility.exe`) in the `References` folder, which can be explored using tools like `DNSpy`.
   - Please note, direct reference or access to this de-obfuscated code is limited, as most of the content in the original program is marked as internal, with only a few public classes available.
   - Due to the game’s protection mechanism, you cannot replace the original program with this de-obfuscated one, but you can access data through reflection. When using `Harmony` for patches, you can use the `MethodWithDecrypt` and `PropertyWithDecrypt` extensions in `Traverse` for better readability. To do so, import the `TABModLoader.Utils` namespace.
   - For custom dictionary handling of obfuscated and de-obfuscated names, refer to the mapping file (`TheyAreBillions-decrypted.exe.srcmap`) in the `References` folder.

- `DXVision.dll` is an important DLL in the game, containing several parent classes for key classes. This file is also available in the `References` folder and is not obfuscated, so you can directly reference and access it. If you encounter inaccessible subclasses, consider converting them to the parent classes in `DXVision.dll` or examine them using tools like `DNSpy`.

For specific usage details, please refer to the code in the `TAB Helper Mod`.

---

## Acknowledgements
This is my current progress, and I hope my experience will help other developers enhance and optimize *They Are Billions*' content, providing players with a richer gaming experience.