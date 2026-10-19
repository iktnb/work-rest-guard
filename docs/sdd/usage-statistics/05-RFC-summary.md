# Review summary

Implement local routine and per-program statistics in the existing Windows app.
Store only process names and durations/states, sampled every five seconds.
Unknown or unrecorded time stays visible. No old usage can be reconstructed.
Keep existing break protection and Obsidian Display Guard unchanged.

No dependencies, services, cloud endpoints or database migration. New local
90-day data store and two additive config options. Build locally pending release.
Main risks: estimated rather than exact attention, 30-second crash tail loss,
and missing data whenever the application is closed. Explicit UI wording and
boundary/failure tests address these limitations.
