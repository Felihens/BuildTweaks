# Build Tweaks

**[English](#english) · [Português](#português)**

---

## English

Rebuilding is less of a chore. Removing a piece with the hammer costs no stamina, and its materials go straight into your inventory instead of dropping on the floor.

### Features

- **Client-side only.** The server and other players don't need the mod.
- **No stamina cost:** remove as many pieces as you like without running out of stamina.
- **Materials to inventory:** wood, stone, nails and everything else a removed piece gives back goes straight into your inventory. Materials fill up stacks you already have; only what doesn't fit anywhere drops on the ground.
- **Works on servers.** If another player's game controls the piece, the mod still collects the materials.
- Pieces broken by damage (monsters, weather, collapse) still drop their materials as usual.

### Requirements

- [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)

### Installation

**r2modman / Thunderstore Mod Manager:** click *Install*. To install a downloaded zip, go to Settings → *Import local mod*.

**Nexus Mods / Vortex:** also on [Nexus Mods](https://www.nexusmods.com/valheim/mods/4898).

**Manual:**
1. Extract the zip.
2. Copy the `plugins/BuildTweaks` folder into `Valheim/BepInEx/plugins/`.
   Result: `Valheim/BepInEx/plugins/BuildTweaks/BuildTweaks.dll`.
3. Launch the game.

To update, delete the old `BuildTweaks` folder before copying the new one.

### Configuration

Press **F1** in game (requires Configuration Manager) or edit `BepInEx/config/felih.BuildTweaks.cfg`.

| Option | Default | What it does |
|---|---|---|
| NoStaminaCost | on | Removing a piece costs no stamina. |
| MaterialsToInventory | on | Materials of a removed piece go straight into your inventory. |
| DetailedLog | off | Writes details to the BepInEx log (for bug reports). |

### Notes

- Works well with **Gather Tweaks** and **DigForHoe** (same author).

---

## Português

Reconstruir fica menos cansativo. Remover uma peça com o martelo não gasta stamina, e os materiais dela vão direto para o inventário em vez de cair no chão.

### Recursos

- **Só no seu jogo.** O servidor e os outros jogadores não precisam ter o mod.
- **Sem gastar stamina:** remova quantas peças quiser sem ficar sem stamina.
- **Materiais para o inventário:** madeira, pedra, pregos e tudo o mais que a peça devolve vai direto para o inventário. Os materiais completam as pilhas que você já tem; só o que não couber em lugar nenhum cai no chão.
- **Funciona em servidores.** Se é o jogo de outro jogador que controla a peça, o mod ainda pega os materiais.
- Peças quebradas por dano (monstros, clima, desabamento) continuam soltando os materiais como no jogo normal.

### Requisitos

- [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)

### Instalação

**r2modman / Thunderstore Mod Manager:** clique em *Install*. Para instalar um zip baixado, use Configurações → *Import local mod*.

**Nexus Mods / Vortex:** também está no [Nexus Mods](https://www.nexusmods.com/valheim/mods/4898).

**À mão:**
1. Extraia o zip.
2. Copie a pasta `plugins/BuildTweaks` para dentro de `Valheim/BepInEx/plugins/`.
   Resultado: `Valheim/BepInEx/plugins/BuildTweaks/BuildTweaks.dll`.
3. Abra o jogo.

Para atualizar, apague a pasta `BuildTweaks` antiga antes de copiar a nova.

### Configuração

Aperte **F1** no jogo (precisa do Configuration Manager) ou edite `BepInEx/config/felih.BuildTweaks.cfg`.

| Opção | Padrão | O que faz |
|---|---|---|
| NoStaminaCost | ligado | Remover uma peça não gasta stamina. |
| MaterialsToInventory | ligado | Os materiais da peça removida vão direto para o inventário. |
| DetailedLog | desligado | Escreve detalhes no log do BepInEx (para relatar bugs). |

### Observações

- Combina com o **Gather Tweaks** e o **DigForHoe** (mesmo autor).
