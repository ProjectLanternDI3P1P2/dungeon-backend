# Changelog

## [2.0.0](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/compare/v1.2.0...v2.0.0) (2026-10-02)


### ⚠ BREAKING CHANGES

* the database is now "dungeon": recreate the local volume.
* rename the presentation, the contracts and the solution to Dungeon
* rename Combat.Infrastructure to Dungeon.Infrastructure
* rename Combat.Application to Dungeon.Application
* rename Combat.Domain to Dungeon.Domain
* **contracts:** release protobuf contracts independently

### Added

* add ef core migration tooling ([b3abca4](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/b3abca46187ee86de0eda40e2da4ef26ef68e275))
* add ef core migration tooling ([e62b3f1](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/e62b3f12e77dce1622db927db6ad55cceaf05166))
* add response compression and update sonar exclusions for improved performance ([d9bf6c4](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/d9bf6c4e499c8895408571fc7bcebfd048e8e559))
* **contracts:** publish protobuf package ([5df7049](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/5df7049dcb876f7f821bc58759f0fef43d7e6ec6))
* **contracts:** release protobuf contracts independently ([d7cefcb](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/d7cefcb8b517075887b9f33c90c4262bac64a212))
* **database:** migrate and seed development data ([64a6992](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/64a6992c07c1f7510c39810263a12a688d304c7b))
* **database:** migrate and seed development data ([7c77c8b](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/7c77c8bec35cb6dba37568f0c3ba38297bcc6314))
* **dungeon:** add pillars, fences and spike traps ([fd2aaed](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/fd2aaedd487c0ae9cddc902d3b5ca52f2dd968bc))
* **dungeon:** add shareable seeds and a deterministic random generator ([667288d](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/667288d1e81f9f24ef5b672b08dafa23eaacb286))
* **dungeon:** create runs, move the hero and take the stairs ([38a3ffa](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/38a3ffa7540867ec22057d4a89968b8432299757))
* **dungeon:** draw rooms from hand-made templates, four floors of ten ([cea9b7e](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/cea9b7ed1fc7fcb44b6878d62fb91be33dc1a91b))
* **dungeon:** enhance SpiralRoom template with detailed layout and traps ([35a81b4](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/35a81b4850b60c1b194be4d4dbae150da4a7b040))
* **dungeon:** expose the dungeon and dungeon-run endpoints ([aec6475](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/aec6475ae81a85f5b6bf826db7700be00f72e923))
* **dungeon:** generate forty connected rooms from a seed ([d66b0ac](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/d66b0ac942cde88522ec66fd3d60e93d94f422ba))
* **dungeon:** introduce room rarity system and enhance room templates ([f4de92f](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/f4de92fdaca5ee0c4d269c8937a2dd04fcdab5a8))
* **dungeon:** lock the stairs behind the gate of each floor boss ([852dfef](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/852dfefcec554390b1dcdf0b99a00713634edef5))
* **dungeon:** model dungeons, floors, rooms and elements ([a2444b4](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/a2444b400596934fce9457e8061a3d55289ab9a1))
* **dungeon:** move the hero one tile per turn in a dungeon run ([97a4b5e](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/97a4b5e530cf2da6bc9f99985b6bad32804702a1))
* **dungeon:** persist dungeon runs and cache generated dungeons ([dc5ac31](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/dc5ac31daeb400f77d76bc11542748b8a74db1e3))
* **dungeon:** read the map and the cells of a known seed ([b5eda4a](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/b5eda4a08cc0113d9c40e833a97ddf187d3d971d))
* **dungeon:** vary room sizes and carve room layouts ([9327fcf](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/9327fcf80de7cb8c0049c299bf1984bce8f725b2))
* enforce command transaction boundary ([4096387](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/40963875235dde96cbfbe0d22352baf86f1d5255))
* enforce command transaction boundary ([8bbedf6](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/8bbedf6877115ad15cc7455e50cf2946f1c05750))
* **grpc:** add infrastructure player client ([00a2531](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/00a2531a16d93773e7bed45485eefc608314030a))
* **grpc:** expose player service internally ([b374c80](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/b374c80622924deaa333dabc33f350175f8b1e88))
* **grpc:** propagate correlation IDs ([ff71dde](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/ff71ddebda9e7b6db5b0e72a3142e56a216987cd))
* **grpc:** propagate correlation IDs ([d31b1af](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/d31b1af279eff3836148f0ed3f54882094b045b3))
* Mise en place d'un exemple de service GRPC client et serveur ([550c759](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/550c759e0f5de9d7d8cebb07b2f489016918cb07))
* **openapi:** persist user ID in Scalar ([9ec7b32](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/9ec7b3256facc8f14894a1c4f32cef252c3a78f8))
* **openapi:** persist user ID in Scalar ([f837244](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/f837244c80f77f5e8b929a72fe761a834094fc0c))
* publish player created events through RabbitMQ ([40727b6](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/40727b6125a422e1cbbeefaab2d17520c4ce2650))
* publish player created events through RabbitMQ ([cccf22e](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/cccf22eebfad251aee42150c4ba83fea8879c20c))
* **template:** complete database and integration examples ([a3ed989](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/a3ed989b90febd9633a06bcddb733393524ba06d))


