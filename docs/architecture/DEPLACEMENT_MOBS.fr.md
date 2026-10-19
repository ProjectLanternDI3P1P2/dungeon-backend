# Déplacement des silhouettes

> US déplacement des mobs, après la génération (`GENERATION_DONJON.fr.md`, v6).

## 1. Principe

La carte reste immuable pour une seed : ses éléments `enemy` ne donnent plus que
les **points d'apparition**. La position courante des mobs devient un état de la
**run**, calculé par le backend à chaque tour. Le front affiche et ne prédit pas
les mobs.

## 2. État de la run

- Table `dungeon_run_mobs`, **étage courant seulement** : run, id de l'élément,
  x, y, aggro, compteur de cadence, case de téléportation annoncée, porte visée,
  `active`.
- Remplie depuis les éléments `enemy` de l'étage quand le groupe y arrive
  (`Start`, descente), vidée à la descente.
- Liste des **salles découvertes** de l'étage : une salle est découverte dès
  qu'**un héros du groupe** pose le pied sur une de ses cases ou sur sa porte.
- Une migration EF, appliquée en local par `make migrate`.

## 3. Un tour

1. Le ou les héros bougent. Un pas vers la case d'un mob est refusé comme un mur
   et compte comme un contact (§6).
2. Mise à jour des salles découvertes, puis des aggros.
3. Chaque mob actif agit, dans l'ordre de son id, si sa cadence le permet ce
   tour. Un mob bloqué par un autre attend ; pas d'échange de cases.
4. Contact (§6).

L'aléatoire est tiré de `(seed, étage, tour, mob)` : un tour rejoué donne le même
résultat.

## 4. Aggro

- Un mob passe en aggro quand un héros est à sa portée, mesurée en **longueur de
  chemin** (pas de vision, pas de vol d'oiseau à travers un mur).
- Le mage n'a pas de portée : il passe en aggro à la **découverte de sa salle**.
- L'aggro est **permanente** jusqu'au contact ou au changement d'étage.
- Un mob en aggro suit le héros le plus proche partout, couloirs compris, par le
  plus court chemin. Une seule carte de distances depuis les héros est calculée
  par tour, partagée par tous les mobs.
- Un mob qui passe en aggro à ce tour agit déjà en poursuite.

## 5. Types et configuration

Section `Dungeon:Mobs` (`IOptions`, ADR-0028). Elle n'est pas recopiée dans les
runs : une modification s'applique dès le tour suivant.

```json
"Mobs": {
  "WanderRadius": 2,
  "Zombie": { "Cadence": "110",  "AggroRange": 10,   "Step": "Orthogonal", "Idle": "Wander" },
  "Bat":    { "Cadence": "1110", "AggroRange": 5,    "Step": "Diagonal",   "Idle": "Wander" },
  "Goblin": { "Cadence": "1",    "AggroRange": 3,    "Step": "Orthogonal", "Idle": "NearestDoor" },
  "Mage":   { "Cadence": "001",  "AggroRange": null, "Step": "Teleport",   "Idle": "None",
              "TeleportDistance": 3 }
}
```

- `Cadence` : motif répété, `1` le mob agit, `0` il passe son tour. Le compteur
  démarre à l'aggro.
- `Step` :
  - `Orthogonal` : 1 pas dans les 4 directions.
  - `Diagonal` : 1 pas dans les 8 directions, sans couper le coin entre deux murs.
  - `Teleport` : saut vers la meilleure case à `TeleportDistance` pas au plus,
    diagonales comprises, sans traverser ni couper un mur : la case libre la plus
    proche du héros en chemin, puis à vol d'oiseau, puis en ordre de lecture,
    jamais celle du héros. La case est choisie et exposée **un tour avant**. Si
    elle est occupée au moment du saut, le mage reste et en choisit une autre.
- `Idle` (hors aggro) :
  - `Wander` : avance vers le centre de sa salle jusqu'à `WanderRadius` cases,
    puis pas aléatoire ou immobile, **sans quitter sa salle**.
  - `NearestDoor` : dès la découverte de sa salle, se rend sur la case de la
    porte la plus proche de lui (égalité : ordre de lecture) et y reste.
  - `None` : immobile.
- Un mob peut passer sur un piège, sans effet.

## 6. Contact (bouchon en attendant Combat)

Un héros à 1 case d'un mob actif (diagonales comprises, `Position.DistanceTo`)
ou qui le bouscule le passe à `active = false` : il est « mort » et disparaît.
L'US déclenchement remplacera ce bouchon par le combat contre son pack.

## 7. API

`POST /dungeon-runs/{id}/moves` et `GET /dungeon-runs/{id}` renvoient en plus :

```json
"mobs": [{ "id": 3, "kind": "mage", "x": 41, "y": 12, "aggro": true,
           "nextTeleport": { "x": 44, "y": 12 } }]
```

Seuls les mobs actifs sont listés. `nextTeleport` n'existe que pour un mage qui
a annoncé son saut.

## 8. Front

- Dessiner les mobs depuis `mobs` de la run, plus depuis `elements` de la carte,
  et seulement sur les cases révélées.
- Animer le passage d'une case à l'autre ; particules sur `nextTeleport`.

## 9. Hors périmètre

Multijoueur, WebSocket, vrai combat (US déclenchement).
