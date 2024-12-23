# Sorcery Cuffs

A dynamic handcuff mod for Blade & Sorcery that lets you restrain and chain NPCs with magical bindings.

## Prerequisites

Before you begin development:
1. Install Unity 2021.3.38f1 (exact version required)
2. Download the latest [Blade & Sorcery SDK](https://github.com/KospY/BasSDK)
3. Set up your development environment following the SDK documentation

## Project Contents

This repository includes everything you need to learn and build upon:
- Complete C# source code
- Ready-to-use handcuff prefab (found in the Prefab folder)
- Basic mod files (manifest.json, item JSONs)

The prefab and JSON files are set up and ready to go, making it easy to understand how everything connects together. Feel free to use these as templates or learning resources for your own mods!

## Features

- **Dynamic Physics-Based Restraints**: Fully physics-enabled cuffs that realistically bind NPCs
- **Chain Multiple NPCs**: Connect up to 9 NPCs together in a prisoner chain
- **Realistic Behavior**: NPCs react naturally to being cuffed - they can't stand up and become pacified
- **Infinite Supply**: Cuffs automatically respawn in your pouch when used
- **Customizable Settings**: Extensive options to tweak every aspect of the cuffs' behavior

## Installation

For Players:
1. Make sure you have the latest version of Blade & Sorcery
2. Download the latest release from Nexus Mods
3. Extract the contents into your `BladeAndSorcery/BladeAndSorcery_Data/StreamingAssets/Mods` folder
4. Launch the game and find the Sorcery Cuffs in your spell wheel

For Developers:
1. Clone this repository
2. Open the project in Unity 2021.3.38f1
3. Make sure you have the B&S SDK imported correctly
4. Build the project using the SDK's build tools

## Usage

1. Grab the cuffs from your pouch
2. Hit an NPC's wrists to bind them
3. Chain multiple NPCs by hitting their wrists in sequence
4. Use alternate trigger (default: grip button) to reset/remove cuffs
5. Drag cuffed NPCs by moving away from them

## Configuration

Access mod settings through the in-game menu to customize:

### Behavior Settings
- Enable/disable No Stand Up effect
- Toggle automatic pacification
- Enable/disable multi-creature chaining
- Set maximum chain length (2-9 NPCs)

### Technical Settings
- Reset delay time
- Joint physics properties
- Spring and damper strengths
- Drag activation distance

### Target Settings
- Customize which body parts can be cuffed
- Adjust left/right targeting

## Known Issues

- Performance may decrease with large chains of NPCs
- Some custom NPC mods may not work correctly with the cuffs
- Can be very wonky 

## Compatibility

- Requires Blade & Sorcery 1.0 or later
- Compatible with most NPC mods
- May conflict with mods that modify NPC behavior significantly

## Open Source Notice

This mod's code is completely free to use, modify, and redistribute. You can:
- Copy any part of the code
- Modify it for your own mods
- Use it as a learning resource
- Incorporate it into other projects

**No credit or attribution is required.** While appreciation is nice, you are under no obligation to credit me if you use this code. Feel free to build upon this work to create something awesome!
