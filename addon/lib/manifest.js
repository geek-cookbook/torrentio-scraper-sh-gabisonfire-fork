import { MochOptions } from '../moch/moch.js';
import { Providers } from './filter.js';
import { showDebridCatalog } from '../moch/options.js';
import { getManifestOverride } from './configuration.js';
import { Type } from './types.js';

const DefaultProviders = Providers.options.map(provider => provider.key);
const CatalogMochs = Object.values(MochOptions).filter(moch => moch.catalog);
const tenant_name = process.env.TENANT_NAME;

export function manifest(config = {}) {
  const overrideManifest = getManifestOverride(config);
  const baseManifest = {
    id: `com.elfhosted.${tenant_name}-torrentio.addon`,
    version: '0.0.14',
    name: getName(overrideManifest, config),
    description: getDescription(config),
    descriptionHTML: getDescriptionHTML(config),
    mochsHTML: getMochsHTML(config),
    catalogs: getCatalogs(config),
    resources: getResources(config),
    types: [Type.MOVIE, Type.SERIES, Type.ANIME, Type.OTHER],
    background: `https://unsplash.com/photos/HTpAIzZRHvw/download?ixid=M3wxMjA3fDB8MXxhbGx8fHx8fHx8fHwxNzA2NjUxMTMxfA&force=true&w=1920`,
    logo: `https://elfhosted.com/images/logo.svg`,
    behaviorHints: {
      configurable: true,
      configurationRequired: false
    }
  };
  return Object.assign(baseManifest, overrideManifest);
}

export function dummyManifest() {
  const manifestDefault = manifest();
  manifestDefault.catalogs = [{ id: 'dummy', type: Type.OTHER }];
  manifestDefault.resources = ['stream', 'meta'];
  return manifestDefault;
}

function getName(manifest, config) {
  const rootName = manifest?.name || `${tenant_name} | ElfHosted`;
  const mochSuffix = Object.values(MochOptions)
      .filter(moch => config[moch.key])
      .map(moch => moch.shortName)
      .join('/');
  return [rootName, mochSuffix].filter(v => v).join(' ');
}

function getDescription(config) {
  const providersList = config[Providers.key] || DefaultProviders;
  const enabledProvidersDesc = Providers.options
      .map(provider => `${provider.label}${providersList.includes(provider.key) ? '(+)' : '(-)'}`)
      .join(', ')
  const enabledMochs = Object.values(MochOptions)
      .filter(moch => config[moch.key])
      .map(moch => moch.name)
      .join(' & ');
  const possibleMochs = Object.values(MochOptions).map(moch => moch.name).join('/')
  const mochsDesc = enabledMochs ? ` and ${enabledMochs} enabled` : '';
  return `${tenant_name}-torrentio provides torrent streams from scraped torrent providers.`
      + ` Currently supports ${enabledProvidersDesc}${mochsDesc}.`
      + ` To configure providers, ${possibleMochs} support and other settings visit https://${tenant_name}-torrentio.elfhosted.com`
}

function getDescriptionHTML(config) {
  const providersList = config[Providers.key] || DefaultProviders;
  const enabledProvidersDesc = Providers.options
      .map(provider => `${provider.label}${providersList.includes(provider.key) ? '(+)' : '(-)'}`)
      .join(' / ')
  const enabledMochs = Object.values(MochOptions)
      .filter(moch => config[moch.key])
      .map(moch => moch.name)
      .join(' & ');
  const possibleMochs = Object.values(MochOptions).map(moch => moch.name).join(' / ')
  const mochsDesc = enabledMochs ? ` and ${enabledMochs} enabled` : '';
  return `This is an open-source, hacky <A HREf="https://github.com/geek-cookbook/torrentio.elfhosted.com">fork</A> of <A HREF="http://torrentio.strem.fm">torrent.strem.fm</A>, implemented by <A HREF="https://elfhosted.com">ElfHosted</A>, which provides torrent streams from scraped torrent providers.`
      + ` <br/><br/>(<I>ElfHosted is a <A HREF="https://elfhosted.com/open/">open-source PaaS</A> built and run by <A HREF="https://geek-cookbook.funkypenguin.co.nz/community/">geeks</A></I>)`
      + ` <br/><br/>The primary instance (<A HREF="http://torrentio.elfhosted.com">torrentio.elfhosted.com</A>) is provided free for public use, and <A HREF="https://github.com/funkypenguin/elf-infra/blob/ci/torrentio/middleware-rate-limit-torrentio.yaml">rate-limited</A> appropriately for casual individual streaming use (<I>not automation</I>).`
      + ` <br/><br/>Hosted instances with <A HREF="https://github.com/geek-cookbook/elf-charts/blob/main/charts/other/myprecious/templates/middleware/middleware-torrentio-rate-limit.yaml">rate-limits</A> appropriate for automation <A HREF="https://elfhosted.com/guides/media/stream-from-real-debrid-with-self-hosted-torrentio/">are available</A> for $0.15/day, with $10 free credit on new accounts.`
      + ` <br/><br/>An internal, un-rate-limited instance is provided free, with all <A HREF="https://elfhosted.com/apps/">ElfHosted apps</A>, for automation.`
      + ` <br/><br/>Indexers currently supported are ${enabledProvidersDesc}${mochsDesc}.`
      + ` <br/><br/>Providers currently supported are ${possibleMochs}`
}

function getMochsHTML(config) {
  const enabledMochs = Object.values(MochOptions)
      .filter(moch => config[moch.key])
      .map(moch => moch.name)
      .join(' & ');
  const possibleMochs = Object.values(MochOptions).map(moch => moch.name).join(' / ')
  const mochsDesc = enabledMochs ? ` and ${enabledMochs} enabled` : '';
  return `Supported providers are currently: <ul>${possibleMochs}</ul>`
}

function getCatalogs(config) {
  return CatalogMochs
      .filter(moch => showDebridCatalog(config) && config[moch.key])
      .map(moch => ({
        id: `torrentio-${moch.key}`,
        name: `${moch.name}`,
        type: 'other',
        extra: [{ name: 'skip' }],
      }));
}

function getResources(config) {
  const streamResource = {
    name: 'stream',
    types: [Type.MOVIE, Type.SERIES],
    idPrefixes: ['tt', 'kitsu']
  };
  const metaResource = {
    name: 'meta',
    types: [Type.OTHER],
    idPrefixes: CatalogMochs.filter(moch => config[moch.key]).map(moch => moch.key)
  };
  if (showDebridCatalog(config) && CatalogMochs.filter(moch => config[moch.key]).length) {
    return [streamResource, metaResource];
  }
  return [streamResource];
}
