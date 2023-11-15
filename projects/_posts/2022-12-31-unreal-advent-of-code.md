---
header:
 teaser: /assets/files/adventOfCode.png
excerpt: solving coding puzzles in Unreal
layout: single
author_profile: true
title: "Unreal Advent of Code"
tags: 
 - Unreal
 - C++
read_time: true
order_priority : 2
---
[**Advent of Code**](https://adventofcode.com/) is a yearly coding challenge that happens in December. A coding puzzle (typically centered around elves or Santa) is revealed every day until Christmas. It's a great way to brush up on old languages or learn new ones, and in 2022 I used it to stay sharp with **C++ and Unreal**. My self-imposed restriction was that I had to visually represent the "solving" of the puzzles in real-time using the engine. 

Here are a few excerpts

<hr class="rounded">

This puzzle involved calculating how a series of Rock Paper Scissors games would play out, given a predetermined strategy guide. The guide was a list of letter pairs like `[A Y] [B X] [C Z]` with letters corresponding to which shape each player would choose. The solution was calculated using a score based on game outcome and shape chosen.

<figure class="align-center">
  <video width="100%" muted autoplay loop playsinline> 
    <source src="/assets/files/RochamboDay.mp4"  type="video/mp4">
    browser doesn't support videos
  </video>
  <figcaption>Each sphere is a game played: sphere material represents what was "thrown" that game, and container it spawns above represents that game's outcome</figcaption>
</figure> 

<hr class="rounded">

This puzzle involved finding matching "items" in sections of an "elf's rucksack." A rucksack was represented as a series of item characters like `vJrwpWtwJgWrhcsFMMfFFhFp`. Each matching item was scored based on its ASCII value, then summed for the solution.

<figure class="align-center">
  <video width="100%" muted autoplay loop playsinline> 
    <source src="/assets/files/bouncingLettersDay.mp4"  type="video/mp4">
    browser doesn't support videos
  </video>
  <figcaption>Each group of letters is a rucksack: red letters represent a match within the rucksack, and blue letters represent a match among a set of 3 concurrent rucksacks</figcaption>
</figure> 

<hr class="rounded">

This puzzle involved finding the "elf" with the highest calorie count among their snacks. Snack calorie counts were represented in a list with separators dividing each elf. Individual elf counts were summed, then solved for the max.

<figure class="align-center">
  <video width="100%" muted autoplay loop playsinline> 
    <source src="/assets/files/variedSizeCubesDay.mp4"  type="video/mp4">
    browser doesn't support videos
  </video>
  <figcaption>Each cube is an elf's calorie count: cube size is scaled based on calories, with yellow cubes spawning anytime a new max calorie count is found.</figcaption>
</figure> 