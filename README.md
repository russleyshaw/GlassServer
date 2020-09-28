# Glass Server

Server to provide simple access to MSFS 2020 via SimConnect.

# Releases

Find the latest release [here](https://github.com/russleyshaw/GlassServer/releases)

# Building

- Copy the `{MSFS SDK}/SimConnect SDK` folder to the repo root.

# Definitions
[Sim Events](https://github.com/russleyshaw/GlassServer/blob/fbaec860ab9d67d6c2efd41c5fde4d6d09db600f/GlassServer/SimManager.cs#L370)
[Sim Data](https://github.com/russleyshaw/GlassServer/blob/fbaec860ab9d67d6c2efd41c5fde4d6d09db600f/GlassServer/SimManager.cs#L155)

# API

### Getting SimData
Request
```ts
// GET https://localhost:5001/api/simdata/?name=ENGINE%20TYPE&name=SURFACE%20TYPE
```

Response 200 OK
```
[
  {
    "name": "ENGINE TYPE",
    "units": "Enum",
    "value": 0,
    "text": "Piston"
  },
  {
    "name": "SURFACE TYPE",
    "units": "Enum",
    "value": 4,
    "text": "Asphalt"
  }
]
```

### Setting SimData
```ts
// POST https://localhost:5001/api/simdata
[
  {"name": "LIGHT TAXI", "value": 0}
]
```
Response 200 OK

### Sending an event
```
// POST https://localhost:5001/api/simevent/
[
	{"name": "PARKING_BRAKES", "value": 0}
]
```
