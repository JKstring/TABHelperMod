# They Are Billions Mod 示例项目

本项目是为《They Are Billions》游戏的 Mod 示例，旨在帮助玩家快速创建和加载自己的 Mod，并展示一些游戏特性。

## 快速创建和加载一个 Mod

### 步骤
1. 创建一个 `.NET Framework` DLL 项目，并引用 `TABModLoader.dll`。
   - 该 DLL 文件会在你订阅 `TAB Mod Loader` 并启动游戏后，自动解压至游戏根目录的 `/Mods` 文件夹中。

2. 定义一个继承自 `TABModLoader.ModBase` 抽象类的类，并实现 `OnLoad` 方法。
   - 该方法包含一个 `TABModLoader.ModInfos` 类型的参数，该参数包含所有 Steam 创意工坊信息，如标题、描述、作者、路径等。

3. 编译并生成 Mod DLL 文件，然后使用 Windows 右键菜单选择“发送到 -> 压缩(zipped)文件夹”，将 `.zip` 扩展名改为 `.jpg`。
   - 将生成的文件放置在 They Are Billions 地图项目文件夹中并上传至 Steam 创意工坊。

完成后，其他玩家只需订阅该 Mod，启动游戏时便会自动加载。

---

## 特别说明

- `ModInfos` 给出的路径为 Steam 创意工坊路径，并非 DLL 加载路径。要访问 DLL 的加载路径，它位于 `游戏根目录/Mods/ModInfos.SteamID/`。

- ModLoader 自带一些库，包括 `Harmony` 和 `Newtonsoft.Json`。**请不要将这些库与 Mod DLL 一起打包进 ZIP 文件中**，这些库会自动解压并加载，否则可能会造成重复加载。

- 游戏代码经过混淆处理，阅读较为困难。不过，我在 `References` 文件夹中提供了反混淆后的游戏程序 (`TheyAreBillions-modifiedVisibility.exe`)。您可以使用 `DNSpy` 等工具查看其代码。
   - 请注意，无法直接引用或访问该反混淆程序中的数据，因为原版程序将大部分内容标记为内部类，仅少部分为 Public。
   - 由于游戏保护机制未完全解除，无法替换原版程序，但可以使用反射访问这些数据。在使用 `Harmony` 修补时，可通过 `Traverse` 中的 `MethodWithDecrypt`、`PropertyWithDecrypt` 等扩展方法，以更高的可读性访问反射数据。使用时需导入 `TABModLoader.Utils` 命名空间。
   - 如有需要，可参考 `References` 文件夹中的混淆和反混淆字典文件（`TheyAreBillions-decrypted.exe.srcmap`）以实现类似功能。

- `DXVision.dll` 是游戏的重要 DLL，包含多个关键类的父类。此文件也在 `References` 文件夹中，并未被混淆，可直接引用访问。当遇到无法访问的子类时，也可尝试将其转换为 `DXVision.dll` 中的父类进行访问。

具体使用细节请参考 `TAB Helper Mod` 中的代码。

---

## 致谢
这是我目前的全部成果，希望我的经验可以帮助更多的开发者完善和优化《They Are Billions》的游戏内容，为玩家带来更丰富的体验。