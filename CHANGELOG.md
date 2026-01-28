# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0] - 2025-01-27

### Added
- Military events: battle outcomes, unit kills/deaths, siege events
- Wonder completion events
- Comprehensive API documentation site (https://becked.github.io/OldWorldAPIEndpoint/)
- OpenAPI 3.0 specification and JSON Schema definitions
- Steam Workshop upload support via SteamCMD
- mod.io upload support via REST API
- Version management with bump-version.sh

### Changed
- Documentation now served via GitHub Pages with Docsify

## [0.0.2] - 2025-01-06

### Added
- Character events: births, deaths, marriages, leader changes, heir changes
- Extended character data with 75+ fields
- HTTP REST API on port 9877 with endpoints: /state, /players, /cities, /characters, /character-events, /tribes
- Team diplomacy data with war states, war scores, alliances
- Per-turn yield rates for players
- Comprehensive city data with improvements, build queues, religion, culture
- Comprehensive character data with traits, ratings, jobs, relationships
- Tribe data with strength, cities, settlements

## [0.0.1] - 2024-12-20

### Added
- Initial release
- TCP broadcast server on port 9876
- Basic player data: nation, stockpiles, legitimacy
- Basic city data: yields, happiness
