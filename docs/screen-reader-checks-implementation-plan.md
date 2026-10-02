# Forma Screen Reader Checks Implementation Plan

The steps that build [the screen reader checks design](screen-reader-checks-design.md), and the
record of what has landed.

## Status

Steps 1 and 2 have landed; the data view's scenarios are next.

## Steps

Each step is a commit of its own, taken in order.

1. **NVDA and its profile.** ✅ The script fetches a pinned NVDA release once into a per-user tool
   cache, makes a portable copy from it, and writes the profile the check runs under, the recorder its
   synthesizer and no add-ons. Gate: the script starts and stops NVDA under that profile, and the
   recorder's file grows as NVDA speaks.
2. **The program and the first scenario.** ✅ With NVDA running, the program opens Chrome, runs
   one data view scenario, the slow source's first load in English, and writes its transcript. Gate:
   the scenario passes against the Showcase, and fails with the expected utterance named when the
   busy announcement is removed from the data view.
3. **The data view's scenarios.** Open. Every announcement in the data view plan's screen reader
   check, in English and in Hebrew. Gate: every scenario passes, and the data view plan's manual
   check is retired.
4. **The task script's coverage.** Open. The script keeps the shape the repository's task scripts
   keep and is covered the way the scale check is, and the repository structure document names it.
