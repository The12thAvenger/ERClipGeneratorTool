# ERClipGeneratorTool
An Editor for hkbClipGenerators in Elden Ring Havok Behavior files.

## Usage
Load a Behbnd or Havok Behavior hkx file using File > Open.
New generators can be added based on tae entries imported from your custom anibnd using File > Import From Anibnd.
They can also be added individually using the "Add" button or duplicated from existing generators by selecting them in the list and using the "Duplicate" option in the context menu.
All operations can be undone/redone in the Edit menu.

It is recommended to use [Ivi's modified c0000.hks](https://github.com/ividyon/EldenRingHKS) in conjunction with this tool to reduce the amount of hks work necessary to have certain types of animations such as custom Ashes of War play in the game.

## Clip Generators and their role in Behavior Files
Clip generators directly represent individual TAE entries in the behavior file. When adding new TAE entries to the game new clip generators representing these entries must be added to the behavior file so that the corresponding animations can be triggered by the animation state machine. Clip generators are selected for playback based on their animation name property which is of the following form: a\[TaeId]_\[AnimationId] where the TaeId is three digits long and AnimationId is 6 digits long. The selection is performed by Fromsoftware's custom generator class, CustomManualSelectorGenerator or CMSG for short. CMSGs each represent a single animation id and contain all clip generators with this animation id. The selection between generators is performed based on the character's active tae ids. For further info on Havok Behaviors see [this wiki article](http://soulsmodding.wikidot.com/topics:havok-behavior-editing).

## FAQ

### Q: Will this automatically make my new TAE entries play in the game without any issues?
A: This is not the aim of the tool as attempting to ensure this would require handling too many edge cases to be viable. Simple behaviors such as regular attacks will usually not require any further work but more complex behaviors will often require extra hks edits. Please refer to the [troubleshooting](#Troubleshooting) section.

### Q: Does this work for enemies or only for the player?
A: The tool does the supported operations equally well for all behavior files. However, due to differences between player and enemy behaviors additional edits which are not supported by this tool might be necessary in order to actually get the animations to play in game.

## Troubleshooting

### My new tae section will only play X animations in a combo.
Combo length/composition is controlled in hks and in most cases by the "IsEnableNextAttack" function.

### The animation will not trigger in game.
This happens when hks logic which controls the triggering of events is dependent on the tae section and does not default correctly when it encounters an unexpected tae section which was added by the user. Typically the cause will lie somewhere in the corresponding Exec function in hks. Common_define.hks also contains some tables which are expected to contain all valid taes for the player character, using the modified c0000.hks linked above can bypass this issue.

### The animation plays but cannot be canceled or causes the character lock up and become unresponsive after playback.
This happens when hks logic which controls the transition to the next event is dependent on the tae section and does not default correctly when it encounters an unexpected tae section which was added by the user. Typically the cause will lie somewhere in the corresponding onUpdate function in hks.

### The animation defaults to a different tae section when played in game.
This indicates that the clip generator was not added properly. Make sure that the new clip generator appears in the sidebar and that you have saved the behavior file, repacked it into the bnd if editing a loose hkx file and reloaded the character in game. If the issue persists please submit a GitHub issue or message me on Discord at The12thAvenger#6149 with a bug report listing the exact steps taken and please ensure that the bug can be reproduced using these steps.

## Libraries Utilized

* [SoulsFormats](https://github.com/JKAnderson/SoulsFormats) by [TKGP](https://github.com/JKAnderson)
* Code from [SoulsAssetPipeline](https://github.com/Meowmaritus/SoulsAssetPipeline) by [Meowmaritus](https://github.com/Meowmaritus)