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
---
<!---
[**Cult of Personality is Out NOW on Steam**](https://store.steampowered.com/app/1699290/Cult_of_Personality/)
-->
Cult of Personality is a 5-8 player online social deception game that bends the rules of what players can see, do, and hear... In life, and in death!

Working on some new devlogs about the development process. For now, here's a few videos showing off some systems and gameplay, and how they compare to Among Us.
<hr class="rounded">
A territorial Bat NPC that will attack when players get close, and can be "possesed" by dead players
<center><image src="/assets/files/batTutorial.gif" alt="Bat AI Behaviour" width="450"/> </center>

<hr class="rounded">
Searchables replace the "task" system from Among Us, having players look/listen for pulsing objects to interact with.
{% include video id="1SRF1OdMjf0BJ43EJS1SpGtrvh4m9ZAAm" provider="google-drive" %}
<hr class="rounded">
Mirrors replace "vents" from Among Us, allowing traitors to secretly navigate around the map, and "possess" things and other players.
{% include video id="1qELC1sUOIoqp-y8psbUEonEXGhgOKoLN" provider="google-drive" %}
<hr class="rounded">
Some live-game footage (from an early 2022 build) showing some online gameplay and native proximity VOIP
{% include video id="1ay7nELyNuoiXjZe9tkh9tI7Tq91lraWO" provider="google-drive" %}

Engineering highlights
- Custom Netcode
- HSM System 

<h3>Custom Netcode</h3>

One of the core features of COP is player on player "possession." A dead Defector (think Traitor from Among Us) has the ability to temporarily gain control of living cultists, and can force them to do suspicious things like sabotage objectives, or attack their allies. An extra wrinkle is that possessed players' can try to resist the possession by holding directional inputs that oppose the possessors. 

Making sure you could "fake" the movement of a possessed player - and therefore pretend to be possessed - was also super important to the Deception aspect of the  game. 

Those requirements led me to build COP's netcode, with server authority and client rollback, from scratch on top of Steamworks P2P. Thankfully there are tons of awesome resources on this stuff. Some of the most important for COP were the [Gaffer On Games](https://gafferongames.com/) articles about transport layer best practices, and [this GDC talk](https://youtu.be/W3aieHjyNvw?si=I3wYFfBCoSFXDZBt&t=1501) about Overwatch's netcode. Hoping to write a deeper dive about it soon!

<h3>Hierarchical State Machines</h3>

Since COP supporting responsive, predicted actions on client was important, we needed to be able to rewind a player's state, then re-play inputs for players in case of a missed prediction. We also needed to ensure separation between input source, and actor state, to allow control swapping when possession happens, and for AI controlled NPC's like bats.

All that factored into my decision to represent player/npc state with Hierarchical State Machines. I definitely recommend the whole book, but Robert Nystrom's [Game Programming Patterns](https://gameprogrammingpatterns.com/state.html#hierarchical-state-machines) was the jumping off point for my HSM implementation. There are also some super neat bonuses to HSM's, like situational behavior changes (holding death triggers for a round end animation) being relatively painless, and super tiny state serialization.