# ChipAte
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
![Project Status: Alpha](https://img.shields.io/badge/Status-Alpha-red.svg)

### a first-pass at a Chip-8 emulated 'virtual' computer system in C# and MonoGame

There are a bunch of test roms (and sample roms) in the repo, but you'll have to edit the Chip8Wrapper file (it's obvious where).  
The keypad is currently hardcoded as  
```
1  2  3  4
Q  W  E  R
A  S  D  F
Z  X  C  V
```

The emulator passes all of the standard 'tests' now, but it fails on the 'oob' out of bounds test rom.  


## todo:
- sound on/off (presently fixed on)
- debugging tools (memory viewer, step execution, breakpoints, etc)
- save/load state
- rom select ui
- change background/foreground colors
- change ui size  
- optimize rendering (presently redraws whole screen every frame)
- command line arguments (rom path, scale, bg/fg colors, etc)


## Documentation references  
https://en.wikipedia.org/wiki/CHIP-8  
http://devernay.free.fr/hacks/chip8/C8TECH10.HTM  
https://github.com/Timendus/chip8-test-suite?tab=readme-ov-file for tests  
(but note this errata - https://github.com/gulrak/cadmium/wiki/CTR-Errata )  
https://www.laurencescotford.net/2020/07/25/chip-8-on-the-cosmac-vip-index/   
https://tobiasvl.github.io/blog/write-a-chip-8-emulator/   
https://chip8.gulrak.net/   