### Fixed

* address the Sonar findings in the presentation and domain layers ([#13](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/issues/13)) ([9e6d9bf](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/9e6d9bfb105c393a2ae62989da2f98ba52cf7774))
* **api:** allow local frontend origins through CORS in Development ([8c159fa](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/8c159fae7be02399360b18b0d55ff9f2bfcafbc7))
* **ci:** pin reusable workflow revision ([73e6cb8](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/73e6cb8007b93288b97e9f48c897fc043ff2ea4a))
* **ci:** provision PostgreSQL for Sonar tests ([125c52a](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/125c52ae5fd0a7e76ff4ada9089b6a19d3ab1b50))
* **database:** add dungeon migration workflow ([07a46b5](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/07a46b5b0df23d5db04b6e29ffb1bd30580b0604))
* **database:** retain migration workflow ([791644c](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/791644c7e7669605c7c753b87d1816502d6568e5))
* **docker:** include dockerignore in application project ([fdedfc4](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/fdedfc42df92463c9fed47d76c222c3f11911dde))
* **docker:** include dockerignore in application project ([6014816](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/60148164bcc39252c68ae46fc06e1f96d68d9e84))
* docs Dockerfile ([e78fce3](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/e78fce3ed74fea003d0fddbee8355f4812bbf629))
* **generation:** put Landing stairs at the far end and update golden master ([eb3f52c](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/eb3f52c9ac2d6cc057a3063c468ed112d469c550))
* Hardening the Dockerfile ([da3e4bc](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/da3e4bc3dde19fc838683033d406591977f3cc48))
* Hardening the Dockerfile ([ffa2502](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/ffa250294b8bb891e3378ba64891c6e392a6893e))
* make the Docker image build, and add a compose stack to run it ([#14](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/issues/14)) ([aa20e81](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/aa20e81a4f3f695e4dd66f2a9e648bf4e29869b3))
* run the container as the unprivileged app user ([#8](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/issues/8)) ([894be3f](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/894be3fe28e64218726b2dbc2e2bccb7efb9846b))
* Update Dockerfile ([da3e4bc](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/da3e4bc3dde19fc838683033d406591977f3cc48))
* Update Dockerfile ([ffa2502](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/ffa250294b8bb891e3378ba64891c6e392a6893e))


### Changed

* **api:** write problem details in one place ([c271378](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/c271378e67894e2aa98b5670a290c9bce2c3dff5))
* **api:** write problem details in one place ([e755993](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/e755993d25f1e60fd192e316e2ce9b00079269b4))
* **dungeon-run:** share run lookup across handlers ([52d497a](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/52d497a9fcf6d3ca6893f36e38869d39cda09660))
* **dungeon-run:** share run lookup across handlers ([e4c1d96](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/e4c1d962aa51ec174722c1662241b02534f5468d))
* **dungeon:** rename the Dungeon entity to GeneratedDungeon ([cd11b97](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/cd11b9711bd2f2c5d2efe5b91ee14cbf40b6f69e))
* **dungeon:** replace stairs with gates and add room templates ([9c6e494](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/9c6e494fe4711503836dd2dd6a9a53b2f81841d1))
* **dungeon:** replace stairs with gates and add room templates ([5355d1a](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/5355d1ac0d62b91389a390222d2b460cdd2024a9))
* improve code readability by formatting multi-line statements ([3877e09](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/3877e09bea308c2e02e80636e5a590d35b971390))
* rename Combat.Application to Dungeon.Application ([b00a77f](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/b00a77f9c704e8f4486a36dc41b7405984f06301))
* rename Combat.Domain to Dungeon.Domain ([eddad39](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/eddad39d37dcc098e82bf858bd102932d92916d4))
* rename Combat.Infrastructure to Dungeon.Infrastructure ([0b71afb](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/0b71afb80e48d542f6ccf774f186db6271683d1c))
* rename the presentation, the contracts and the solution to Dungeon ([0ddb904](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/0ddb9041bcb768601c10823d6d7869a2a9ab156c))
* simplify exception handling by consolidating response writing logic ([9596cda](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/9596cda2c38ad8f8f04676604886d570fb9683c0))
* simplify validation behavior and startup wiring ([fe65f35](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/fe65f3581e428126cade04053983958d8473dd3d))
* simplify validation behavior and startup wiring ([5fb24fd](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/5fb24fd46a473084f6b98a65dbe20d5a5c2786bf))


### Build

* rename the database, broker exchange and CI jobs to dungeon ([f9804df](https://github.com/ProjectLanternDI3P1P2/dungeon-backend/commit/f9804dff3deb229ba9218d0932eb74a91fd0f4a2))

## [1.2.0](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/compare/v1.1.0...v1.2.0) (2026-09-27)


### Added

* **database:** migrate and seed development data ([64a6992](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/commit/64a6992c07c1f7510c39810263a12a688d304c7b))
* **database:** migrate and seed development data ([7c77c8b](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/commit/7c77c8bec35cb6dba37568f0c3ba38297bcc6314))
* **grpc:** propagate correlation IDs ([ff71dde](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/commit/ff71ddebda9e7b6db5b0e72a3142e56a216987cd))
* **grpc:** propagate correlation IDs ([d31b1af](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/commit/d31b1af279eff3836148f0ed3f54882094b045b3))
* **openapi:** persist user ID in Scalar ([9ec7b32](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/commit/9ec7b3256facc8f14894a1c4f32cef252c3a78f8))
* **openapi:** persist user ID in Scalar ([f837244](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/commit/f837244c80f77f5e8b929a72fe761a834094fc0c))
* **template:** complete database and integration examples ([a3ed989](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/commit/a3ed989b90febd9633a06bcddb733393524ba06d))


### Fixed

* **ci:** provision PostgreSQL for Sonar tests ([125c52a](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/commit/125c52ae5fd0a7e76ff4ada9089b6a19d3ab1b50))
* **database:** retain migration workflow ([791644c](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/commit/791644c7e7669605c7c753b87d1816502d6568e5))
* **docker:** include dockerignore in application project ([fdedfc4](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/commit/fdedfc42df92463c9fed47d76c222c3f11911dde))
* **docker:** include dockerignore in application project ([6014816](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/commit/60148164bcc39252c68ae46fc06e1f96d68d9e84))

## [1.1.0](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/compare/v1.0.0...v1.1.0) (2026-09-23)


### Added

* add ef core migration tooling ([b3abca4](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/commit/b3abca46187ee86de0eda40e2da4ef26ef68e275))
* add ef core migration tooling ([e62b3f1](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/commit/e62b3f12e77dce1622db927db6ad55cceaf05166))
* enforce command transaction boundary ([4096387](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/commit/40963875235dde96cbfbe0d22352baf86f1d5255))
* publish player created events through RabbitMQ ([40727b6](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/commit/40727b6125a422e1cbbeefaab2d17520c4ce2650))
* publish player created events through RabbitMQ ([cccf22e](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/commit/cccf22eebfad251aee42150c4ba83fea8879c20c))

## [1.0.0](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/compare/v0.1.1...v1.0.0) (2026-09-22)


### ⚠ BREAKING CHANGES

* **contracts:** release protobuf contracts independently

### Added

* **contracts:** publish protobuf package ([5df7049](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/commit/5df7049dcb876f7f821bc58759f0fef43d7e6ec6))
* **contracts:** release protobuf contracts independently ([d7cefcb](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/commit/d7cefcb8b517075887b9f33c90c4262bac64a212))
* **grpc:** add infrastructure player client ([00a2531](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/commit/00a2531a16d93773e7bed45485eefc608314030a))
* **grpc:** expose player service internally ([b374c80](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/commit/b374c80622924deaa333dabc33f350175f8b1e88))
* Mise en place d'un exemple de service GRPC client et serveur ([550c759](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/commit/550c759e0f5de9d7d8cebb07b2f489016918cb07))

## [0.1.1](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/compare/v0.1.0...v0.1.1) (2026-09-05)


### Fixed

* run the container as the unprivileged app user ([#8](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/issues/8)) ([894be3f](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/commit/894be3fe28e64218726b2dbc2e2bccb7efb9846b))
