# Forma Screen Reader Checks Design

## Scope

A Windows check that drives the Showcase in Chrome with NVDA running, records what NVDA speaks after
each action, and compares it with what a scenario expects, for the announcements a component makes:
a busy state, a count, a refusal, a status.

## Purpose

A component's announcements are what a screen reader user hears, and the DOM alone cannot prove them:
a live region can hold the right text and still be spoken late, twice, out of order or not at all. A
check made by hand in NVDA's Speech Viewer proves them once and is never repeated. Recorded speech,
asserted in order, proves them on every run, in every culture the Showcase serves.

## The check

`scripts/Test-ScreenReaderSpeech.ps1` starts a portable NVDA with a profile of its own, so a reader's
own settings and add-ons take no part, the profile's synthesizer being the speech recorder. It then
runs `scripts/cs/CheckScreenReaderSpeech.cs` against a running Showcase, the way the scale check runs,
and quits NVDA when the program is done. The program:

1. Opens Chrome through Playwright, visibly, since NVDA reads the window the desktop shows.
2. Runs each scenario: it opens the scenario's page in its culture, then for each step marks the
   position in the recorded speech, performs the step's action, and waits for the step's expected
   utterances, in order, after that mark, each within a timeout.
3. Writes a transcript of every step, what it expected and what NVDA said, and exits non-zero when
   any expectation was not met.

The speech recorder is an NVDA synthesizer driver, in Python as NVDA's drivers are, loaded from the
profile's developer scratchpad and set as its synthesizer, which is how NVDA's own system tests
capture speech. It speaks nothing: each utterance NVDA hands it is recorded as it arrives, with a
sequence number, as one JSON line in a file the run keeps beside the profile, which is what the
Speech Viewer shows a reader checking by hand. The program reads the file from the last mark onward.

## Scenarios

A scenario is a page, a culture and a list of steps; a step is an action, a click, a key, or a
navigation, and the utterances it must produce, each matched as whole words an utterance contains,
so a count of 9 is never satisfied by 19. Marking before every action means an utterance from an
earlier step, such as a count spoken while the page loaded, can never satisfy a later one.

The first scenarios are the data view's, in English and in Hebrew: the slow source's first load, a
move to its second page and a refresh each saying loading and then the count; the tree's first load
the same; taking away the last page's only item and putting everything back each saying the new
count; and a fourth box pressed in the bounded selection saying the refusal. The slow sources answer
after a fixed delay, which leaves the busy announcement its time to be spoken before the count.

## Where it runs

On Windows with a desktop session, by hand or in a Windows CI job, against a Showcase already
listening, as the scale check runs. A run made to be watched opens NVDA's Speech Viewer and slows the
browser enough to follow, and one made to be heard has the recorder speak each utterance through
Windows' OneCore voice as it writes it down. It stays outside the build gate, which runs headless on any platform.

## What it proves

It proves that NVDA in Chrome speaks what each scenario expects, in order. A human evaluation of the
experience, and other screen readers and browsers, stay checks of their own.
