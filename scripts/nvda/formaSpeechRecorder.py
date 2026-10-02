"""An NVDA synthesizer that records each utterance it is handed.

The screen reader check loads it from its profile's developer scratchpad as the profile's synthesizer.
Each utterance is one JSON line, its sequence number and its text, in the file the FORMA_SPEECH_LOG
environment variable names. With FORMA_SPEECH_AUDIBLE set to 1 it also speaks each utterance through
Windows' OneCore voice; otherwise it speaks nothing.
"""

import json
import os
import queue
import threading

import synthDriverHandler
from logHandler import log
from speech.commands import IndexCommand


class SynthDriver(synthDriverHandler.SynthDriver):
	# NVDA finds a driver by its module's name, so the two are the same.
	name = "formaSpeechRecorder"
	description = "Forma speech recorder"

	supportedSettings = ()
	supportedCommands = frozenset({IndexCommand})
	supportedNotifications = frozenset({synthDriverHandler.synthIndexReached, synthDriverHandler.synthDoneSpeaking})

	@classmethod
	def check(cls):
		return True

	def __init__(self):
		super().__init__()
		self._path = os.environ["FORMA_SPEECH_LOG"]
		self._sequence = 0
		self._lock = threading.Lock()
		self._pending = queue.SimpleQueue()
		self._running = True
		self._voice = self._createVoice() if os.environ.get("FORMA_SPEECH_AUDIBLE") == "1" else None
		self._worker = threading.Thread(target=self._reportProgress, name="Forma speech recorder", daemon=True)
		self._worker.start()

	def terminate(self):
		self._running = False
		self._pending.put(None)
		self._worker.join()

		if self._voice is not None:
			self._voice.terminate()

		super().terminate()

	def speak(self, speechSequence):
		text = "".join(item for item in speechSequence if isinstance(item, str)).strip()

		if text:
			with self._lock:
				self._sequence += 1
				with open(self._path, "a", encoding="utf-8") as file:
					file.write(json.dumps({"sequence": self._sequence, "text": text}, ensure_ascii=False) + "\n")

		if self._voice is not None:
			self._voice.speak(speechSequence)

		self._pending.put([item.index for item in speechSequence if isinstance(item, IndexCommand)])

	def cancel(self):
		if self._voice is not None:
			self._voice.cancel()

		while True:
			try:
				self._pending.get_nowait()
			except queue.Empty:
				return

	@staticmethod
	def _createVoice():
		# NVDA heeds progress only from its active synthesizer, so this voice speaks while the recorder alone
		# tells NVDA where its speech has reached.
		try:
			from synthDrivers.oneCore import SynthDriver as OneCore

			voice = OneCore()
			# Its settings section is made as NVDA makes it for a synthesizer it selects, so the configuration
			# saved on exit holds the voice's settings.
			voice.initSettings()
			return voice
		except Exception:
			log.error("The OneCore voice would not start, so the recorder stays silent", exc_info=True)
			return None

	def _reportProgress(self):
		# NVDA's speech manager moves on as each index is reached and each sequence is done, and the
		# recorder reaches them all at once, whether or not its voice speaks.
		while self._running:
			indexes = self._pending.get()

			if indexes is None:
				return

			for index in indexes:
				synthDriverHandler.synthIndexReached.notify(synth=self, index=index)

			synthDriverHandler.synthDoneSpeaking.notify(synth=self)
