# OWTrackerJson
Local and open source version of OverwatchTracker that stores games locally in JSON files. Efficiency is not promised.

This program was originally created to be used with SQL server to maintain users across machines. I have no desire to open source the version that connects to SQL, but I wanted to create an open source version of the project. This version stores all games locally in JSON files in AppData. Because this program was designed for SQL, the data is not stored in JSON in the most efficient way, notably with IDs that don't make sense. Just keep that in mind when looking through the source.
