# Team Resurgent's Repackinator - Modern Original Xbox ISO Manager
<div align="center">
[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://github.com/Team-Resurgent/Repackinator/blob/main/LICENSE.md)
[![.NET](https://github.com/Team-Resurgent/Repackinator/actions/workflows/dotnet.yml/badge.svg)](https://github.com/Team-Resurgent/Repackinator/actions/workflows/dotnet.yml)
[![Discord](https://img.shields.io/badge/chat-on%20discord-7289da.svg?logo=discord)](https://discord.gg/VcdSfajQGK)

[![Patreon](https://img.shields.io/badge/Patreon-F96854?style=for-the-badge&logo=patreon&logoColor=white)](https://www.patreon.com/teamresurgent)
</div>
Repackinator was designed to be modern all-in-one ISO Management tool for the Original Xbox. 

It will provide you the ability to convert your full OG Xbox ISO dumps into full split ISO images (as well as optionally replacing padding for greater compression potential). Repackinator can also create reduced size ISO files by trimming the unused space if desired. Additionally, The ability to create playable compresses ISO images was introduced when the .cci(CerBios Compresses ISO) compression method was added.

                    [ Program ..................................... Repackinator V1.2.4 ]
                    [ Type ................................................ Iso Manager ]
                    [ Patreon ....................https://www.patreon.com/teamresurgent ]
                    [ Release date ......................................... 01.01.2023 ]
            
                    [                          Team Members:                            ]
                    [ EqUiNoX ......................................... Lead Programmer ]
                    [ HoRnEyDvL ............................... Tester/ Project Manager ]
                    [ Hazeno ................................................... Tester ]

## System Requirements
### Minimum
* OS: Windows 7+ x64, x86-64 Linux, or MacOS (verified on Big Sur, should run from High Sierra onwards, please report any findings). 32-bit is not supported.
    * Repackinator can be ran in a virtual machine with GPU passthrough. (Virtual GPU must be disabled)
* RAM: 8gb of RAM for proper operation.

## Prerequisites
  * [64-bit (x64) Visual C++ 2022 Redistributable](https://aka.ms/vs/17/release/vc_redist.x86.exe)

## Core Features & Functionality
Repackinator will extract Certs, Title ID & Title Image from the XBE located inside the ISO Dump. It will then generate a Default.XBE which Will be used to load the ISO Games on ISO Enabled Bioses such as Cerbios (Native Iso Support) , IND-Bios (Patched) , EVOX (Patched)

The generated Default.XBE will use the XBE Title Column as the New Title Name. This is the name of the game which is displayed on your favorite dashboard.

Please note that the region shown in Repackinator is calculated based on the Region that is extracted from the Games XBE. These regions are:
  * GLO = (GLOBAL) USA,PAL,JAP
  * JAP = (JAPAN/ASIA) JAP
  * PAL = (Europe/Australia) PAL
  * USA = (USA) USA
  * USA-JAP = (USA,JAPAN/ASIA) USA,JAP
  * USA-PAL = (USA,Europe/Australia) USA,PAL

Current DB contains 1044 Games, The info shown has been compiled by extracting Title Name, Region, Version & Title ID from the Default.XBE of each game. DB contains all USA Region Games, PAL Only Exclusives & JAP Only Exclusives. **Full Xbox library support to come in a future release. JSON file can be edited to include missing titles if desired in the interim** 

Ability to easily update legacy Attacher(Default.XBE) created by tools like DVD2Xbox with new improved CerBios Attacher(Default.XBE).

## Install Notes
* Run Repackinator.exe **first run must be as administrator to enable context menu**
![GUI](https://github.com/zatchbot/Repackinator/blob/main/readmeStuff/gui.png?raw=true)
* Select Grouping Type *creates grouped folders in the output directory. Default = no grouping*
* Set Input Folder. (Path to your Redump .ZIP/.7Z or .ISO Files) **SHOULD NOT INCLUDE REPACKINATOR'S ROOT, ANY SYSTEM FILES, OR BE A CHILD OF 'OUTPUT'**
* Set Output Path. (Path to where you want to save your processed game)
* **Process** Must be selected for titles you desire to have prepaired
* **Scrub** is selected by default. This will replace the padding with zeros, for greater compressability. (de-selecting will simply split ISO for Xbox Fatx file system durring process)
* **Use Uppercase** will output file/folder names with all uppercase characters.
* **Compress** will add .cci compression to the output. *Note: .cci is only supported while using CerBios currently* 
* **Trim Scrub** will remove all unused data at the end of data partition. *Similar to XISO*  
* **Traverse Input Subdir's** will look for files to process inside any additional directories within your selected input folder.

## Context Menu



## Acknowledgements
* First, we would like to thank all of our Patreon supporters! You are the reason we can continue to advance our open source vision of the Xbox Scene!
* We can't thank Team Cerbios enough for their amazing Bios, as well as their continued addition of features to a decades old gaming console. This program began as a collaboration with them to modernize the Original Xbox. They also provided the modernized ISO Attach (Default.XBE) with bug fixes and improvements. Thank you again!
* We want to thank all the Original Xbox devs for bringing us the awesome applications, dashboards and emulators we have grown to love and for kickstarting the scene back in the day.
* Thanks to the team at [Xbox-Scene Discord](https://discord.gg/VcdSfajQGK) - Haguero, AmyGrrl, CrunchBite, Derf, Risk, Sn34K, ngrst183
* Huge Shout-out to [Kekule](https://github.com/Kekule-OXC), [Ryzee119](https://github.com/Ryzee119), & [ChimericSystems](https://chimericsystems.com/) for all the time & effort they have put towards reverse engineering & creation of new hardware mods.
* To all the people behind projects such as [xemu](https://github.com/mborgerson/xemu) and [Insignia](https://insignia.live/). Keep up the amazing work, cant wait to for your final product releases.
* Greetz to the following scene people - Milenko, Iriez, Mattie, ODB718, ILTB, HoZy, IceKiller, Rowdy360, Lantus, Kl0wn, nghtshd, Redline99, The_Mad_M, Und3ad, HermesConrad, Rocky5, xbox7887, tuxuser, Masonly, manderson, InsaneNutter, IDC, Fyb3roptik, Bucko, Aut0botKilla, headph0ne,Xer0 449, hazardous774, rusjr1908, Octal450, Gunz4Hire, Dai, bluemeanie23, T3, ToniHC, Emaxx, Incursion64, empyreal96, Fredr1kh, Natetronn, braxtron
<!--* I'm sure there is someone else that belongs here too ;)--> 
