# Forma Image Editor Design

## Scope

`FormaImageEditor`: what it edits, what it leaves to the host, and the two coordinate
mistakes it avoids.

## Decision summary

- **In: crop, rotate, flip, resize, zoom.** Crop with a free or locked ratio and named
  presets, rotation in 90° steps, horizontal and vertical flips, resizing to target
  dimensions, and zoom for precision.
- **Out: filters, text, stickers, drawing, shapes, gradients, layers.** That is a canvas
  compositing app rather than a control, and reaching it by pulling in a library would
  spend the external-JS exception the library grants to `FormaComposer` alone.
- **The component describes the edit; the host performs it.** `OnApply` emits an
  `ImageEdit`, the crop rectangle, the rotation, the flips and the target size, and the
  host renders it with ImageSharp or SkiaSharp, exactly as `FormaFileUploader` leaves the
  destination to an `IFileStore` and the browsing surfaces leave every mutation to a
  callback. A module drives the handles and the live preview through Pointer Events and
  Canvas 2D, hand-written interop for behavior only script can provide.
- **Never export the canvas.** Returning the finished bitmap from the browser looks
  simpler and is the wrong trade: a base64 export of a multi-megabyte photo crosses interop
  in every runtime, and over a circuit meets its receive limit, which rejects it outright;
  and re-encoding through the browser discards EXIF, ICC color profiles and the original's
  quality. A rectangle is a few numbers.
- **Coordinates are the image's natural pixels.** The editor scales the image to fit its
  box, and a crop that forgets the scale factor is wrong by exactly that ratio, so the
  rectangle is reported in natural pixels, never displayed CSS pixels.
- **EXIF orientation is honored before coordinates are read.** A phone photo displays
  upright through `image-orientation` while its stored pixels are rotated, so coordinates
  taken from the display do not address the bytes the host will cut unless the orientation
  is applied first.
- **Reachable without a mouse.** The selection is nudged by the arrow keys, resized with a
  modifier, set exactly through numeric width and height fields, and announced through a
  live region as the rectangle changes.
- **The handles are touch targets.** 44×44 under a coarse pointer around a rectangle whose
  corners may sit at the image's edge, with press-and-hold to start a drag so an ordinary
  swipe still scrolls.

## Open decisions

- **Does straightening by an arbitrary angle join the 90° steps?** It forces canvas
  resampling and a bounds recalculation that quarter-turns avoid entirely, so it is a
  different order of work than the rotation already in scope.
