---
layout: single
author_profile: true
title: "Unreal Advent of Code"
Engine: "Unreal - C++"
published: false
read_time: true
---
[**Advent of Code**](https://adventofcode.com/) is a yearly coding challenge that happens in December. A coding puzzle (typically centered around elves or Santa) is revealed every day until Christmas. It's a great way to brush up on old languages or learn new ones, and in 2022 I used it to stay sharp with **C++ and Unreal**. My self imposed restriction was that I had to visually represent the "solving" of the puzzles in realtime using the engine. 

Here are a few excerpts

<hr class="rounded">

This puzzle involved calcuating how a series of Rock Paper Scissors games would play out, given a predetermined strategy guide. The guide was a list of letter pairs like `[A Y] [B X] [C Z]` with letters corresponding to which shape each player would choose. The solution was calculated using a score based on game outcome and shape chosen.

<figure class="align-center">
  <video width="100%" muted autoplay loop playsinline> 
    <source src="/assets/files/RochamboDay.mp4"  type="video/mp4">
    browser doesn't support videos
  </video>
  <figcaption>Each sphere is a game played - sphere material representing what was "thrown" that game, and container it spawns above representing game outcome</figcaption>
</figure> 

<hr class="rounded">

This puzzle involved finding matching "items" in sections of an "elf's rucksack." A rucksack was represented as a series of item characters like `vJrwpWtwJgWrhcsFMMfFFhFp`. Each matching item was scored based on its ASCII value, then summed for the solution.

<figure class="align-center">
  <video width="100%" muted autoplay loop playsinline> 
    <source src="/assets/files/bouncingLettersDay.mp4"  type="video/mp4">
    browser doesn't support videos
  </video>
  <figcaption>Each group of letters is a rucksack - red letters representing a match within the rucksack, and blue letters representing a match among a set of 3 concurrent rucksacks</figcaption>
</figure> 

<hr class="rounded">

This puzzle involved finding the "elf" with the highest calorie count among their snacks. Snack calorie counts were represented in a list with separators dividing each elf. Indivual elf counts were summed, the solved for the max.

<figure class="align-center">
  <video width="100%" muted autoplay loop playsinline> 
    <source src="/assets/files/variedSizeCubesDay.mp4"  type="video/mp4">
    browser doesn't support videos
  </video>
  <figcaption>Each cube is an elf - cube size being proportional to calorie count. A yellow cube represents the newest elf with the calorie count max.</figcaption>
</figure> 