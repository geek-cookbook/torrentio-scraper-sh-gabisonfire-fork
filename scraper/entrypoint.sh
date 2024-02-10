#!/bin/ash

# This is a hacky little trick to allow running multiple scraper pods, one per target scraper

# If we've been given a scraper, then just enable that
if [[ ! -z "$SCRAPER" ]]; then
    # Disable all scrapers
    sed -i  "s|{ scraper: |// { scraper: |" scheduler/scrapers.js

    # Now enable the one we want
    sed -i  "s|// { scraper: $SCRAPER|{ scraper: $SCRAPER|" scheduler/scrapers.js
fi 

exec node --insecure-http-parser index.js