# Build Tweaks

A client-side BepInEx mod for Valheim: removing building pieces with the hammer costs no stamina, and their materials go straight into your inventory.

Mod para Valheim (BepInEx, client-side): remover peças com o martelo não gasta stamina, e os materiais vão direto para o inventário.

For players, see [package/README.md](package/README.md). That file is also the Thunderstore page.
Para jogadores, veja [package/README.md](package/README.md).

Download: [Thunderstore](https://thunderstore.io/c/valheim/p/DJAventuras/BuildTweaks/) · [Nexus Mods](https://www.nexusmods.com/valheim/mods/4898) · [GitHub Releases](https://github.com/Felihens/BuildTweaks/releases)

Sister mods / mods irmãos: [Gather Tweaks](https://github.com/Felihens/GatherTweaks), [Build Tweaks](https://github.com/Felihens/BuildTweaks), [DigForHoe](https://github.com/Felihens/DigForHoe), [Resource Scanner](https://github.com/Felihens/ResourceScanner), [Ore Weak Spots](https://github.com/Felihens/OreWeakSpots), [Tree Weak Spots](https://github.com/Felihens/TreeWeakSpots).

## Building

`sh build.sh` compiles `src/*.cs` against the game's assemblies and installs the dll into `BepInEx/plugins/BuildTweaks/` (`sh build.sh nocopy` only compiles). Then `powershell -File pack.ps1` builds `dist/BuildTweaks-<version>.zip` from `package/`.

## License

MIT
