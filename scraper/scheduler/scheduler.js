const { scheduleScraping, scrapeAll } = require('./scraper')
const { scheduleUpdateSeeders, scheduleUpdateSeedersForNewTorrents } = require('./seeders')

function startScraper() {
  if (process.env.ENABLE_SCHEDULING) {
    scheduleScraping();
    // broken currently @funkypenguin
    // scheduleUpdateSeeders();
    // scheduleUpdateSeedersForNewTorrents();
  } else {
    scrapeAll()
  }
}

module.exports = { startScraper }