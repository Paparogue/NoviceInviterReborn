# NoviceInviterReborn
![NoviceInviterReborn Logo](https://raw.github.com/Paparogue/NoviceInviterReborn/ff850b1fd057abfff38318e20b1f48b1cd68e837/NoviceInviter.png)

A Dalamud plugin for FFXIV that automatically invites sprouts to the Novice Network.

## Features

- **Auto-Invite System**: Automatically detects and invites nearby sprout players to the Novice Network
- **Mass Invitation**: Send invites to hundreds of sprouts at once using the player search system
- **Anti-Bot Measures**: Configurable filters to avoid inviting potential bot accounts
- **Distance & Range Control**: Set maximum invite distance and timing between invites
- **Player Tracking**: Keeps track of already invited players to avoid duplicate invitations

## Installation

Add the following URL to your third-party repository list in the Dalamud Plugin Installer:

```
https://raw.githubusercontent.com/Paparogue/PaparogueRepo/refs/heads/main/repo.json
```

## Configuration Guide

![NoviceInviterReborn UI](https://raw.github.com/Paparogue/NoviceInviterReborn/d97dfa01a1a213d0f3eb67c42052431847f4bc17/grafik.png)

### General Settings

- **Enable/Disable**: Toggle the plugin functionality on or off

### Invite Settings

- **Max Invite Range**: Set the maximum distance for inviting sprouts (0-200 yalms)
- **Time Between Invites**: Configure the delay between each invite (500-30000ms)

### Anti-Bot Settings

- **Do you want to invite possible bots?**: Not recommended, but can be enabled
  - When disabled, the plugin will filter out characters playing as Archer, Lancer, Bard, or Marauder (common bot classes)
  - When disabled, characters below level 5 will not receive invites

### Mass Invitation System

- **Send Mass Invitation**: Initiates a search through all world regions to find and invite sprouts
- **Clear Invitation List**: Removes all players from the current search results

## Commands

- `/nirui`: Opens the configuration window

## Important Notes

- You must be a Mentor with Novice Network invitation privileges for this plugin to work
- The plugin saves a list of invited players to avoid sending duplicate invitations
- The plugin will not function while in duties

## Requirements

- Mentor status with invitation privileges
