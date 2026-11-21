# Build Tweaks

**[English](#english) · [Português](#português)**

---

## English

Building is less of a chore. Removing a piece with the hammer costs no stamina and its materials go straight into your inventory. Chests, furniture and other pieces without snap points now snap, you can turn pieces to many more angles, and the fires you built stay lit and keep you warm.

### Features

- **Client-side only.** The server and other players don't need the mod.
- **No stamina cost:** remove as many pieces as you like without running out of stamina.
- **Materials to inventory:** wood, stone, nails and everything else a removed piece gives back goes straight into your inventory. Materials fill up stacks you already have; only what doesn't fit anywhere drops on the ground.
- **Works on servers.** If another player's game controls the piece, the mod still collects the materials.
- Pieces broken by damage (monsters, weather, collapse) still drop their materials as usual.
- **Snapping for everything:** chests, furniture, the workbench and the other pieces that never snapped get snap points at the corners of their box and the middle of their bottom and top. A chest snaps beside or on top of another, on the corner of a floor or centred on a beam. The game's own snap points don't change. Pick which point of the piece snaps with **Q** / **E** as usual.
- **Snaps from a bit farther:** 0.8 m instead of the game's 0.5 m.
- **More rotation angles:** each turn of the mouse wheel turns the piece 11.25° (32 angles instead of 16, every angle of the game included). Hold **Left Alt** to turn 1° at a time. The angle shows under the crosshair.
- **Fires stay lit:** built fires within 50 m of you (torches, sconces, braziers, campfires, hearths...) that burn out are filled up again for free. Fires you turned off on purpose stay off.
- **Every fire warms you:** any lit built fire warms you like a campfire (same distance), and so does the torch in your hand. Only you feel it.
- **"(owned)" on station upgrades:** while you place a station upgrade (chopping block, tanning rack, forge bellows...), its name at the bottom of the screen shows **(owned)** when the nearest station of its kind already has it, so you don't build the same one twice.

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
| SnapEverything | on | Pieces without snap points get some (restart the game after changing). |
| NoSnapPieces | (empty) | Pieces (prefab names, comma separated) that stay without snap points. |
| SnapDistance | 0.8 | How close a snap point must come to snap, in metres (game: 0.5). |
| MoreAngles | on | Turn pieces in RotationStep degrees. |
| RotationStep | 11.25 | Degrees per turn of the mouse wheel (game: 22.5). |
| FineRotationKey | Left Alt | Hold it to turn in FineRotationStep degrees. |
| FineRotationStep | 1 | Degrees per turn while the key is held. |
| ShowAngle | on | Shows the angle under the crosshair after each turn. |
| AutoLight | on | Built fires near you that burn out are filled up again for free. |
| AutoLightRange | 50 | How far from you (metres) fires are kept lit. |
| LightsWarmYou | on | Every lit built fire warms you like a campfire (restart after changing). |
| TorchInHandWarmsYou | on | The torch in your hand warms you like a campfire. |
| OwnedTag | on | Station upgrades show "(owned)" when the nearest station of their kind already has them. |
| DetailedLog | off | Writes details to the BepInEx log (for bug reports). |

### Notes

- Works well with **Gather Tweaks** and **DigForHoe** (same author).

---

## Português

Construir fica menos cansativo. Remover uma peça com o martelo não gasta stamina e os materiais vão direto para o inventário. Baús, móveis e outras peças sem pontos de encaixe agora encaixam, dá para girar as peças em muito mais ângulos, e os fogos que você construiu ficam acesos e te aquecem.

### Recursos

- **Só no seu jogo.** O servidor e os outros jogadores não precisam ter o mod.
- **Sem gastar stamina:** remova quantas peças quiser sem ficar sem stamina.
- **Materiais para o inventário:** madeira, pedra, pregos e tudo o mais que a peça devolve vai direto para o inventário. Os materiais completam as pilhas que você já tem; só o que não couber em lugar nenhum cai no chão.
- **Funciona em servidores.** Se é o jogo de outro jogador que controla a peça, o mod ainda pega os materiais.
- Peças quebradas por dano (monstros, clima, desabamento) continuam soltando os materiais como no jogo normal.
- **Encaixe para tudo:** baús, móveis, a bancada e as outras peças que nunca encaixavam ganham pontos de encaixe nos cantos e no meio de baixo e de cima. Um baú encaixa ao lado ou em cima de outro, no canto de um piso ou centralizado numa viga. Os pontos do próprio jogo não mudam. Escolha qual ponto da peça encaixa com **Q** / **E**, como sempre.
- **Encaixa de um pouco mais longe:** 0,8 m em vez dos 0,5 m do jogo.
- **Mais ângulos de rotação:** cada clique da rodinha gira a peça 11,25° (32 ângulos em vez de 16, incluindo todos os do jogo). Segure **Alt esquerdo** para girar de 1° em 1°. O ângulo aparece embaixo da mira.
- **Fogos continuam acesos:** fogos construídos a até 50 m de você (tochas, arandelas, braseiros, fogueiras, lareiras...) que se apagam são reabastecidos de graça. Fogos que você apagou de propósito continuam apagados.
- **Todo fogo aquece:** qualquer fogo construído aceso te aquece como uma fogueira (mesma distância), e a tocha na mão também. Só você sente.
- **"(owned)" nas melhorias de bancada:** ao colocar uma melhoria de bancada (cepo, varal de couro, fole da forja...), o nome dela embaixo da tela mostra **(owned)** quando a bancada mais próxima daquele tipo já tem essa melhoria, pra você não construir a mesma duas vezes.

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
| SnapEverything | ligado | Peças sem pontos de encaixe ganham alguns (reinicie o jogo depois de mudar). |
| NoSnapPieces | (vazio) | Peças (nomes de prefab, separados por vírgula) que ficam sem pontos de encaixe. |
| SnapDistance | 0.8 | Quão perto o ponto precisa chegar para encaixar, em metros (jogo: 0,5). |
| MoreAngles | ligado | Gira as peças em RotationStep graus. |
| RotationStep | 11.25 | Graus por clique da rodinha (jogo: 22,5). |
| FineRotationKey | Alt esquerdo | Segure para girar em FineRotationStep graus. |
| FineRotationStep | 1 | Graus por clique com a tecla segurada. |
| ShowAngle | ligado | Mostra o ângulo embaixo da mira depois de cada giro. |
| AutoLight | ligado | Fogos construídos perto de você que se apagam são reabastecidos de graça. |
| AutoLightRange | 50 | Até que distância de você (metros) os fogos ficam acesos. |
| LightsWarmYou | ligado | Todo fogo construído aceso te aquece como uma fogueira (reinicie depois de mudar). |
| TorchInHandWarmsYou | ligado | A tocha na mão te aquece como uma fogueira. |
| OwnedTag | ligado | Melhorias de bancada mostram "(owned)" quando a bancada mais próxima do tipo já tem. |
| DetailedLog | desligado | Escreve detalhes no log do BepInEx (para relatar bugs). |

### Observações

- Combina com o **Gather Tweaks** e o **DigForHoe** (mesmo autor).
