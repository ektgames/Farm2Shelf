# Taxi, store access, parking and placement follow-up

Implemented against the existing project; no copied Unity project or unattended play session.

- Taxis use the directed public-road graph, excluding the private courier aisle. Stand manoeuvre points remain explicit, and invalid corners are never skipped to create diagonal shortcuts.
- Placement reserves a 1.5 m front aisle within the building. It checks both the new object's access and existing objects' access, while allowing side-by-side shelves and shared aisles. Existing saved furniture is not moved automatically.
- Customers reserve shelf visits in FIFO order and seek separate unobstructed waiting positions. Shelf lookup is cached for 0.5 real seconds or until the waypoint changes.
- Boulevard sidewalk gaps match both parking gates. Flat driveways are excluded from old-road cleanup. Both gates have crossings. Entrance hatch marks are removed, and perimeter lights grow with parking size; boulevard lights avoid junctions, gates, railway and bridge areas.
- Placement preserves camera position. Drag pans, a clean tap selects the preview position, and Assemble commits. Furniture information panels permit camera gestures outside their UI while retaining the world-interaction modal guard.

## Verification

Runtime C# compilation passed. AccessFollowupCheck passed 730 taxi routes across all five stand slots and 73 catalog addresses, 1,162,950 pavement samples, straight-segment lane direction assertions, and four rotated access rectangles. TaxiStandCheck passed ten stand manoeuvre routes. WorldFollowupCheck was rebuilt against the new method signature and passed 219 courier routes plus 32 parking bays across all three levels.

These are compiled-code and geometric checks, not Unity Play Mode visual, device input, FPS or crowd soak tests. Procedural geometry requires a fresh Play session. Existing layouts with furniture blocking access still need player rearrangement.
