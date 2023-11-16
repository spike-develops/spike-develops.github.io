---
header: 
 overlay_image: /assets/files/CoP_Library-Hero.jpg  
 caption: "Capsule Art: [**MeyersIllustration**](https://www.meyersillustration.com/)"
 actions:
 - label: "Steam Page"
   url: "https://store.steampowered.com/app/1699290/Cult_of_Personality/"
layout: single
author_profile: true
title: "Cult of Personality"
permalink:  /cult-of-personality/
tagline: "Lead Network and Gameplay Engineer"
tags: 
 - Unity 
 - C#
hide_updated_date: true

mdev_gallery:
  - image_path: /assets/files/mdev/Sam_Booth_Square.jpg
    url: /assets/files/mdev/Sam_Booth_Square.jpg
  - image_path: /assets/files/mdev/Public_Showcase_Square.jpg
    url: /assets/files/mdev/Public_Showcase_Square.jpg
  - image_path: /assets/files/mdev/Courtney_Booth_Square.jpg
    url: /assets/files/mdev/Courtney_Booth_Square.jpg


possessed_gallery:
  - url: /assets/files/PossessingExample.png
    image_path: /assets/files/PossessingExample.png
    alt: "Possessing Player Perspective"
    title: "Perspective of the player doing the possessing"
  - url: /assets/files/PossessedExample.png
    image_path: /assets/files/PossessedExample.png
    alt: "Possessed Player Perspective"
    title: "Perspective of the possessed player"

---



**Development Time:** 2 years

**Team Composition:**
 - Pixel Artist, Designer
 - **Engineer, Designer (My role)**
 - Capsule Artist
 - Tools Programmer

TODO expand this engineering highlights section

Engineering highlights
- [Custom Netcode](#custom-netcode)
- [Hierarchical State Machines](#hierarchical-state-machines)


{% include gallery id="mdev_gallery" caption="Showing off CoP at the 2023 M+Dev Games Showcase" %}


### Gameplay
Recording from an October 2023 beta test
{% include video id="at7E-_t5S2g" provider="youtube" %}

## Custom Netcode

One of the core features of CoP is player-on-player "possession." A dead Defector (think Traitor from Among Us) has the ability to temporarily gain control of living cultists, and can force them to do suspicious things like sabotage objectives, or attack their allies. An extra wrinkle is that possessed players can try to resist the possession by holding directional inputs that oppose the possessors. 

{% include gallery id="possessed_gallery" layout="half" caption="**Left:** Possessor perspective **Right:** Possessed perspective" %}

Making sure you could "fake" the movement of a possessed player - and therefore pretend to be possessed - was also super important to the Deception aspect of the  game. 

Those requirements led me to build CoP's netcode, with server authority and client rollback, from scratch on top of Steamworks P2P. Thankfully there are tons of awesome resources on this stuff. Some of the most important for CoP were the [Gaffer On Games](https://gafferongames.com/) articles about transport layer best practices, and [this GDC talk](https://youtu.be/W3aieHjyNvw?si=I3wYFfBCoSFXDZBt&t=1501) about Overwatch's netcode. Hoping to write a deeper dive about it soon!

## Hierarchical State Machines

Because it was important for CoP to support responsive, predicted actions on client, we needed to be able to rewind a player's state, then re-play inputs for players in case of a missed prediction. We also needed to ensure separation between input source, and actor state, to allow control swapping when possession happens, and for AI controlled NPC's like bats.

All that factored into my decision to represent actor state with Hierarchical State Machines. I definitely recommend the whole book, but Robert Nystrom's [Game Programming Patterns](https://gameprogrammingpatterns.com/state.html#hierarchical-state-machines) was the jumping off point for my HSM implementation. There are also some super neat bonuses to HSM's, like situational behavior changes (holding death triggers for a round-end animation) being relatively painless, and super tiny state serialization.

