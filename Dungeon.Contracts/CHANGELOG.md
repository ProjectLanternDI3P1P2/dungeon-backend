# Changelog

## [6.0.0](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/compare/contracts-v5.0.0...contracts-v6.0.0) (2026-11-27)


### Added

* **contracts:** add ReportCombatResult to the Dungeon contract ([7507cea](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/7507cea83a953ce878c2697c9efc78ad9fb552d6))

## [5.0.0](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/compare/contracts-v4.0.0...contracts-v5.0.0) (2026-11-25)


### Added

* **dungeon:** add server-side chest loot opening ([0d4caaa](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/0d4caaaf84fc89e46a4a66853efccfaac92fa6e6))
* **dungeon:** integrate Reward chest loot contract ([a29c5b4](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/a29c5b48257aae4497cf51eaae06ab27811e7f79))
* **dungeon:** use UUIDs for generated elements ([911371e](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/911371e316f6adcb24008a28e7bd27e399bb87cf))

## [4.0.0](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/compare/contracts-v3.0.0...contracts-v4.0.0) (2026-10-20)


### Added

* Implement Dungeon Run functionality with gRPC support ([edb7038](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/edb70384d121b3d30bf185e760e4869329175d0b))

## [3.0.0](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/compare/contracts-v2.0.0...contracts-v3.0.0) (2026-10-02)


### ⚠ BREAKING CHANGES

* rename the presentation, the contracts and the solution to Dungeon

### Changed

* rename the presentation, the contracts and the solution to Dungeon ([0ddb904](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/0ddb9041bcb768601c10823d6d7869a2a9ab156c))

## [2.0.0](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/compare/contracts-v1.0.0...contracts-v2.0.0) (2026-09-23)


### Added

* publish player created events through RabbitMQ ([40727b6](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/commit/40727b6125a422e1cbbeefaab2d17520c4ce2650))
* publish player created events through RabbitMQ ([cccf22e](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/commit/cccf22eebfad251aee42150c4ba83fea8879c20c))

## 1.0.0 (2026-09-22)


### ⚠ BREAKING CHANGES

* **contracts:** release protobuf contracts independently

### Added

* **contracts:** publish protobuf package ([5df7049](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/commit/5df7049dcb876f7f821bc58759f0fef43d7e6ec6))
* **contracts:** release protobuf contracts independently ([d7cefcb](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/commit/d7cefcb8b517075887b9f33c90c4262bac64a212))
* Mise en place d'un exemple de service GRPC client et serveur ([550c759](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/commit/550c759e0f5de9d7d8cebb07b2f489016918cb07))

## Changelog

All notable changes to the Protocol Buffer contracts are documented in this file.

Contract releases use the form `Vn` (`n.0.0` in the NuGet package and the
`contracts-vn.0.0` Git tag). A release is created only when files under
`Combat.Contracts/` change.
