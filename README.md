# Interaction & Loot

The first-person interaction and loot system from a Unity dungeon crawler. One raycast
from the camera drives everything the player can look at, highlight, pick up, unlock,
and open.

*An excerpt from a larger project. These nine files show one system end to end, but
won't compile on their own.*

### How it fits together

`InteractionSystem` casts a single ray each frame and resolves what's under the
crosshair. Loose items get picked up directly. Anything with real behaviour goes
through `Interactable`, an abstract base that handles prompts, highlighting, and
locking, so adding a chest, door, or lever means overriding three methods and
touching nothing else.

Loot is data-driven: designers build `LootTable` assets, and `LootDropper` rolls them
into actual drops at runtime.

### Files

| | |
|---|---|
| `InteractionSystem.cs` | The raycast loop, prompts, highlighting, item dropping |
| `Interactable.cs` | Abstract base for anything the player can use |
| `Lockable.cs` | Drop-in component that makes an interactable require a key |
| `ChestLoot.cs` | A chest, the fuller example |
| `DoorController.cs` | A door, the minimal example at ~50 lines |
| `LootTable.cs` / `LootEntry.cs` | Designer-authored drop pools |
| `LootDropper.cs` | Rolls a table into drops |
| `WorldItem.cs` | An item lying on the ground |
